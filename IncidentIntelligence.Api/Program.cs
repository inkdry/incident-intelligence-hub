using IncidentIntelligence.Api.GraphQL;
using IncidentIntelligence.Application.Incidents;

using IncidentIntelligence.Infrastructure.Persistence;
using IncidentIntelligence.Infrastructure.Persistence.Incidents;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddGraphQLServer().AddQueryType<Query>().AddMutationType<Mutation>();
var frontendOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        if (frontendOrigins.Length > 0)
        {
            policy.WithOrigins(frontendOrigins).AllowAnyHeader().AllowAnyMethod();
        }
    });
});
var connectionString = builder.Configuration.GetConnectionString("IncidentDatabase")
    ?? throw new InvalidOperationException("The IncidentDatabase connection string is missing.");

builder.Services.AddDbContext<IncidentIntelligenceDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddScoped<IIncidentRepository, EntityFrameworkIncidentRepository>();
builder.Services.AddScoped<IIncidentReportingService, IncidentReportingService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// TestServer does not expose an HTTPS port. Production-like environments keep
// HTTPS redirection enabled while integration tests exercise the HTTP pipeline.
if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseCors("Frontend");

// Map GraphQL and enable the GraphQL tooling only in Development via HotChocolate options.
app.MapGraphQL()
    .WithOptions(options =>
    {
        options.Tool.Enable = app.Environment.IsDevelopment();
    });

app.Run();

/// <summary>
/// Exposes the application entry point to integration tests.
/// </summary>
public partial class Program;

