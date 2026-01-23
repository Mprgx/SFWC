using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MobyPark.Models;
using MobyPark.Services;

namespace MobyPark.Controllers
{
    [ApiController]
    [Route("reservations")]
    [Authorize]
    public class ReservationsController(IReservationService reservationService) : ControllerBase
    {
        [HttpGet("by-id/{reservationid}")]
        public async Task<ActionResult<GetReservationDto>> GetById(int reservationid)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var reservation = await reservationService.GetById(reservationid, userId);
            if (reservation is null)
                return NotFound(new
                {
                    message = $"Reservation with id {reservationid} not found"
                });

            return Ok(reservation);
        }

        [HttpGet("by-vehicle-id/{vehicleid}")]
        public async Task<ActionResult<GetReservationDto>> GetByVehicleId(int vehicleid)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var reservation = await reservationService.GetByVehicleId(vehicleid, userId);
            if (reservation is null)
                return NotFound(new { message = "No reservation found" });

            return Ok(reservation);
        }

        [HttpPost]
        public async Task<ActionResult<GetReservationDto>> CreateReservation(PostReservationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    message = ModelState
                });

            GetReservationDto reservation;

            if (!TryGetUserId(out var userId)) return Unauthorized();

            try
            {
                reservation = await reservationService.CreateReservation(dto, userId);
            }
            catch (ParkingLotFullException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            return CreatedAtAction(
                nameof(GetById),
                new { reservationid = reservation.Id },
                reservation
            );
        }

        [HttpDelete("{reservationid}")]
        public async Task<IActionResult> DeleteReservation(int reservationid)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var deleted = await reservationService.DeleteReservation(reservationid, userId);
            if (!deleted)
                return NotFound(new { message = "Reservation not found" });

            return NoContent();
        }

        [HttpPut("{reservationid}")]
        public async Task<ActionResult<GetReservationDto>> UpdateReservation(int reservationid, PutReservationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = ModelState });

            if (!TryGetUserId(out var userId)) return Unauthorized();

            try
            {
                var updated = await reservationService.UpdateReservation(reservationid, dto, userId);
                return Ok(updated);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        private bool TryGetUserId(out Guid id)
        {
            var s = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(s, out id);
        }
    }
}

