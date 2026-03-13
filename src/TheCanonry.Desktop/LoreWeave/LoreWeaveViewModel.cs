using System.Windows.Input;
using TheCanonry.Desktop.Shared;
using TheCanonry.Engine.Config;
using TheCanonry.Engine.Engine;
using TheCanonry.Engine.Pressures;
using TheCanonry.Engine.Statistics;
using TheCanonry.Engine.Templates;
using TheCanonry.Schema.Config;
using TheCanonry.Schema.Domain;
using TheCanonry.Schema.Ids;
using TheCanonry.Schema.World;

namespace TheCanonry.Desktop.LoreWeave;

// CA1001: _cts is created and disposed within RunSimulationAsync; the ViewModel doesn't own a long-lived disposable.
#pragma warning disable CA1001
internal sealed class LoreWeaveViewModel : ViewModelBase
#pragma warning restore CA1001
{
    private bool _isRunning;
    private int _currentTick;
    private string _currentEra = "";
    private int _entityCount;
    private int _relationshipCount;
    private double _progress;
    private string _statusMessage = "Idle";
    private string _configPath = "";
    private string _logOutput = "";
    private int _maxTicks = 500;
    private double _scaleFactor = 1.0;
    private CancellationTokenSource? _cts;

    // Loaded domain config components
    private DomainSchema? _loadedSchema;
    private IReadOnlyList<Era>? _loadedEras;
    private IReadOnlyList<DeclarativeTemplate>? _loadedTemplates;
    private IReadOnlyList<DeclarativeSystem>? _loadedSystems;
    private IReadOnlyList<DeclarativePressure>? _loadedPressures;
    private bool _configLoaded;

    public LoreWeaveViewModel()
    {
        RunSimulationCommand = new AsyncRelayCommand(RunSimulationAsync, () => !IsRunning);
        StopSimulationCommand = new RelayCommand(StopSimulation, () => IsRunning);
        LoadConfigCommand = new AsyncRelayCommand(LoadConfigAsync);
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
            {
                ((AsyncRelayCommand)RunSimulationCommand).RaiseCanExecuteChanged();
                ((RelayCommand)StopSimulationCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public int CurrentTick
    {
        get => _currentTick;
        private set => SetProperty(ref _currentTick, value);
    }

    public string CurrentEra
    {
        get => _currentEra;
        private set => SetProperty(ref _currentEra, value);
    }

    public int EntityCount
    {
        get => _entityCount;
        private set => SetProperty(ref _entityCount, value);
    }

    public int RelationshipCount
    {
        get => _relationshipCount;
        private set => SetProperty(ref _relationshipCount, value);
    }

    public double Progress
    {
        get => _progress;
        private set => SetProperty(ref _progress, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string ConfigPath
    {
        get => _configPath;
        set => SetProperty(ref _configPath, value);
    }

    public string LogOutput
    {
        get => _logOutput;
        private set => SetProperty(ref _logOutput, value);
    }

    public int MaxTicks
    {
        get => _maxTicks;
        set => SetProperty(ref _maxTicks, value);
    }

    public double ScaleFactor
    {
        get => _scaleFactor;
        set => SetProperty(ref _scaleFactor, value);
    }

    public ICommand RunSimulationCommand { get; }
    public ICommand StopSimulationCommand { get; }
    public ICommand LoadConfigCommand { get; }

    private async Task RunSimulationAsync()
    {
        if (!_configLoaded)
        {
            StatusMessage = "No config loaded — run Load Config first.";
            AppendLog("ERROR: No domain config loaded. Set ConfigPath and click Load Config.");
            return;
        }

        IsRunning = true;
        StatusMessage = "Running simulation...";
        LogOutput = "";
        _cts = new CancellationTokenSource();

        try
        {
            var emitter = new LoggingEmitter(AppendLog);
            var config = new EngineConfig(
                Schema: _loadedSchema!,
                Eras: _loadedEras!,
                Templates: _loadedTemplates!,
                Systems: _loadedSystems!,
                Pressures: _loadedPressures!,
                TicksPerEpoch: 20,
                MaxEpochs: MaxTicks / 20 + 1,
                MaxTicks: MaxTicks,
                MaxRelationshipsPerType: 500,
                RelationshipBudget: new RelationshipBudget(50, 200),
                ScaleFactor: ScaleFactor,
                DefaultMinDistance: 0.05,
                PressureDeltaSmoothing: 0.1,
                DistributionTargets: new DistributionTargets(),
                NameService: new StubNameService(),
                SeedRelationships: [],
                Emitter: emitter,
                NarrativeConfig: new NarrativeConfig(Enabled: true, MinSignificance: 0));

            var engine = new WorldEngine(config);

            engine.OnProgress += payload =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    CurrentTick = payload.Tick;
                    Progress = payload.MaxTicks > 0
                        ? (double)payload.Tick / payload.MaxTicks * 100
                        : 0;
                    EntityCount = payload.EntityCount;
                    RelationshipCount = payload.RelationshipCount;
                    CurrentEra = $"Epoch {payload.Epoch}/{payload.TotalEpochs}";
                });
            };

            var startTime = DateTime.Now;
            var result = await engine.RunAsync(_cts.Token);
            var elapsed = DateTime.Now - startTime;

            var summary =
                $"Done — {result.TotalEntities} entities, {result.TotalRelationships} relationships, " +
                $"{result.TotalTicks} ticks in {elapsed.TotalSeconds:F1}s";
            StatusMessage = _cts.IsCancellationRequested ? "Simulation stopped" : summary;
            AppendLog(summary);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Simulation cancelled";
            AppendLog("Simulation cancelled by user.");
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            StatusMessage = $"Error: {ex.Message}";
            AppendLog($"ERROR: {ex.Message}");
        }
        finally
        {
            IsRunning = false;
            _cts.Dispose();
            _cts = null;
        }
    }

    private void StopSimulation()
    {
        _cts?.Cancel();
        StatusMessage = "Stopping...";
    }

    private Task LoadConfigAsync()
    {
        StatusMessage = "Loading configuration...";

        if (string.IsNullOrWhiteSpace(ConfigPath))
        {
            StatusMessage = "ConfigPath is empty — enter a domain directory path.";
            AppendLog("ERROR: ConfigPath is empty.");
            return Task.CompletedTask;
        }

        if (!Directory.Exists(ConfigPath))
        {
            StatusMessage = $"Directory not found: {ConfigPath}";
            AppendLog($"ERROR: Directory not found: {ConfigPath}");
            return Task.CompletedTask;
        }

        var required = new[] { "eras.json", "generators.json", "systems.json", "pressures.json" };
        var missing = required.Where(f => !File.Exists(Path.Combine(ConfigPath, f))).ToList();
        if (missing.Count > 0)
        {
            var msg = $"Missing required files: {string.Join(", ", missing)}";
            StatusMessage = msg;
            AppendLog($"ERROR: {msg}");
            return Task.CompletedTask;
        }

        try
        {
            _loadedSchema = DomainSchemaLoader.LoadFromDirectory(ConfigPath);
            _loadedEras = EngineConfigLoader.LoadEras(ConfigPath);
            _loadedTemplates = EngineConfigLoader.LoadTemplates(ConfigPath);
            _loadedSystems = EngineConfigLoader.LoadSystems(ConfigPath);
            _loadedPressures = EngineConfigLoader.LoadPressures(ConfigPath);
            _configLoaded = true;

            var msg =
                $"Config loaded: schema={_loadedSchema.Name}, " +
                $"eras={_loadedEras.Count}, templates={_loadedTemplates.Count}, " +
                $"systems={_loadedSystems.Count}, pressures={_loadedPressures.Count}";
            StatusMessage = msg;
            AppendLog(msg);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            _configLoaded = false;
            StatusMessage = $"Load failed: {ex.Message}";
            AppendLog($"ERROR loading config: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    private void AppendLog(string message)
    {
        LogOutput += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
    }

    private sealed class StubNameService : INameGenerationService
    {
        private int _counter;

        public Task<string> GenerateAsync(
            EntityKind kind,
            string subtype,
            Prominence prominence,
            EntityTags tags,
            CultureId culture,
            string? context = null)
        {
            _counter++;
            var name = $"{kind.Value}_{subtype}_{_counter}";
            return Task.FromResult(name);
        }
    }

    private sealed class LoggingEmitter(Action<string> appendLog) : ISimulationEmitter
    {
        public void Progress(ProgressPayload payload) { }

        public void Log(LogLevel level, string message, IReadOnlyDictionary<string, object?>? context = null)
        {
            appendLog($"[{level}] {message}");
        }

        public void EpochStart(EpochStartPayload payload)
        {
            appendLog($"[Epoch {payload.Epoch}] Era: {payload.EraName}");
        }

        public void SystemAction(SystemActionPayload payload) { }

        public void EraTransition(EraId fromEra, string fromName, EraId toEra, string toName, int tick)
        {
            appendLog($"Era transition at tick {tick}: {fromName} → {toName}");
        }

        public void Error(ErrorPayload payload)
        {
            appendLog($"[ERROR] {payload.Message}");
        }

        public void Complete()
        {
            appendLog("Simulation complete.");
        }
    }
}
