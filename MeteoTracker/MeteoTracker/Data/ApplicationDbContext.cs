using Microsoft.EntityFrameworkCore;
using MeteoTracker.Entities;

namespace MeteoTracker.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<WeatherMeasurement> WeatherMeasurements { get; set; }
        public DbSet<WeatherStation> WeatherStations { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Index for fast lookup by timestamp, which is crucial for time-series data
            modelBuilder.Entity<WeatherMeasurement>()
                .HasIndex(w => w.Timestamp);

            // Combined index: filtering by Station + Time simultaneously (key for analysis!)
            modelBuilder.Entity<WeatherMeasurement>()
                .HasIndex(w => new { w.StationId, w.Timestamp });
        }
    }
}