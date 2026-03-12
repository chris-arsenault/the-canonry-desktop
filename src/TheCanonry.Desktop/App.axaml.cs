namespace TheCanonry.Desktop;

using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TheCanonry.Desktop.Archivist;
using TheCanonry.Desktop.AwsSync;
using TheCanonry.Desktop.DomainEditor;
using TheCanonry.Desktop.Forge;
using TheCanonry.Desktop.Illuminator;
using TheCanonry.Desktop.Shared;
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

        // Database
        services.AddDbContextFactory<CanonryDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));
        services.AddDbContext<CanonryDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // Shared services
        services.AddSingleton<NavigationService>();
        services.AddSingleton<WindowManager>();

        // Shell
        services.AddSingleton<ShellViewModel>();

        // Feature ViewModels (transient so each navigation creates a fresh instance)
        services.AddTransient<ForgeViewModel>();
        services.AddTransient<IlluminatorViewModel>();
        services.AddTransient<EntityBrowserViewModel>();
        services.AddTransient<ChronicleViewModel>();
        services.AddTransient<ImageCurationViewModel>();
        services.AddTransient<CatalogViewModel>();
        services.AddTransient<ArchivistViewModel>();
        services.AddTransient<DomainEditorViewModel>();
        services.AddTransient<AwsSyncViewModel>();

        _serviceProvider = services.BuildServiceProvider();

        // Ensure database is created
        using (var scope = _serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CanonryDbContext>();
            db.Database.EnsureCreated();
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Initialize navigation service
            var nav = _serviceProvider.GetRequiredService<NavigationService>();
            nav.RegisterView<ForgeViewModel>("Forge", "\u2692");
            nav.RegisterView<IlluminatorViewModel>("Illuminator", "\u2728");
            nav.RegisterView<EntityBrowserViewModel>("Entity Browser", "\u2637");
            nav.RegisterView<ChronicleViewModel>("Chronicles", "\u2706");
            nav.RegisterView<ImageCurationViewModel>("Image Curation", "\u25A3");
            nav.RegisterView<CatalogViewModel>("Catalog Review", "\u2611");
            nav.RegisterView<ArchivistViewModel>("Archivist", "\u26B1");
            nav.RegisterView<DomainEditorViewModel>("Domain Editor", "\u2699");
            nav.RegisterView<AwsSyncViewModel>("AWS Sync", "\u2601");

            var vm = _serviceProvider.GetRequiredService<ShellViewModel>();
            vm.DatabaseStatus = $"DB: {dbPath}";
            vm.StatusText = "Ready";

            desktop.MainWindow = new ShellWindow { DataContext = vm };
            desktop.ShutdownRequested += (_, _) =>
            {
                _serviceProvider.GetRequiredService<WindowManager>().CloseAll();
                _serviceProvider?.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
