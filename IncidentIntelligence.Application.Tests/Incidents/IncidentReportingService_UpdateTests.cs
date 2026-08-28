using IncidentIntelligence.Application.Incidents;
using IncidentIntelligence.Domain.Incidents;

namespace IncidentIntelligence.Application.Tests.Incidents;

public sealed class IncidentReportingService_UpdateTests
{
    [Fact]
    public async Task UpdateAsync_ExistingIncident_UpdatesAndSaves()
    {
        var existing = new Incident("Old title", "Old description", IncidentSeverity.Low);
        var repository = new RecordingIncidentRepository(existing);
        var service = new IncidentReportingService(repository);

        var command = new UpdateIncidentCommand(existing.Id, "Updated title", "Updated description", IncidentSeverity.High);
        var cancellationToken = TestContext.Current.CancellationToken;

        var updated = await service.UpdateAsync(command, cancellationToken);

        Assert.Same(existing, updated);
        Assert.Equal("Updated title", updated.Title);
        Assert.Equal("Updated description", updated.Description);
        Assert.Equal(IncidentSeverity.High, updated.Severity);
        Assert.Equal(1, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task UpdateAsync_NonexistentIncident_ThrowsKeyNotFoundException()
    {
        var repository = new RecordingIncidentRepository(null);
        var service = new IncidentReportingService(repository);

        var command = new UpdateIncidentCommand(Guid.NewGuid(), "Title", "Description", IncidentSeverity.Low);
        var cancellationToken = TestContext.Current.CancellationToken;

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateAsync(command, cancellationToken));
    }

    [Fact]
    public async Task UpdateAsync_InvalidData_BubblesArgumentException()
    {
        var existing = new Incident("Old title", "Old description", IncidentSeverity.Low);
        var repository = new RecordingIncidentRepository(existing);
        var service = new IncidentReportingService(repository);

        var command = new UpdateIncidentCommand(existing.Id, " ", "Description", IncidentSeverity.Low);
        var cancellationToken = TestContext.Current.CancellationToken;

        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync(command, cancellationToken));

        // Save should not have been called
        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    private sealed class RecordingIncidentRepository : IIncidentRepository
    {
        private readonly Incident? _existing;

        public RecordingIncidentRepository(Incident? existing)
        {
            _existing = existing;
        }

        public Task AddAsync(Incident incident, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<Incident>> GetAllAsync(CancellationToken cancellationToken)
        {
            IReadOnlyCollection<Incident> incidents = [];
            return Task.FromResult(incidents);
        }

        public Task<Incident?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            if (_existing is null) return Task.FromResult<Incident?>(null);
            return Task.FromResult<Incident?>(_existing.Id == id ? _existing : null);
        }

        public int SaveChangesCallCount { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }
}
