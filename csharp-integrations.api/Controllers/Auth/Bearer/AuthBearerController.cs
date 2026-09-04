using csharp_integrations.core.Auth;
using csharp_integrations.core.GlobalResources.Models;
using csharp_integrations.core.GlobalResources.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace csharp_integrations.api.Controllers.Auth.Bearer;

[ApiController]
[Route("Auth/Bearer/[controller]")]
public class AuthBearerController : Controller
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
            token,
        };

        return Content(Newtonsoft.Json.JsonConvert.SerializeObject(result), "application/json");
    }
}