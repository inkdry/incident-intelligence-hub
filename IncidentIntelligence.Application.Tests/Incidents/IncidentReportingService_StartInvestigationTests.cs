using IncidentIntelligence.Application.Incidents;
using IncidentIntelligence.Domain.Incidents;

namespace IncidentIntelligence.Application.Tests.Incidents;

public sealed class IncidentReportingService_StartInvestigationTests
{
    [Fact]
    public async Task StartInvestigationAsync_ReportedIncident_TransitionsAndSaves()
    {
        var existing = new Incident("API unavailable", "Requests are timing out.", IncidentSeverity.High);
        var repository = new RecordingIncidentRepository(existing);
        var service = new IncidentReportingService(repository);

        var result = await service.StartInvestigationAsync(
            new StartIncidentInvestigationCommand(existing.Id),
            TestContext.Current.CancellationToken);

        Assert.Same(existing, result);
        Assert.Equal(IncidentStatus.Investigating, result.Status);
        Assert.NotNull(result.InvestigationStartedAtUtc);
        Assert.Equal(1, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task StartInvestigationAsync_MissingIncident_ThrowsAndDoesNotSave()
    {
        var repository = new RecordingIncidentRepository(null);
        var service = new IncidentReportingService(repository);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.StartInvestigationAsync(
            new StartIncidentInvestigationCommand(Guid.NewGuid()),
            TestContext.Current.CancellationToken));

        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task StartInvestigationAsync_AlreadyInvestigating_ThrowsAndDoesNotSave()
    {
        var existing = new Incident("API unavailable", "Requests are timing out.", IncidentSeverity.High);
        existing.StartInvestigation();
        var repository = new RecordingIncidentRepository(existing);
        var service = new IncidentReportingService(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.StartInvestigationAsync(
            new StartIncidentInvestigationCommand(existing.Id),
            TestContext.Current.CancellationToken));

        Assert.Equal(0, repository.SaveChangesCallCount);
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
