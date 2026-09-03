using System.Net.Http.Json;
using System.Text.Json;

namespace IncidentIntelligence.Api.Tests.GraphQL;

public sealed class StartIncidentInvestigationMutationTests(CustomWebApplicationFactory application)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task StartIncidentInvestigation_ReportedIncident_PersistsTransition()
    {
        using var client = application.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        var createRequest = new
        {
            query = """
                mutation {
                  reportIncident(input: {
                    title: "Payment failures",
                    description: "Card payments are failing.",
                    severity: HIGH
                  }) { id }
                }
                """
        };
        using var createResponse = await client.PostAsJsonAsync("/graphql", createRequest, cancellationToken);
        createResponse.EnsureSuccessStatusCode();
        using var createDocument = await createResponse.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken)
            ?? throw new InvalidOperationException("The report incident response did not contain JSON.");
        var id = createDocument.RootElement.GetProperty("data").GetProperty("reportIncident").GetProperty("id").GetString();

        var startRequest = new
        {
            query = """
                mutation StartInvestigation($id: UUID!) {
                  startIncidentInvestigation(id: $id) {
                    status
                    investigationStartedAtUtc
                  }
                }
                """,
            variables = new { id }
        };
        using var startResponse = await client.PostAsJsonAsync("/graphql", startRequest, cancellationToken);
        startResponse.EnsureSuccessStatusCode();
        using var startDocument = await startResponse.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken)
            ?? throw new InvalidOperationException("The start investigation response did not contain JSON.");
        var started = startDocument.RootElement.GetProperty("data").GetProperty("startIncidentInvestigation");
        Assert.Equal("INVESTIGATING", started.GetProperty("status").GetString());
        Assert.NotEqual(JsonValueKind.Null, started.GetProperty("investigationStartedAtUtc").ValueKind);

        var queryRequest = new { query = $"query {{ incidentById(id: \"{id}\") {{ status investigationStartedAtUtc }} }}" };
        using var queryResponse = await client.PostAsJsonAsync("/graphql", queryRequest, cancellationToken);
        queryResponse.EnsureSuccessStatusCode();
        using var queryDocument = await queryResponse.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken)
            ?? throw new InvalidOperationException("The incident response did not contain JSON.");
        var persisted = queryDocument.RootElement.GetProperty("data").GetProperty("incidentById");
        Assert.Equal("INVESTIGATING", persisted.GetProperty("status").GetString());
        Assert.NotEqual(JsonValueKind.Null, persisted.GetProperty("investigationStartedAtUtc").ValueKind);
    }
}
