using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using VeloTracker.Models;
using VeloTracker.Services;

namespace VeloTracker.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _service;
    private readonly AppearanceService _appearance;
    private AppSettings? _originalSettings;
    public event EventHandler? RequestClose;
    public string[] Themes => ThemeHelper.Themes;
    public ColorPreset[] OverlayColors => AppBackgroundService.Presets;

    [ObservableProperty] private string selectedTheme = "Dark";
    [ObservableProperty] private string? backgroundImagePath;
    [ObservableProperty] private int overlayOpacity = 40;
    [ObservableProperty] private string overlayColor = "#000000";
        [ObservableProperty] private bool tileBackground;
    [ObservableProperty] private int textureScale = 100;

    private bool _loaded;

    public SettingsViewModel(SettingsService service, AppearanceService appearance)
    {
        _service = service;
        _appearance = appearance;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (_loaded) return;
        var settings = await _service.LoadAsync();
        _originalSettings = Copy(settings);
        SelectedTheme = Themes.Contains(settings.Theme, StringComparer.OrdinalIgnoreCase) ? settings.Theme : "Dark";
        BackgroundImagePath = settings.BackgroundImagePath;
        OverlayOpacity = Math.Clamp(settings.OverlayOpacity, 0, 100);
        OverlayColor = OverlayColors.Any(x => string.Equals(x.Hex, settings.OverlayColor, StringComparison.OrdinalIgnoreCase))
            ? settings.OverlayColor : "#000000";
        TileBackground = settings.TileBackground;
        TextureScale = Math.Clamp(settings.TextureScale, 5, 150);
        _loaded = true;
        ApplyPreview();
    }

    partial void OnSelectedThemeChanged(string value) => ApplyPreview();
    partial void OnBackgroundImagePathChanged(string? value) => ApplyPreview();
    partial void OnOverlayOpacityChanged(int value) => ApplyPreview();
    partial void OnOverlayColorChanged(string value) => ApplyPreview();
    partial void OnTileBackgroundChanged(bool value) => ApplyPreview();
    partial void OnTextureScaleChanged(int value) => ApplyPreview();

    private AppSettings BuildSettings() => new()
    {
        Theme = SelectedTheme,
        BackgroundImagePath = BackgroundImagePath,
        OverlayOpacity = Math.Clamp(OverlayOpacity, 0, 100),
        OverlayColor = OverlayColor,
        TileBackground = TileBackground,
        TextureScale = Math.Clamp(TextureScale, 5, 150)
    };

    private void ApplyPreview()
    {
        if (_loaded) _appearance.Apply(BuildSettings());
    }

    [RelayCommand]
    private void BrowseBackground()
    {
        var dialog = new OpenFileDialog { Title = "Выбрать фоновое изображение", Filter = "Изображения|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff|Все файлы|*.*" };
        if (dialog.ShowDialog() == true) BackgroundImagePath = dialog.FileName;
    }

    [RelayCommand]
    private void ClearBackground()
    {
        BackgroundImagePath = null;
        TileBackground = false;
    }

    private static AppSettings Copy(AppSettings settings) => new()
    {
        Theme = settings.Theme,
        BackgroundImagePath = settings.BackgroundImagePath,
        OverlayOpacity = settings.OverlayOpacity,
        OverlayColor = settings.OverlayColor,
        TileBackground = settings.TileBackground,
        TextureScale = settings.TextureScale
    };

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
            MessageBox.Show("Выбери корректный цвет.", "VeloTracker", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var settings = BuildSettings();
        await _service.SaveAsync(settings);
        _originalSettings = Copy(settings);
        _appearance.Apply(settings);
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel()
    {
        if (_originalSettings is not null) _appearance.Apply(_originalSettings);
        RequestClose?.Invoke(this, EventArgs.Empty);
    }
}
