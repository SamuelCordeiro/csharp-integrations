using csharp_integrations.api.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace csharp_integrations.tests.Integration;

/// <summary>
/// Creates the API host with deterministic configuration for integration tests.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString = $"Data Source=csharp-integrations.tests.{Guid.NewGuid():N}.db";

    static ApiFactory()
    {
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("BearerToken__ApiKey", "test-signing-key-that-is-long-enough-for-hmac-sha256");
        Environment.SetEnvironmentVariable("BearerToken__Issuer", "csharp-integrations-tests");
        Environment.SetEnvironmentVariable("BearerToken__Audience", "csharp-integrations-tests-client");
        Environment.SetEnvironmentVariable("Ollama__Url", "http://127.0.0.1:1");
        Environment.SetEnvironmentVariable("Ollama__DefaultModel", "test-model");
    }

    /// <summary>
    /// Configures test-only settings without reading local user secrets or external services.
    /// </summary>
    /// <param name="builder">Web host builder used by the test server.</param>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connectionString));
            services.AddSingleton<IPasswordResetNotifier, TestPasswordResetNotifier>();
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
        });
        builder.ConfigureLogging(logging => logging.ClearProviders());
    }
}
