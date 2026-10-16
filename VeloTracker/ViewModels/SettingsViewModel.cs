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
        OverlayColorPreview = new SolidColorBrush(ParseColor(settings.OverlayColor));
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
    private void PickOverlayColor()
    {
        var current = ParseColor(OverlayColor);
        var native = new ChooseColorDialog
        {
            StructSize = Marshal.SizeOf<ChooseColorDialog>(),
            Flags = 0x00000001 | 0x00000002, // CC_RGBINIT | CC_FULLOPEN
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
        OverlayColorPreview = new SolidColorBrush(Color.FromRgb(r, g, b));
    }

    private static Color ParseColor(string value)
    {
        try { return (Color)ColorConverter.ConvertFromString(value); }
        catch { return Color.FromRgb(244, 246, 245); }
    }

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
        _appearance.Apply(new AppSettings
        {
            Theme = SelectedTheme,
            BackgroundImagePath = BackgroundImagePath,
            OverlayOpacity = Math.Clamp(OverlayOpacity, 0, 100),
            OverlayColor = OverlayColor,
            TileBackground = TileBackground,
            TextureScale = Math.Clamp(TextureScale, 5, 150)
        });
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

}
