using System.Text;
using csharp_integrations.api.Data;
using csharp_integrations.core.AI.Providers.Ollama.Models;
using csharp_integrations.core.AI.Providers.Ollama.Services;
using csharp_integrations.core.AI.Providers.Ollama.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace csharp_integrations.api.Controllers.AI.Providers.Ollama;

/// <summary>
/// Exposes authenticated operations for the configured Ollama server.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OllamaController : ControllerBase
{
    private readonly OllamaClient _ollama;
    
    /// <summary>
    /// Initializes the controller with the configured Ollama client.
    /// </summary>
    /// <param name="ollama">Client used to communicate with Ollama.</param>
    public OllamaController(OllamaClient ollama)
    {
        _ollama = ollama;
    }
    
    /// <summary>
    /// Checks whether the Ollama server is available.
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Health()
    {
        var available = await _ollama.IsAvailableAsync();

        return Ok(new
        {
            Available = available
        });
    }
    
    /// <summary>
    /// Lists the models available on Ollama.
    /// </summary>
    [HttpGet("models")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Models()
    {
        var models = await _ollama.ListModelsAsync();

        return Ok(models);
    }
    
    /// <summary>
    /// Downloads a model to the Ollama server.
    /// </summary>
    [HttpPost("models/download")]
    [Authorize(Policy = ApplicationAuthorizationPolicies.CanManageModels)]
    [EnableRateLimiting("ollama-download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Download(
        DownloadModelRequest request)
    {
        await _ollama.PullModelAsync(request.Model);

        return Ok(new
        {
            Message = "Model downloaded successfully."
        });
    }

    /// <summary>
    /// Sends a message to Ollama and returns the complete response.
    /// </summary>
    [HttpPost("chat")]
    [Authorize]
    [EnableRateLimiting("ollama")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Chat(
        ChatRequest request)
    {
        var response = new StringBuilder();

        await foreach (var token in _ollama.ChatAsync(
                           request.Prompt,
                           new ChatOptions
                           {
                               Model = request.Model,
                               SystemPrompt = request.SystemPrompt
                           }))
        {
            response.Append(token);
        }

        return Ok(new
        {
            Response = response.ToString()
        });
    }

    /// <summary>
    /// Sends a message to Ollama and streams the response.
    /// </summary>
    [HttpPost("chat/stream")]
    [Authorize]
    [EnableRateLimiting("ollama")]
    [Produces("text/plain")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task Stream(
        ChatRequest request)
    {
        Response.ContentType = "text/plain";

        await foreach (var token in _ollama.ChatAsync(
                           request.Prompt,
                           new ChatOptions
                           {
                               Model = request.Model,
                               SystemPrompt = request.SystemPrompt
                           }))
        {
            await Response.WriteAsync(token);
            await Response.Body.FlushAsync();
        }
    }
}
