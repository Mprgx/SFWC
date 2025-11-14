using Microsoft.EntityFrameworkCore;
using MobyPark.Data;
using MobyPark.Entities;

namespace MobyPark.Services
{
    public class ParkingLotService(UserDbContext db) : IParkingLotService
    {
        public async Task<List<ParkingLot>> GetAllAsync()
        {
            return await db.Set<ParkingLot>().ToListAsync();
        }

        public async Task<ParkingLot?> GetByIdAsync(int lid)
        {
            return await db.Set<ParkingLot>()
                .FirstOrDefaultAsync(p => p.Id == lid);
        }

        public async Task<List<Session>> GetSessionsAsync(
            int lid, string? username, bool isAdmin)
        {
            var lot = await db.Set<ParkingLot>().AnyAsync(p => p.Id == lid);
            if (!lot)
                return new List<Session>();

            var query = db.ParkingSessions
                .Include(s => s.User)
                .Include(s => s.Vehicle)
                .Where(s => s.ParkingLotId == lid); //s.ParkingLotId == lid

            if (!isAdmin && username is not null)
                query = query.Where(s => s.User.Username == username);

            return await query.ToListAsync();
        }

        public async Task<Session?> GetSessionByIdAsync(
            int lid, string sid, string? username, bool isAdmin)
        {
            var session = await db.ParkingSessions
                .Include(s => s.User)
                .Include(s => s.Vehicle)
                .FirstOrDefaultAsync(s =>
                    s.ParkingLotId == lid && s.Id.ToString() == sid);

            if (session is null)
                return null;

            if (!isAdmin && session.User.Username != username)
                return null;

            return session;
        }
    }
}
