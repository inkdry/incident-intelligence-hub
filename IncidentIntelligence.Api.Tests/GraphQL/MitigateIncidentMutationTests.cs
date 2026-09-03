using System.Net.Http.Json;
using System.Text.Json;

namespace IncidentIntelligence.Api.Tests.GraphQL;

public sealed class MitigateIncidentMutationTests(CustomWebApplicationFactory application)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task MitigateIncident_InvestigatingIncident_PersistsTransition()
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
        using var mitigateDocument = await PostMutationAsync(client, "mitigateIncident", id, cancellationToken);
        var mitigated = mitigateDocument.RootElement.GetProperty("data").GetProperty("mitigateIncident");

        Assert.Equal("MITIGATED", mitigated.GetProperty("status").GetString());
        Assert.NotEqual(JsonValueKind.Null, mitigated.GetProperty("mitigatedAtUtc").ValueKind);
    }

    [Fact]
    public async Task MitigateIncident_MissingIncident_ReturnsGraphQlError()
    {
        using var client = application.CreateClient();
        using var document = await PostMutationAsync(
            client,
            "mitigateIncident",
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
                    mitigatedAtUtc
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
