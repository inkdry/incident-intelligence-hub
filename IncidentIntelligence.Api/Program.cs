using IncidentIntelligence.Api.GraphQL;
using IncidentIntelligence.Application.Incidents;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddGraphQLServer().AddQueryType<Query>().AddMutationType<Mutation>();
builder.Services.AddSingleton<IIncidentRepository, InMemoryIncidentRepository>();
builder.Services.AddScoped<IIncidentReportingService, IncidentReportingService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGraphQL();

app.Run();

/// <summary>
/// Exposes the application entry point to integration tests.
/// </summary>
public partial class Program;

