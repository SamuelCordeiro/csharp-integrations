namespace csharp_integrations.core.AI.Providers.Ollama.Options;

/// <summary>
/// Contains configuration used to communicate with an Ollama server.
/// </summary>
public class OllamaOptions
{
    /// <summary>
    /// Gets the configuration section name.
    /// </summary>
    public const string SectionName = "Ollama";
    
    /// <summary>
    /// Gets the Ollama server URL.
    /// </summary>
    public required string Url { get; init; }
    
    /// <summary>
    /// Gets the model used when a request does not specify one.
    /// </summary>
    public required string DefaultModel { get; init; }
    
    /// <summary>
    /// Gets the optional system prompt used when a request does not specify one.
    /// </summary>
    public string? SystemPrompt { get; init; }
}
