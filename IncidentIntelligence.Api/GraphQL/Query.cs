namespace IncidentIntelligence.Api.GraphQL;

public sealed class Query
{
    // Confirms that the GraphQL endpoint is responding.
    public string GetStatus()
    {
        return "Incident Intelligence API is healthy";
    }
}