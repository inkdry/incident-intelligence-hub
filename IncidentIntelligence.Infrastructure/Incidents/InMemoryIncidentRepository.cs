using IncidentIntelligence.Application.Incidents;
using IncidentIntelligence.Domain.Incidents;
using System.Collections.Concurrent;

namespace IncidentIntelligence.Infrastructure.Incidents;

/// <summary>
/// Stores incidents in memory during application development.
/// </summary>
public sealed class InMemoryIncidentRepository : IIncidentRepository
{
    private readonly ConcurrentDictionary<Guid, Incident> _incidents = new();

    /// <inheritdoc />
    public Task AddAsync(Incident incident, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_incidents.TryAdd(incident.Id, incident))
        {
            throw new InvalidOperationException($"Incident '{incident.Id}' already exists.");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<Incident>> GetAllAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyCollection<Incident> incidents = _incidents.Values
                .OrderByDescending(incident => incident.ReportedAtUtc)
                .ToArray();

        return Task.FromResult(incidents);
    }

    /// <inheritdoc />
    public Task<Incident?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _incidents.TryGetValue(id, out var incident);

        return Task.FromResult(incident);
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // In-memory changes are immediately available.
        return Task.CompletedTask;
    }
}