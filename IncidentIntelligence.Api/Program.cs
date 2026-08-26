using IncidentIntelligence.Api.GraphQL;
using IncidentIntelligence.Application.Incidents;

using IncidentIntelligence.Infrastructure.Persistence;
using IncidentIntelligence.Infrastructure.Persistence.Incidents;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddGraphQLServer().AddQueryType<Query>().AddMutationType<Mutation>();
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

app.UseHttpsRedirection();

app.MapGraphQL();

app.Run();

/// <summary>
/// Exposes the application entry point to integration tests.
/// </summary>
public partial class Program;

