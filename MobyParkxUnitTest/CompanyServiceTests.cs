using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;
using MobyPark.Services;
using Xunit;

namespace MobyParkxUnitTest
{
    public class CompanyServiceTests
    {
        private static UserDbContext CreateDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new UserDbContext(options);
        }

        private static CompanyService CreateService(UserDbContext context)
        {
            return new CompanyService(context);
        }

        private static Company CreateCompanyEntity(
            Guid? id = null,
            string name = "Test Company",
            string street = "Test Street 1",
            string postalCode = "1234AB",
            string city = "Rotterdam",
            string country = "Netherlands",
            string contactEmail = "info@test.com",
            string? contactPhone = "0612345678",
            string? contactPerson = "John Doe",
            bool isActive = true)
        {
            return new Company
            {
                Id = id ?? Guid.NewGuid(),
                CompanyName = name,

                Street = street,
                PostalCode = postalCode,
                City = city,
                Country = country,

                ContactEmail = contactEmail,
                ContactPhone = contactPhone,
                ContactPerson = contactPerson,

                CreatedAt = DateTimeOffset.UtcNow,
                IsActive = isActive
            };
        }

        private static CreateCompanyDto CreateDto(
            string name = "New Company",
            string street = "New Street 10",
            string postalCode = "9999ZZ",
            string city = "Amsterdam",
            string country = "Netherlands",
            string email = "contact@new.com",
            string? phone = "0600000000",
            string? person = "Jane Doe")
        {
            return new CreateCompanyDto
            {
                CompanyName = name,
                Street = street,
                PostalCode = postalCode,
                City = city,
                Country = country,
                ContactEmail = email,
                ContactPhone = phone,
                ContactPerson = person
            };
        }


        [Fact]
        public async Task GetCompanyByIdAsync_ReturnsCompany_WhenCompanyExists()
        {
            using var context = CreateDbContext(nameof(GetCompanyByIdAsync_ReturnsCompany_WhenCompanyExists));
            var service = CreateService(context);

            var company = CreateCompanyEntity(name: "ACME");
            context.Companies.Add(company);
            await context.SaveChangesAsync();

            var dto = await service.GetCompanyByIdAsync(company.Id);

            Assert.NotNull(dto);
            Assert.Equal(company.Id, dto!.Id);
            Assert.Equal("ACME", dto.CompanyName);
            Assert.Equal(company.Street, dto.Street);
            Assert.Equal(company.PostalCode, dto.PostalCode);
            Assert.Equal(company.City, dto.City);
            Assert.Equal(company.Country, dto.Country);
            Assert.Equal(company.ContactEmail, dto.ContactEmail);
            Assert.Equal(company.ContactPhone, dto.ContactPhone);
            Assert.Equal(company.ContactPerson, dto.ContactPerson);
            Assert.Equal(company.IsActive, dto.IsActive);
        }

        [Fact]
        public async Task GetCompanyByIdAsync_ReturnsNull_WhenCompanyDoesNotExist()
        {
            using var context = CreateDbContext(nameof(GetCompanyByIdAsync_ReturnsNull_WhenCompanyDoesNotExist));
            var service = CreateService(context);

            var dto = await service.GetCompanyByIdAsync(Guid.NewGuid());

            Assert.Null(dto);
        }


        [Fact]
        public async Task GetAllCompaniesAsync_ReturnsAllCompanies()
        {
            using var context = CreateDbContext(nameof(GetAllCompaniesAsync_ReturnsAllCompanies));
            var service = CreateService(context);

            context.Companies.Add(CreateCompanyEntity(name: "Company A"));
            context.Companies.Add(CreateCompanyEntity(name: "Company B"));
            await context.SaveChangesAsync();

            var result = await service.GetAllCompaniesAsync();

            var list = result.ToList();
            Assert.Equal(2, list.Count);
            Assert.Contains(list, c => c.CompanyName == "Company A");
            Assert.Contains(list, c => c.CompanyName == "Company B");
        }

        [Fact]
        public async Task GetAllCompaniesAsync_ReturnsEmptyList_WhenNoCompanies()
        {
            using var context = CreateDbContext(nameof(GetAllCompaniesAsync_ReturnsEmptyList_WhenNoCompanies));
            var service = CreateService(context);

            var result = await service.GetAllCompaniesAsync();

            Assert.NotNull(result);
            Assert.Empty(result);
        }


