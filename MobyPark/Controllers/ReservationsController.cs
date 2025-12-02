using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobyPark.Entities;
using MobyPark.Models;
using MobyPark.Services;
using System.Security.Claims;

namespace MobyPark.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReservationsController(IReservationService reservationService) : ControllerBase
    {
        [HttpGet("by-id/{reservationid}")]
        public ActionResult<GetReservationDto> GetById(int reservationid)
        {
            var reservation = reservationService.GetById(reservationid);
            if (reservation is null)
                return NotFound(new
                {
                    statuscode = 404,
                    message = $"Reservation with id {reservationid} not found"
                });

            return Ok(reservation);
        }

        [HttpGet("by-vehicle-id/{vehicleid}")]
        public ActionResult<GetReservationDto> GetByVehicleId(int vehicleid)
        {
            var reservation = reservationService.GetByVehicleId(vehicleid);
            if (reservation is null)
                return NotFound(new
                {
                    statuscode = 404,
                    message = $"No reservation found"
                });

            return Ok(reservation);
        }

        [HttpPost]
        public ActionResult<GetReservationDto> CreateReservation(PostReservationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    statuscode = 400,
                    message = ModelState
                });

            var reservation = reservationService.CreateReservation(dto);

            return CreatedAtAction(
                nameof(GetById),
                new { reservationid = reservation.Id },
                reservation
            );
        }

        [HttpDelete("{reservationid}")]
        public ActionResult DeleteReservation(int reservationid)
        {
            var deleted = reservationService.DeleteReservation(reservationid);
            if (!deleted)
                return NotFound(new
                {
                    statuscode = 404,
                    message = $"Reservation with id {reservationid} not found"
                });

            _reservationService.DeleteReservation(reservation);

            return NoContent(); 
        }

        [HttpPut("{reservationid}")]
        public ActionResult<GetReservationDto> UpdateReservation(int reservationid, PostReservationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    statuscode = 400,
                    message = ModelState
                });

            var updated = reservationService.UpdateReservation(reservationid, dto);
            if (updated is null)
                return NotFound(new
                {
                    statuscode = 404,
                    message = $"Reservation with id {reservationid} not found"
                });

            return Ok(updated);
        }
    }
}

