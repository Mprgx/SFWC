using Microsoft.EntityFrameworkCore;
using MobyPark.Entities;

namespace MobyPark.Data
{
    public class UserDbContext(DbContextOptions<UserDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Vehicle> Vehicles => Set<Vehicle>();
        public DbSet<ParkingSession> ParkingSessions => Set<ParkingSession>();
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
            modelBuilder.Entity<ParkingSession>()
                .HasOne(s => s.User)
                .WithMany(u => u.ParkingSessions)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Session ↔ Vehicle
            modelBuilder.Entity<ParkingSession>()
                .HasOne(s => s.Vehicle)
                .WithMany()
                .HasForeignKey(s => s.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            // ParkingLots primary key
            modelBuilder.Entity<ParkingLot>()
                .HasKey(p => p.Id);
        }
    }
}
