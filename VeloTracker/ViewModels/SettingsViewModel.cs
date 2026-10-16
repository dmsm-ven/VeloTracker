using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using VeloTracker.Models;
using VeloTracker.Services;

namespace VeloTracker.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _service;
    private readonly AppearanceService _appearance;
    private AppSettings? _originalSettings;
    public event EventHandler? RequestClose;
    public ObservableCollection<string> Themes { get; } = new() { "System", "Light", "Dark" };

    [ObservableProperty] private string selectedTheme = "System";
    [ObservableProperty] private string? backgroundImagePath;
    [ObservableProperty] private int overlayOpacity = 85;
    [ObservableProperty] private string overlayColor = "#F4F6F5";
    [ObservableProperty] private SolidColorBrush overlayColorPreview = new(Color.FromRgb(244, 246, 245));
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
        SelectedTheme = Themes.Contains(settings.Theme) ? settings.Theme : "System";
        BackgroundImagePath = settings.BackgroundImagePath;
        OverlayOpacity = Math.Clamp(settings.OverlayOpacity, 0, 100);
        OverlayColor = settings.OverlayColor;
        OverlayColorPreview = new SolidColorBrush(ParseColor(settings.OverlayColor));
        TileBackground = settings.TileBackground;
        TextureScale = Math.Clamp(settings.TextureScale, 5, 150);
        _loaded = true;
        ApplyPreview();
    }

    partial void OnSelectedThemeChanged(string value) => ApplyPreview();
    partial void OnBackgroundImagePathChanged(string? value) => ApplyPreview();
    partial void OnOverlayOpacityChanged(int value) => ApplyPreview();
    partial void OnOverlayColorChanged(string value)
    {
        OverlayColorPreview = new SolidColorBrush(ParseColor(value));
        ApplyPreview();
    }
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

    [RelayCommand]
    private void PickOverlayColor()
    {
        var current = ParseColor(OverlayColor);
        var native = new ChooseColorDialog
        {
            StructSize = Marshal.SizeOf<ChooseColorDialog>(),
            Flags = 0x00000001 | 0x00000002,
            RgbResult = (uint)(current.R | (current.G << 8) | (current.B << 16))
        };
        native.CustomColors = Marshal.AllocHGlobal(16 * sizeof(uint));
        try
        {
            Marshal.Copy(new int[16], 0, native.CustomColors, 16);
            if (!ChooseColorW(ref native)) return;
        }
        finally { Marshal.FreeHGlobal(native.CustomColors); }

        var r = (byte)(native.RgbResult & 0xFF);
        var g = (byte)((native.RgbResult >> 8) & 0xFF);
        var b = (byte)((native.RgbResult >> 16) & 0xFF);
        OverlayColor = $"#{r:X2}{g:X2}{b:X2}";
    }

    private static Color ParseColor(string value)
    {
        try { return (Color)ColorConverter.ConvertFromString(value); }
        catch { return Color.FromRgb(244, 246, 245); }
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

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ChooseColorDialog
    {
        public int StructSize;
        public IntPtr Owner;
        public IntPtr Instance;
        public uint RgbResult;
        public IntPtr CustomColors;
        public uint Flags;
        public IntPtr CustomData;
        public IntPtr Hook;
        public IntPtr TemplateName;
    }

    [DllImport("comdlg32.dll", EntryPoint = "ChooseColorW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ChooseColorW(ref ChooseColorDialog dialog);

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
