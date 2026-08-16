namespace IncidentIntelligence.Domain.Incidents;

public sealed class Incident
{
    public Incident(
        string title,
        string description,
        IncidentSeverity severity)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Incident title is required.", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Incident description is required.", nameof(description));
        }

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
}
