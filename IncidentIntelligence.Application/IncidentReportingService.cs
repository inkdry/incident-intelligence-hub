using IncidentIntelligence.Domain.Incidents;

namespace IncidentIntelligence.Application.Incidents;

public sealed class IncidentReportingService(IIncidentRepository repository) : IIncidentReportingService
{
    public async Task<Incident> ReportAsync(ReportIncidentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // The domain entity validates the incident details.
        var incident = new Incident(command.Title, command.Description, command.Severity);

        await repository.AddAsync(incident, cancellationToken);

        await repository.SaveChangesAsync(cancellationToken);

        return incident;
    }
}
