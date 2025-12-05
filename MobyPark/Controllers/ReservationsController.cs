using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobyPark.Entities;
using MobyPark.Models;
using MobyPark.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace MobyPark.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReservationsController(IReservationService reservationService) : ControllerBase
    {
        [HttpGet("by-id/{reservationid}")]
        public async Task<ActionResult<GetReservationDto>> GetById(int reservationid)
        {
            var reservation = await reservationService.GetById(reservationid);
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
            var reservation = await reservationService.GetByVehicleId(vehicleid);
            if (reservation is null)
                return NotFound(new
                {
                    message = $"No reservation found"
                });

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

            try
            {
                reservation = await reservationService.CreateReservation(dto);
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
            var deleted = await reservationService.DeleteReservation(reservationid);
            if (!deleted)
                return NotFound(new
                {
                    message = $"Reservation not found"
                });

            return NoContent();
        }

        [HttpPut("{reservationid}")]
        public async Task<ActionResult<GetReservationDto>> UpdateReservation(int reservationid, PutReservationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    message = ModelState
                });

            GetReservationDto? updated;

            try
            {
                updated = await reservationService.UpdateReservation(reservationid, dto);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }

            if (updated is null)
                return NotFound(new
                {
                    message = $"Reservation not found"
                });

            return Ok(updated);
        }
    }
}

