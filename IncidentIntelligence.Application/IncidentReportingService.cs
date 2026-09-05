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

    public async Task<Incident> UpdateAsync(UpdateIncidentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var incident = await repository.GetByIdAsync(command.Id, cancellationToken);

        if (incident is null)
        {
            throw new KeyNotFoundException($"Incident '{command.Id}' was not found.");
        }

        // Domain enforces validation.
        incident.UpdateDetails(command.Title, command.Description, command.Severity);

        await repository.SaveChangesAsync(cancellationToken);

        return incident;
    }

    public async Task<Incident> StartInvestigationAsync(
        StartIncidentInvestigationCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var incident = await repository.GetByIdAsync(command.Id, cancellationToken);

        if (incident is null)
        {
            throw new KeyNotFoundException($"Incident '{command.Id}' was not found.");
        }

        incident.StartInvestigation();
        await repository.SaveChangesAsync(cancellationToken);

        return incident;
    }

    public async Task<Incident> MitigateAsync(
        MitigateIncidentCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var incident = await repository.GetByIdAsync(command.Id, cancellationToken);

        if (incident is null)
        {
            throw new KeyNotFoundException($"Incident '{command.Id}' was not found.");
        }

        incident.Mitigate();
        await repository.SaveChangesAsync(cancellationToken);

        return incident;
    }

    public async Task<Incident> ResolveAsync(
        ResolveIncidentCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var incident = await repository.GetByIdAsync(command.Id, cancellationToken);

        if (incident is null)
        {
            throw new KeyNotFoundException($"Incident '{command.Id}' was not found.");
        }

        incident.Resolve();
        await repository.SaveChangesAsync(cancellationToken);

        return incident;
    }
}
