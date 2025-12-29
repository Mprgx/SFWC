using System.ComponentModel.DataAnnotations;

using Microsoft.EntityFrameworkCore;

using MobyPark.Data;
using MobyPark.EncryptionHelper;
using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public class ReservationService(UserDbContext _context, IEncryptionService encryption, IDiscountService discountService) : IReservationService
    {
        //POST
        public async Task<GetReservationDto> CreateReservation(PostReservationDto dto, Guid userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var parkingLot = await _context.ParkingLots
                    .FirstOrDefaultAsync(p => p.Id == dto.ParkingLotId)
                    ?? throw new KeyNotFoundException("Parking lot not found");

                if (dto.StartTime.Offset != TimeSpan.Zero || dto.EndTime.Offset != TimeSpan.Zero)
                    throw new ValidationException("StartTime and EndTime must be UTC (offset +00:00).");

                var estimatedCost = CalculateEstimatedCost(parkingLot, dto.StartTime, dto.EndTime);

                decimal estimatedWithDiscount = estimatedCost;
                string? normalizedDiscountCode = null;

                if (!string.IsNullOrWhiteSpace(dto.DiscountCode))
                {
                    var preview = await discountService.PreviewDiscountAsync(
                        dto.DiscountCode,
                        userId,
                        dto.ParkingLotId,
                        dto.StartTime,  
                        estimatedCost);

                    if (preview.statusCode == 404)
                        throw new KeyNotFoundException(preview.message);

                    if (preview.statusCode != 200)
                        throw new ValidationException(preview.message);

                    normalizedDiscountCode = preview.normalizedCode;
                    estimatedWithDiscount = preview.amountWithDiscount ?? estimatedCost;
                }

                var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
                if (!userExists) throw new KeyNotFoundException("User not found");

                Vehicle vehicle;

                if (dto.VehicleId.HasValue)
                {
                    vehicle = await _context.Vehicles
                        .FirstOrDefaultAsync(v => v.Id == dto.VehicleId.Value && v.UserId == userId)
                        ?? throw new ValidationException("Vehicle not found for this user.");
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(dto.LicensePlate))
                        throw new ValidationException("VehicleId or LicensePlate is required.");

                    var wanted = LicensePlateProtector.Normalize(dto.LicensePlate);

                    var userVehicles = await _context.Vehicles
                        .Where(v => v.UserId == userId)
                        .ToListAsync();

                    vehicle = userVehicles.FirstOrDefault(v =>
                        LicensePlateProtector.DecryptNormalized(encryption, v.LicensePlate) == wanted)
                        ?? throw new ValidationException($"Vehicle with license plate {dto.LicensePlate} does not exist for this user.");
                }

                var reservedCount = await _context.Reservations
                    .Where(r => r.IsActive)
                    .Where(r => r.ParkingLotId == dto.ParkingLotId)
                    .Where(r => r.StartTime < dto.EndTime && r.EndTime > dto.StartTime)
                    .CountAsync();

                if (reservedCount >= parkingLot.Capacity)
                    throw new ParkingLotFullException("No spots available for the selected time slot.");
         
                var userOverlap = await _context.Reservations
                    .Where(r => r.IsActive)
                    .Where(r => r.UserId == userId)
                    .Where(r => r.ParkingLotId == dto.ParkingLotId)
                    .Where(r => r.StartTime < dto.EndTime && r.EndTime > dto.StartTime)
                    .AnyAsync();

                if (userOverlap)
                    throw new ValidationException("There is already an overlapping reservation for this user at this time.");

             
                var reservation = new Reservation
                {
                    ParkingLotId = dto.ParkingLotId,
                    UserId = userId,                
                    VehicleId = vehicle.Id,         
                    LicensePlate = vehicle.LicensePlate, 
                    StartTime = dto.StartTime,
                    EndTime = dto.EndTime,
                    IsActive = true,
                    DiscountCode = normalizedDiscountCode
                };

                await _context.Reservations.AddAsync(reservation);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new GetReservationDto
                {
                    Id = reservation.Id,
                    ParkingLotId = reservation.ParkingLotId,
                    UserId = reservation.UserId,
                    VehicleId = reservation.VehicleId,
                    LicensePlate = LicensePlateProtector.DecryptNormalized(encryption, reservation.LicensePlate),
                    StartTime = reservation.StartTime,
                    EndTime = reservation.EndTime,
                    IsActive = reservation.IsActive,
                    DiscountCode = reservation.DiscountCode,
                    EstimatedCost = estimatedCost,
                    EstimatedCostWithDiscount = estimatedWithDiscount
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        //GET
        public async Task<GetReservationDto?> GetById(int reservationId, Guid userId)
        {
            var r = await _context.Reservations
                .AsNoTracking()
                .Where(r => r.Id == reservationId && r.UserId == userId)
                .Select(r => new
                {
                    Reservation = r,
                    Tariff = r.ParkingLot!.Tariff
                })
                .FirstOrDefaultAsync();

            if (r is null) return null;

            var estimatedCost = CalculateEstimatedCostFromTariff(r.Tariff, r.Reservation.StartTime, r.Reservation.EndTime);
            var estimatedWithDiscount = estimatedCost;

            if (!string.IsNullOrWhiteSpace(r.Reservation.DiscountCode))
            {
                var preview = await discountService.PreviewDiscountAsync(
                    r.Reservation.DiscountCode,
                    userId,
                    r.Reservation.ParkingLotId,
                    r.Reservation.StartTime,
                    estimatedCost);

                if (preview.statusCode == 200 && preview.amountWithDiscount.HasValue)
                    estimatedWithDiscount = preview.amountWithDiscount.Value;
            }

            return new GetReservationDto
            {
                Id = r.Reservation.Id,
                ParkingLotId = r.Reservation.ParkingLotId,
                UserId = r.Reservation.UserId,
                VehicleId = r.Reservation.VehicleId,
                LicensePlate = LicensePlateProtector.DecryptNormalized(encryption, r.Reservation.LicensePlate),
                StartTime = r.Reservation.StartTime,
                EndTime = r.Reservation.EndTime,
                IsActive = r.Reservation.IsActive,
                DiscountCode = r.Reservation.DiscountCode,
                EstimatedCost = estimatedCost,
                EstimatedCostWithDiscount = estimatedWithDiscount
            };
        }

        //GET
        public async Task<GetReservationDto?> GetByVehicleId(int vehicleId, Guid userId)
        {
            var r = await _context.Reservations
                .AsNoTracking()
                .Where(r => r.VehicleId == vehicleId && r.UserId == userId)
                .OrderByDescending(r => r.StartTime)
                .Select(r => new { Reservation = r, Tariff = r.ParkingLot!.Tariff })
                .FirstOrDefaultAsync();

            if (r is null) return null;

            var estimatedCost = CalculateEstimatedCostFromTariff(r.Tariff, r.Reservation.StartTime, r.Reservation.EndTime);
            var estimatedWithDiscount = estimatedCost;

            if (!string.IsNullOrWhiteSpace(r.Reservation.DiscountCode))
            {
                var preview = await discountService.PreviewDiscountAsync(
                    r.Reservation.DiscountCode,
                    userId,
                    r.Reservation.ParkingLotId,
                    r.Reservation.StartTime,
                    estimatedCost);

                if (preview.statusCode == 200 && preview.amountWithDiscount.HasValue)
                    estimatedWithDiscount = preview.amountWithDiscount.Value;
            }

            return new GetReservationDto
            {
                Id = r.Reservation.Id,
                ParkingLotId = r.Reservation.ParkingLotId,
                UserId = r.Reservation.UserId,
                VehicleId = r.Reservation.VehicleId,
                LicensePlate = LicensePlateProtector.DecryptNormalized(encryption, r.Reservation.LicensePlate),
                StartTime = r.Reservation.StartTime,
                EndTime = r.Reservation.EndTime,
                IsActive = r.Reservation.IsActive,
                DiscountCode = r.Reservation.DiscountCode,
                EstimatedCost = estimatedCost,
                EstimatedCostWithDiscount = estimatedWithDiscount
            };
        }

        //PUT
        public async Task<GetReservationDto?> UpdateReservation(int reservationId, PutReservationDto dto, Guid userId)
        {
            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.Id == reservationId && r.UserId == userId)
                ?? throw new KeyNotFoundException("Reservation not found");

            var newStart = dto.StartTime ?? reservation.StartTime;
            var newEnd = dto.EndTime ?? reservation.EndTime;

            if (newStart.Offset != TimeSpan.Zero || newEnd.Offset != TimeSpan.Zero)
                throw new ValidationException("StartTime and EndTime must be UTC (offset +00:00).");

            var parkingLot = await _context.ParkingLots
                .FirstOrDefaultAsync(pl => pl.Id == reservation.ParkingLotId)
                    ?? throw new KeyNotFoundException("Parking lot not found");

            if (newStart >= newEnd)
                throw new ValidationException("StartTime must be before EndTime.");

            if (newStart < DateTimeOffset.UtcNow)
                throw new ValidationException("StartTime cannot be in the past.");

            if (dto.StartTime.HasValue || dto.EndTime.HasValue)
            {
                var reservedCount = await _context.Reservations
                    .Where(r => r.IsActive)
                    .Where(r => r.Id != reservation.Id)
                    .Where(r => r.ParkingLotId == reservation.ParkingLotId)
                    .Where(r => r.StartTime < newEnd && r.EndTime > newStart)
                    .CountAsync();

                if (reservedCount >= parkingLot.Capacity)
                    throw new ParkingLotFullException("No spots available for the selected time slot.");
            }

            if (dto.StartTime.HasValue) reservation.StartTime = dto.StartTime.Value;
            if (dto.EndTime.HasValue) reservation.EndTime = dto.EndTime.Value;
            if (!string.IsNullOrWhiteSpace(dto.LicensePlate))
            {
                var wanted = LicensePlateProtector.Normalize(dto.LicensePlate);

                var userVehicles = await _context.Vehicles
                    .Where(v => v.UserId == reservation.UserId)
                    .ToListAsync();

                var vehicle = userVehicles.FirstOrDefault(v =>
                    LicensePlateProtector.DecryptNormalized(encryption, v.LicensePlate) == wanted);

                if (vehicle is null)
                    throw new ValidationException($"Vehicle with license plate {dto.LicensePlate} does not exist for this user.");

                reservation.VehicleId = vehicle.Id;
                reservation.LicensePlate = vehicle.LicensePlate;
            }

            if (dto.DiscountCode is not null)
            {
                var est = CalculateEstimatedCost(parkingLot, newStart, newEnd);

                if (string.IsNullOrWhiteSpace(dto.DiscountCode))
                {
                    reservation.DiscountCode = null;
                }
                else
                {
                    var preview = await discountService.PreviewDiscountAsync(
                        dto.DiscountCode,
                        userId,
                        reservation.ParkingLotId,
                        newStart,
                        est);

                    if (preview.statusCode == 404) throw new KeyNotFoundException(preview.message);
                    if (preview.statusCode != 200) throw new ValidationException(preview.message);

                    reservation.DiscountCode = preview.normalizedCode;
                }
            }

            if (dto.DiscountCode is null && (dto.StartTime.HasValue || dto.EndTime.HasValue) && !string.IsNullOrWhiteSpace(reservation.DiscountCode))
            {
                var est = CalculateEstimatedCost(parkingLot, newStart, newEnd);

                var preview = await discountService.PreviewDiscountAsync(
                    reservation.DiscountCode,
                    userId,
                    reservation.ParkingLotId,
                    newStart,
                    est);

                if (preview.statusCode != 200)
                    throw new ValidationException($"Existing discount is no longer valid: {preview.message}");
            }

            await _context.SaveChangesAsync();

            var estimatedCost = CalculateEstimatedCostFromTariff(parkingLot.Tariff, reservation.StartTime, reservation.EndTime);
            var estimatedWithDiscount = estimatedCost;

            if (!string.IsNullOrWhiteSpace(reservation.DiscountCode))
            {
                var preview = await discountService.PreviewDiscountAsync(
                    reservation.DiscountCode,
                    userId,
                    reservation.ParkingLotId,
                    reservation.StartTime,
                    estimatedCost);

                if (preview.statusCode == 200 && preview.amountWithDiscount.HasValue)
                    estimatedWithDiscount = preview.amountWithDiscount.Value;
            }

            return new GetReservationDto
            {
                Id = reservation.Id,
                ParkingLotId = reservation.ParkingLotId,
                UserId = reservation.UserId,
                VehicleId = reservation.VehicleId,
                LicensePlate = LicensePlateProtector.DecryptNormalized(encryption, reservation.LicensePlate),
                StartTime = reservation.StartTime,
                EndTime = reservation.EndTime,
                IsActive = reservation.IsActive,
                DiscountCode = reservation.DiscountCode,
                EstimatedCost = estimatedCost,
                EstimatedCostWithDiscount = estimatedWithDiscount
            };
        }

        //DELETE
        public async Task<bool> DeleteReservation(int reservationId, Guid userId)
        {
            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.Id == reservationId && r.UserId == userId);

            if (reservation is null)
                return false;

            _context.Reservations.Remove(reservation);
            await _context.SaveChangesAsync();
            return true;
        }

        private static decimal CalculateEstimatedCost(ParkingLot parkingLot, DateTimeOffset startUtc, DateTimeOffset endUtc)
        {
            var minutes = (endUtc - startUtc).TotalMinutes;
            var hours = (decimal)Math.Ceiling(minutes / 60.0);

            var hourlyRate = Convert.ToDecimal(parkingLot.Tariff);

            return Math.Max(0, hours * hourlyRate);
        }

        private static decimal CalculateEstimatedCostFromTariff(double tariff, DateTimeOffset startUtc, DateTimeOffset endUtc)
        {
            var minutes = (endUtc - startUtc).TotalMinutes;
            var hours = (decimal)Math.Ceiling(minutes / 60.0);
            var hourlyRate = Convert.ToDecimal(tariff);
            return Math.Max(0, hours * hourlyRate);
        }
    }

    public class ParkingLotFullException : Exception
    {
        public ParkingLotFullException(string message) : base(message) { }
    }

}
