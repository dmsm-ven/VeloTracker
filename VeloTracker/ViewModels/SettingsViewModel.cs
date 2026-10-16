using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using VeloTracker.Models;
using VeloTracker.Services;

namespace VeloTracker.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _service;
    public event EventHandler? RequestClose;
    public ObservableCollection<string> Themes { get; } = new() { "System", "Light", "Dark" };

    [ObservableProperty] private string selectedTheme = "System";
    [ObservableProperty] private string? backgroundImagePath;
    [ObservableProperty] private int overlayOpacity = 85;
    [ObservableProperty] private string overlayColor = "#F4F6F5";
    [ObservableProperty] private bool tileBackground;
    [ObservableProperty] private int textureScale = 100;

    private bool _loaded;

    public SettingsViewModel(SettingsService service) => _service = service;

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (_loaded) return;
        _loaded = true;
        await LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        var settings = await _service.LoadAsync();
        SelectedTheme = Themes.Contains(settings.Theme) ? settings.Theme : "System";
        BackgroundImagePath = settings.BackgroundImagePath;
        OverlayOpacity = Math.Clamp(settings.OverlayOpacity, 0, 100);
        OverlayColor = settings.OverlayColor;
        TileBackground = settings.TileBackground;
        TextureScale = Math.Clamp(settings.TextureScale, 5, 150);
    }

    [RelayCommand]
    private void BrowseBackground()
    {
        var dialog = new OpenFileDialog { Title = "Выбрать фоновое изображение", Filter = "Изображения|*.png;*.jpg;*.jpeg;*.bmp;*.webp|Все файлы|*.*" };
        if (dialog.ShowDialog() == true) BackgroundImagePath = dialog.FileName;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!string.IsNullOrWhiteSpace(BackgroundImagePath) && !File.Exists(BackgroundImagePath))
        {
            MessageBox.Show("Выбранный файл фона не найден. Выбери изображение заново.", "VeloTracker", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!System.Text.RegularExpressions.Regex.IsMatch(OverlayColor, "^#(?:[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$"))
        {
            MessageBox.Show("Цвет должен быть указан в формате #RRGGBB или #AARRGGBB.", "VeloTracker", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        await _service.SaveAsync(new AppSettings
        {
            Theme = SelectedTheme,
            BackgroundImagePath = BackgroundImagePath,
            OverlayOpacity = Math.Clamp(OverlayOpacity, 0, 100),
            OverlayColor = OverlayColor,
            TileBackground = TileBackground,
            TextureScale = Math.Clamp(TextureScale, 5, 150)
        });
        ApplyTheme(SelectedTheme);
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    private static void ApplyTheme(string theme)
    {
        var app = Application.Current;
        if (app is null) return;
        var dark = theme == "Dark" || (theme == "System" && app.ActualThemeIsDark());
        app.Resources["WindowBackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#151A18" : "#F4F6F5"));
        app.Resources["SurfaceBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#202824" : "#FFFFFF"));
        app.Resources["PrimaryTextBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#F0F5F1" : "#17211D"));
        app.Resources["SecondaryTextBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#A1B0A7" : "#68756E"));
    }
}

internal static class ThemeHelpers
{
    public static bool ActualThemeIsDark(this Application app)
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        catch { return false; }
    }
}
