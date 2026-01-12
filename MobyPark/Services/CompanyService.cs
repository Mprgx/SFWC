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
            var company = await context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            return company == null ? null : ToDto(company);
        }

        public async Task<IEnumerable<CompanyResponseDto>> GetAllCompaniesAsync()
        {
            var companies = await context.Companies.AsNoTracking().ToListAsync();
            return companies.Select(ToDto);
        }

        public async Task<CompanyResponseDto> CreateCompanyAsync(CreateCompanyDto dto)
        {
            var company = new Company
            {
                Id = Guid.NewGuid(),
                CompanyName = dto.CompanyName,
                Discount = dto.Discount,
                Perks = dto.Perks,
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

            if (dto.CompanyName != null) company.CompanyName = dto.CompanyName;
            if (dto.Discount.HasValue) company.Discount = dto.Discount.Value;
            if (dto.Perks != null) company.Perks = dto.Perks;
            if (dto.IsActive.HasValue) company.IsActive = dto.IsActive.Value;

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

        private static CompanyResponseDto ToDto(Company c) => new()
        {
            Id = c.Id,
            CompanyName = c.CompanyName,
            Discount = c.Discount,
            Perks = c.Perks,
            CreatedAt = c.CreatedAt,
            IsActive = c.IsActive
        };
    }
}
