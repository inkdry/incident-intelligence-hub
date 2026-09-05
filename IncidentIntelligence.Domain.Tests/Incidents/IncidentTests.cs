using IncidentIntelligence.Domain.Incidents;

namespace IncidentIntelligence.Domain.Tests.Incidents;

public sealed class IncidentTests
{
    [Fact]
    public void ConstructorCreatesReportedIncident()
    {
        var incident = new Incident("Payment API unavailable", "Customers cannot complete payments.", IncidentSeverity.Critical);

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
    public void ConstructorRejectsLongTitle()
    {
        var longTitle = new string('X', 201);

        var exception = Assert.Throws<ArgumentException>(() => new Incident(longTitle, "A useful description.", IncidentSeverity.Medium));

        Assert.Equal("title", exception.ParamName);
    }

    [Fact]
    public void ConstructorRejectsLongDescription()
    {
        var longDescription = new string('Y', 4001);

        var exception = Assert.Throws<ArgumentException>(() => new Incident("Title", longDescription, IncidentSeverity.Medium));

        Assert.Equal("description", exception.ParamName);
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

    [Fact]
    public void MitigateChangesInvestigatingIncident()
    {
        var incident = new Incident("Authentication failures", "Users cannot sign in.", IncidentSeverity.High);
        incident.StartInvestigation();

        incident.Mitigate();

        Assert.Equal(IncidentStatus.Mitigated, incident.Status);
        Assert.NotNull(incident.MitigatedAtUtc);
        Assert.True(incident.MitigatedAtUtc >= incident.InvestigationStartedAtUtc);
    }

    [Fact]
    public void MitigateRejectsIncidentThatIsNotUnderInvestigation()
    {
        var incident = new Incident("Authentication failures", "Users cannot sign in.", IncidentSeverity.High);

        var exception = Assert.Throws<InvalidOperationException>(incident.Mitigate);

        Assert.Contains("under investigation", exception.Message);
        Assert.Null(incident.MitigatedAtUtc);
    }

    [Fact]
    public void ResolveChangesMitigatedIncident()
    {
        var incident = new Incident("Authentication failures", "Users cannot sign in.", IncidentSeverity.High);
        incident.StartInvestigation();
        incident.Mitigate();

        incident.Resolve();

        Assert.Equal(IncidentStatus.Resolved, incident.Status);
        Assert.NotNull(incident.ResolvedAtUtc);
        Assert.True(incident.ResolvedAtUtc >= incident.MitigatedAtUtc);
    }

    [Fact]
    public void ResolveRejectsIncidentThatIsNotMitigated()
    {
        var incident = new Incident("Authentication failures", "Users cannot sign in.", IncidentSeverity.High);

        var exception = Assert.Throws<InvalidOperationException>(incident.Resolve);

        Assert.Contains("mitigated incident", exception.Message);
        Assert.Null(incident.ResolvedAtUtc);
    }
}
