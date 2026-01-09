using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace csharp_integrations_core.Auth.Bearer;

public static class BearerMiddleware
{
    /// <summary>
    /// Authentication configuration middleware with Bearer token for API.
    /// </summary>
    public static IServiceCollection AddBearerAuthentication(this IServiceCollection services,
        IConfiguration configuration)
    {
        var apiKey = Environment.GetEnvironmentVariable("APIKEY") ?? throw new InvalidOperationException("ApiKey not configured.");

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(apiKey)),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });

        return services;
    }
}