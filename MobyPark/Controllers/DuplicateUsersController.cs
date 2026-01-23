using Microsoft.AspNetCore.Mvc;
using MobyPark.Services;
using MobyPark.Models;

namespace MobyPark.Controllers
{
    [ApiController]
    [Route("api/duplicate-users")]
    public sealed class DuplicateUsersController : ControllerBase
    {
        private readonly IDuplicateUsersService _service;

        public DuplicateUsersController(IDuplicateUsersService service)
        {
            _service = service;
        }

        [HttpGet("by-username/{username}")]
        public async Task<IActionResult> GetByUsername(string username)
        {
            var (dto, msg, status) = await _service.GetByUsernameAsync(username);
            return status == 200
                ? Ok(new { message = msg, data = dto })
                : StatusCode(status, new { error = msg });
        }

        [HttpGet("by-id/{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var (dto, msg, status) = await _service.GetByIdAsync(id);
            return status == 200
                ? Ok(new { message = msg, data = dto })
                : StatusCode(status, new { error = msg });
        }

        [HttpGet("by-legacy-id/{legacyId}")]
        public async Task<IActionResult> GetByLegacyId(string legacyId)
        {
            var (dto, msg, status) = await _service.GetByLegacyIdAsync(legacyId);
            return status == 200
                ? Ok(new { message = msg, data = dto })
                : StatusCode(status, new { error = msg });
        }

        [HttpGet("by-email")]
        public async Task<IActionResult> GetByEmail([FromQuery] string email)
        {
            var (dto, msg, status) = await _service.GetByEmailAsync(email);
            return status == 200
                ? Ok(new { message = msg, data = dto })
                : StatusCode(status, new { error = msg });
        }

        [HttpPut("resolve/{duplicateUserId:guid}")]
        public async Task<IActionResult> Resolve(Guid duplicateUserId, [FromBody] DuplicateUserResolveRequestDto request)
        {
            var (dto, err, status) = await _service.ResolveAsync(duplicateUserId, request);
            return status == 200
                ? Ok(dto)
                : StatusCode(status, new { error = err });
        }
    }
}
