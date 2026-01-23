using Microsoft.EntityFrameworkCore;

using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public class DiscountService(UserDbContext context) : IDiscountService
    {
        public async Task<(int statusCode, string message, DiscountReadDto?)> CreateDiscountAsync(DiscountPostDto dto, Guid userId)
        {
            var code = NormalizeCode(dto.Code);

            var allowedLocations = (dto.AllowedLocations ?? []).Distinct().ToList();
            var validForUsers = (dto.ValidForUsers ?? []).Distinct().ToList();
            var validForCompanies = (dto.ValidForCompanies ?? []).Distinct().ToList();

            var exists = await context.Discounts.AnyAsync(d => d.Code == code);
            if (exists) return (409, $"The code {code} already exists.", null);

            if (dto.ValidFrom >= dto.ValidUntil)
                return (400, "ValidUntil must be after ValidFrom.", null);

            if (dto.ValidUntil < DateTimeOffset.UtcNow)
                return (400, "ValidUntil is in the past.", null);

            if (dto.Type == DiscountType.Percentage && dto.Value > 100m)
                return (400, "Percentage discount cannot be > 100%.", null);

            if (dto.MaxUsage is not null && dto.MaxUsage < 1)
                return (400, "MaxUsage must be 1 or higher.", null);

            if (dto.Value <= 0)
                return (400, "Value must be > 0.", null);

            if (allowedLocations.Any())
            {
                var existing = await context.ParkingLots
                    .Where(p => allowedLocations.Contains(p.Id))
                    .Select(p => p.Id)
                    .ToListAsync();

                var missing = allowedLocations.Except(existing).ToList();
                if (missing.Any()) return (404, $"Parking lot(s) not found: {string.Join(", ", missing)}", null);
            }

            if (validForUsers.Any())
            {
                var existing = await context.Users
                    .Where(u => validForUsers.Contains(u.Id))
                    .Select(u => u.Id)
                    .ToListAsync();

                var missing = validForUsers.Except(existing).ToList();
                if (missing.Any()) return (404, $"User(s) not found: {string.Join(", ", missing)}", null);
            }

            if (validForCompanies.Any())
            {
                var existing = await context.Companies
                    .Where(c => validForCompanies.Contains(c.Id))
                    .Select(c => c.Id)
                    .ToListAsync();

                var missing = validForCompanies.Except(existing).ToList();
                if (missing.Any()) return (404, $"Company(s) not found: {string.Join(", ", missing)}", null);
            }

            var discount = new Discount
            {
                Code = code,
                CreatedBy = userId,
                CreatedAt = DateTimeOffset.UtcNow,

                Type = dto.Type,
                Value = dto.Value,

                ValidFrom = dto.ValidFrom,
                ValidUntil = dto.ValidUntil,
                Active = true,

                TimeWindowStart = dto.TimeWindowStart,
                TimeWindowEnd = dto.TimeWindowEnd,

                MaxUsage = dto.MaxUsage,
                CurrentUsage = dto.MaxUsage is null ? null : 0,

                AllowedLocations = allowedLocations.Select(id => new DiscountLocation { ParkingLotId = id, Code = code }).ToList(),
                ValidForUsers = validForUsers.Select(id => new DiscountUser { UserId = id, Code = code }).ToList(),
                ValidForCompanies = validForCompanies.Select(id => new DiscountCompany { CompanyId = id, Code = code }).ToList(),
            };

            await context.Discounts.AddAsync(discount);
            await context.SaveChangesAsync();

            return (201, "Success", ToDto(discount));
        }

        public async Task<(int statusCode, string message)> ApplyDiscountAsync(string? discountCode, string transaction, Guid userId)
        {
            if (userId == Guid.Empty)
                return (400, "User ID is required.");

            if (string.IsNullOrWhiteSpace(transaction))
                return (400, "Transaction is required.");

            var tx = transaction.Trim();

            var payment = await context.Payments
                .FirstOrDefaultAsync(p => p.Transaction == tx);

            if (payment is null)
                return (404, "Payment not found.");

            if (payment.UserId != userId)
                return (403, "You are not allowed to apply a discount to this payment.");

            if (payment.Completed is not null)
                return (403, "Payment is already completed; a discount can no longer be applied.");

            if (!string.IsNullOrWhiteSpace(payment.DiscountCode))
                return (403, "A discount has already been applied.");

            if (string.IsNullOrWhiteSpace(discountCode))
                return (200, "No discount applied.");

            var atTime = DateTimeOffset.UtcNow;
            var parkingLotId = payment.ParkingLotId;

            if (payment.SessionId.HasValue)
            {
                var s = await context.Sessions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == payment.SessionId.Value);

                if (s is not null)
                {
                    if (s.Stopped.HasValue)
                        atTime = s.Stopped.Value;

                    parkingLotId = s.ParkingLotId;
                }
            }

            var preview = await PreviewDiscountAsync(discountCode, userId, parkingLotId, atTime, payment.Amount);

            if (preview.statusCode != 200)
                return (preview.statusCode, preview.message);

            if (preview.normalizedCode is null)
                return (200, "No discount applied.");

            var discount = await context.Discounts
                .FirstOrDefaultAsync(d => d.Code == preview.normalizedCode);

            if (discount is null)
                return (404, $"Discount code {preview.normalizedCode} not found.");

            if (!discount.Active)
                return (403, "This discount code is deactivated.");

            if (discount.MaxUsage is not null)
            {
                discount.CurrentUsage ??= 0;

                if (discount.CurrentUsage >= discount.MaxUsage)
                    return (422, $"Discount code {discount.Code} has reached its maximum usage.");

                discount.CurrentUsage += 1;
            }

            payment.AmountWithDiscount = preview.amountWithDiscount ?? payment.Amount;
            payment.DiscountCode = preview.normalizedCode;

            if (payment.SessionId.HasValue)
            {
                var sessionId = payment.SessionId.Value;

                var session = await context.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId);
                if (session is not null)
                    session.Cost = payment.AmountWithDiscount;

                var billing = await context.Billings.FirstOrDefaultAsync(b => b.SessionId == sessionId);
                if (billing is not null)
                    billing.Cost = payment.AmountWithDiscount;
            }

            await context.SaveChangesAsync();
            return (200, "Discount successfully applied.");
        }

        public async Task<(int statusCode, string message, DiscountReadDto?)> UpdateDiscountAsync(string code, DiscountPatchDto dto)
        {
            if (string.IsNullOrWhiteSpace(code))
                return (400, "Discount code is required.", null);

            var normalizedCode = NormalizeCode(code);

            var discount = await context.Discounts
                .Include(d => d.AllowedLocations)
                .Include(d => d.ValidForUsers)
                .Include(d => d.ValidForCompanies)
                .FirstOrDefaultAsync(d => d.Code == normalizedCode);

            if (discount is null)
                return (404, $"Discount code '{normalizedCode}' not found.", null);

            if (dto.Type.HasValue && !dto.Value.HasValue)
                return (400, "Value is required when Type is set.", null);

            var finalType = dto.Type ?? discount.Type;
            var finalValue = dto.Value ?? discount.Value;

            if (finalValue <= 0)
                return (400, "Value must be > 0.", null);

            if (finalType == DiscountType.Percentage && finalValue > 100m)
                return (400, "Percentage discount cannot be > 100%.", null);

            if (dto.Active.HasValue)
                discount.Active = dto.Active.Value;

            if (dto.Type.HasValue)
                discount.Type = dto.Type.Value;

            if (dto.Value.HasValue)
                discount.Value = dto.Value.Value;

            if (dto.ValidFrom.HasValue)
                discount.ValidFrom = dto.ValidFrom.Value;

            if (dto.ValidUntil.HasValue)
                discount.ValidUntil = dto.ValidUntil.Value;

            if (discount.ValidFrom >= discount.ValidUntil)
                return (400, "ValidUntil must be after ValidFrom.", null);

            if (dto.ClearTimeWindow)
            {
                discount.TimeWindowStart = null;
                discount.TimeWindowEnd = null;
            }
            else if (dto.TimeWindowStart.HasValue || dto.TimeWindowEnd.HasValue)
            {
                if (dto.TimeWindowStart.HasValue != dto.TimeWindowEnd.HasValue)
                    return (400, "TimeWindowStart and TimeWindowEnd must both be set, or both be null.", null);

                discount.TimeWindowStart = dto.TimeWindowStart;
                discount.TimeWindowEnd = dto.TimeWindowEnd;
            }

            if (dto.ClearMaxUsage)
            {
                discount.MaxUsage = null;
                discount.CurrentUsage = null;
            }
            else if (dto.MaxUsage.HasValue)
            {
                if (dto.MaxUsage.Value < 1)
                    return (400, "MaxUsage must be 1 or higher.", null);

                var currentUsage = discount.CurrentUsage ?? 0;
                if (currentUsage > dto.MaxUsage.Value)
                    return (409, $"MaxUsage cannot be set below current usage ({currentUsage}).", null);

                discount.MaxUsage = dto.MaxUsage.Value;
                discount.CurrentUsage ??= 0;
            }

            if (dto.AllowedLocations is not null)
            {
                var allowedLocations = dto.AllowedLocations.Distinct().ToList();

                if (allowedLocations.Any())
                {
                    var existing = await context.ParkingLots
                        .Where(p => allowedLocations.Contains(p.Id))
                        .Select(p => p.Id)
                        .ToListAsync();

                    var missing = allowedLocations.Except(existing).ToList();
                    if (missing.Any())
                        return (404, $"Parking lot(s) not found: {string.Join(", ", missing)}", null);
                }

                if (discount.AllowedLocations is not null && discount.AllowedLocations.Count > 0)
                    context.RemoveRange(discount.AllowedLocations);

                discount.AllowedLocations = allowedLocations
                    .Select(id => new DiscountLocation { ParkingLotId = id, Code = discount.Code })
                    .ToList();
            }

            if (dto.ValidForUsers is not null)
            {
                var validForUsers = dto.ValidForUsers.Distinct().ToList();

                if (validForUsers.Any())
                {
                    var existing = await context.Users
                        .Where(u => validForUsers.Contains(u.Id))
                        .Select(u => u.Id)
                        .ToListAsync();

                    var missing = validForUsers.Except(existing).ToList();
                    if (missing.Any())
                        return (404, $"User(s) not found: {string.Join(", ", missing)}", null);
                }

                if (discount.ValidForUsers is not null && discount.ValidForUsers.Count > 0)
                    context.RemoveRange(discount.ValidForUsers);

                discount.ValidForUsers = validForUsers
                    .Select(id => new DiscountUser { UserId = id, Code = discount.Code })
                    .ToList();
            }

            if (dto.ValidForCompanies is not null)
            {
                var validForCompanies = dto.ValidForCompanies.Distinct().ToList();

                if (validForCompanies.Any())
                {
                    var existing = await context.Companies
                        .Where(c => validForCompanies.Contains(c.Id))
                        .Select(c => c.Id)
                        .ToListAsync();

                    var missing = validForCompanies.Except(existing).ToList();
                    if (missing.Any())
                        return (404, $"Company(s) not found: {string.Join(", ", missing)}", null);
                }

                if (discount.ValidForCompanies is not null && discount.ValidForCompanies.Count > 0)
                    context.RemoveRange(discount.ValidForCompanies);

                discount.ValidForCompanies = validForCompanies
                    .Select(id => new DiscountCompany { CompanyId = id, Code = discount.Code })
                    .ToList();
            }

            await context.SaveChangesAsync();
            return (200, "OK", ToDto(discount));
        }

        public async Task<(int statusCode, string message, decimal? amountWithDiscount, string? normalizedCode)> PreviewDiscountAsync(string? discountCode, Guid userId, int parkingLotId, DateTimeOffset atTime, decimal amount)
        {
            var code = NormalizeCode(discountCode);

            if (string.IsNullOrWhiteSpace(code))
                return (200, "No discount applied.", amount, null);

            var discount = await context.Discounts
                .AsNoTracking()
                .Include(d => d.ValidForUsers)
                .Include(d => d.ValidForCompanies)
                .Include(d => d.AllowedLocations)
                .FirstOrDefaultAsync(d => d.Code == code);

            if (discount is null)
                return (404, $"Discount code {code} not found.", null, null);

            if (!discount.Active)
                return (403, "This discount code is deactivated.", null, null);

            if (discount.MaxUsage is not null)
            {
                var currentUsage = discount.CurrentUsage ?? 0;
                if (currentUsage >= discount.MaxUsage)
                    return (422, $"Discount code {code} has reached its maximum usage.", null, null);
            }

            if (discount.ValidForUsers.Any() && !discount.ValidForUsers.Any(vu => vu.UserId == userId))
                return (403, "This discount code is not valid for your account.", null, null);

            if (discount.ValidForCompanies.Any())
            {
                var authorizedCompanyIds = discount.ValidForCompanies.Select(vc => vc.CompanyId).ToList();

                var ok = await context.CompanyUsers
                    .AsNoTracking()
                    .AnyAsync(cu => cu.UserId == userId && authorizedCompanyIds.Contains(cu.CompanyId));

                if (!ok)
                    return (403, "This discount code is only valid for specific companies you are not a part of.", null, null);
            }

            if (discount.AllowedLocations.Any() && !discount.AllowedLocations.Any(al => al.ParkingLotId == parkingLotId))
                return (403, "This discount is not valid for this parking lot.", null, null);

            if (atTime > discount.ValidUntil)
                return (410, "This discount has expired.", null, null);

            if (atTime < discount.ValidFrom)
                return (403, "This discount is not yet active.", null, null);

            if (discount.TimeWindowStart.HasValue && discount.TimeWindowEnd.HasValue)
            {
                var current = atTime.TimeOfDay;
                var start = discount.TimeWindowStart.Value;
                var end = discount.TimeWindowEnd.Value;

                var inside = start <= end
                    ? current >= start && current <= end
                    : current >= start || current <= end;

                if (!inside)
                    return (403, $"This discount is only valid between {start:hh\\:mm} and {end:hh\\:mm} UTC.", null, null);
            }

            var discounted = ComputeDiscountedAmount(discount.Type, discount.Value, amount);
            return (200, "Discount preview OK.", discounted, discount.Code);
        }

        public async Task<(int statusCode, string message, List<DiscountCodeAnalyticsReadDto>? dto)> GetDiscountCodesAllAnalyticsAsync(DiscountCodeStatus status)
        {
            var now = DateTimeOffset.UtcNow;

            var q = context.Discounts.AsNoTracking().AsQueryable();

            q = status switch
            {
                DiscountCodeStatus.Active => q.Where(d => d.Active && d.ValidUntil >= now),
                DiscountCodeStatus.Inactive => q.Where(d => !d.Active),
                DiscountCodeStatus.Expired => q.Where(d => d.ValidUntil < now),
                DiscountCodeStatus.All => q,
                _ => null!
            };

            if (q is null)
                return (400, "Invalid status value.", null);

            var discounts = await q.ToListAsync();

            if (discounts.Count == 0)
                return (200, "OK", new List<DiscountCodeAnalyticsReadDto>());

            var codes = discounts.Select(d => d.Code).Distinct().ToList();

            var paymentStats = await context.Payments
                .AsNoTracking()
                .Where(p => p.DiscountCode != null && codes.Contains(p.DiscountCode))
                .Select(p => new
                {
                    Code = p.DiscountCode!,
                    Original = p.Amount,
                    Discounted = p.AmountWithDiscount
                })
                .ToListAsync();

            var grouped = paymentStats
                .GroupBy(x => x.Code)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        UsedCount = g.Count(),
                        SavedTotal = g.Sum(x => x.Original - x.Discounted)
                    }
                );

            var result = discounts
                .Select(d =>
                {
                    grouped.TryGetValue(d.Code, out var s);

                    var saved = s?.SavedTotal ?? 0m;
                    if (saved < 0) saved = 0m;

                    return new DiscountCodeAnalyticsReadDto
                    {
                        Code = d.Code,
                        Type = d.Type,
                        Value = d.Value,

                        ValidFrom = d.ValidFrom,
                        ValidUntil = d.ValidUntil,

                        IsActive = d.Active,

                        MaxUsageCount = d.MaxUsage,
                        CurrentUsageCount = d.CurrentUsage,

                        ReservationsUsedCount = s?.UsedCount ?? 0,

                        TotalSavedAmount = Math.Round(saved, 2, MidpointRounding.AwayFromZero),
                    };
                })
                .OrderBy(x => x.ValidUntil)
                .ThenBy(x => x.Code)
                .ToList();

            return (200, "OK", result);
        }

        public async Task<(int statusCode, string message, DiscountCodeAnalyticsReadDto?)> GetDiscountCodeAnalyticsByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return (400, "Discount code is required.", null);

            var normalizedCode = NormalizeCode(code);
            var now = DateTimeOffset.UtcNow;

            var discount = await context.Discounts
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Code == normalizedCode);

            if (discount is null)
                return (404, $"Discount code '{normalizedCode}' not found.", null);

            var payments = await context.Payments
                .AsNoTracking()
                .Where(p => p.DiscountCode == normalizedCode)
                .Select(p => new
                {
                    Original = p.Amount,
                    Discounted = p.AmountWithDiscount
                })
                .ToListAsync();

            var usedCount = payments.Count;

            var totalSaved = payments.Sum(p => p.Original - p.Discounted);
            if (totalSaved < 0) totalSaved = 0;

            var dto = new DiscountCodeAnalyticsReadDto
            {
                Code = discount.Code,
                Type = discount.Type,
                Value = discount.Value,

                ValidFrom = discount.ValidFrom,
                ValidUntil = discount.ValidUntil,

                IsActive = discount.Active,

                MaxUsageCount = discount.MaxUsage,
                CurrentUsageCount = discount.CurrentUsage,

                ReservationsUsedCount = usedCount,
                TotalSavedAmount = Math.Round(totalSaved, 2, MidpointRounding.AwayFromZero)
            };

            return (200, "OK", dto);
        }


        private static decimal ComputeDiscountedAmount(DiscountType type, decimal value, decimal amount)
        {
            if (type == DiscountType.FixedAmount)
                return Math.Max(0, amount - value);

            var pct = Math.Clamp(value, 0, 100);
            return Math.Max(0, amount - (amount * pct / 100m));
        }

        private static string NormalizeCode(string? code)
        {
            return (code ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static DiscountReadDto ToDto(Discount d) => new()
        {
            Code = d.Code,
            CreatedBy = d.CreatedBy,
            CreatedAt = d.CreatedAt,
            Active = d.Active,
            Type = d.Type,
            Value = d.Value,
            ValidFrom = d.ValidFrom,
            ValidUntil = d.ValidUntil,
            TimeWindowStart = d.TimeWindowStart,
            TimeWindowEnd = d.TimeWindowEnd,
            MaxUsage = d.MaxUsage,
            AllowedLocations = d.AllowedLocations.Select(x => x.ParkingLotId).ToList(),
            ValidForUsers = d.ValidForUsers.Select(x => x.UserId).ToList(),
            ValidForCompanies = d.ValidForCompanies.Select(x => x.CompanyId).ToList(),
        };


    }
}
