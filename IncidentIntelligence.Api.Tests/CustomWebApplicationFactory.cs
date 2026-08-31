using IncidentIntelligence.Application.Incidents;
using IncidentIntelligence.Infrastructure.Incidents;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace IncidentIntelligence.Api.Tests;

/// <summary>
/// Configures isolated dependencies for API integration tests.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting(
            "ConnectionStrings:IncidentDatabase",
            "Server=(localdb)\\MSSQLLocalDB;Database=IncidentIntelligenceHubTests;Trusted_Connection=True;TrustServerCertificate=True");

        // The Windows Event Log provider can require elevated permissions and is
        // unnecessary for integration tests running against TestServer.
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddDebug();
        });

        builder.ConfigureServices(services =>
            {
                // Keep integration tests independent from SQL Server.
                services.RemoveAll<IIncidentRepository>();

                services.AddSingleton<IIncidentRepository, InMemoryIncidentRepository>();
            });
    }
}
