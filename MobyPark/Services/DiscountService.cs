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
            // Validation
            var checkExisting = await context.Discounts.AnyAsync(d => d.Code == dto.Code);
            if (checkExisting) return (409, $"The code {dto.Code} already exists. If inactive, consider reactivating or deleting it.", null);

            if (dto.Value <= 0) return (400, "The Value can not be 0 or less.", null);

            // Check if a newly posted discount is active at the time of posting, since it is uneccesary to post an expired discount.
            // The ValidFrom doesn't matter since time won't go backwards (I hope)
            if (dto.ValidUntil < DateTimeOffset.UtcNow) return (400, "The ValidUntil date is in the past.", null);

            if (dto.ValidFrom >= dto.ValidUntil) return (400, "The ValidUntil date is before the ValidFrom date.", null);

            foreach (int lotId in dto.allowedLocations)
            {
                var checkExistance = await context.ParkingLots.AnyAsync(pl => pl.Id == lotId);
                if (!checkExistance) return (404, $"The parking lot with id {lotId} was not found", null);
            }

            if (dto.MaxUsage < 1) return (400, $"The MaxUsage should be 1 or higher", null);

            foreach (Guid userid in dto.ValidForUsers)
            {
                var checkExistance = await context.Users.AnyAsync(u => u.Id == userid);
                if (!checkExistance) return (404, $"The user with id {userid} was not found", null);
            }

            // For when companies are implemented 
            // UNCOMMENT AT THAT POINT PRETTY PLEASE WITH CHEESE ON TOP

            // foreach (Guid companyId in dto.ValidForCompanies)
            // {
            //     var checkExistance = await context.Companies.AnyAsync(c => c.Id == companyId);
            //     if (!checkExistance) return (404, $"The company with id {companyId} was not found", null);
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
                allowedLocations = dto.allowedLocations?
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

                allowedLocations = discount.allowedLocations?.Select(dl => dl.ParkingLotId).ToList() ?? new List<int>(),
                ValidForUsers = discount.ValidForUsers?.Select(du => du.UserId).ToList() ?? new List<Guid>(),
                ValidForCompanies = discount.ValidForCompanies?.Select(dc => dc.CompanyId).ToList() ?? new List<Guid>()
            };
        }


    }
}
