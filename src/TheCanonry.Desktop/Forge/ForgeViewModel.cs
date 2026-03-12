namespace TheCanonry.Desktop.Forge;

using System.Windows.Input;
using TheCanonry.Desktop.Shared;

public class ForgeViewModel : ViewModelBase
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

    public ForgeViewModel()
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
        IsRunning = true;
        StatusMessage = "Running simulation...";
        LogOutput = "";
        _cts = new CancellationTokenSource();

        try
        {
            await Task.Run(() =>
            {
                // Simulation integration point.
                // WorldRuntime would be created from EngineConfig and ticked here.
                for (var tick = 0; tick < MaxTicks && !_cts.Token.IsCancellationRequested; tick++)
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        CurrentTick = tick;
                        Progress = (double)tick / MaxTicks * 100;
                    });
                }
            }, _cts.Token);

            StatusMessage = _cts.IsCancellationRequested ? "Simulation stopped" : "Simulation complete";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Simulation cancelled";
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
        AppendLog("Configuration loading not yet connected to file picker.");
        return Task.CompletedTask;
    }

    private void AppendLog(string message)
    {
        LogOutput += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
    }
}
