using Microsoft.EntityFrameworkCore;
using OrbitHealth.Api.Models;

namespace OrbitHealth.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<CrewEntity> Crew => Set<CrewEntity>();
    public DbSet<SymptomEntity> Symptoms => Set<SymptomEntity>();
    public DbSet<TelemetryEntity> Telemetry => Set<TelemetryEntity>();
    public DbSet<ReportEntity> Reports => Set<ReportEntity>();
    public DbSet<SensorEntity> Sensors => Set<SensorEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CrewEntity>().HasKey(x => x.Id);
        modelBuilder.Entity<SymptomEntity>().HasKey(x => x.Id);
        modelBuilder.Entity<TelemetryEntity>().HasKey(x => x.Id);
        modelBuilder.Entity<ReportEntity>().HasKey(x => x.Id);
        modelBuilder.Entity<SensorEntity>().HasKey(x => x.Id);

        modelBuilder.Entity<SymptomEntity>().HasIndex(x => new { x.CrewId, x.Ts });
        modelBuilder.Entity<TelemetryEntity>().HasIndex(x => new { x.CrewId, x.Ts });
        modelBuilder.Entity<SensorEntity>().HasIndex(x => new { x.CrewId, x.Ts });
    }
}
