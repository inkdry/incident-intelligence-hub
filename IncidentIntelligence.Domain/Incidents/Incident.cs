namespace IncidentIntelligence.Domain.Incidents;

public sealed class Incident
{
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

        // Enforce database constraints: Title max length 200
        if (title.Trim().Length > 200)
        {
            throw new ArgumentException("Incident title must not exceed 200 characters.", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Incident description is required.", nameof(description));
        }

        // Enforce database constraints: Description max length 4000
        if (description.Trim().Length > 4000)
        {
            throw new ArgumentException("Incident description must not exceed 4000 characters.", nameof(description));
        }
    }
}
