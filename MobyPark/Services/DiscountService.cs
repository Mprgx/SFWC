using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public class DiscountService(UserDbContext context, IConfiguration configuration) : IDiscountService
    {
        public async Task<(int statusCode, string message, DiscountReadDto?)> CreateDiscountAsync(DiscountPostDto dto, Guid userId)
        {
            // Normalize

            var allowedLocations = dto.AllowedLocations ?? [];
            var validForUsers = dto.ValidForUsers ?? [];
            var validForCompanies = dto.ValidForCompanies ?? [];

            // Validation
            var checkExisting = await context.Discounts.AnyAsync(d => d.Code == dto.Code);
            if (checkExisting) return (409, $"The code {dto.Code} already exists. If inactive, consider reactivating or deleting it.", null);

            if (dto.Value <= 0) return (400, "The Value can not be 0 or less.", null);

            if (dto.Type == DiscountType.Percentage && dto.Value > 100m) return (400, "Discount value can not be more than 100%", null);

            // Check if a newly posted discount is active at the time of posting, since it is uneccesary to post an expired discount.
            // The ValidFrom doesn't matter since time won't go backwards (I hope)
            if (dto.ValidUntil < DateTimeOffset.UtcNow) return (400, "The ValidUntil date is in the past.", null);

            if (dto.ValidFrom >= dto.ValidUntil) return (400, "The ValidUntil date is before the ValidFrom date.", null);

            if (allowedLocations.Any())
            {
                var existingLotIds = await context.ParkingLots
                    .Where(p => allowedLocations.Contains(p.Id))
                    .Select(p => p.Id)
                    .ToListAsync();

                var missingLotIds = allowedLocations.Except(existingLotIds).ToList();

                if (missingLotIds.Any())
                {
                    return (404, $"Parking lot(s) not found: {string.Join(", ", missingLotIds)}", null);
                }
            }

            if (dto.MaxUsage < 1) return (400, $"The MaxUsage should be 1 or higher", null);

            if (validForUsers.Any())
            {
                var existingUserIds = await context.Users
                    .Where(u => validForUsers.Contains(u.Id))
                    .Select(u => u.Id)
                    .ToListAsync();

                var missingUserIds = validForUsers.Except(existingUserIds).ToList();

                if (missingUserIds.Any())
                {
                    return (404, $"User(s) not found: {string.Join(", ", missingUserIds)}", null);
                }
            }

            // For when companies are implemented 
            // UNCOMMENT AT THAT POINT PRETTY PLEASE WITH CHEESE ON TOP

            // if (validForCompanies.Any())
            // {
            //     var existingCompanyIds = await context.Companies
            //         .Where(c => validForCompanies.Contains(c.Id))
            //         .Select(c => c.Id)
            //         .ToListAsync();

            //     var missingCompanyIds = validForCompanies.Except(existingCompanyIds).ToList();

            //     if (missingCompanyIds.Any())
            //     {
            //         return (404, $"Companies not found: {string.Join(", ", missingCompanyIds)}", null);
            //     }
            // }

            // Writing to db
            var discount = new Discount
            {
                Code = dto.Code,
                CreatedBy = userId,
                CreatedAt = DateTimeOffset.UtcNow,
                Type = dto.Type,
                Value = dto.Value,
                ValidFrom = dto.ValidFrom,
                ValidUntil = dto.ValidUntil,
                Active = true,
                AllowedLocations = dto.AllowedLocations?
                    .Select(id => new DiscountLocation { ParkingLotId = id, Code = dto.Code })
                    .ToList() ?? new List<DiscountLocation>(),
                TimeWindowStart = dto.TimeWindowStart,
                TimeWindowEnd = dto.TimeWindowEnd,
                MaxUsage = dto.MaxUsage,
                CurrentUsage = dto.MaxUsage is null ? null : 0,
                ValidForUsers = dto.ValidForUsers?
                    .Select(id => new DiscountUser { UserId = id, Code = dto.Code })
                    .ToList() ?? new List<DiscountUser>(),
                ValidForCompanies = dto.ValidForCompanies?
                    .Select(id => new DiscountCompany { CompanyId = id, Code = dto.Code })
                    .ToList() ?? new List<DiscountCompany>()
            };

            await context.Discounts.AddAsync(discount);
            await context.SaveChangesAsync();

            return (201, "Success", ToDto(discount));
        }

        public async Task<(int statusCode, string message)> ApplyDiscountAsync(string discountCode, string transaction, Guid userId)
        {
            var now = DateTimeOffset.UtcNow;

            var paymentInDb = await context.Payments.FirstOrDefaultAsync(p => p.Transaction == transaction);

            if (paymentInDb is null)
                return (404, "Payment not found");

            if (paymentInDb.DiscountCode != null)
                return (403, "A discount has already been applied");

            if (string.IsNullOrWhiteSpace(discountCode))
                return (200, "No discount applied");

            var discount = await context.Discounts
                .Include(d => d.ValidForUsers)
                .Include(d => d.ValidForCompanies)
                .Include(d => d.AllowedLocations)
                .Where(d => d.Code == discountCode.Trim())
                .FirstOrDefaultAsync();

            if (discount is null)
                return (404, $"Discount code {discountCode} not found");

            if (discount.MaxUsage is not null && discount.CurrentUsage >= discount.MaxUsage)
                return (422, $"Discount code {discountCode} has reached its maximum usage");

            if (discount.ValidForUsers.Any())
            {
                bool isUserAuthorized = discount.ValidForUsers.Any(vu => vu.UserId == userId);

                if (!isUserAuthorized)
                {
                    return (403, "This discount code is not valid for your account.");
                }
            }

            if (discount.ValidForCompanies.Any())
            {
                bool isUserInAuthorizedCompany = discount.ValidForCompanies
                    .Any(vc => context.CompanyUsers.Any(cu => cu.UserId == userId && cu.CompanyId == vc.CompanyId));

                if (!isUserInAuthorizedCompany)
                {
                    return (403, "This discount code is only valid for specific companies you are not a part of.");
                }
            }

            if (discount.AllowedLocations.Any())
            {
                bool isLocationValid = discount.AllowedLocations
                    .Any(al => al.ParkingLotId == paymentInDb.ParkingLotId);

                if (!isLocationValid)
                {
                    return (403, "This discount is not valid for this parking lot.");
                }
            }

            if (now > discount.ValidUntil)
                return (410, "This discount has expired");

            if (now < discount.ValidFrom)
                return (403, "This discount is not yet active");

            if (discount.TimeWindowStart.HasValue && discount.TimeWindowEnd.HasValue)
            {
                var currentTime = now.TimeOfDay;
                var start = discount.TimeWindowStart.Value;
                var end = discount.TimeWindowEnd.Value;

                bool isInsideWindow;

                if (start <= end)
                    isInsideWindow = currentTime >= start && currentTime <= end;
                else
                    isInsideWindow = currentTime >= start || currentTime <= end;

                if (!isInsideWindow)
                    return (403, $"This discount is only valid between {start:hh\\:mm} and {end:hh\\:mm} UTC.");
            }

            decimal discountedCost;
            if (discount.Type == DiscountType.FixedAmount)
                discountedCost = Math.Max(0, paymentInDb.Amount - discount.Value);
            else
            {
                var percentage = Math.Clamp(discount.Value, 0, 100);
                discountedCost = Math.Max(0, paymentInDb.Amount - (paymentInDb.Amount * percentage / 100m));
            }

            paymentInDb.AmountWithDiscount = discountedCost;
            paymentInDb.DiscountCode = discount.Code;

            if (discount.MaxUsage != null)
                discount.CurrentUsage++;

            await context.SaveChangesAsync();
            return (200, "Discount succesfully applied");
        }

        private DiscountReadDto ToDto(Discount discount)
        {
            return new DiscountReadDto
            {
                Code = discount.Code,
                CreatedBy = discount.CreatedBy,
                CreatedAt = discount.CreatedAt,
                Type = discount.Type,
                Value = discount.Value,
                ValidFrom = discount.ValidFrom,
                ValidUntil = discount.ValidUntil,
                TimeWindowStart = discount.TimeWindowStart,
                TimeWindowEnd = discount.TimeWindowEnd,
                MaxUsage = discount.MaxUsage,

                AllowedLocations = discount.AllowedLocations?.Select(dl => dl.ParkingLotId).ToList() ?? new List<int>(),
                ValidForUsers = discount.ValidForUsers?.Select(du => du.UserId).ToList() ?? new List<Guid>(),
                ValidForCompanies = discount.ValidForCompanies?.Select(dc => dc.CompanyId).ToList() ?? new List<Guid>()
            };
        }


    }
}
