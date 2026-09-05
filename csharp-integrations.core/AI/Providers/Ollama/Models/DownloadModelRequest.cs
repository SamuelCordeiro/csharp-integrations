using System.ComponentModel.DataAnnotations;

namespace csharp_integrations.core.AI.Providers.Ollama.Models;

/// <summary>
/// Represents a request to download an Ollama model.
/// </summary>
public class DownloadModelRequest
{
    /// <summary>
    /// Gets the model name to download.
    /// </summary>
    [Required]
    [StringLength(128)]
    public required string Model { get; init; }
}
