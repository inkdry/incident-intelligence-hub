using IncidentIntelligence.Domain.Incidents;

namespace IncidentIntelligence.Application.Incidents;

public interface IIncidentReportingService
{
    Task<Incident> ReportAsync(ReportIncidentCommand command, CancellationToken cancellationToken);

    Task<Incident> UpdateAsync(UpdateIncidentCommand command, CancellationToken cancellationToken);

    Task<Incident> StartInvestigationAsync(
        StartIncidentInvestigationCommand command,
        CancellationToken cancellationToken);

    Task<Incident> MitigateAsync(
        MitigateIncidentCommand command,
        CancellationToken cancellationToken);
}
