using TheCanonry.Engine.Engine;
using TheCanonry.Engine.Runtime;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.World;

namespace TheCanonry.Engine.Systems;

/// <summary>
/// Computes diffusion fields on semantic planes using a 100x100 grid.
///
/// Each tick:
/// 1. Sources SET their values at grid positions (Dirichlet boundary conditions)
/// 2. Sinks SET negative values at grid positions
/// 3. Apply diffusion step (heat equation: each cell exchanges with neighbors)
/// 4. Apply optional decay (values drift toward zero)
/// 5. Sample grid at entity positions to set tags (clamped to -100 to 100)
/// </summary>
public sealed class PlaneDiffusion : ISimulationSystem
{
    private const int GridSize = 100;
    private const double OutputMin = -100.0;
    private const double OutputMax = 100.0;

    private readonly IReadOnlyDictionary<string, object?> _config;
    private float[]? _grid;
    private float[]? _tempGrid;
    private bool _initialized;

    public PlaneDiffusion(
        string id, string name,
        IReadOnlyDictionary<string, object?> config,
        WorldRuntime runtime)
    {
        Id = id;
        Name = name;
        _config = config;
        Runtime = runtime;
    }

    public string Id { get; }
    public string Name { get; }

    /// <summary>The runtime this system operates on.</summary>
    internal WorldRuntime Runtime { get; }

    /// <summary>The raw configuration dictionary for this system.</summary>
    internal IReadOnlyDictionary<string, object?> Config => _config;

    public void Initialize()
    {
        _grid = new float[GridSize * GridSize];
        _tempGrid = new float[GridSize * GridSize];
        _initialized = true;
    }

