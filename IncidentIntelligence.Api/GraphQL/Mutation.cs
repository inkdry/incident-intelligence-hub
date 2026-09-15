using IncidentIntelligence.Application.Incidents;
using IncidentIntelligence.Domain.Incidents;

namespace IncidentIntelligence.Api.GraphQL;

public sealed class Mutation
{
    public async Task<Incident> GenerateIncidentSummaryAsync(
        Guid id,
        [Service] IncidentSummaryService summaryService,
        CancellationToken cancellationToken)
    {
        try
        {
            return await summaryService.GenerateAsync(id, cancellationToken);
        }
        catch (SummaryGenerationException exception)
        {
            throw new GraphQLException(ErrorBuilder.New().SetMessage(exception.Message).SetCode("SUMMARY_GENERATION_FAILED").Build());
        }
        catch (System.Collections.Generic.KeyNotFoundException)
        {
            throw new GraphQLException(ErrorBuilder.New().SetMessage("This incident no longer exists.").SetCode("INCIDENT_NOT_FOUND").Build());
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            throw new GraphQLException(ErrorBuilder.New().SetMessage("The incident changed while generating its summary. Refresh and try again.").SetCode("INCIDENT_CHANGED").Build());
        }
    }

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
