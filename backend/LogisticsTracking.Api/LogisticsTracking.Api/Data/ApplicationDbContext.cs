using LogisticsTracking.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogisticsTracking.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Branch> Branches => Set<Branch>();
        public DbSet<Shipment> Shipments => Set<Shipment>();
        public DbSet<TrackingEvent> TrackingEvents => Set<TrackingEvent>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Shipment>()
                .HasOne(s => s.OriginBranch)
                .WithMany(b => b.OriginShipments)
                .HasForeignKey(s => s.OriginBranchId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Shipment>()
                .HasOne(s => s.DestinationBranch)
                .WithMany(b => b.DestinationShipments)
                .HasForeignKey(s => s.DestinationBranchId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
