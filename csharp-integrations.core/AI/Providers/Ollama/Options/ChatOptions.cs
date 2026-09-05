namespace csharp_integrations.core.AI.Providers.Ollama.Options;

/// <summary>
/// Contains optional overrides for an Ollama chat request.
/// </summary>
public class ChatOptions
{
    /// <summary>
    /// Modelo utilizado na conversa.
    /// Caso não informado, utiliza o DefaultModel.
    /// </summary>
    public string? Model { get; init; }

    /// <summary>
    /// Prompt de sistema enviado antes da conversa.
    /// </summary>
    public string? SystemPrompt { get; init; }
}
