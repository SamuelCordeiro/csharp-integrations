using System.ComponentModel.DataAnnotations;

namespace csharp_integrations.core.AI.Providers.Ollama.Models;

/// <summary>
/// Represents a message sent to the Ollama chat endpoint.
/// </summary>
public class ChatRequest
{
    /// <summary>
    /// Gets the user prompt sent to the model.
    /// </summary>
    [Required]
    [StringLength(4_000)]
    public required string Prompt { get; init; }

    /// <summary>
    /// Gets the optional model name that overrides the configured default model.
    /// </summary>
    [StringLength(128)]
    public string? Model { get; init; }
    
    /// <summary>
    /// Gets the optional system prompt that instructs the model behavior.
    /// </summary>
    [StringLength(4_000)]
    public string? SystemPrompt { get; init; }
}
