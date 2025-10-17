using Microsoft.EntityFrameworkCore;
using MobyPark.Entities;
using System.Collections.Generic;

namespace MobyPark.Data
{
    public class UserDbContext(DbContextOptions<UserDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(b =>
            {
                b.Property(u => u.Username).IsRequired().HasMaxLength(50);
                b.HasIndex(u => u.Username).IsUnique();

                b.Property(u => u.Email).IsRequired().HasMaxLength(256);
                b.HasIndex(u => u.Email).IsUnique();

                b.Property(u => u.PhoneNumber).IsRequired().HasMaxLength(30);
            });
        }
    }
}
