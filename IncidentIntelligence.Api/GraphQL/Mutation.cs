using IncidentIntelligence.Application.Incidents;
using IncidentIntelligence.Domain.Incidents;

namespace IncidentIntelligence.Api.GraphQL;

public sealed class Mutation
{
    public async Task<Incident> ReportIncidentAsync(
        ReportIncidentInput input,
        [Service] IIncidentReportingService reportingService,
        CancellationToken cancellationToken)
    {
        // Translate the GraphQL input into an application command.
        var command = new ReportIncidentCommand(input.Title, input.Description, input.Severity);

        return await reportingService.ReportAsync(command, cancellationToken);
    }

    public async Task<Incident> UpdateIncidentAsync(
        UpdateIncidentInput input,
        [Service] IIncidentReportingService reportingService,
        CancellationToken cancellationToken)
    {
        var command = new UpdateIncidentCommand(input.Id, input.Title, input.Description, input.Severity);

        return await reportingService.UpdateAsync(command, cancellationToken);
    }

    public Task<Incident> StartIncidentInvestigationAsync(
        Guid id,
        [Service] IIncidentReportingService reportingService,
        CancellationToken cancellationToken)
    {
        return reportingService.StartInvestigationAsync(
            new StartIncidentInvestigationCommand(id),
            cancellationToken);
    }

    public Task<Incident> MitigateIncidentAsync(
        Guid id,
        [Service] IIncidentReportingService reportingService,
        CancellationToken cancellationToken)
    {
        return reportingService.MitigateAsync(
            new MitigateIncidentCommand(id),
            cancellationToken);
    }

    public Task<Incident> ResolveIncidentAsync(
        Guid id,
        [Service] IIncidentReportingService reportingService,
        CancellationToken cancellationToken)
    {
        return reportingService.ResolveAsync(
            new ResolveIncidentCommand(id),
            cancellationToken);
    }
}
