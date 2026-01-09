using csharp_integrations_core.Auth.Bearer;
using csharp_integrations_core.GlobalResources.Models;
using csharp_integrations_core.GlobalResources.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace csharp_integrations_api.Controllers.Auth.Bearer;

[ApiController]
[Route("Auth/Bearer/[controller]")]
public class AuthBearerController: Controller
{
    [HttpPost("Login")]
    [AllowAnonymous]
    public ActionResult<dynamic> Login([FromBody] UserLogin model)
    {
        var user = UserRepository.Get(model.Username, model.Password);

        if (user == null) return NotFound();

        var token = new TokenService().Generate(user, 5);

        var result = new
        {
            username = model.Username,
            token
        };

        return Content(JsonConvert.SerializeObject(result), "application/json");
    }
}