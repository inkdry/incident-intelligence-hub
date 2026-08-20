using IncidentIntelligence.Domain.Incidents;

namespace IncidentIntelligence.Application.Incidents;

public interface IIncidentRepository
{
    Task AddAsync(Incident incident, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
