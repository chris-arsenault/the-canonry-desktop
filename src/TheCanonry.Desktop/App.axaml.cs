using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TheCanonry.Desktop.Archivist;
using TheCanonry.Desktop.AwsSync;
using TheCanonry.Desktop.Cosmographer;
using TheCanonry.Desktop.Enumerist;
using TheCanonry.Desktop.LoreWeave;
using TheCanonry.Desktop.Illuminator;
using TheCanonry.Desktop.NameForge;
using TheCanonry.Desktop.Shared;
using TheCanonry.Desktop.Shell;
using TheCanonry.Persistence;

namespace TheCanonry.Desktop;

internal sealed partial class App : Application
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
        services.AddSingleton<ProjectService>();

        // Shell
        services.AddSingleton<ShellViewModel>();

        // Sectioned modules (singletons — retain state across navigations)
        services.AddSingleton<EnumeristViewModel>();
        services.AddSingleton<NameForgeViewModel>();
        services.AddSingleton<CosmographerViewModel>();

        // Feature ViewModels (transient so each navigation creates a fresh instance)
        services.AddTransient<LoreWeaveViewModel>();
        services.AddTransient<IlluminatorViewModel>();
        services.AddTransient<EntityBrowserViewModel>();
        services.AddTransient<ChronicleViewModel>();
        services.AddTransient<ImageCurationViewModel>();
        services.AddTransient<CatalogViewModel>();
        services.AddTransient<ArchivistViewModel>();
        services.AddTransient<AwsSyncViewModel>();
        services.AddTransient<ChronicleWizardViewModel>();
        services.AddTransient<EditionComparisonViewModel>();

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
            nav.RegisterView<EnumeristViewModel>("Enumerist", "\u2637");
            nav.RegisterView<NameForgeViewModel>("Name Forge", "\u2692");
            nav.RegisterView<CosmographerViewModel>("Cosmographer", "\u2609");
            nav.RegisterView<LoreWeaveViewModel>("Lore Weave", "\u2692");
            nav.RegisterView<IlluminatorViewModel>("Illuminator", "\u2728");
            nav.RegisterView<EntityBrowserViewModel>("Entity Browser", "\u2637");
            nav.RegisterView<ChronicleViewModel>("Chronicles", "\u2706");
            nav.RegisterView<ImageCurationViewModel>("Image Curation", "\u25A3");
            nav.RegisterView<CatalogViewModel>("Catalog Review", "\u2611");
            nav.RegisterView<ArchivistViewModel>("Archivist", "\u26B1");
            nav.RegisterView<AwsSyncViewModel>("AWS Sync", "\u2601");
            nav.RegisterView<ChronicleWizardViewModel>("Chronicle Wizard", "\u270E");
            nav.RegisterView<EditionComparisonViewModel>("Edition Comparison", "\u2194");

            var vm = _serviceProvider.GetRequiredService<ShellViewModel>();
            vm.DatabaseStatus = $"DB: {dbPath}";
            vm.StatusText = "Ready";

            // Auto-load domain config if default project exists
            var defaultProject = Path.Combine(
                AppContext.BaseDirectory, "domain", "default-project");
            if (!Directory.Exists(defaultProject))
            {
                // Try relative path for dev environment
                defaultProject = Path.GetFullPath(
                    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "domain", "default-project"));
            }
            if (Directory.Exists(defaultProject))
            {
                var projectService = _serviceProvider.GetRequiredService<ProjectService>();
                projectService.Load(defaultProject);
                vm.StatusText = projectService.StatusMessage;
            }

            vm.NavigateToDefault();

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