    public Task<SystemResult> ApplyAsync(double modifier)
    {
        if (!_initialized) Initialize();

        var runtime = Runtime;
        var grid = _grid!;
        var tempGrid = _tempGrid!;

        // Read config
        var throttleChance = GetDouble("throttleChance", 1.0);
        var entityKind = GetString("entityKind", "");
        var sourceTag = GetString("sourceTag", "source");
        var sinkTag = GetString("sinkTag", "sink");
        var defaultSourceStrength = GetDouble("defaultSourceStrength", 50.0);
        var defaultSinkStrength = GetDouble("defaultSinkStrength", 50.0);
        var diffusionRate = GetDouble("diffusionRate", 0.2);
        var sourceRadius = GetInt("sourceRadius", 1);
        var decayRate = GetDouble("decayRate", 0.0);
        var iterationsPerTick = GetInt("iterationsPerTick", 20);
        var valueTag = GetString("valueTag", "");
        var outputTagConfigs = GetOutputTags();

        // Throttle
        if (throttleChance < 1.0 && runtime.NextRandomDouble() >= throttleChance)
        {
            return Task.FromResult(new SystemResult(
                [], [], [], [],
                new Dictionary<string, double>(),
                $"{Name}: dormant",
                new Dictionary<string, object?>(),
                new Dictionary<string, string>()));
        }

        // Get entities
        var allEntities = string.IsNullOrEmpty(entityKind)
            ? runtime.GetEntities()
            : runtime.GetEntitiesByKind(new EntityKind(entityKind));

        var sources = new List<Entity>();
        var sinks = new List<Entity>();
        var entitiesWithCoords = new List<Entity>();

        foreach (var entity in allEntities)
        {
            entitiesWithCoords.Add(entity);
            if (entity.Tags.Contains(sourceTag))
                sources.Add(entity);
            if (!string.IsNullOrEmpty(sinkTag) && entity.Tags.Contains(sinkTag))
                sinks.Add(entity);
        }

        // Determine source mode based on decay
        var sourceMode = decayRate > 0 ? SourceMode.Add : SourceMode.Set;

        // Build fixed boundary cells (only for non-decay mode)
        var fixedCells = new HashSet<int>();
        if (decayRate == 0)
        {
            foreach (var source in sources)
            {
                var gx = CoordToGrid(source.Coordinates.X);
                var gy = CoordToGrid(source.Coordinates.Y);
                for (var dy = -sourceRadius; dy <= sourceRadius; dy++)
                {
                    for (var dx = -sourceRadius; dx <= sourceRadius; dx++)
                    {
                        if (Math.Sqrt(dx * dx + dy * dy) <= sourceRadius)
                        {
                            var nx = gx + dx;
                            var ny = gy + dy;
                            if (nx >= 0 && nx < GridSize && ny >= 0 && ny < GridSize)
                                fixedCells.Add(ny * GridSize + nx);
                        }
                    }
                }
            }
        }

        // Inject source values
        foreach (var source in sources)
        {
            var gx = CoordToGrid(source.Coordinates.X);
            var gy = CoordToGrid(source.Coordinates.Y);
            var strength = (float)GetEntityStrength(source, "strengthTag", defaultSourceStrength);

            for (var dy = -sourceRadius; dy <= sourceRadius; dy++)
            {
                for (var dx = -sourceRadius; dx <= sourceRadius; dx++)
                {
                    var dist = Math.Sqrt(dx * dx + dy * dy);
                    if (dist > sourceRadius) continue;

                    var nx = gx + dx;
                    var ny = gy + dy;
                    if (nx < 0 || nx >= GridSize || ny < 0 || ny >= GridSize) continue;

                    var falloff = sourceRadius > 0 ? (float)(1.0 - dist / (sourceRadius + 1)) : 1.0f;
                    var idx = ny * GridSize + nx;

                    if (sourceMode == SourceMode.Add)
                        grid[idx] += strength * falloff;
                    else
                        grid[idx] = strength * falloff;
                }
            }
        }

        // Apply sink values
        foreach (var sink in sinks)
        {
            var gx = CoordToGrid(sink.Coordinates.X);
            var gy = CoordToGrid(sink.Coordinates.Y);
            var strength = (float)GetEntityStrength(sink, "sinkStrengthTag", defaultSinkStrength);

            for (var dy = -sourceRadius; dy <= sourceRadius; dy++)
            {
                for (var dx = -sourceRadius; dx <= sourceRadius; dx++)
                {
                    var dist = Math.Sqrt(dx * dx + dy * dy);
                    if (dist > sourceRadius) continue;

                    var nx = gx + dx;
                    var ny = gy + dy;
                    if (nx < 0 || nx >= GridSize || ny < 0 || ny >= GridSize) continue;

                    var falloff = sourceRadius > 0 ? (float)(1.0 - dist / (sourceRadius + 1)) : 1.0f;
                    var idx = ny * GridSize + nx;
                    grid[idx] -= strength * falloff;
                }
            }
        }

        // Run diffusion iterations
        for (var iter = 0; iter < iterationsPerTick; iter++)
        {
            Array.Copy(grid, tempGrid, grid.Length);

            for (var y = 0; y < GridSize; y++)
            {
                for (var x = 0; x < GridSize; x++)
                {
                    var idx = y * GridSize + x;
                    if (fixedCells.Contains(idx)) continue;

                    var current = tempGrid[idx];
                    var north = y > 0 ? tempGrid[(y - 1) * GridSize + x] : 0f;
                    var south = y < GridSize - 1 ? tempGrid[(y + 1) * GridSize + x] : 0f;
                    var east = x < GridSize - 1 ? tempGrid[y * GridSize + x + 1] : 0f;
                    var west = x > 0 ? tempGrid[y * GridSize + x - 1] : 0f;

                    grid[idx] = (float)(current + diffusionRate * ((north + south + east + west) / 4.0 - current));
                }
            }
        }

        // Apply decay
        if (decayRate > 0)
        {
            for (var i = 0; i < grid.Length; i++)
            {
                if (!fixedCells.Contains(i))
                    grid[i] *= (float)(1.0 - decayRate);
            }
        }

        // Sample grid at entity positions and update tags
        var ctx = new ActionContext("system", Id, true);
        var modifications = new List<EntityModified>();
        var narrations = new Dictionary<string, string>();
        var significantCount = 0;

        foreach (var entity in entitiesWithCoords)
        {
            var fieldValue = ClampToOutput(SampleGrid(grid, entity.Coordinates.X, entity.Coordinates.Y));
            var changes = new Dictionary<string, object?>();
            var hasChanges = false;

            // Remove old output tags
            foreach (var outputTag in outputTagConfigs)
            {
                if (entity.Tags.Contains(outputTag.Tag))
                {
                    changes["tag_remove:" + outputTag.Tag] = true;
                    hasChanges = true;
                }
            }

            // Apply new output tags based on thresholds
            var gainedNew = false;
            foreach (var outputTag in outputTagConfigs)
            {
                if (fieldValue >= outputTag.MinValue && fieldValue < outputTag.MaxValue)
                {
                    changes["tag:" + outputTag.Tag] = true;
                    hasChanges = true;
                    if (!entity.Tags.Contains(outputTag.Tag))
                        gainedNew = true;
                }
            }

            // Store raw field value
            if (!string.IsNullOrEmpty(valueTag))
            {
                changes["tag:" + valueTag] = fieldValue.ToString("F1");
                hasChanges = true;
            }

            if (!hasChanges) continue;

            modifications.Add(new EntityModified(
                entity.Id, changes, ctx,
                gainedNew ? entity.Id.Value : ""));

            if (gainedNew) significantCount++;

            // Apply directly
            runtime.UpdateEntity(entity.Id, e =>
            {
                foreach (var outputTag in outputTagConfigs)
                    e.Tags.Remove(outputTag.Tag);

                foreach (var outputTag in outputTagConfigs)
                {
                    if (fieldValue >= outputTag.MinValue && fieldValue < outputTag.MaxValue)
                        e.Tags.Set(outputTag.Tag, true);
                }

                if (!string.IsNullOrEmpty(valueTag))
                    e.Tags.Set(valueTag, fieldValue.ToString("F1"));
            });
        }

        var pressureChanges = (sources.Count > 0 || sinks.Count > 0)
            ? GetPressureChanges()
            : new Dictionary<string, double>();

        return Task.FromResult(new SystemResult(
            [],
            [],
            [],
            modifications,
            pressureChanges,
            $"{Name}: {sources.Count} sources, {sinks.Count} sinks, {significantCount} entities gained output tags",
            new Dictionary<string, object?>(),
            narrations));
    }

