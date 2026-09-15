using System.Net.Http.Json;
using System.Text.Json;
using IncidentIntelligence.Application.Incidents;
using IncidentIntelligence.Domain.Incidents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IncidentIntelligence.Api.Tests.GraphQL;

public sealed class GenerateIncidentSummaryMutationTests
{
    [Fact]
    public async Task SlowLocalGeneration_CanFinishBeyondDefaultGraphQlTimeout()
    {
        using var factory = new CustomWebApplicationFactory();
        using var application = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IIncidentSummaryGenerator>();
            services.AddSingleton<IIncidentSummaryGenerator>(new SlowGenerator());
        }));
        using var client = application.CreateClient();
        var incident = new Incident("Slow model", "Cold start", IncidentSeverity.High);
        await application.Services.GetRequiredService<IIncidentRepository>().AddAsync(incident, TestContext.Current.CancellationToken);
        using var result = await Post(client, "mutation($id: UUID!) { generateIncidentSummary(id: $id) { summary } }", incident.Id);
        Assert.False(result.RootElement.TryGetProperty("errors", out _), result.RootElement.ToString());
        Assert.Equal("Slow local draft", incident.Summary);
    }

    private sealed class SlowGenerator : IIncidentSummaryGenerator
    {
        public async Task<GeneratedIncidentSummary> GenerateAsync(IncidentSummarySource source, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(31), cancellationToken);
            return new GeneratedIncidentSummary("Slow local draft", "test-model");
        }
    }

    [Fact]
    public async Task OllamaProvider_GeneratesAndReturnsSavedSummaryThroughGraphQl()
    {
        using var factory = new CustomWebApplicationFactory();
        using var application = factory.WithWebHostBuilder(builder => builder.UseSetting("AI:Provider", "Ollama").ConfigureServices(services =>
            services.AddHttpClient<IncidentIntelligence.Infrastructure.AI.OllamaIncidentSummaryGenerator>()
                .ConfigurePrimaryHttpMessageHandler(() => new OllamaHandler())));
        using var client = application.CreateClient();
        var incident = new Incident("Outage", "Timeouts", IncidentSeverity.High);
        await application.Services.GetRequiredService<IIncidentRepository>().AddAsync(incident, TestContext.Current.CancellationToken);
        using var result = await Post(client, "mutation($id: UUID!) { generateIncidentSummary(id: $id) { summary summaryModel } }", incident.Id);
        Assert.False(result.RootElement.TryGetProperty("errors", out _), result.RootElement.ToString());
        Assert.Equal("Local draft", incident.Summary);
        Assert.Equal("ollama/llama3.2:3b", incident.SummaryModel);
        using var query = await Post(client, "query($id: UUID!) { incidentById(id: $id) { summary } }", incident.Id);
        Assert.Equal("Local draft", query.RootElement.GetProperty("data").GetProperty("incidentById").GetProperty("summary").GetString());
    }

    private sealed class OllamaHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal("localhost", request.RequestUri!.Host);
            Assert.Null(request.Headers.Authorization);
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { done = true, done_reason = "stop", message = new { content = "Local draft" } })
            });
        }
    }

    [Fact]
    public async Task GenerateAndRegenerate_AreAvailableOnSubsequentQuery()
    {
        using var factory = new CustomWebApplicationFactory();
        using var application = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IIncidentSummaryGenerator>();
            services.AddSingleton<IIncidentSummaryGenerator>(new Generator());
        }));
        using var client = application.CreateClient();
        var repository = application.Services.GetRequiredService<IIncidentRepository>();
        var incident = new Incident("Outage", "Timeouts", IncidentSeverity.High);
        await repository.AddAsync(incident, TestContext.Current.CancellationToken);

        for (var count = 1; count <= 2; count++)
        {
            using var result = await Post(client, "mutation($id: UUID!) { generateIncidentSummary(id: $id) { summary summaryGeneratedAtUtc summaryModel summaryIsStale status } }", incident.Id);
            Assert.False(result.RootElement.TryGetProperty("errors", out _), result.RootElement.ToString());
            var generated = result.RootElement.GetProperty("data").GetProperty("generateIncidentSummary");
            Assert.Equal($"Draft {count}", generated.GetProperty("summary").GetString());
            Assert.Equal("test-model", generated.GetProperty("summaryModel").GetString());
            Assert.False(generated.GetProperty("summaryIsStale").GetBoolean());
            Assert.Equal("REPORTED", generated.GetProperty("status").GetString());
            Assert.NotEqual(JsonValueKind.Null, generated.GetProperty("summaryGeneratedAtUtc").ValueKind);
        }
        using var query = await Post(client, "query($id: UUID!) { incidentById(id: $id) { summary } }", incident.Id);
        Assert.Equal("Draft 2", query.RootElement.GetProperty("data").GetProperty("incidentById").GetProperty("summary").GetString());
    }

    [Fact]
    public async Task MissingIncident_ReturnsUsefulError()
    {
        using var application = new CustomWebApplicationFactory();
        using var client = application.CreateClient();
        using var result = await Post(client, "mutation($id: UUID!) { generateIncidentSummary(id: $id) { summary } }", Guid.NewGuid());
        Assert.Equal("INCIDENT_NOT_FOUND", result.RootElement.GetProperty("errors")[0].GetProperty("extensions").GetProperty("code").GetString());
    }

    [Fact]
    public async Task UnconfiguredProvider_ReturnsUsefulErrorAndPreservesSummary()
    {
        using var factory = new CustomWebApplicationFactory();
        using var application = factory.WithWebHostBuilder(builder => builder.UseSetting("AI:Provider", "OpenAI").ConfigureServices(services =>
            services.PostConfigure<IncidentIntelligence.Infrastructure.AI.OpenAiSummaryOptions>(options => options.ApiKey = null)));
        using var client = application.CreateClient();
        var incident = new Incident("Outage", "Timeouts", IncidentSeverity.High);
        incident.SaveSummary("Saved draft", "test", incident.Version);
        await application.Services.GetRequiredService<IIncidentRepository>().AddAsync(incident, TestContext.Current.CancellationToken);
        using var result = await Post(client, "mutation($id: UUID!) { generateIncidentSummary(id: $id) { summary } }", incident.Id);
        var error = result.RootElement.GetProperty("errors")[0];
        Assert.Equal("SUMMARY_GENERATION_FAILED", error.GetProperty("extensions").GetProperty("code").GetString());
        Assert.Contains("not configured", error.GetProperty("message").GetString());
        Assert.Equal("Saved draft", incident.Summary);
    }

    private static async Task<JsonDocument> Post(HttpClient client, string query, Guid id)
    {
        using var response = await client.PostAsJsonAsync("/graphql", new { query, variables = new { id } }, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonDocument>(TestContext.Current.CancellationToken))!;
    }

    private sealed class Generator : IIncidentSummaryGenerator
    {
        private int count;
        public Task<GeneratedIncidentSummary> GenerateAsync(IncidentSummarySource source, CancellationToken cancellationToken)
            => Task.FromResult(new GeneratedIncidentSummary($"Draft {++count}", "test-model"));
    }
}
