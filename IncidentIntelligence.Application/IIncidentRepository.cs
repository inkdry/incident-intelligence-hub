using IncidentIntelligence.Domain.Incidents;

namespace IncidentIntelligence.Application.Incidents;

/// <summary>
/// Provides storage operations for incidents.
/// </summary>
public interface IIncidentRepository
{
    /// <summary>
    /// Adds an incident to storage.
    /// </summary>
    Task AddAsync(Incident incident, CancellationToken cancellationToken);

    /// <summary>
    /// Returns all incidents.
    /// </summary>
    Task<IReadOnlyCollection<Incident>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Saves pending storage changes.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}