using ITfoxtec.Identity.Saml2;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ITfoxtec.Identity.Saml2.Schemas.Metadata;
using ITfoxtec.Identity.Saml2.MvcCore.Configuration;
using System.ServiceModel.Security;

namespace csharp_integrations.core.Auth.SAML;

/// <summary>
/// Provides optional SAML service-provider registration and configuration validation.
/// </summary>
public static class SamlMiddleware
{
    private const string SamlMetadataHttpClientName = "SamlMetadata";

    private static readonly string[] RequiredConfigurationKeys =
    [
        "SAML:IdPMetadata",
        "SAML:Issuer",
        "SAML:SignatureAlgorithm",
        "SAML:CertificateValidationMode",
        "SAML:RevocationMode",
        "SAML:AudienceRestricted"
    ];

    /// <summary>
    /// Determines whether SAML authentication is enabled and validates a partial SAML configuration.
    /// </summary>
    /// <param name="configuration">Application configuration.</param>
    /// <returns><see langword="true"/> when every required SAML setting is configured; otherwise, <see langword="false"/> when SAML is not configured.</returns>
    /// <exception cref="InvalidOperationException">Thrown when only part of the SAML configuration is supplied.</exception>
    public static bool IsSamlAuthenticationEnabled(this IConfiguration configuration)
    {
        var configuredKeys = RequiredConfigurationKeys
            .Where(key => !string.IsNullOrWhiteSpace(configuration[key]))
            .ToArray();

        if (configuredKeys.Length == 0)
        {
            return false;
        }

        var missingKeys = RequiredConfigurationKeys
            .Where(key => string.IsNullOrWhiteSpace(configuration[key]))
            .ToArray();

        if (missingKeys.Length > 0)
        {
            throw new InvalidOperationException($"Incomplete SAML configuration. Missing: {string.Join(", ", missingKeys)}.");
        }

        return true;
    }

    /// <summary>
    /// Registers the SAML service-provider services when SAML is enabled.
    /// </summary>
    /// <param name="services">Service collection to update.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddSamlAuthentication(this IServiceCollection services,
        IConfiguration configuration)
    {
        if (!configuration.IsSamlAuthenticationEnabled())
        {
            return services;
        }

        // Load certificated
        var certificateFile = configuration["SAML:SigningCertificateFile"];
        X509Certificate2? certificate = null;

        if (!string.IsNullOrWhiteSpace(certificateFile))
        {
            certificate = X509CertificateLoader.LoadCertificateFromFile(certificateFile);
            var clientCertificate = certificate;

            services.AddHttpClient(SamlMetadataHttpClientName)
                .ConfigurePrimaryHttpMessageHandler(() =>
                {
                    var handler = new HttpClientHandler();
                    handler.ClientCertificates.Add(clientCertificate);
                    return handler;
                });
        }
        else
        {
            services.AddHttpClient(SamlMetadataHttpClientName);
        }

        // Load Sp Saml configurations
        services.AddOptions<Saml2Configuration>().Configure<IHttpClientFactory>((saml2Configuration, httpClientFactory) =>
        {
            if (certificate is not null)
            {
                saml2Configuration.SigningCertificate = certificate;
            }

            saml2Configuration.Issuer = configuration["SAML:Issuer"] ??
                                        throw new InvalidOperationException("SAML:Issuer not configured.");
            saml2Configuration.AllowedAudienceUris.Add(configuration["SAML:Issuer"] ??
                                                       throw new InvalidOperationException("SAML:Issuer not configured."));
            saml2Configuration.SignatureAlgorithm = configuration["SAML:SignatureAlgorithm"] ??
                                                    throw new InvalidOperationException("SAML:SignatureAlgorithm not configured.");
            saml2Configuration.CertificateValidationMode =
                Enum.Parse<X509CertificateValidationMode>(
                    configuration["SAML:CertificateValidationMode"] ??
                    throw new InvalidOperationException("SAML:CertificateValidationMode not configured."));
            saml2Configuration.RevocationMode =
                Enum.Parse<X509RevocationMode>(configuration["SAML:RevocationMode"] ??
                                               throw new InvalidOperationException(
                                                   "SAML:RevocationMode not configured."));
            saml2Configuration.AudienceRestricted =
                bool.Parse(configuration["SAML:AudienceRestricted"] ??
                           throw new InvalidOperationException("SAML:AudienceRestricted not configured."));

            var metadataUri = new Uri(configuration["SAML:IdPMetadata"] ??
                                      throw new InvalidOperationException("SAML:IdPMetadata not configured."));
            var entityDescriptor = new EntityDescriptor();
            entityDescriptor.ReadIdPSsoDescriptorFromUrlAsync(
                    httpClientFactory,
                    metadataUri,
                    cancellationToken: null,
                    httpClientName: SamlMetadataHttpClientName)
                .GetAwaiter()
                .GetResult();


            if (entityDescriptor.IdPSsoDescriptor != null)
            {
                saml2Configuration.SingleSignOnDestination =
                    entityDescriptor.IdPSsoDescriptor.SingleSignOnServices.First().Location;
                saml2Configuration.SignatureValidationCertificates.AddRange(entityDescriptor.IdPSsoDescriptor
                    .SigningCertificates);
            }
            else
            {
                throw new InvalidOperationException("IdPSsoDescriptor not loaded from metadata.");
            }
        });

        services.AddSaml2();

        return services;
    }
}
