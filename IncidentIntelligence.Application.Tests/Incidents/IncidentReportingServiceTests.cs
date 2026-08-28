using IncidentIntelligence.Application.Incidents;
using IncidentIntelligence.Domain.Incidents;

namespace IncidentIntelligence.Application.Tests.Incidents;

public sealed class IncidentReportingServiceTests
{
    [Fact]
    public async Task ReportAsyncCreatesAndSavesIncident()
    {
        var repository = new RecordingIncidentRepository();
        var service = new IncidentReportingService(repository);
        var command = new ReportIncidentCommand(
            "Checkout unavailable",
            "Customers cannot submit orders.",
            IncidentSeverity.Critical);
        var cancellationToken =
            TestContext.Current.CancellationToken;

        var incident = await service.ReportAsync(
            command,
            cancellationToken);

        Assert.Same(incident, repository.AddedIncident);
        Assert.Equal(1, repository.SaveChangesCallCount);
        Assert.Equal(IncidentStatus.Reported, incident.Status);
    }

    [Fact]
    public async Task ReportAsyncRejectsBlankTitle()
    {
        var repository = new RecordingIncidentRepository();
        var service = new IncidentReportingService(repository);
        var command = new ReportIncidentCommand(" ", "A useful description.", IncidentSeverity.Medium);
        var cancellationToken = TestContext.Current.CancellationToken;

        await Assert.ThrowsAsync<ArgumentException>(() => service.ReportAsync(command, cancellationToken));

        Assert.Null(repository.AddedIncident);
        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    private sealed class RecordingIncidentRepository : IIncidentRepository
    {
        public Incident? AddedIncident { get; private set; }

        public int SaveChangesCallCount { get; private set; }

        public Task AddAsync(Incident incident, CancellationToken cancellationToken)
        {
            AddedIncident = incident;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<IReadOnlyCollection<Incident>> GetAllAsync(
            CancellationToken cancellationToken)
        {
            IReadOnlyCollection<Incident> incidents = [];

            return Task.FromResult(incidents);
        }

        public Task<Incident?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult<Incident?>(null);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }
}
