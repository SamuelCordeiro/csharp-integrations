using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace csharp_integrations.core.Swagger;

/// <summary>
/// Provides Swagger registration with JWT bearer support.
/// </summary>
public static class SwaggerMiddleware
{
    /// <summary>
    /// Registers Swagger, optional XML documentation and operation-level bearer requirements.
    /// </summary>
    /// <param name="services">Service collection to update.</param>
    /// <param name="title">OpenAPI document title.</param>
    /// <param name="version">OpenAPI document version.</param>
    /// <param name="description">OpenAPI document description.</param>
    /// <param name="xmlDocumentationPaths">Optional paths to XML documentation files.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddSwaggerWithBearerSupport(
        this IServiceCollection services,
        string title = "API",
        string version = "v1",
        string description = "API description",
        IEnumerable<string>? xmlDocumentationPaths = null)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = title,
                Version = version,
                Description = description
            });

            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Informe somente o token JWT.",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            };

            c.AddSecurityDefinition("Bearer", securityScheme);
            c.OperationFilter<AuthorizeOperationFilter>();

            foreach (var xmlDocumentationPath in xmlDocumentationPaths ?? [])
            {
                if (File.Exists(xmlDocumentationPath))
                {
                    c.IncludeXmlComments(xmlDocumentationPath);
                }
            }
        });

        return services;
    }
}

internal sealed class AuthorizeOperationFilter : IOperationFilter
{
    /// <summary>
    /// Adds a bearer requirement only when the MVC action requires authorization.
    /// </summary>
    /// <param name="operation">OpenAPI operation being generated.</param>
    /// <param name="context">Swagger generation context.</param>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var controllerType = context.MethodInfo.DeclaringType;
        var allowsAnonymous = context.MethodInfo.GetCustomAttributes(true).OfType<IAllowAnonymous>().Any()
                              || controllerType?.GetCustomAttributes(true).OfType<IAllowAnonymous>().Any() == true;
        var requiresAuthorization = context.MethodInfo.GetCustomAttributes(true).OfType<IAuthorizeData>().Any()
                                    || controllerType?.GetCustomAttributes(true).OfType<IAuthorizeData>().Any() == true;

        if (allowsAnonymous || !requiresAuthorization)
        {
            return;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
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
        });
    }
}
