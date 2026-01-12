using ITfoxtec.Identity.Saml2;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ITfoxtec.Identity.Saml2.Schemas.Metadata;
using ITfoxtec.Identity.Saml2.MvcCore.Configuration;
using System.ServiceModel.Security;

namespace csharp_integrations_core.Auth.SAML;

public static class SamlMiddleware
{
    /// <summary>
    /// Generic integration of the SP Saml2 middleware
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static IServiceCollection AddSamlAuthentication(this IServiceCollection services,
        IConfiguration configuration)
    {
        if (Environment.GetEnvironmentVariable("IdPMetadata") == null) return services;

        // Load certificated
        var certificateFile = Environment.GetEnvironmentVariable("SigningCertificateFile");
        var certificate = new X509Certificate2();

        if (certificateFile != null)
        {
            certificate = new X509Certificate2(certificateFile);

            // Add the certificate to the application's trusted certificate store.
            services.AddHttpClient("YourHttpClientName")
                .ConfigurePrimaryHttpMessageHandler(() =>
                {
                    var handler = new HttpClientHandler();
                    handler.ClientCertificates.Add(certificate);
                    return handler;
                });
        }

        // Load Sp Saml configurations
        services.Configure<Saml2Configuration>(saml2Configuration =>
        {
            if (certificateFile != null)
            {
                saml2Configuration.SigningCertificate = certificate;
            }

            saml2Configuration.Issuer = Environment.GetEnvironmentVariable("Issuer");
            saml2Configuration.AllowedAudienceUris.Add(Environment.GetEnvironmentVariable("Issuer"));
            saml2Configuration.SignatureAlgorithm = Environment.GetEnvironmentVariable("SignatureAlgorithm");
            saml2Configuration.CertificateValidationMode =
                Enum.Parse<X509CertificateValidationMode>(
                    Environment.GetEnvironmentVariable("CertificateValidationMode"));
            saml2Configuration.RevocationMode =
                Enum.Parse<X509RevocationMode>(Environment.GetEnvironmentVariable("RevocationMode"));
            saml2Configuration.AudienceRestricted =
                bool.Parse(Environment.GetEnvironmentVariable("AudienceRestricted"));

            var entityDescriptor = new EntityDescriptor();
            entityDescriptor.ReadIdPSsoDescriptorFromUrl(new Uri(Environment.GetEnvironmentVariable("IdPMetadata")));


            if (entityDescriptor.IdPSsoDescriptor != null)
            {
                saml2Configuration.SingleSignOnDestination =
                    entityDescriptor.IdPSsoDescriptor.SingleSignOnServices.First().Location;
                saml2Configuration.SignatureValidationCertificates.AddRange(entityDescriptor.IdPSsoDescriptor
                    .SigningCertificates);
            }
            else
            {
                throw new Exception("IdPSsoDescriptor not loaded from metadata.");
            }
        });

        services.AddSaml2();

        return services;
    }
}