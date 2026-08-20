using IncidentIntelligence.Domain.Incidents;

namespace IncidentIntelligence.Application.Incidents;

public interface IIncidentReportingService
{
    Task<Incident> ReportAsync(ReportIncidentCommand command,  CancellationToken cancellationToken);
}
