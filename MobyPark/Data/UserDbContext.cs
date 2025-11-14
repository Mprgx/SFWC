using Microsoft.EntityFrameworkCore;
using MobyPark.Entities;

namespace MobyPark.Data
{
    public class UserDbContext(DbContextOptions<UserDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Vehicle> Vehicles => Set<Vehicle>();
        public DbSet<Session> ParkingSessions => Set<Session>();
        public DbSet<ParkingLot> ParkingLots => Set<ParkingLot>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Vehicle ↔ User
            modelBuilder.Entity<Vehicle>()
                .HasOne(v => v.User)
                .WithMany(u => u.Vehicles)
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Session ↔ User
            modelBuilder.Entity<Session>()
                .HasOne(s => s.User)
                .WithMany(u => u.ParkingSessions)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Session ↔ Vehicle
            modelBuilder.Entity<Session>()
                .HasOne(s => s.Vehicle)
                .WithMany()
                .HasForeignKey(s => s.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Session ↔ ParkingLot
            modelBuilder.Entity<Session>()
                .HasOne(s => s.ParkingLot)
                .WithMany(p => p.ParkingSessions)
                .HasForeignKey(s => s.ParkingLotId);

            // ParkingLots primary key
            modelBuilder.Entity<ParkingLot>()
                .HasKey(p => p.Id);
        }
    }
}
