using IncidentIntelligence.Api.GraphQL;
using IncidentIntelligence.Application.Incidents;
using IncidentIntelligence.Infrastructure.Incidents;

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

