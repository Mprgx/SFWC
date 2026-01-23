using MobyPark.Models;

namespace MobyPark.Services
{
    public interface ICompanyService
    {
        Task<CompanyResponseDto> GetCompanyByIdAsync(Guid id);
        Task<IEnumerable<CompanyResponseDto>> GetAllCompaniesAsync();
        Task<IEnumerable<UserReadDto>?> GetAllCompanyEmployees(Guid id);
        Task<CompanyResponseDto> CreateCompanyAsync(CreateCompanyDto createCompanyDto);
        Task<CompanyResponseDto> UpdateCompanyAsync(Guid id, UpdateCompanyDto updateCompanyDto);
        Task<bool> DeleteCompanyAsync(Guid id);
        Task<bool> AddUserToCompanyAsync(Guid userId, Guid companyId);
    }
}
