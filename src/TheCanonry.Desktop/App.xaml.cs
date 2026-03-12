namespace TheCanonry.Desktop;

using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TheCanonry.Desktop.Shell;
using TheCanonry.Persistence;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TheCanonry");
        Directory.CreateDirectory(dataDir);
        var dbPath = Path.Combine(dataDir, "canonry.db");

        var services = new ServiceCollection();

        services.AddDbContext<CanonryDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        services.AddTransient<ShellViewModel>();
        services.AddTransient<ShellWindow>();

        _serviceProvider = services.BuildServiceProvider();

        // Ensure database is created
        using (var scope = _serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CanonryDbContext>();
            db.Database.EnsureCreated();
        }

        var shell = _serviceProvider.GetRequiredService<ShellWindow>();
        var vm = (ShellViewModel)shell.DataContext;
        vm.DatabaseStatus = $"DB: {dbPath}";
        vm.StatusText = "Ready";
        shell.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
