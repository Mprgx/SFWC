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
    public class ReservationsController(ReservationService _reservationService) : ControllerBase
    {

        [HttpGet("/by-id/{reservationid}")]
        public ActionResult<Reservation> GetById(int reservationid)
        {
            var reservation = _reservationService.GetById(reservationid);
            if (reservation == null)
                return NotFound(new
                {
                    statuscode = 404,
                    message = $"Reservation with id {reservationid} not found"
                });

            return Ok(reservation);
        }

        [HttpGet("by-vehicle-id/{vehicleid}")]
        public ActionResult<Reservation> GetByVehicleId(int vehicleid)
        {
            var reservation = _reservationService.GetByVehicleId(vehicleid);
            if (reservation == null)
                return NotFound(new
                {
                    statuscode = 404,
                    message = $"No reservation found"
                });

            return Ok(reservation);
        }

        [HttpPost]
        public ActionResult<Reservation> CreateReservation(PostReservationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(
                    new
                    {
                        statuscode = 400,
                        message = ModelState
                    });

            var reservation = _reservationService.CreateReservation(dto);
            return CreatedAtAction(nameof(GetById), new { reservationid = reservation.Id }, reservation);
        }

        [HttpDelete("{reservationid}")]
        public ActionResult<Reservation> DeleteReservation(int reservationid)
        {
            var reservation = _reservationService.GetById(reservationid);
            if (reservation == null)
                return NotFound(new
                {
                    statuscode = 404,
                    message = $"Reservation with id {reservationid} not found"
                });

            _reservationService.DeleteReservation(reservation);

            return NoContent(); // retturns 204 if deletion successful
        }

        [HttpPut("{reservationid}")]
        public ActionResult<Reservation> UpdateReservation(int reservationid, PostReservationDto dto)
        {
            var reservation = _reservationService.GetById(reservationid);
            if (reservation == null)
                return NotFound(new
                {
                    statuscode = 404,
                    message = $"Reservation with id {reservationid} not found"
                });

            _reservationService.UpdateReservation(reservation, dto);
            return CreatedAtAction(nameof(GetById), new { reservationid = reservation.Id }, reservation);

        }

    }
}

