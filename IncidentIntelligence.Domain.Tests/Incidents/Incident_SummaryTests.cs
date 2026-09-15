using IncidentIntelligence.Domain.Incidents;

namespace IncidentIntelligence.Domain.Tests.Incidents;

public sealed class Incident_SummaryTests
{
    [Fact]
    public void Summary_IsPreservedAndFlaggedAfterDetailsOrLifecycleChange()
    {
        var incident = new Incident("Outage", "Requests fail", IncidentSeverity.High);
        Assert.Null(incident.Summary);
        Assert.False(incident.SummaryIsStale);
        incident.SaveSummary(" Initial draft ", "model", incident.Version);
        Assert.Equal("Initial draft", incident.Summary);
        Assert.NotNull(incident.SummaryGeneratedAtUtc);
        Assert.False(incident.SummaryIsStale);
        incident.UpdateDetails("Outage", "Requests fail", IncidentSeverity.High);
        Assert.False(incident.SummaryIsStale);
        incident.UpdateDetails("Outage", "Some requests fail", IncidentSeverity.High);
        Assert.True(incident.SummaryIsStale);
        Assert.Equal("Initial draft", incident.Summary);
        incident.SaveSummary("New draft", "model", incident.Version);
        Assert.False(incident.SummaryIsStale);
        incident.StartInvestigation();
        Assert.True(incident.SummaryIsStale);
        incident.SaveSummary("Investigating", "model", incident.Version);
        incident.Mitigate();
        Assert.True(incident.SummaryIsStale);
        incident.SaveSummary("Mitigated", "model", incident.Version);
        incident.Resolve();
        Assert.True(incident.SummaryIsStale);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8001)]
    public void InvalidSummary_DoesNotReplaceSavedDraft(int length)
    {
        var incident = new Incident("Outage", "Requests fail", IncidentSeverity.High);
        incident.SaveSummary("Keep this draft", "model", incident.Version);
        Assert.Throws<ArgumentException>(() => incident.SaveSummary(new string('x', length), "model", incident.Version));
        Assert.Equal("Keep this draft", incident.Summary);
    }
}
