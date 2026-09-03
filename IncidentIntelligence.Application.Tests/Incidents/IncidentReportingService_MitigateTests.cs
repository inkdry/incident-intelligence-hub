using IncidentIntelligence.Application.Incidents;
using IncidentIntelligence.Domain.Incidents;

namespace IncidentIntelligence.Application.Tests.Incidents;

public sealed class IncidentReportingService_MitigateTests
{
    [Fact]
    public async Task MitigateAsync_InvestigatingIncident_TransitionsAndSaves()
    {
        var existing = CreateInvestigatingIncident();
        var repository = new RecordingIncidentRepository(existing);
        var service = new IncidentReportingService(repository);

        var result = await service.MitigateAsync(
            new MitigateIncidentCommand(existing.Id),
            TestContext.Current.CancellationToken);

        Assert.Same(existing, result);
        Assert.Equal(IncidentStatus.Mitigated, result.Status);
        Assert.NotNull(result.MitigatedAtUtc);
        Assert.Equal(1, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task MitigateAsync_MissingIncident_ThrowsAndDoesNotSave()
    {
        var repository = new RecordingIncidentRepository(null);
        var service = new IncidentReportingService(repository);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.MitigateAsync(
            new MitigateIncidentCommand(Guid.NewGuid()),
            TestContext.Current.CancellationToken));

        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task MitigateAsync_ReportedIncident_ThrowsAndDoesNotSave()
    {
        var existing = new Incident("API unavailable", "Requests are timing out.", IncidentSeverity.High);
        var repository = new RecordingIncidentRepository(existing);
        var service = new IncidentReportingService(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.MitigateAsync(
            new MitigateIncidentCommand(existing.Id),
            TestContext.Current.CancellationToken));

        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    private static Incident CreateInvestigatingIncident()
    {
        var incident = new Incident("API unavailable", "Requests are timing out.", IncidentSeverity.High);
        incident.StartInvestigation();
        return incident;
    }

    private sealed class RecordingIncidentRepository(Incident? existing) : IIncidentRepository
    {
        public int SaveChangesCallCount { get; private set; }
        public Task AddAsync(Incident incident, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyCollection<Incident>> GetAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<Incident>>([]);
        public Task<Incident?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(existing?.Id == id ? existing : null);
        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }
}