    // =========================================================================
    // GRID MATH
    // =========================================================================

    private static int CoordToGrid(double coord)
    {
        return Math.Max(0, Math.Min(GridSize - 1, (int)Math.Floor(coord)));
    }

    private static float SampleGrid(float[] grid, double x, double y)
    {
        x = Math.Max(0, Math.Min(GridSize - 1, x));
        y = Math.Max(0, Math.Min(GridSize - 1, y));

        var x0 = (int)Math.Floor(x);
        var y0 = (int)Math.Floor(y);
        var x1 = Math.Min(x0 + 1, GridSize - 1);
        var y1 = Math.Min(y0 + 1, GridSize - 1);

        var xFrac = x - x0;
        var yFrac = y - y0;

        var v00 = grid[y0 * GridSize + x0];
        var v10 = grid[y0 * GridSize + x1];
        var v01 = grid[y1 * GridSize + x0];
        var v11 = grid[y1 * GridSize + x1];

        var v0 = v00 * (1 - xFrac) + v10 * xFrac;
        var v1 = v01 * (1 - xFrac) + v11 * xFrac;

        return (float)(v0 * (1 - yFrac) + v1 * yFrac);
    }

    private static double ClampToOutput(double value)
    {
        return Math.Max(OutputMin, Math.Min(OutputMax, value));
    }

    // =========================================================================
    // CONFIG HELPERS
    // =========================================================================

    private enum SourceMode { Set, Add }

    private sealed record OutputTagConfig(string Tag, double MinValue, double MaxValue);

    private double GetEntityStrength(Entity entity, string strengthTagKey, double defaultStrength)
    {
        var strengthTag = GetString(strengthTagKey, "");
        if (string.IsNullOrEmpty(strengthTag)) return defaultStrength;

        var strVal = entity.Tags.GetString(strengthTag);
        if (strVal is not null && double.TryParse(strVal, out var parsed))
            return parsed;

        return defaultStrength;
    }

    private IReadOnlyList<OutputTagConfig> GetOutputTags()
    {
        if (!_config.TryGetValue("outputTags", out var val)) return [];

        if (val is IReadOnlyList<IReadOnlyDictionary<string, object?>> list)
        {
            var result = new List<OutputTagConfig>();
            foreach (var item in list)
            {
                var tag = item.TryGetValue("tag", out var t) && t is string ts ? ts : "";
                var min = item.TryGetValue("minValue", out var mn) ? ToDouble(mn) : 0.0;
                var max = item.TryGetValue("maxValue", out var mx) ? ToDouble(mx) : 100.0;
                if (!string.IsNullOrEmpty(tag))
                    result.Add(new OutputTagConfig(tag, min, max));
            }
            return result;
        }

        return [];
    }

    private Dictionary<string, double> GetPressureChanges()
    {
        var result = new Dictionary<string, double>();
        if (!_config.TryGetValue("pressureChanges", out var pcObj) || pcObj is not IReadOnlyDictionary<string, object?> pc)
            return result;

        foreach (var (pressureId, deltaObj) in pc)
        {
            if (deltaObj is double delta)
                result[pressureId] = delta;
        }
        return result;
    }

    private static double ToDouble(object? val)
    {
        if (val is double d) return d;
        if (val is int i) return i;
        if (val is float f) return f;
        return 0.0;
    }

    private string GetString(string key, string defaultValue)
    {
        if (_config.TryGetValue(key, out var val) && val is string s)
            return s;
        return defaultValue;
    }

    private double GetDouble(string key, double defaultValue)
    {
        if (_config.TryGetValue(key, out var val))
        {
            if (val is double d) return d;
            if (val is int i) return i;
            if (val is float f) return f;
        }
        return defaultValue;
    }

    private int GetInt(string key, int defaultValue)
    {
        if (_config.TryGetValue(key, out var val))
        {
            if (val is int i) return i;
            if (val is double d) return (int)d;
        }
        return defaultValue;
    }
}
