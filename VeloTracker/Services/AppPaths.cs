namespace VeloTracker.Services;

public sealed class AppPaths
{
    public string RoamingDirectory { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VeloTracker");
    public string LocalDirectory { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VeloTracker");
    public string DatabasePath => Path.Combine(LocalDirectory, "velotracker.db");
    public string SettingsPath => Path.Combine(RoamingDirectory, "settings.json");
    public string LogDirectory => Path.Combine(LocalDirectory, "Logs");

    public void EnsureDirectories()
    {
        Directory.CreateDirectory(RoamingDirectory);
        Directory.CreateDirectory(LocalDirectory);
        Directory.CreateDirectory(LogDirectory);
    }
}
