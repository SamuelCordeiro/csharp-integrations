using OllamaSharp;
using System.Runtime.CompilerServices;
using csharp_integrations.core.AI.Providers.Ollama.Options;
using Microsoft.Extensions.Options;
using OllamaSharp.Models;

namespace csharp_integrations.core.AI.Providers.Ollama.Services;

/// <summary>
/// Encapsulates communication with the configured Ollama server.
/// </summary>
public class OllamaClient
{
    private readonly OllamaApiClient _client;
    private readonly OllamaOptions _options;
    
    /// <summary>
    /// Initializes a client using the configured Ollama options.
    /// </summary>
    /// <param name="options">Bound Ollama configuration.</param>
    public OllamaClient(IOptions<OllamaOptions> options)
    {
        _options = options.Value;

        _client = new OllamaApiClient(new Uri(_options.Url))
        {
            SelectedModel = _options.DefaultModel
        };
    }
    
    /// <summary>
    /// Lists model names available on the local Ollama server.
    /// </summary>
    /// <returns>A read-only list of model names.</returns>
    public async Task<IReadOnlyList<string>> ListModelsAsync()
    {
        var models = await _client.ListLocalModelsAsync();

        return models.Select(x => x.Name).ToList();
    }

    /// <summary>
    /// Downloads a model and waits until the operation completes.
    /// </summary>
    /// <param name="model">Model name to download.</param>
    public async Task PullModelAsync(string model)
    {
        await foreach (var _ in _client.PullModelAsync(model))
        {
        }
    }

    /// <summary>
    /// Downloads a model and yields progress updates.
    /// </summary>
    /// <param name="model">Model name to download.</param>
    /// <returns>An asynchronous sequence of pull progress updates.</returns>
    public IAsyncEnumerable<PullModelResponse?> PullModelWithProgressAsync(string model)
    {
        return _client.PullModelAsync(model);
    }

    /// <summary>
    /// Sends a prompt to Ollama and yields generated response fragments.
    /// </summary>
    /// <param name="prompt">Prompt sent to the model.</param>
    /// <param name="options">Optional chat overrides.</param>
    /// <param name="cancellationToken">Cancellation token for the upstream request.</param>
    /// <returns>An asynchronous sequence of generated response fragments.</returns>
    public async IAsyncEnumerable<string> ChatAsync(
        string prompt,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var previousModel = _client.SelectedModel;

        if (!string.IsNullOrWhiteSpace(options?.Model))
            _client.SelectedModel = options.Model;

        var systemPrompt = options?.SystemPrompt ?? _options.SystemPrompt;
        
        var chat = string.IsNullOrWhiteSpace(systemPrompt)
            ? new Chat(_client)
            : new Chat(_client, systemPrompt);

        await foreach (var response in chat.SendAsync(prompt, cancellationToken))
            yield return response;

        _client.SelectedModel = previousModel;
    }

    /// <summary>
    /// Checks whether the configured Ollama server responds to requests.
    /// </summary>
    /// <returns><see langword="true"/> when Ollama is available; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            await _client.ListLocalModelsAsync();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
