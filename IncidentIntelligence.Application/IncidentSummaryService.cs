using IncidentIntelligence.Domain.Incidents;

namespace IncidentIntelligence.Application.Incidents;

public sealed record IncidentSummarySource(
    string Title, string Description, string Severity, string Status,
    DateTimeOffset ReportedAtUtc, DateTimeOffset? InvestigationStartedAtUtc,
    DateTimeOffset? MitigatedAtUtc, DateTimeOffset? ResolvedAtUtc);

public sealed record GeneratedIncidentSummary(string Text, string Model);

public interface IIncidentSummaryGenerator
{
    Task<GeneratedIncidentSummary> GenerateAsync(IncidentSummarySource source, CancellationToken cancellationToken);
}

public sealed class SummaryGenerationException(string message) : Exception(message);

public sealed class IncidentSummaryService(IIncidentRepository repository, IIncidentSummaryGenerator generator)
{
    public async Task<Incident> GenerateAsync(Guid id, CancellationToken cancellationToken)
    {
        var incident = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Incident '{id}' was not found.");
        var version = incident.Version;
        var source = new IncidentSummarySource(incident.Title, incident.Description,
            incident.Severity.ToString(), incident.Status.ToString(), incident.ReportedAtUtc,
            incident.InvestigationStartedAtUtc, incident.MitigatedAtUtc, incident.ResolvedAtUtc);
        var summary = await generator.GenerateAsync(source, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        incident.SaveSummary(summary.Text, summary.Model, version);
        await repository.SaveChangesAsync(cancellationToken);
        return incident;
    }
}
