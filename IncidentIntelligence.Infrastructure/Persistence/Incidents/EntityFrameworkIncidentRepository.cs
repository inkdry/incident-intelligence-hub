using IncidentIntelligence.Application.Incidents;
using IncidentIntelligence.Domain.Incidents;
using Microsoft.EntityFrameworkCore;

namespace IncidentIntelligence.Infrastructure.Persistence.Incidents;

/// <summary>
/// Stores incidents using Entity Framework Core.
/// </summary>
public sealed class EntityFrameworkIncidentRepository(IncidentIntelligenceDbContext dbContext) : IIncidentRepository
{
    /// <inheritdoc />
    public async Task AddAsync(Incident incident, CancellationToken cancellationToken)
    {
        await dbContext.Incidents.AddAsync(incident, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Incident>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Incidents.AsNoTracking().OrderByDescending(incident => incident.ReportedAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
