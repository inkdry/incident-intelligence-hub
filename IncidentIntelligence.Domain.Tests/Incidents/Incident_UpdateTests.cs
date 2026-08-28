using IncidentIntelligence.Domain.Incidents;

namespace IncidentIntelligence.Domain.Tests.Incidents;

public sealed class Incident_UpdateTests
{
    [Fact]
    public void UpdateDetails_ValidInputs_UpdatesFields()
    {
        var incident = new Incident("Original title", "Original description", IncidentSeverity.Low);

        incident.UpdateDetails("New title", "New description", IncidentSeverity.High);

        Assert.Equal("New title", incident.Title);
        Assert.Equal("New description", incident.Description);
        Assert.Equal(IncidentSeverity.High, incident.Severity);
    }

    [Fact]
    public void UpdateDetails_EmptyTitle_ThrowsArgumentException()
    {
        var incident = new Incident("Original title", "Original description", IncidentSeverity.Low);

        var ex = Assert.Throws<ArgumentException>(() => incident.UpdateDetails(" ", "Desc", IncidentSeverity.Low));

        Assert.Equal("title", ex.ParamName);
    }

    [Fact]
    public void UpdateDetails_EmptyDescription_ThrowsArgumentException()
    {
        var incident = new Incident("Original title", "Original description", IncidentSeverity.Low);

        var ex = Assert.Throws<ArgumentException>(() => incident.UpdateDetails("Title", " ", IncidentSeverity.Low));

        Assert.Equal("description", ex.ParamName);
    }
}
