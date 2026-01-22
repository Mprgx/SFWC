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
        public DbSet<Company> Companies => Set<Company>();
        public DbSet<UserVehicle> UserVehicles => Set<UserVehicle>();
        public DbSet<CompanyUser> CompanyUsers => Set<CompanyUser>();
        public DbSet<Billing> Billings { get; set; }
        public DbSet<Discount> Discounts => Set<Discount>();
        public DbSet<Invoice> Invoices => Set<Invoice>();



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
                .OnDelete(DeleteBehavior.Cascade);

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

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Discount)
                .WithMany()
                .HasForeignKey(r => r.DiscountCode)
                .OnDelete(DeleteBehavior.SetNull);

            // Payments
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.User)
                .WithMany(u => u.Payments)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Discounts
            modelBuilder.Entity<Discount>()
                .HasKey(d => d.Code);

            modelBuilder.Entity<Discount>()
                .HasOne(d => d.User)
                .WithMany(u => u.Discounts)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // DiscountUsers
            modelBuilder.Entity<DiscountUser>()
                .HasKey(du => new { du.Code, du.UserId });

            modelBuilder.Entity<DiscountUser>()
                .HasOne(du => du.Discount)
                .WithMany(d => d.ValidForUsers)
                .HasForeignKey(du => du.Code)
                .OnDelete(DeleteBehavior.Restrict);

            // DiscountCompanies
            modelBuilder.Entity<DiscountCompany>()
                .HasKey(dc => new { dc.Code, dc.CompanyId });

            modelBuilder.Entity<DiscountCompany>()
                .HasOne(dc => dc.Discount)
                .WithMany(d => d.ValidForCompanies)
                .HasForeignKey(dc => dc.Code)
                .OnDelete(DeleteBehavior.Restrict);

            // DiscountLocations
            modelBuilder.Entity<DiscountLocation>()
                .HasKey(dl => new { dl.Code, dl.ParkingLotId });

            modelBuilder.Entity<DiscountLocation>()
                .HasOne(dl => dl.Discount)
                .WithMany(d => d.AllowedLocations)
                .HasForeignKey(dl => dl.Code)
                .OnDelete(DeleteBehavior.Restrict);

        }

    }
}
