namespace IncidentIntelligence.Domain.Incidents;

public sealed class Incident
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 4000;

    private Incident()
    {
        // Required by Entity Framework Core.
        Title = string.Empty;
        Description = string.Empty;
    }

    public Incident(
        string title,
        string description,
        IncidentSeverity severity)
    {
        ValidateTitleAndDescription(title, description);

        Id = Guid.NewGuid();
        Title = title.Trim();
        Description = description.Trim();
        Severity = severity;
        Status = IncidentStatus.Reported;
        ReportedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; }

    public string Description { get; private set; }

    public IncidentSeverity Severity { get; private set; }

    public IncidentStatus Status { get; private set; }

    public DateTimeOffset ReportedAtUtc { get; private set; }

    public DateTimeOffset? InvestigationStartedAtUtc { get; private set; }

    public DateTimeOffset? MitigatedAtUtc { get; private set; }

    public DateTimeOffset? ResolvedAtUtc { get; private set; }

    public void StartInvestigation()
    {
        if (Status != IncidentStatus.Reported)
        {
            throw new InvalidOperationException("Only a reported incident can begin investigation.");
        }

        // Record the lifecycle change and its audit timestamp.
        Status = IncidentStatus.Investigating;
        InvestigationStartedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Mitigate()
    {
        if (Status != IncidentStatus.Investigating)
        {
            throw new InvalidOperationException("Only an incident under investigation can be mitigated.");
        }

        Status = IncidentStatus.Mitigated;
        MitigatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Resolve()
    {
        if (Status != IncidentStatus.Mitigated)
        {
            throw new InvalidOperationException("Only a mitigated incident can be resolved.");
        }

        Status = IncidentStatus.Resolved;
        ResolvedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates the editable details of the incident.
    /// </summary>
    public void UpdateDetails(string title, string description, IncidentSeverity severity)
    {
        ValidateTitleAndDescription(title, description);

        Title = title.Trim();
        Description = description.Trim();
        Severity = severity;
    }

    private static void ValidateTitleAndDescription(string title, string description)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Incident title is required.", nameof(title));
        }

        // Enforce database constraints: Title max length
        if (title.Trim().Length > TitleMaxLength)
        {
            throw new ArgumentException($"Incident title must not exceed {TitleMaxLength} characters.", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Incident description is required.", nameof(description));
        }

        // Enforce database constraints: Description max length
        if (description.Trim().Length > DescriptionMaxLength)
        {
            throw new ArgumentException($"Incident description must not exceed {DescriptionMaxLength} characters.", nameof(description));
        }
    }
}
