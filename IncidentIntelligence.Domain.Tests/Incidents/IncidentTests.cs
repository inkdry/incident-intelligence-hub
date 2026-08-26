using IncidentIntelligence.Domain.Incidents;

namespace IncidentIntelligence.Domain.Tests.Incidents;

public sealed class IncidentTests
{
    [Fact]
    public void ConstructorCreatesReportedIncident()
    {
        var incident = new Incident(
            "Payment API unavailable",
            "Customers cannot complete payments.",
            IncidentSeverity.Critical);

        Assert.NotEqual(Guid.Empty, incident.Id);
        Assert.Equal("Payment API unavailable", incident.Title);
        Assert.Equal(IncidentSeverity.Critical, incident.Severity);
        Assert.Equal(IncidentStatus.Reported, incident.Status);
    }

    [Fact]
    public void ConstructorTrimsText()
    {
        var incident = new Incident("  Database latency  ",  "  Queries are timing out.  ",  IncidentSeverity.High);
        Assert.Equal("Database latency", incident.Title);
        Assert.Equal("Queries are timing out.", incident.Description);
    }

    [Fact]
    public void ConstructorRejectsBlankTitle()
    {
        var exception = Assert.Throws<ArgumentException>(() => new Incident(" ", "A useful description.", IncidentSeverity.Medium));

        Assert.Equal("title", exception.ParamName);
    }

    [Fact]
    public void StartInvestigationChangesReportedIncident()
    {
        var incident = new Incident("Authentication failures", "Users cannot sign in.", IncidentSeverity.High);

        incident.StartInvestigation();

        Assert.Equal(IncidentStatus.Investigating, incident.Status);
        Assert.NotNull(incident.InvestigationStartedAtUtc);
    }

    [Fact]
    public void StartInvestigationRejectsInvalidTransition()
    {
        var incident = new Incident("Authentication failures", "Users cannot sign in.", IncidentSeverity.High);

        incident.StartInvestigation();

        var exception = Assert.Throws<InvalidOperationException>(incident.StartInvestigation);

        Assert.Contains("reported incident", exception.Message);
    }
}
