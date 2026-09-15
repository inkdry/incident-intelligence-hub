using IncidentIntelligence.Domain.Incidents;
using IncidentIntelligence.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace IncidentIntelligence.Api.Tests.Persistence;

public sealed class IncidentSummaryPersistenceTests
{
    [Fact]
    public async Task SummaryAndStaleness_PersistAcrossContexts()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var options = new DbContextOptionsBuilder<IncidentIntelligenceDbContext>().UseSqlite(connection).Options;
        var incident = new Incident("Outage", "Timeouts", IncidentSeverity.High);
        incident.SaveSummary("Saved draft", "test-model", incident.Version);
        await using (var context = new IncidentIntelligenceDbContext(options))
        {
            await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            context.Add(incident);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        await using (var context = new IncidentIntelligenceDbContext(options))
        {
            var loaded = await context.Set<Incident>().SingleAsync(TestContext.Current.CancellationToken);
            Assert.Equal("Saved draft", loaded.Summary);
            Assert.Equal(incident.SummaryGeneratedAtUtc, loaded.SummaryGeneratedAtUtc);
            Assert.Equal("test-model", loaded.SummaryModel);
            Assert.False(loaded.SummaryIsStale);
            loaded.StartInvestigation();
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        await using var verify = new IncidentIntelligenceDbContext(options);
        var stale = await verify.Set<Incident>().SingleAsync(TestContext.Current.CancellationToken);
        Assert.True(stale.SummaryIsStale);
        Assert.Equal("Saved draft", stale.Summary);
    }

    [Fact]
    public async Task ConcurrentIncidentEdit_PreventsSavingAnOutdatedGeneration()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var options = new DbContextOptionsBuilder<IncidentIntelligenceDbContext>().UseSqlite(connection).Options;
        await using var generationContext = new IncidentIntelligenceDbContext(options);
        await generationContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var incident = new Incident("Outage", "Timeouts", IncidentSeverity.High);
        generationContext.Add(incident);
        await generationContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        await using (var editingContext = new IncidentIntelligenceDbContext(options))
        {
            var updated = await editingContext.Set<Incident>().SingleAsync(TestContext.Current.CancellationToken);
            updated.UpdateDetails("Outage", "Updated facts", IncidentSeverity.High);
            await editingContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        incident.SaveSummary("Outdated draft", "test-model", incident.Version);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => generationContext.SaveChangesAsync(TestContext.Current.CancellationToken));
        await using var verify = new IncidentIntelligenceDbContext(options);
        var persisted = await verify.Set<Incident>().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Null(persisted.Summary);
        Assert.Equal("Updated facts", persisted.Description);
    }
}
