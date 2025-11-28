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
        // public DbSet<ParkingSession> ParkingSessions => Set<ParkingSession>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Reservation> Reservations => Set<Reservation>();
        public DbSet<UserVehicle> UserVehicles => Set<UserVehicle>();
        public DbSet<CompanyUser> CompanyUsers => Set<CompanyUser>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // UserVehicles
            modelBuilder.Entity<UserVehicle>()
                .HasKey(uv => new { uv.UserId, uv.VehicleId });

            modelBuilder.Entity<UserVehicle>()
                .HasOne(uv => uv.User)
                .WithMany(u => u.UserVehicles)
                .HasForeignKey(uv => uv.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserVehicle>()
                .HasOne(uv => uv.Vehicle)
                .WithMany(v => v.UserVehicles)
                .HasForeignKey(uv => uv.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            // CompanyUsers
            modelBuilder.Entity<CompanyUser>()
                .HasKey(cu => new { cu.CompanyId, cu.UserId });

            modelBuilder.Entity<CompanyUser>()
                .HasOne(cu => cu.Company)
                .WithMany(c => c.CompanyUsers)
                .HasForeignKey(cu => cu.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CompanyUser>()
                .HasOne(cu => cu.User)
                .WithMany(u => u.CompanyUsers)
                .HasForeignKey(cu => cu.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Sessions
            modelBuilder.Entity<Session>()
                .HasOne(s => s.Vehicle)
                .WithMany()
                .HasForeignKey(s => s.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Session>()
                .HasOne(s => s.User)
                .WithMany(u => u.ParkingSessions)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Session>()
                .HasOne(s => s.ParkingLot)
                .WithMany(p => p.ParkingSessions)
                .HasForeignKey(s => s.ParkingLotId)
                .OnDelete(DeleteBehavior.Restrict);

            // Reservations
            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reservations)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Vehicle)
                .WithMany()
                .HasForeignKey(r => r.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Company)
                .WithMany(c => c.Reservations)
                .HasForeignKey(r => r.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.ParkingLot)
                .WithMany(p => p.Reservations)
                .HasForeignKey(r => r.ParkingLotId)
                .OnDelete(DeleteBehavior.Restrict);

            // Payments
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.User)
                .WithMany(u => u.Payments)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }

    }
}
