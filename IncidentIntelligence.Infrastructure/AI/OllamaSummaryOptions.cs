namespace IncidentIntelligence.Infrastructure.AI;

public sealed class OllamaSummaryOptions
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "llama3.2:3b";
}
