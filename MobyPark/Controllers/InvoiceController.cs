using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MobyPark.Constants;
using MobyPark.Entities;
using MobyPark.Models;
using MobyPark.Services;

namespace MobyPark.Controllers
{
    [Authorize]
    [Route("invoices")]
    [ApiController]

    public class InvoiceController(IInvoiceService _invoiceService) : ControllerBase
    {
        [Authorize(Roles = Roles.OrganisationAdmin + "," + Roles.Admin)]
        [HttpGet("{companyId:guid}/pdf")]
        public async Task<IActionResult> GetMonthlyInvoiceAsync(Guid companyId, [FromQuery] int month, [FromQuery] int year)
        {
            if (month < 1 || month > 12)
                return BadRequest(new { message = "Invalid month" });

            if (year > DateTime.Now.Year || year <= 1950)
                return BadRequest(new { message = "Invalid year" });

            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "User not found in token" });

            var pdfBytes = await _invoiceService.GenerateMonthlyInvoicePdfAsync(userId, companyId, month, year);

            if (pdfBytes == null || pdfBytes.Length == 0)
                return NotFound(new { message = "No invoice found for this month" });

            return File(
                pdfBytes,
                "application/pdf",
                $"invoice-{year}-{month:D2}.pdf"
            );
        }

        [Authorize(Roles = Roles.OrganisationAdmin + "," + Roles.Admin)]
        [HttpGet("{companyId:guid}/generated")]
        public async Task<IActionResult> GetAllGeneratedInvoicesAsync(Guid companyId, [FromQuery] DateTimeOffset? startDateInclusive = null, [FromQuery] DateTimeOffset? endDateInclusive = null)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "User not found in token" });

            var zipBytes = await _invoiceService.GenerateAllPdfAsync(userId, companyId, startDateInclusive, endDateInclusive);

            if (zipBytes == null || zipBytes.Length == 0)
                return NotFound(new { message = "No invoices found" });

            return File(zipBytes, "application/zip", "invoices.zip");
        }

        private bool TryGetUserId(out Guid id)
        {
            var s = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(s, out id);
        }

    }

}
