namespace TheCanonry.Desktop;

using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TheCanonry.Desktop.Shell;
using TheCanonry.Persistence;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TheCanonry");
        Directory.CreateDirectory(dataDir);
        var dbPath = Path.Combine(dataDir, "canonry.db");

        var services = new ServiceCollection();

        services.AddDbContext<CanonryDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        services.AddTransient<ShellViewModel>();

        _serviceProvider = services.BuildServiceProvider();

        // Ensure database is created
        using (var scope = _serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CanonryDbContext>();
            db.Database.EnsureCreated();
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = _serviceProvider.GetRequiredService<ShellViewModel>();
            vm.DatabaseStatus = $"DB: {dbPath}";
            vm.StatusText = "Ready";

            desktop.MainWindow = new ShellWindow { DataContext = vm };
            desktop.ShutdownRequested += (_, _) => _serviceProvider?.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
