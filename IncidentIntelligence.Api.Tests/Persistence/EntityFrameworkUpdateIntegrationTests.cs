using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using IncidentIntelligence.Infrastructure.Persistence;
using IncidentIntelligence.Infrastructure.Persistence.Incidents;
using IncidentIntelligence.Application.Incidents;
using IncidentIntelligence.Domain.Incidents;

namespace IncidentIntelligence.Api.Tests.Persistence;

public sealed class EntityFrameworkUpdateIntegrationTests
{
    [Fact]
    public async Task UpdateAsync_PersistsAcrossDbContexts()
    {
        // Use an open SQLite in-memory connection so multiple contexts see the same DB
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<IncidentIntelligenceDbContext>()
            .UseSqlite(connection)
            .Options;

        // Ensure schema
        using (var createContext = new IncidentIntelligenceDbContext(options))
        {
            createContext.Database.EnsureCreated();
        }

        Guid id;

        var cancellationToken = TestContext.Current.CancellationToken;

        // Create and update using first context
        using (var context = new IncidentIntelligenceDbContext(options))
        {
            var repository = new EntityFrameworkIncidentRepository(context);
            var service = new IncidentReportingService(repository);

            var created = await service.ReportAsync(new ReportIncidentCommand("EF title", "EF description", IncidentSeverity.High), cancellationToken);
            id = created.Id;

            await service.UpdateAsync(new UpdateIncidentCommand(id, "EF updated", "EF updated desc", IncidentSeverity.Medium), cancellationToken);
            await service.StartInvestigationAsync(new StartIncidentInvestigationCommand(id), cancellationToken);
            await service.MitigateAsync(new MitigateIncidentCommand(id), cancellationToken);
        }

        // Reload in a new context and verify
        using (var verifyContext = new IncidentIntelligenceDbContext(options))
        {
            var repository = new EntityFrameworkIncidentRepository(verifyContext);
            var loaded = await repository.GetByIdAsync(id, cancellationToken);

            Assert.NotNull(loaded);
            Assert.Equal("EF updated", loaded!.Title);
            Assert.Equal("EF updated desc", loaded.Description);
            Assert.Equal(IncidentSeverity.Medium, loaded.Severity);
            Assert.Equal(IncidentStatus.Mitigated, loaded.Status);
            Assert.NotNull(loaded.MitigatedAtUtc);
        }
    }
}
