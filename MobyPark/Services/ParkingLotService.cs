using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using MobyPark.Data;
using MobyPark.Entities;
using MobyPark.Models;

namespace MobyPark.Services
{
    public class ParkingLotService(UserDbContext db, IEncryptionService encryption) : IParkingLotService
    {
        private static ParkingLotReadDto ToDto(ParkingLot lot) => new()
        {
            Id = lot.Id,
            Name = lot.Name,
            Location = lot.Location,
            Address = lot.Address,
            Capacity = lot.Capacity,
            Tariff = lot.Tariff,
            DayTariff = lot.DayTariff,
            Coordinates = JsonSerializer.Deserialize<Dictionary<string, double>>(lot.Coordinates) ?? new()
        };

        private SessionReadDto ToSessionDto(Session s)
        {
            return new SessionReadDto
            {
                Id = s.Id,
                UserId = s.UserId,
                VehicleId = s.VehicleId,
                ParkingLotId = s.ParkingLotId,
                LicensePlate = encryption.Decrypt(s.LicensePlate) ?? string.Empty,
                Started = s.Started,
                Stopped = s.Stopped,
                DurationMinutes = s.DurationMinutes,
                Cost = s.Cost,
                PaymentStatus = s.PaymentStatus,
                IsCancelled = s.IsCancelled,
                CancelledAt = s.CancelledAt,
                IsRefunded = s.IsRefunded,
                RefundDate = s.RefundDate
            };
        }

        // POST
        public async Task<(ParkingLotReadDto?, string?, int?)> CreateAsync(ParkingLotRequestDto dto, bool isAdmin)
        {
            if (!isAdmin)
                return (null, "Access denied", 403);

            var lot = new ParkingLot
            {
                Id = 0,
                Name = dto.Name,
                Location = dto.Location,
                Address = dto.Address,
                Capacity = dto.Capacity,
                Tariff = dto.Tariff,
                DayTariff = dto.DayTariff,
                Coordinates = JsonSerializer.Serialize(dto.Coordinates)
            };

            db.ParkingLots.Add(lot);
            await db.SaveChangesAsync();

            return (ToDto(lot), $"Parking lot saved under ID: {lot.Id}", 201);
        }

        // GET ALL
        public async Task<List<ParkingLotReadDto>> GetAllAsync()
            => (await db.ParkingLots.ToListAsync()).Select(ToDto).ToList();

        // GET BY ID
        public async Task<ParkingLotReadDto?> GetByIdAsync(int lid)
        {
            var lot = await db.ParkingLots.FindAsync(lid);
            return lot is null ? null : ToDto(lot);
        }

        // PUT
        public async Task<(ParkingLotReadDto?, string?, int?)> UpdateAsync(int lid, ParkingLotUpdateDto dto, bool isAdmin)
        {
            if (!isAdmin)
                return (null, "Access denied", 403);

            var lot = await db.ParkingLots.FindAsync(lid);
            if (lot is null) return (null, "Parking lot not found", 404);

            if (!string.IsNullOrWhiteSpace(dto.Name)) lot.Name = dto.Name;
            if (!string.IsNullOrWhiteSpace(dto.Location)) lot.Location = dto.Location;
            if (!string.IsNullOrWhiteSpace(dto.Address)) lot.Address = dto.Address;
            if (dto.Capacity.HasValue) lot.Capacity = dto.Capacity.Value;
            if (dto.Tariff.HasValue) lot.Tariff = dto.Tariff.Value;
            if (dto.DayTariff.HasValue) lot.DayTariff = dto.DayTariff.Value;
            if (dto.Coordinates is not null)
                lot.Coordinates = JsonSerializer.Serialize(dto.Coordinates);

            await db.SaveChangesAsync();
            return (ToDto(lot), "Parking lot modified", 200);
        }

        // DELETE
        public async Task<(bool, string?, int?)> DeleteAsync(int lid, bool isAdmin)
        {
            if (!isAdmin)
                return (false, "Access denied", 403);

            var lot = await db.ParkingLots.FindAsync(lid);
            if (lot is null) return (false, "Parking lot not found", 404);

            db.ParkingLots.Remove(lot);
            await db.SaveChangesAsync();
            return (true, "Parking lot deleted", 200);
        }

        // SESSIONS
        public async Task<(List<SessionReadDto>?, string?, int?)> GetSessionsAsync(int lid, string? username, bool isAdmin)
        {
            var exists = await db.ParkingLots.AnyAsync(p => p.Id == lid);
            if (!exists) return (null, null, 404);

            var query = db.Sessions.Where(s => s.ParkingLotId == lid);

            if (!isAdmin && username is not null)
                query = query.Where(s => s.User.Username == username);

            var result = (await query.ToListAsync()).Select(ToSessionDto).ToList();
            return (result, null, null);
        }

        public async Task<(SessionReadDto?, string?, int?)> GetSessionByIdAsync(int lid, Guid sid, string? username, bool isAdmin)
        {
            var session = await db.Sessions
                .FirstOrDefaultAsync(s => s.ParkingLotId == lid && s.Id == sid);

            if (session is null) return (null, null, 404);
            if (!isAdmin && session.User.Username != username)
                return (null, null, 404);

            return (ToSessionDto(session), null, null);
        }

        public async Task<(bool, string?, int?)> DeleteSessionAsync(int lid, Guid sid, bool isAdmin)
        {
            if (!isAdmin)
                return (false, "Access denied", 403);

            var session = await db.Sessions
                .FirstOrDefaultAsync(s => s.ParkingLotId == lid && s.Id == sid);

            if (session is null) return (false, "Session not found", 404);

            db.Sessions.Remove(session);
            await db.SaveChangesAsync();
            return (true, "Sessions deleted", 200);
        }
    }
}
