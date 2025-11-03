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

    public class ReservationsController : ControllerBase
    {
        private readonly ReservationService _reservationService;

        public ReservationsController(ReservationService reservationService)
        {
            _reservationService = reservationService;
        }

        [HttpGet("{reservationid}")]
        public ActionResult<Reservation> GetById(int reservationid)
        {
            var reservation = _reservationService.GetById(reservationid);
            if (reservation == null)
                return NotFound($"Reservation with id {reservationid} not found");

            return Ok(reservation);
        }

        [HttpPost]
        public ActionResult<Reservation> CreateReservation(CreateReservationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var reservation = _reservationService.CreateReservation(dto);
            return CreatedAtAction(nameof(GetById), new { reservationid = reservation.ReservationId }, reservation);
        }

        [HttpDelete("{reservationid}")]
        public ActionResult<Reservation> DeleteReservation(int reservationid)
        {
            var reservation = _reservationService.GetById(reservationid);
            if (reservation == null)
                return NotFound($"Reservation with id {reservationid} not found");

            _reservationService.DeleteReservation(reservation);

            return NoContent(); // retturns 204 if deletion successful
        }

        [HttpPut("{reservationid}")]
        public ActionResult<Reservation> UpdateReservation(int reservationid, CreateReservationDto dto)
        {
            var reservation = _reservationService.GetById(reservationid);
            if (reservation == null)
                return NotFound($"Reservation with id {reservationid} not found");

            _reservationService.UpdateReservation(reservation, dto);
            return CreatedAtAction(nameof(GetById), new { reservationid = reservation.ReservationId }, reservation);

        }

    }
}

