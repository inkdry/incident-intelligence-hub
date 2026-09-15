namespace IncidentIntelligence.Infrastructure.AI;

public sealed class OpenAiSummaryOptions
{
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "gpt-4.1-mini";
}
