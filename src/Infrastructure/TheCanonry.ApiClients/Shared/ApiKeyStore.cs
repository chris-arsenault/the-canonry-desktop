using System.Collections.Concurrent;

namespace TheCanonry.ApiClients.Shared;

/// <summary>
/// Thread-safe in-memory store for API keys, keyed by provider name
/// (e.g. "anthropic", "openai", "bfl", "fal", "wavespeed").
/// </summary>
public sealed class ApiKeyStore
{
    private readonly ConcurrentDictionary<string, string> _keys = new(StringComparer.OrdinalIgnoreCase);

    public string? GetKey(string provider) =>
        _keys.TryGetValue(provider, out var key) ? key : null;

    public void SetKey(string provider, string key) =>
        _keys[provider] = key;

    public bool HasKey(string provider) =>
        _keys.ContainsKey(provider);
}
