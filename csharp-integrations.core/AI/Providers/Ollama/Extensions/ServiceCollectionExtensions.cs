using csharp_integrations.core.AI.Providers.Ollama.Options;
using csharp_integrations.core.AI.Providers.Ollama.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace csharp_integrations.core.AI.Providers.Ollama.Extensions;

/// <summary>
/// Provides dependency-injection registration for Ollama services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers validated Ollama options and the scoped Ollama client.
    /// </summary>
    /// <param name="services">Service collection to update.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddOllama(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<OllamaOptions>()
            .Bind(configuration.GetSection(OllamaOptions.SectionName))
            .Validate(options =>
                    !string.IsNullOrWhiteSpace(options.Url),
                "Ollama:Url is required.")
            .Validate(options =>
                    !string.IsNullOrWhiteSpace(options.DefaultModel),
                "Ollama:DefaultModel is required.")
            .ValidateOnStart();

        services.AddScoped<OllamaClient>();

        return services;
    }
}
