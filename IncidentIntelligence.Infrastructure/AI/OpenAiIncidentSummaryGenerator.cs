using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using IncidentIntelligence.Application.Incidents;
using IncidentIntelligence.Domain.Incidents;
using Microsoft.Extensions.Options;

namespace IncidentIntelligence.Infrastructure.AI;

public sealed class OpenAiIncidentSummaryGenerator(HttpClient client, IOptions<OpenAiSummaryOptions> options)
    : IIncidentSummaryGenerator
{
    public async Task<GeneratedIncidentSummary> GenerateAsync(IncidentSummarySource source, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new SummaryGenerationException("AI summaries are not configured. Set OpenAI:ApiKey in the API's user secrets and restart the API.");
        if (string.IsNullOrWhiteSpace(settings.Model) || settings.Model.Trim().Length > 200)
            throw new SummaryGenerationException("The AI summary model is not configured correctly.");

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey.Trim());
        request.Content = JsonContent.Create(new
        {
            model = settings.Model.Trim(),
            store = false,
            max_output_tokens = 1200,
            instructions = IncidentSummaryPrompt.Instructions,
            input = JsonSerializer.Serialize(source)
        });

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new SummaryGenerationException(response.StatusCode switch
                {
                    System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden =>
                        "OpenAI rejected the API credentials. Check the API's OpenAI configuration.",
                    System.Net.HttpStatusCode.TooManyRequests =>
                        "OpenAI's quota or rate limit was reached. Check your OpenAI account or try again later.",
                    _ => "OpenAI could not generate a summary. Check the configured model or try again later."
                });

            using var document = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken)
                ?? throw new JsonException();
            var root = document.RootElement;
            if (!root.TryGetProperty("status", out var status) || status.GetString() != "completed")
                throw new SummaryGenerationException("OpenAI did not finish the summary. Please try again.");

            var parts = new List<string>();
            foreach (var item in root.GetProperty("output").EnumerateArray())
            {
                if (item.GetProperty("type").GetString() != "message") continue;
                foreach (var content in item.GetProperty("content").EnumerateArray())
                {
                    var type = content.GetProperty("type").GetString();
                    if (type == "refusal")
                        throw new SummaryGenerationException("OpenAI could not summarize this incident. Review its details before retrying.");
                    if (type == "output_text") parts.Add(content.GetProperty("text").GetString() ?? "");
                }
            }
            var text = string.Join("\n", parts).Trim();
            if (string.IsNullOrWhiteSpace(text) || text.Length > Incident.SummaryMaxLength)
                throw new SummaryGenerationException("OpenAI returned an empty or oversized summary. Please try again.");
            return new GeneratedIncidentSummary(text, settings.Model.Trim());
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new SummaryGenerationException("Summary generation timed out. Please try again.");
        }
        catch (HttpRequestException)
        {
            throw new SummaryGenerationException("Cannot reach OpenAI. Please try again later.");
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new SummaryGenerationException("OpenAI returned an invalid summary response. Please try again.");
        }
    }
}
