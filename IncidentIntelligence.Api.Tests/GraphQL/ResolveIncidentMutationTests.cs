using System.Net.Http.Json;
using System.Text.Json;

namespace IncidentIntelligence.Api.Tests.GraphQL;

public sealed class ResolveIncidentMutationTests(CustomWebApplicationFactory application)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task ResolveIncident_MitigatedIncident_PersistsTransition()
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

        await PostMutationAsync(client, "startIncidentInvestigation", id, cancellationToken);
        await PostMutationAsync(client, "mitigateIncident", id, cancellationToken);
        using var resolveDocument = await PostMutationAsync(client, "resolveIncident", id, cancellationToken);
        var resolved = resolveDocument.RootElement.GetProperty("data").GetProperty("resolveIncident");

        Assert.Equal("RESOLVED", resolved.GetProperty("status").GetString());
        Assert.NotEqual(JsonValueKind.Null, resolved.GetProperty("resolvedAtUtc").ValueKind);
    }

    [Fact]
    public async Task ResolveIncident_MissingIncident_ReturnsGraphQlError()
    {
        using var client = application.CreateClient();
        using var document = await PostMutationAsync(
            client,
            "resolveIncident",
            Guid.NewGuid().ToString(),
            TestContext.Current.CancellationToken);

        Assert.True(document.RootElement.TryGetProperty("errors", out _));
    }

    private static async Task<JsonDocument> PostMutationAsync(
        HttpClient client,
        string mutation,
        string? id,
        CancellationToken cancellationToken)
    {
        var request = new
        {
            query = $$"""
                mutation Transition($id: UUID!) {
                  {{mutation}}(id: $id) {
                    status
                    resolvedAtUtc
                  }
                }
                """,
            variables = new { id }
        };
        using var response = await client.PostAsJsonAsync("/graphql", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken)
            ?? throw new InvalidOperationException("The transition response did not contain JSON.");
    }
}
