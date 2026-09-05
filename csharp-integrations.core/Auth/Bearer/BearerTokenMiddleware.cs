using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace csharp_integrations.core.Auth.Bearer;

/// <summary>
/// Provides JWT bearer-authentication registration.
/// </summary>
public static class BearerTokenMiddleware
{
    /// <summary>
    /// Registers JWT validation using the configured signing key, issuer and audience.
    /// </summary>
    /// <param name="services">Service collection to update.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddBearerAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var apiKey = configuration["BearerToken:ApiKey"]
                     ?? throw new InvalidOperationException("BearerToken:ApiKey not configured.");
        var issuer = configuration["BearerToken:Issuer"]
                     ?? throw new InvalidOperationException("BearerToken:Issuer not configured.");
        var audience = configuration["BearerToken:Audience"]
                       ?? throw new InvalidOperationException("BearerToken:Audience not configured.");

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(apiKey)),
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        return services;
    }
}
