using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using System.Text.Json;

namespace IncidentIntelligence.Api.Tests.GraphQL;

/// <summary>
/// Verifies the GraphQL endpoint through the HTTP pipeline.
/// </summary>
public sealed class GraphQlEndpointTests(
    WebApplicationFactory<Program> application)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task StatusQueryReturnsHealthyMessage()
    {
        using var client = application.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        var request = new
        {
            query = "query { status }"
        };

        using var response = await client.PostAsJsonAsync("/graphql", request, cancellationToken);

        response.EnsureSuccessStatusCode();

        using var document = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken);

        Assert.NotNull(document);

        var status = document.RootElement.GetProperty("data").GetProperty("status").GetString();

        Assert.Equal("Incident Intelligence API is healthy", status);
    }

    [Fact]
    public async Task ReportIncidentMutationAddsIncidentToQuery()
    {
        using var client = application.CreateClient();
        var cancellationToken =
            TestContext.Current.CancellationToken;

        var mutationRequest = new
        {
            query = """
            mutation ReportIncident($input: ReportIncidentInput!) {
              reportIncident(input: $input) {
                id
                title
                status
              }
            }
            """,
            variables = new
            {
                input = new
                {
                    title = "Integration test incident",
                    description = "Created through the GraphQL endpoint.",
                    severity = "HIGH"
                }
            }
        };

        using var mutationResponse = await client.PostAsJsonAsync(
            "/graphql",
            mutationRequest,
            cancellationToken);

        mutationResponse.EnsureSuccessStatusCode();

        using var mutationDocument = await mutationResponse
            .Content.ReadFromJsonAsync<JsonDocument>(cancellationToken);

        Assert.NotNull(mutationDocument);

        var createdIncident = mutationDocument.RootElement.GetProperty("data").GetProperty("reportIncident");

        var incidentId = createdIncident.GetProperty("id").GetString();

        Assert.Equal("Integration test incident",  createdIncident.GetProperty("title").GetString());

        Assert.Equal("REPORTED", createdIncident.GetProperty("status").GetString());

        var queryRequest = new
        {
            query = """
            query {
              incidents {
                id
                title
              }
            }
            """
        };

        using var queryResponse = await client.PostAsJsonAsync("/graphql", queryRequest, cancellationToken);

        queryResponse.EnsureSuccessStatusCode();

        using var queryDocument = await queryResponse.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken);

        Assert.NotNull(queryDocument);

        var incidents = queryDocument.RootElement.GetProperty("data").GetProperty("incidents").EnumerateArray();

        Assert.Contains(incidents, incident => incident.GetProperty("id").GetString() == incidentId);
    }
}
