using System.Threading;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VeloTracker.Data;
using VeloTracker.Services;
using VeloTracker.ViewModels;
using VeloTracker.Views;

namespace VeloTracker;

public partial class App : Application
{
    private static Mutex? _mutex;
    private IHost? _host;

    private async void OnStartup(object sender, StartupEventArgs e)
    {
        bool createdNew;
        _mutex = new Mutex(true, "VeloTracker.SingleInstance", out createdNew);
        if (!createdNew)
        {
            _mutex.Dispose();
            _mutex = null;
            MessageBox.Show("VeloTracker is already running.", "VeloTracker", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        try
        {
            var paths = new AppPaths();
            paths.EnsureDirectories();
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton(paths);
                    services.AddSingleton<SettingsService>();
                    services.AddSingleton<AppBackgroundService>();
                    services.AddSingleton<AppearanceService>();
                    services.AddSingleton<DatabaseInitializer>();
                    services.AddSingleton<EventRepository>();
                    services.AddSingleton<MainViewModel>();
                    services.AddSingleton<MainWindow>();
                    services.AddTransient<SettingsViewModel>();
                    services.AddTransient<SettingsWindow>();
                    services.AddTransient<StatisticsViewModel>();
                    services.AddTransient<StatisticsWindow>();
                }).Build();

            await _host.StartAsync();
            await _host.Services.GetRequiredService<DatabaseInitializer>().InitializeAsync();
            var settings = await _host.Services.GetRequiredService<SettingsService>().LoadAsync();
            _host.Services.GetRequiredService<AppearanceService>().Apply(settings);
            var window = _host.Services.GetRequiredService<MainWindow>();
            MainWindow = window;
            window.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось запустить VeloTracker:\n{ex.Message}", "Ошибка запуска", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    private async void OnExit(object sender, ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        if (_mutex is not null)
        {
            try { _mutex.ReleaseMutex(); } catch (ApplicationException) { }
            _mutex.Dispose();
        }
    }
}
