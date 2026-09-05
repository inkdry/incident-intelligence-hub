using IncidentIntelligence.Application.Incidents;
using IncidentIntelligence.Domain.Incidents;

namespace IncidentIntelligence.Application.Tests.Incidents;

public sealed class IncidentReportingService_ResolveTests
{
    [Fact]
    public async Task ResolveAsync_MitigatedIncident_TransitionsAndSaves()
    {
        var existing = CreateMitigatedIncident();
        var repository = new RecordingIncidentRepository(existing);
        var service = new IncidentReportingService(repository);

        var result = await service.ResolveAsync(
            new ResolveIncidentCommand(existing.Id),
            TestContext.Current.CancellationToken);

        Assert.Same(existing, result);
        Assert.Equal(IncidentStatus.Resolved, result.Status);
        Assert.NotNull(result.ResolvedAtUtc);
        Assert.Equal(1, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task ResolveAsync_MissingIncident_ThrowsAndDoesNotSave()
    {
        var repository = new RecordingIncidentRepository(null);
        var service = new IncidentReportingService(repository);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ResolveAsync(
            new ResolveIncidentCommand(Guid.NewGuid()),
            TestContext.Current.CancellationToken));

        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task ResolveAsync_InvestigatingIncident_ThrowsAndDoesNotSave()
    {
        var existing = new Incident("API unavailable", "Requests are timing out.", IncidentSeverity.High);
        existing.StartInvestigation();
        var repository = new RecordingIncidentRepository(existing);
        var service = new IncidentReportingService(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ResolveAsync(
            new ResolveIncidentCommand(existing.Id),
            TestContext.Current.CancellationToken));

        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    private static Incident CreateMitigatedIncident()
    {
        var incident = new Incident("API unavailable", "Requests are timing out.", IncidentSeverity.High);
        incident.StartInvestigation();
        incident.Mitigate();
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
