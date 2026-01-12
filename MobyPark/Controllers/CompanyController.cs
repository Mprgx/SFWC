using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MobyPark.Constants;
using MobyPark.Models;
using MobyPark.Services;

namespace MobyPark.Controllers
{
    [ApiController]
    [Route("companies")]
    public class CompanyController(ICompanyService companyService) : ControllerBase
    {

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CompanyResponseDto>>> GetAllCompanies()
        {
            var companies = await companyService.GetAllCompaniesAsync();
            return Ok(companies);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CompanyResponseDto>> GetCompanyById(Guid id)
        {
            var company = await companyService.GetCompanyByIdAsync(id);
            if (company == null)
                return NotFound();

            return Ok(company);
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpPost]
        public async Task<ActionResult<CreateCompanyDto>> CreateCompany(CreateCompanyDto createCompanyDto)
        {
            var company = await companyService.CreateCompanyAsync(createCompanyDto);
            return CreatedAtAction(nameof(GetCompanyById), new { id = company.Id }, company);
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCompany(Guid id, UpdateCompanyDto updateCompanyDto)
        {
            var company = await companyService.UpdateCompanyAsync(id, updateCompanyDto);
            if (company == null)
                return NotFound();

            return Ok(company);
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCompany(Guid id)
        {
            var result = await companyService.DeleteCompanyAsync(id);
            if (!result)
                return NotFound();

            return NoContent();
        }
    }
}
