using System.Net.Http.Json;
using System.Text.Json;

namespace IncidentIntelligence.Api.Tests.GraphQL;

public sealed class UpdateIncidentMutationTests(CustomWebApplicationFactory application)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task UpdateIncidentMutation_UpdatesPersistedIncident()
    {
        using var client = application.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // First create an incident
        var createRequest = new
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
                    title = "Integration incident",
                    description = "Created for update test.",
                    severity = "HIGH"
                }
            }
        };

        using var createResponse = await client.PostAsJsonAsync("/graphql", createRequest, cancellationToken);
        createResponse.EnsureSuccessStatusCode();

        using var createDoc = await createResponse.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken)
            ?? throw new InvalidOperationException("The create incident response did not contain JSON.");
        var created = createDoc.RootElement.GetProperty("data").GetProperty("reportIncident");
        var id = created.GetProperty("id").GetString();

        // Now update the incident
        var updateRequest = new
        {
            query = """
            mutation UpdateIncident($input: UpdateIncidentInput!) {
              updateIncident(input: $input) {
                id
                title
                severity
              }
            }
            """,
            variables = new
            {
                input = new
                {
                    id = id,
                    title = "Updated integration incident",
                    description = "Updated through mutation.",
                    severity = "MEDIUM"
                }
            }
        };

        using var updateResponse = await client.PostAsJsonAsync("/graphql", updateRequest, cancellationToken);
        updateResponse.EnsureSuccessStatusCode();

        using var updateDoc = await updateResponse.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken)
            ?? throw new InvalidOperationException("The update incident response did not contain JSON.");

        var updated = updateDoc.RootElement.GetProperty("data").GetProperty("updateIncident");

        Assert.Equal("Updated integration incident", updated.GetProperty("title").GetString());
        Assert.Equal("MEDIUM", updated.GetProperty("severity").GetString());

        // Verify via query
        var queryRequest = new { query = """query { incidents { id title severity } }""" };
        using var queryResponse = await client.PostAsJsonAsync("/graphql", queryRequest, cancellationToken);
        queryResponse.EnsureSuccessStatusCode();

        using var queryDoc = await queryResponse.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken)
            ?? throw new InvalidOperationException("The incidents query response did not contain JSON.");
        var incidents = queryDoc.RootElement.GetProperty("data").GetProperty("incidents").EnumerateArray();

        Assert.Contains(incidents, i => i.GetProperty("id").GetString() == id && i.GetProperty("title").GetString() == "Updated integration incident");
    }

    [Fact]
    public async Task UpdateIncidentMutation_NonexistentId_ReturnsGraphQLError()
    {
        using var client = application.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        var updateRequest = new
        {
            query = """
            mutation UpdateIncident($input: UpdateIncidentInput!) {
              updateIncident(input: $input) {
                id
                title
              }
            }
            """,
            variables = new
            {
                input = new
                {
                    id = Guid.NewGuid().ToString(),
                    title = "Doesn't matter",
                    description = "No-op",
                    severity = "LOW"
                }
            }
        };

        using var updateResponse = await client.PostAsJsonAsync("/graphql", updateRequest, cancellationToken);
        updateResponse.EnsureSuccessStatusCode();

        using var updateDoc = await updateResponse.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken)
            ?? throw new InvalidOperationException("The update incident response did not contain JSON.");

        // GraphQL should include errors when the operation throws
        Assert.True(updateDoc.RootElement.TryGetProperty("errors", out _));
    }
}
