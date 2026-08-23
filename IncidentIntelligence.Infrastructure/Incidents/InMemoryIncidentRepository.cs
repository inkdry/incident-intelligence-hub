using System.Collections.Concurrent;
using IncidentIntelligence.Application.Incidents;
using IncidentIntelligence.Domain.Incidents;

namespace IncidentIntelligence.Infrastructure.Incidents;

public sealed class InMemoryIncidentRepository : IIncidentRepository
{
    private readonly ConcurrentDictionary<Guid, Incident> _incidents = new();

    public Task AddAsync(Incident incident, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_incidents.TryAdd(incident.Id, incident))
        {
            throw new InvalidOperationException($"Incident '{incident.Id}' already exists.");
        }

        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // In-memory changes are immediately available.
        return Task.CompletedTask;
    }
}
