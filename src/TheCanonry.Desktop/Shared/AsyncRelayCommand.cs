using System.Windows.Input;

namespace TheCanonry.Desktop.Shared;

internal sealed class AsyncRelayCommand : ViewModelBase, ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    private bool _isExecuting;

    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool IsExecuting
    {
        get => _isExecuting;
        private set
        {
            if (SetProperty(ref _isExecuting, value))
                RaiseCanExecuteChanged();
        }
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !IsExecuting && (_canExecute?.Invoke() ?? true);

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;
        IsExecuting = true;
        try
        {
            await _execute();
        }
        finally
        {
            IsExecuting = false;
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

internal sealed class AsyncRelayCommand<T> : ViewModelBase, ICommand
{
    private readonly Func<T?, Task> _execute;
    private readonly Func<T?, bool>? _canExecute;
    private bool _isExecuting;

    public AsyncRelayCommand(Func<T?, Task> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool IsExecuting
    {
        get => _isExecuting;
        private set
        {
            if (SetProperty(ref _isExecuting, value))
                RaiseCanExecuteChanged();
        }
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        if (IsExecuting) return false;
        if (parameter is T typed)
            return _canExecute?.Invoke(typed) ?? true;
        if (parameter is null)
            return _canExecute?.Invoke(default) ?? true;
        return false;
    }

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;
        IsExecuting = true;
        try
        {
            if (parameter is T typed)
                await _execute(typed);
            else
                await _execute(default);
        }
        finally
        {
            IsExecuting = false;
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
