using IncidentIntelligence.Domain.Incidents;
using Microsoft.EntityFrameworkCore;

namespace IncidentIntelligence.Infrastructure.Persistence;

/// <summary>
/// Provides database access for the application.
/// </summary>
public sealed class IncidentIntelligenceDbContext(
    DbContextOptions<IncidentIntelligenceDbContext> options)
    : DbContext(options)
{
    /// <summary>
    /// Gets the stored incidents.
    /// </summary>
    public DbSet<Incident> Incidents => Set<Incident>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(IncidentIntelligenceDbContext).Assembly);
    }
}
