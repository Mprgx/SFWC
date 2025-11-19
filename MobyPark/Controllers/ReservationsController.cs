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
    public class ReservationsController(ReservationService reservationService) : ControllerBase
    {

        [HttpGet("{reservationid}")]
        public ActionResult<Reservation> GetById(int reservationid)
        {
            var reservation = reservationService.GetById(reservationid);
            if (reservation == null)
                return NotFound(new
                {
                    statuscode = 404,
                    message = $"Reservation with id {reservationid} not found"
                });

            return Ok(reservation);
        }

        [HttpPost]
        public ActionResult<Reservation> CreateReservation(CreateReservationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(
                    new
                    {
                        statuscode = 400,
                        message = ModelState
                    });

            var reservation = reservationService.CreateReservation(dto);
            return CreatedAtAction(nameof(GetById), new { reservationid = reservation.ReservationId }, reservation);
        }

        [HttpDelete("{reservationid}")]
        public ActionResult<Reservation> DeleteReservation(int reservationid)
        {
            var reservation = reservationService.GetById(reservationid);
            if (reservation == null)
                return NotFound(new
                {
                    statuscode = 404,
                    message = $"Reservation with id {reservationid} not found"
                });

            reservationService.DeleteReservation(reservation);

            return NoContent(); // retturns 204 if deletion successful
        }

        [HttpPut("{reservationid}")]
        public ActionResult<Reservation> UpdateReservation(int reservationid, CreateReservationDto dto)
        {
            var reservation = reservationService.GetById(reservationid);
            if (reservation == null)
                return NotFound(new
                {
                    statuscode = 404,
                    message = $"Reservation with id {reservationid} not found"
                });

            reservationService.UpdateReservation(reservation, dto);
            return CreatedAtAction(nameof(GetById), new { reservationid = reservation.ReservationId }, reservation);

        }

    }
}

