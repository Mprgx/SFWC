using Microsoft.EntityFrameworkCore;

using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public class CompanyService(UserDbContext context) : ICompanyService
    {
        public async Task<CompanyResponseDto?> GetCompanyByIdAsync(Guid id)
        {
            var company = await context.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            return company == null ? null : ToDto(company);
        }

        public async Task<IEnumerable<CompanyResponseDto>> GetAllCompaniesAsync()
        {
            var companies = await context.Companies
                .AsNoTracking()
                .ToListAsync();

            return companies.Select(ToDto);
        }

        public async Task<CompanyResponseDto> CreateCompanyAsync(CreateCompanyDto dto)
        {
            var company = new Company
            {
                Id = Guid.NewGuid(),
                CompanyName = dto.CompanyName,

                // Required address fields
                Street = dto.Street,
                PostalCode = dto.PostalCode,
                City = dto.City,
                Country = dto.Country,

                // Required contact fields
                ContactEmail = dto.ContactEmail,
                ContactPhone = dto.ContactPhone,
                ContactPerson = dto.ContactPerson,

                // Defaults / metadata
                CreatedAt = DateTimeOffset.UtcNow,
                IsActive = true
            };

            context.Companies.Add(company);
            await context.SaveChangesAsync();

            return ToDto(company);
        }

        public async Task<CompanyResponseDto?> UpdateCompanyAsync(Guid id, UpdateCompanyDto dto)
        {
            var company = await context.Companies.FirstOrDefaultAsync(c => c.Id == id);
            if (company == null) return null;

            if (!string.IsNullOrWhiteSpace(dto.CompanyName))
                company.CompanyName = dto.CompanyName;

            if (!string.IsNullOrWhiteSpace(dto.Street))
                company.Street = dto.Street;

            if (!string.IsNullOrWhiteSpace(dto.PostalCode))
                company.PostalCode = dto.PostalCode;

            if (!string.IsNullOrWhiteSpace(dto.City))
                company.City = dto.City;

            if (!string.IsNullOrWhiteSpace(dto.Country))
                company.Country = dto.Country;

            if (!string.IsNullOrWhiteSpace(dto.ContactEmail))
                company.ContactEmail = dto.ContactEmail;

            if (!string.IsNullOrWhiteSpace(dto.ContactPhone))
                company.ContactPhone = dto.ContactPhone;

            if (!string.IsNullOrWhiteSpace(dto.ContactPerson))
                company.ContactPerson = dto.ContactPerson;

            if (dto.IsActive.HasValue)
                company.IsActive = dto.IsActive.Value;

            await context.SaveChangesAsync();
            return ToDto(company);
        }

        public async Task<bool> DeleteCompanyAsync(Guid id)
        {
            var company = await context.Companies.FindAsync(id);
            if (company == null) return false;

            context.Companies.Remove(company);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AddUserToCompanyAsync(Guid userId, Guid companyId)
        {
            if (!await context.Users.AnyAsync(u => u.Id == userId))
                return false;

            if (!await context.Companies.AnyAsync(c => c.Id == companyId))
                return false;

            bool alreadyLinked = await context.CompanyUsers
                .AnyAsync(cu => cu.UserId == userId && cu.CompanyId == companyId);

            if (alreadyLinked)
                return false;

            await context.CompanyUsers.AddAsync(
                new CompanyUser
                {
                    UserId = userId,
                    CompanyId = companyId
                }
            );

            await context.SaveChangesAsync();
            return true;
        }


        private static CompanyResponseDto ToDto(Company c) => new()
        {
            Id = c.Id,
            CompanyName = c.CompanyName,

            Street = c.Street,
            PostalCode = c.PostalCode,
            City = c.City,
            Country = c.Country,

            ContactEmail = c.ContactEmail,
            ContactPhone = c.ContactPhone,
            ContactPerson = c.ContactPerson,

            CreatedAt = c.CreatedAt,
            IsActive = c.IsActive
        };
    }
}
