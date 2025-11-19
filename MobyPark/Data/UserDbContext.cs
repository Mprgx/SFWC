using Microsoft.EntityFrameworkCore;
using MobyPark.Entities;

namespace MobyPark.Data
{
    public class UserDbContext(DbContextOptions<UserDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Vehicle> Vehicles => Set<Vehicle>();
        public DbSet<Session> Sessions => Set<Session>();
        public DbSet<ParkingLot> ParkingLots => Set<ParkingLot>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Reservation> Reservations => Set<Reservation>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ---------------------
            // User
            // ---------------------
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                // hier kun je eventueel nog property-config doen (MaxLength, Required, etc.)
            });

            // ---------------------
            // Vehicle  (1 User -> N Vehicles)
            // ---------------------
            modelBuilder.Entity<Vehicle>(entity =>
            {
                entity.HasKey(v => v.Id);

                entity.HasOne(v => v.User)
                    .WithMany(u => u.Vehicles)
                    .HasForeignKey(v => v.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Optioneel: unieke combi User + LicensePlate
                // entity.HasIndex(v => new { v.UserId, v.LicensePlate }).IsUnique();
            });

            // ---------------------
            // ParkingLot
            // ---------------------
            modelBuilder.Entity<ParkingLot>(entity =>
            {
                entity.HasKey(p => p.Id);
                // extra config (Required, MaxLength) kan hier
            });

            // ---------------------
            // Session  (User, Vehicle, ParkingLot)
            // ---------------------
            modelBuilder.Entity<Session>(entity =>
            {
                entity.HasKey(s => s.Id);

                // Session ↔ User  (1 User -> N Sessions)
                entity.HasOne(s => s.User)
                    .WithMany(u => u.Sessions)          // 👈 let op: Sessions in User
                    .HasForeignKey(s => s.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Session ↔ Vehicle (1 Vehicle -> N Sessions)
                entity.HasOne(s => s.Vehicle)
                    .WithMany(v => v.Sessions)         // 👈 Sessions in Vehicle
                    .HasForeignKey(s => s.VehicleId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Session ↔ ParkingLot (1 ParkingLot -> N Sessions)
                entity.HasOne(s => s.ParkingLot)
                    .WithMany(p => p.Sessions)         // 👈 Sessions in ParkingLot
                    .HasForeignKey(s => s.ParkingLotId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Optioneel: extra config
                // entity.Property(s => s.LicensePlate).HasMaxLength(20).IsRequired();
                // entity.Property(s => s.PaymentStatus).HasMaxLength(20).IsRequired();
                // entity.Property(s => s.Cost).HasPrecision(10, 2);
            });

            // ---------------------
            // Payment  (1 User -> N Payments)
            // ---------------------
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.HasOne(p => p.User)
                    .WithMany(u => u.Payments)
                    .HasForeignKey(p => p.Initiator)
                    .OnDelete(DeleteBehavior.Cascade);

                // entity.Property(p => p.Amount).HasColumnType("decimal(18,2)");
            });

            // ---------------------
            // Reservation  (User + ParkingLot)
            // ---------------------
            modelBuilder.Entity<Reservation>(entity =>
            {
                entity.HasKey(r => r.ReservationId);

                entity.HasOne(r => r.User)
                    .WithMany(u => u.Reservations)
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.ParkingLot)
                    .WithMany(p => p.Reservations)
                    .HasForeignKey(r => r.ParkingLotId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
