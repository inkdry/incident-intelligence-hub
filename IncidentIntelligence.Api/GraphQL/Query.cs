using IncidentIntelligence.Application.Incidents;
using IncidentIntelligence.Domain.Incidents;

namespace IncidentIntelligence.Api.GraphQL;

/// <summary>
/// Defines the available GraphQL queries.
/// </summary>
public sealed class Query
{
    /// <summary>
    /// Returns the current API health status.
    /// </summary>
    public string GetStatus()
    {
        return "Incident Intelligence API is healthy";
    }

    /// <summary>
    /// Returns all reported incidents.
    /// </summary>
    public Task<IReadOnlyCollection<Incident>> GetIncidentsAsync(
        [Service] IIncidentRepository repository,
        CancellationToken cancellationToken)
    {
        return repository.GetAllAsync(cancellationToken);
    }

    /// <summary>
    /// Returns an incident by its identifier.
    /// </summary>
    public Task<Incident?> GetIncidentByIdAsync(
        Guid id,
        [Service] IIncidentRepository repository,
        CancellationToken cancellationToken)
    {
        return repository.GetByIdAsync(
            id,
            cancellationToken);
    }
}