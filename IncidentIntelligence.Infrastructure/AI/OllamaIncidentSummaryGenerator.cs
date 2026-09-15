using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IncidentIntelligence.Application.Incidents;
using IncidentIntelligence.Domain.Incidents;
using Microsoft.Extensions.Options;

namespace IncidentIntelligence.Infrastructure.AI;

public sealed class OllamaIncidentSummaryGenerator(HttpClient client, IOptions<OllamaSummaryOptions> options)
    : IIncidentSummaryGenerator
{
    public async Task<GeneratedIncidentSummary> GenerateAsync(IncidentSummarySource source, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out var baseUri)
            || (baseUri.Scheme != "http" && baseUri.Scheme != "https")
            || !string.IsNullOrEmpty(baseUri.UserInfo) || !string.IsNullOrEmpty(baseUri.Query)
            || !string.IsNullOrEmpty(baseUri.Fragment))
            throw new SummaryGenerationException("Ollama:BaseUrl must be a valid HTTP or HTTPS server address.");
        if (string.IsNullOrWhiteSpace(settings.Model) || settings.Model.Trim().Length > 190)
            throw new SummaryGenerationException("Set Ollama:Model to the name of an installed local model.");
        var model = settings.Model.Trim();
        var endpoint = new Uri(baseUri.AbsoluteUri.TrimEnd('/') + "/api/chat");

        try
        {
            using var response = await client.PostAsJsonAsync(endpoint, new
            {
                model,
                stream = false,
                messages = new[]
                {
                    new { role = "system", content = IncidentSummaryPrompt.Instructions },
                    new { role = "user", content = JsonSerializer.Serialize(source) }
                },
                options = new { temperature = 0.2, num_predict = 1200, num_ctx = 8192 }
            }, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new SummaryGenerationException(response.StatusCode switch
                {
                    HttpStatusCode.NotFound => $"Ollama could not find the model. Run 'ollama pull {model}' and check Ollama:BaseUrl.",
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "Ollama denied access. Use a downloaded local model and check the server configuration.",
                    _ => "Ollama could not generate a summary. Check that the model can run on your machine and try again."
                });

            using var document = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken)
                ?? throw new JsonException();
            var root = document.RootElement;
            if (root.TryGetProperty("error", out _))
                throw new SummaryGenerationException("Ollama reported a generation error. Check the local server and try again.");
            if (!root.GetProperty("done").GetBoolean()
                || (root.TryGetProperty("done_reason", out var reason) && reason.GetString() != "stop"))
                throw new SummaryGenerationException("Ollama did not finish the summary. Try again or select another local model.");
            var text = root.GetProperty("message").GetProperty("content").GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(text) || text.Length > Incident.SummaryMaxLength)
                throw new SummaryGenerationException("Ollama returned an empty or oversized summary. Try again or select another local model.");
            return new GeneratedIncidentSummary(text, $"ollama/{model}");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new SummaryGenerationException("Ollama took too long to generate a summary. Let the model finish loading or try a smaller local model.");
        }
        catch (HttpRequestException)
        {
            throw new SummaryGenerationException("Cannot reach Ollama. Start the Ollama app or run 'ollama serve', then try again.");
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new SummaryGenerationException("Ollama returned an invalid response. Check Ollama:BaseUrl and try again.");
        }
    }
}
