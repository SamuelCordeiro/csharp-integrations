using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace csharp_integrations.api.Infrastructure;

/// <summary>
/// Removes SAML controllers from MVC when SAML authentication is not enabled.
/// </summary>
public sealed class ConditionalSamlControllerConvention : IApplicationModelConvention
{
    /// <summary>
    /// Removes controllers in the SAML authentication namespace from the application model.
    /// </summary>
    /// <param name="application">MVC application model being built.</param>
    public void Apply(ApplicationModel application)
    {
        var samlControllers = application.Controllers
            .Where(controller => controller.ControllerType.Namespace?.Contains(".Auth.SAML", StringComparison.Ordinal) == true)
            .ToArray();

        foreach (var controller in samlControllers)
        {
            application.Controllers.Remove(controller);
        }
    }
}
