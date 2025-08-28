using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RoadReady1.Models;

namespace RoadReady1.Context
{
   
    public class RoadReadyDbContext : DbContext
    {
        // Parameterless ctor for design-time instantiation
        public RoadReadyDbContext() { }

        // Normal DI ctor
        public RoadReadyDbContext(DbContextOptions<RoadReadyDbContext> options)
            : base(options)
        {
        }

       
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Build config
                var config = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                    .AddEnvironmentVariables()
                    .Build();

                var cs = config.GetConnectionString("defaultConnection")
                         ?? config["ConnectionStrings:defaultConnection"];
                if (string.IsNullOrWhiteSpace(cs))
                    throw new InvalidOperationException("Connection string 'defaultConnection' not found.");

                optionsBuilder.UseSqlServer(cs);
            }
        }

        // DbSets
        public DbSet<Role> Roles { get; set; }
        public DbSet<BookingStatus> BookingStatuses { get; set; }
        public DbSet<CarStatus> CarStatuses { get; set; }
        public DbSet<CarBrand> CarBrands { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Car> Cars { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<MaintenanceRequest> MaintenanceRequests { get; set; }
        public DbSet<BookingIssue> BookingIssues { get; set; }
        public DbSet<Refund> Refunds { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Booking → Location (restrict)
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.PickupLocation)
                .WithMany(l => l.Pickups)
                .HasForeignKey(b => b.PickupLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.DropoffLocation)
                .WithMany(l => l.Dropoffs)
                .HasForeignKey(b => b.DropoffLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            // Money precision
            modelBuilder.Entity<Car>()
                .Property(c => c.DailyRate).HasPrecision(10, 2);
            modelBuilder.Entity<Booking>()
                .Property(b => b.TotalAmount).HasPrecision(10, 2);
            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount).HasPrecision(10, 2);
            modelBuilder.Entity<Refund>()
                .Property(r => r.Amount).HasPrecision(10, 2);

            // Cascade deletes
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Booking).WithMany(b => b.Payments)
                .HasForeignKey(p => p.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Booking).WithMany(b => b.Reviews)
                .HasForeignKey(r => r.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MaintenanceRequest>()
                .HasOne(m => m.Car).WithMany(c => c.MaintenanceRequests)
                .HasForeignKey(m => m.CarId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Refund>()
                .HasOne(rf => rf.Booking).WithMany(b => b.Refunds)
                .HasForeignKey(rf => rf.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            // Restrict remaining cascades
            var fks = modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetForeignKeys())
                .Where(fk => fk.DeleteBehavior == DeleteBehavior.Cascade)
                .ToList();
            fks.ForEach(fk => fk.DeleteBehavior = DeleteBehavior.Restrict);
        }
    }
}