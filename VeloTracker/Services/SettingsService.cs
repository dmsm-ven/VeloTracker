using System.IO;
using System.Text.Json;
using VeloTracker.Models;

namespace VeloTracker.Services;

public sealed class SettingsService(AppPaths paths)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async Task<AppSettings> LoadAsync()
    {
        if (!File.Exists(paths.SettingsPath)) return new AppSettings();
        try
        {
            await using var stream = File.OpenRead(paths.SettingsPath);
            return await JsonSerializer.DeserializeAsync<AppSettings>(stream) ?? new AppSettings();
        }
        catch (JsonException) { return new AppSettings(); }
        catch (IOException) { return new AppSettings(); }
    }

    public async Task SaveAsync(AppSettings settings)
    {
        var tempPath = paths.SettingsPath + ".tmp";
        await using (var stream = File.Create(tempPath))
            await JsonSerializer.SerializeAsync(stream, settings, JsonOptions);
        File.Move(tempPath, paths.SettingsPath, true);
    }
}
