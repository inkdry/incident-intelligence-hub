using System.Net;
using System.Text;
using System.Text.Json;
using IncidentIntelligence.Application.Incidents;
using IncidentIntelligence.Infrastructure.AI;
using Microsoft.Extensions.Options;

namespace IncidentIntelligence.Api.Tests.AI;

public sealed class OllamaIncidentSummaryGeneratorTests
{
    private static readonly IncidentSummarySource Source = new("API down", "Timeouts", "High", "Reported", DateTimeOffset.UtcNow, null, null, null);

    [Fact]
    public async Task Generate_UsesLocalChatApiWithoutCredentials()
    {
        using var handler = new Handler(HttpStatusCode.OK, """
            {"done":true,"done_reason":"stop","message":{"role":"assistant","content":" Draft summary "}}
            """);
        using var client = new HttpClient(handler);
        var result = await Create(client).GenerateAsync(Source, TestContext.Current.CancellationToken);
        Assert.Equal("Draft summary", result.Text);
        Assert.Equal("ollama/llama3.2:3b", result.Model);
        Assert.Equal("http://localhost:11434/api/chat", handler.Url);
        Assert.Null(handler.Authorization);
        using var body = JsonDocument.Parse(handler.Body!);
        Assert.False(body.RootElement.GetProperty("stream").GetBoolean());
        var messages = body.RootElement.GetProperty("messages");
        Assert.Equal("system", messages[0].GetProperty("role").GetString());
        Assert.Equal("user", messages[1].GetProperty("role").GetString());
        using var input = JsonDocument.Parse(messages[1].GetProperty("content").GetString()!);
        Assert.Equal("Timeouts", input.RootElement.GetProperty("Description").GetString());
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("{\"done\":false,\"message\":{\"content\":\"partial\"}}")]
    [InlineData("{\"done\":true,\"done_reason\":\"length\",\"message\":{\"content\":\"partial\"}}")]
    [InlineData("{\"done\":true,\"message\":{\"content\":\" \"}}")]
    [InlineData("{\"error\":\"private details\"}")]
    public async Task InvalidOutput_IsNotSaved(string body)
    {
        using var handler = new Handler(HttpStatusCode.OK, body);
        using var client = new HttpClient(handler);
        var error = await Assert.ThrowsAsync<SummaryGenerationException>(() => Create(client).GenerateAsync(Source, TestContext.Current.CancellationToken));
        Assert.DoesNotContain("private details", error.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, "ollama pull llama3.2:3b")]
    [InlineData(HttpStatusCode.InternalServerError, "could not generate")]
    [InlineData(HttpStatusCode.Unauthorized, "denied access")]
    public async Task HttpFailure_ReturnsActionableMessage(HttpStatusCode status, string expected)
    {
        using var handler = new Handler(status, "private details");
        using var client = new HttpClient(handler);
        var error = await Assert.ThrowsAsync<SummaryGenerationException>(() => Create(client).GenerateAsync(Source, TestContext.Current.CancellationToken));
        Assert.Contains(expected, error.Message);
        Assert.DoesNotContain("private details", error.Message);
    }

    [Fact]
    public async Task OversizedOutput_IsRejected()
    {
        using var handler = new Handler(HttpStatusCode.OK, JsonSerializer.Serialize(new { done = true, message = new { content = new string('x', 8001) } }));
        using var client = new HttpClient(handler);
        await Assert.ThrowsAsync<SummaryGenerationException>(() => Create(client).GenerateAsync(Source, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ServerUnavailable_ExplainsHowToStartOllama()
    {
        using var client = new HttpClient(new FailureHandler(new HttpRequestException()));
        var error = await Assert.ThrowsAsync<SummaryGenerationException>(() => Create(client).GenerateAsync(Source, TestContext.Current.CancellationToken));
        Assert.Contains("ollama serve", error.Message);
    }

    [Fact]
    public async Task Timeout_IsDistinguishedFromCallerCancellation()
    {
        using var client = new HttpClient(new FailureHandler(new TaskCanceledException()));
        var error = await Assert.ThrowsAsync<SummaryGenerationException>(() => Create(client).GenerateAsync(Source, TestContext.Current.CancellationToken));
        Assert.Contains("too long", error.Message);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Create(client).GenerateAsync(Source, cancellation.Token));
    }

    [Theory]
    [InlineData("file:///tmp", "model")]
    [InlineData("not a url", "model")]
    [InlineData("http://localhost:11434", " ")]
    public async Task InvalidConfiguration_DoesNotSendRequest(string url, string model)
    {
        using var handler = new Handler(HttpStatusCode.OK, "{}");
        using var client = new HttpClient(handler);
        var generator = new OllamaIncidentSummaryGenerator(client, Options.Create(new OllamaSummaryOptions { BaseUrl = url, Model = model }));
        await Assert.ThrowsAsync<SummaryGenerationException>(() => generator.GenerateAsync(Source, TestContext.Current.CancellationToken));
        Assert.Null(handler.Url);
    }

    private static OllamaIncidentSummaryGenerator Create(HttpClient client) => new(client, Options.Create(new OllamaSummaryOptions()));

    private sealed class FailureHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromException<HttpResponseMessage>(exception);
    }

    private sealed class Handler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public string? Body { get; private set; }
        public string? Url { get; private set; }
        public string? Authorization { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Url = request.RequestUri!.ToString();
            Authorization = request.Headers.Authorization?.ToString();
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }
}
