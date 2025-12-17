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
                allowedLocations = dto.AllowedLocations?
                    .Select(id => new DiscountLocation { ParkingLotId = id, Code = dto.Code })
                    .ToList() ?? new List<DiscountLocation>(),
                TimeWindowStart = dto.TimeWindowStart,
                TimeWindowEnd = dto.TimeWindowEnd,
                MaxUsage = dto.MaxUsage,
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

                AllowedLocations = discount.allowedLocations?.Select(dl => dl.ParkingLotId).ToList() ?? new List<int>(),
                ValidForUsers = discount.ValidForUsers?.Select(du => du.UserId).ToList() ?? new List<Guid>(),
                ValidForCompanies = discount.ValidForCompanies?.Select(dc => dc.CompanyId).ToList() ?? new List<Guid>()
            };
        }


    }
}