        [Fact]
        public async Task CreateCompanyAsync_CreatesAndPersistsCompany()
        {
            using var context = CreateDbContext(nameof(CreateCompanyAsync_CreatesAndPersistsCompany));
            var service = CreateService(context);

            var dto = CreateDto(
                name: "My Org",
                street: "Mainstreet 12",
                postalCode: "1111AA",
                city: "Utrecht",
                country: "Netherlands",
                email: "org@my.com",
                phone: "0611111111",
                person: "Soufiane");

            var created = await service.CreateCompanyAsync(dto);

            Assert.NotNull(created);
            Assert.NotEqual(Guid.Empty, created.Id);
            Assert.Equal("My Org", created.CompanyName);
            Assert.Equal("Mainstreet 12", created.Street);
            Assert.Equal("1111AA", created.PostalCode);
            Assert.Equal("Utrecht", created.City);
            Assert.Equal("Netherlands", created.Country);
            Assert.Equal("org@my.com", created.ContactEmail);
            Assert.Equal("0611111111", created.ContactPhone);
            Assert.Equal("Soufiane", created.ContactPerson);
            Assert.True(created.IsActive);
            Assert.True(created.CreatedAt <= DateTimeOffset.UtcNow);

            var companyInDb = await context.Companies.SingleAsync();
            Assert.Equal(created.Id, companyInDb.Id);
            Assert.Equal("My Org", companyInDb.CompanyName);
            Assert.True(companyInDb.IsActive);
        }


        [Fact]
        public async Task UpdateCompanyAsync_UpdatesOnlyProvidedFields()
        {
            using var context = CreateDbContext(nameof(UpdateCompanyAsync_UpdatesOnlyProvidedFields));
            var service = CreateService(context);

            var company = CreateCompanyEntity(
                name: "Old Name",
                street: "Old Street",
                postalCode: "2222BB",
                city: "Rotterdam",
                country: "Netherlands",
                contactEmail: "old@company.com",
                contactPhone: "0600000000",
                contactPerson: "Old Person",
                isActive: true);

            context.Companies.Add(company);
            await context.SaveChangesAsync();

            var updateDto = new UpdateCompanyDto
            {
                CompanyName = "New Name",
                City = "Den Haag",
                IsActive = false
                // rest intentionally null (should remain unchanged)
            };

            var updated = await service.UpdateCompanyAsync(company.Id, updateDto);

            Assert.NotNull(updated);
            Assert.Equal("New Name", updated!.CompanyName);
            Assert.Equal("Den Haag", updated.City);
            Assert.False(updated.IsActive);

            // unchanged fields
            Assert.Equal("Old Street", updated.Street);
            Assert.Equal("2222BB", updated.PostalCode);
            Assert.Equal("Netherlands", updated.Country);
            Assert.Equal("old@company.com", updated.ContactEmail);
            Assert.Equal("0600000000", updated.ContactPhone);
            Assert.Equal("Old Person", updated.ContactPerson);
        }

        [Fact]
        public async Task UpdateCompanyAsync_DoesNotOverwrite_WhenStringsAreWhitespace()
        {
            using var context = CreateDbContext(nameof(UpdateCompanyAsync_DoesNotOverwrite_WhenStringsAreWhitespace));
            var service = CreateService(context);

            var company = CreateCompanyEntity(
                name: "Keep Name",
                street: "Keep Street",
                postalCode: "3333CC",
                city: "Keep City",
                country: "Keep Country",
                contactEmail: "keep@company.com",
                contactPhone: "0612345678",
                contactPerson: "Keep Person");

            context.Companies.Add(company);
            await context.SaveChangesAsync();

            var updateDto = new UpdateCompanyDto
            {
                CompanyName = "   ",
                ContactEmail = "",
                ContactPhone = "   ",
                ContactPerson = null
            };

            var updated = await service.UpdateCompanyAsync(company.Id, updateDto);

            Assert.NotNull(updated);
            Assert.Equal("Keep Name", updated!.CompanyName);
            Assert.Equal("keep@company.com", updated.ContactEmail);
            Assert.Equal("0612345678", updated.ContactPhone);
            Assert.Equal("Keep Person", updated.ContactPerson);
        }

        [Fact]
        public async Task UpdateCompanyAsync_ReturnsNull_WhenCompanyDoesNotExist()
        {
            using var context = CreateDbContext(nameof(UpdateCompanyAsync_ReturnsNull_WhenCompanyDoesNotExist));
            var service = CreateService(context);

            var updated = await service.UpdateCompanyAsync(Guid.NewGuid(), new UpdateCompanyDto { CompanyName = "X" });

            Assert.Null(updated);
        }


        [Fact]
        public async Task DeleteCompanyAsync_DeletesCompany_WhenCompanyExists()
        {
            using var context = CreateDbContext(nameof(DeleteCompanyAsync_DeletesCompany_WhenCompanyExists));
            var service = CreateService(context);

            var company = CreateCompanyEntity(name: "To Delete");
            context.Companies.Add(company);
            await context.SaveChangesAsync();

            var result = await service.DeleteCompanyAsync(company.Id);

            Assert.True(result);
            Assert.Equal(0, await context.Companies.CountAsync());
        }

        [Fact]
        public async Task DeleteCompanyAsync_ReturnsFalse_WhenCompanyDoesNotExist()
        {
            using var context = CreateDbContext(nameof(DeleteCompanyAsync_ReturnsFalse_WhenCompanyDoesNotExist));
            var service = CreateService(context);

            var result = await service.DeleteCompanyAsync(Guid.NewGuid());

            Assert.False(result);
        }
    }
}
