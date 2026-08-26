using IncidentIntelligence.Application.Incidents;
using IncidentIntelligence.Infrastructure.Incidents;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IncidentIntelligence.Api.Tests;

/// <summary>
/// Configures isolated dependencies for API integration tests.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
            {
                // Keep integration tests independent from SQL Server.
                services.RemoveAll<IIncidentRepository>();

                services.AddSingleton<IIncidentRepository, InMemoryIncidentRepository>();
            });
    }
}
