using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MobyPark.Constants;
using MobyPark.Models;
using MobyPark.Services;

namespace MobyPark.Controllers
{
    [ApiController]
    [Route("company")]
    [Authorize]
    public class CompanyController(ICompanyService companyService) : ControllerBase
    {

        [Authorize(Roles = Roles.Admin)]
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<CompanyResponseDto>>> GetAllCompanies()
        {
            var companies = await companyService.GetAllCompaniesAsync();
            return Ok(companies);
        }

        [Authorize(Roles = Roles.OrganisationAdmin + "," + Roles.Admin)]
        [HttpGet("by-id/{id}")]
        public async Task<ActionResult<CompanyResponseDto>> GetCompanyById(Guid id)
        {
            var company = await companyService.GetCompanyByIdAsync(id);
            if (company == null)
                return NotFound();

            return Ok(company);
        }

        [Authorize(Roles = Roles.OrganisationAdmin + "," + Roles.Admin)]
        [HttpGet("{id}/users")]
        public async Task<ActionResult<CompanyResponseDto>> GetCompanyEmployeesById(Guid id)
        {
            var users = await companyService.GetAllCompanyEmployees(id);
            if (users == null)
                return NotFound();

            return Ok(users);
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

        [Authorize(Roles = Roles.Admin)]
        [HttpPost("add-user/{userId:guid}")]
        public async Task<ActionResult<CreateCompanyDto>> AddUserToCompany(Guid userId, [FromQuery] Guid companyId)
        {
            var added = await companyService.AddUserToCompanyAsync(userId, companyId);
            if (!added)
            {
                return NotFound(new { message = "User is already apart of the company, or the user and/or company does not exist." });
            }
            return Ok(new { message = "User successfully added to company" });
        }
    }
}
