using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace csharp_integrations_core.Swagger;

public static class SwaggerMiddleware
{
    /// <summary>
    /// Generic Swagger with Bearer authentication configuration for Api
    /// </summary>
    /// <param name="services">Swagger IServiceCollection</param>
    /// <param name="title">Api title</param>
    /// <param name="version">Api version</param>
    /// <param name="description">Api description</param>
    /// <returns></returns>
    public static IServiceCollection AddSwaggerWithBearerSupport(this IServiceCollection services, string title = "API", string version = "v1", string description = "API description")
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = title,
                Version = version,
                Description = description
            });

            // Bearer authentication support in Swagger
            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Enter your Bearer token",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT"
            };

            c.AddSecurityDefinition("Bearer", securityScheme);

            var securityRequirement = new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            };

            c.AddSecurityRequirement(securityRequirement);
        });

        return services;
    }
}