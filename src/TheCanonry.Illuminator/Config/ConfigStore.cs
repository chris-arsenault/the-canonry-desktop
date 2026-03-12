namespace TheCanonry.Illuminator.Config;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TheCanonry.Persistence;
using TheCanonry.Persistence.Entities;

/// <summary>
/// Manages persistence of all config types to DB using ConfigSlotEntity for per-type storage.
/// Each (projectId, configType) pair holds a serialized JSON blob.
/// </summary>
public class ConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private const string WorldContextType = "world_context";
    private const string EntityGuidanceType = "entity_guidance";
    private const string CultureIdentitiesType = "culture_identities";
    private const string LlmCallConfigType = "llm_call_config";
    private const string ImageConfigType = "image_config";

    private readonly CanonryDbContext _db;

    public ConfigStore(CanonryDbContext db) { _db = db; }

    public Task SaveWorldContextAsync(string projectId, WorldContext context, CancellationToken ct = default)
        => SaveAsync(projectId, WorldContextType, context, ct);

    public async Task<WorldContext?> LoadWorldContextAsync(string projectId, CancellationToken ct = default)
        => await LoadAsync<WorldContext>(projectId, WorldContextType, ct);

    public Task SaveEntityGuidanceAsync(string projectId, EntityGuidance guidance, CancellationToken ct = default)
        => SaveAsync(projectId, EntityGuidanceType, guidance, ct);

    public async Task<EntityGuidance?> LoadEntityGuidanceAsync(string projectId, CancellationToken ct = default)
        => await LoadAsync<EntityGuidance>(projectId, EntityGuidanceType, ct);

    public Task SaveCultureIdentitiesAsync(string projectId, CultureIdentityMap identities, CancellationToken ct = default)
        => SaveAsync(projectId, CultureIdentitiesType, identities, ct);

    public async Task<CultureIdentityMap?> LoadCultureIdentitiesAsync(string projectId, CancellationToken ct = default)
        => await LoadAsync<CultureIdentityMap>(projectId, CultureIdentitiesType, ct);

    public Task SaveLlmCallConfigAsync(string projectId, LlmCallConfigMap config, CancellationToken ct = default)
        => SaveAsync(projectId, LlmCallConfigType, config, ct);

    public async Task<LlmCallConfigMap?> LoadLlmCallConfigAsync(string projectId, CancellationToken ct = default)
        => await LoadAsync<LlmCallConfigMap>(projectId, LlmCallConfigType, ct);

    public Task SaveImageConfigAsync(string projectId, ImageGenerationConfig config, CancellationToken ct = default)
        => SaveAsync(projectId, ImageConfigType, config, ct);

    public async Task<ImageGenerationConfig?> LoadImageConfigAsync(string projectId, CancellationToken ct = default)
        => await LoadAsync<ImageGenerationConfig>(projectId, ImageConfigType, ct);

    private async Task SaveAsync<T>(string projectId, string configType, T value, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        var existing = await _db.ConfigSlots
            .FirstOrDefaultAsync(s => s.ProjectId == projectId && s.ConfigType == configType, ct);

        if (existing is null)
        {
            _db.ConfigSlots.Add(new ConfigSlotEntity
            {
                ProjectId = projectId,
                ConfigType = configType,
                ValueJson = json,
                UpdatedAt = DateTime.UtcNow,
            });
        }
        else
        {
            existing.ValueJson = json;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task<T?> LoadAsync<T>(string projectId, string configType, CancellationToken ct) where T : class
    {
        var slot = await _db.ConfigSlots
            .FirstOrDefaultAsync(s => s.ProjectId == projectId && s.ConfigType == configType, ct);

        if (slot is null) return null;

        return JsonSerializer.Deserialize<T>(slot.ValueJson, JsonOptions);
    }
}
