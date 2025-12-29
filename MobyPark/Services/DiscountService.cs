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
