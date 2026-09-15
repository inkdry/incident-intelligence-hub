using IncidentIntelligence.Application.Incidents;
using IncidentIntelligence.Domain.Incidents;

namespace IncidentIntelligence.Application.Tests.Incidents;

public sealed class IncidentSummaryServiceTests
{
    [Fact]
    public async Task Generate_SavesSummaryFromIncidentFacts()
    {
        var incident = new Incident("Outage", "Timeouts", IncidentSeverity.Critical);
        incident.StartInvestigation();
        var repository = new Repository(incident);
        var generator = new Generator();
        var result = await new IncidentSummaryService(repository, generator).GenerateAsync(incident.Id, TestContext.Current.CancellationToken);
        Assert.Same(incident, result);
        Assert.Equal("Draft summary", result.Summary);
        Assert.Equal("test-model", result.SummaryModel);
        Assert.False(result.SummaryIsStale);
        Assert.Equal(1, repository.Saves);
        Assert.Equal("Timeouts", generator.Source!.Description);
        Assert.Equal("Investigating", generator.Source.Status);
        Assert.Equal(incident.InvestigationStartedAtUtc, generator.Source.InvestigationStartedAtUtc);
    }

    [Fact]
    public async Task MissingIncident_DoesNotCallGenerator()
    {
        var repository = new Repository(null);
        var generator = new Generator();
        await Assert.ThrowsAsync<KeyNotFoundException>(() => new IncidentSummaryService(repository, generator)
            .GenerateAsync(Guid.NewGuid(), TestContext.Current.CancellationToken));
        Assert.Null(generator.Source);
        Assert.Equal(0, repository.Saves);
    }

    [Fact]
    public async Task FailedRegeneration_PreservesPreviousDraft()
    {
        var incident = new Incident("Outage", "Timeouts", IncidentSeverity.High);
        incident.SaveSummary("Existing draft", "model", incident.Version);
        var timestamp = incident.SummaryGeneratedAtUtc;
        var repository = new Repository(incident);
        await Assert.ThrowsAsync<SummaryGenerationException>(() => new IncidentSummaryService(repository, new Generator { Fail = true })
            .GenerateAsync(incident.Id, TestContext.Current.CancellationToken));
        Assert.Equal("Existing draft", incident.Summary);
        Assert.Equal(timestamp, incident.SummaryGeneratedAtUtc);
        Assert.Equal(0, repository.Saves);
    }

    private sealed class Generator : IIncidentSummaryGenerator
    {
        public bool Fail { get; init; }
        public IncidentSummarySource? Source { get; private set; }
        public Task<GeneratedIncidentSummary> GenerateAsync(IncidentSummarySource source, CancellationToken cancellationToken)
        {
            Source = source;
            if (Fail) throw new SummaryGenerationException("Unavailable");
            return Task.FromResult(new GeneratedIncidentSummary("Draft summary", "test-model"));
        }
    }

    private sealed class Repository(Incident? incident) : IIncidentRepository
    {
        public int Saves { get; private set; }
        public Task AddAsync(Incident value, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyCollection<Incident>> GetAllAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<Incident>>([]);
        public Task<Incident?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(incident?.Id == id ? incident : null);
        public Task SaveChangesAsync(CancellationToken cancellationToken) { Saves++; return Task.CompletedTask; }
    }
}
