using System.Net;
using System.Text;
using System.Text.Json;
using IncidentIntelligence.Application.Incidents;
using IncidentIntelligence.Infrastructure.AI;
using Microsoft.Extensions.Options;

namespace IncidentIntelligence.Api.Tests.AI;

public sealed class OpenAiIncidentSummaryGeneratorTests
{
    private static readonly IncidentSummarySource Source = new("API down", "Requests time out", "High", "Reported", DateTimeOffset.UtcNow, null, null, null);

    [Fact]
    public async Task Generate_UsesResponsesApiAndCombinesOutputText()
    {
        using var handler = new Handler(HttpStatusCode.OK, """
            {"status":"completed","output":[{"type":"reasoning"},{"type":"message","content":[{"type":"output_text","text":"First paragraph."},{"type":"output_text","text":"Second paragraph."}]}]}
            """);
        using var client = new HttpClient(handler);
        var generator = Create(client);
        var result = await generator.GenerateAsync(Source, TestContext.Current.CancellationToken);
        Assert.Equal("First paragraph.\nSecond paragraph.", result.Text);
        Assert.Equal("gpt-4.1-mini", result.Model);
        Assert.Equal("https://api.openai.com/v1/responses", handler.Url);
        Assert.Equal("Bearer test-key", handler.Authorization);
        using var body = JsonDocument.Parse(handler.Body!);
        Assert.False(body.RootElement.GetProperty("store").GetBoolean());
        Assert.Equal(1200, body.RootElement.GetProperty("max_output_tokens").GetInt32());
        using var input = JsonDocument.Parse(body.RootElement.GetProperty("input").GetString()!);
        Assert.Equal(Source.Description, input.RootElement.GetProperty("Description").GetString());
    }

    [Theory]
    [InlineData("{\"status\":\"incomplete\",\"output\":[]}")]
    [InlineData("{\"status\":\"completed\",\"output\":[]}")]
    [InlineData("{\"status\":\"completed\",\"output\":[{\"type\":\"message\",\"content\":[{\"type\":\"refusal\"}]}]}")]
    [InlineData("not json")]
    [InlineData("{}")]
    public async Task InvalidOrIncompleteOutput_IsNotAccepted(string body)
    {
        using var handler = new Handler(HttpStatusCode.OK, body);
        using var client = new HttpClient(handler);
        await Assert.ThrowsAsync<SummaryGenerationException>(() => Create(client).GenerateAsync(Source, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task ProviderError_DoesNotExposeResponseBody(HttpStatusCode status)
    {
        using var handler = new Handler(status, "sensitive provider detail");
        using var client = new HttpClient(handler);
        var error = await Assert.ThrowsAsync<SummaryGenerationException>(() => Create(client).GenerateAsync(Source, TestContext.Current.CancellationToken));
        Assert.DoesNotContain("sensitive", error.Message);
    }

    [Fact]
    public async Task MissingKey_DoesNotSendRequest()
    {
        using var handler = new Handler(HttpStatusCode.OK, "{}");
        using var client = new HttpClient(handler);
        var generator = new OpenAiIncidentSummaryGenerator(client, Options.Create(new OpenAiSummaryOptions()));
        await Assert.ThrowsAsync<SummaryGenerationException>(() => generator.GenerateAsync(Source, TestContext.Current.CancellationToken));
        Assert.Null(handler.Url);
    }

    private static OpenAiIncidentSummaryGenerator Create(HttpClient client) => new(client, Options.Create(new OpenAiSummaryOptions { ApiKey = "test-key" }));

    private sealed class Handler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public string? Body { get; private set; }
        public string? Url { get; private set; }
        public string? Authorization { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Url = request.RequestUri!.ToString();
            Authorization = request.Headers.Authorization!.ToString();
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }
}
