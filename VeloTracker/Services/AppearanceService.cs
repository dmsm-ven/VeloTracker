using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VeloTracker.Models;
using Wpf.Ui.Appearance;

namespace VeloTracker.Services;

/// <summary>Applies the WPF-UI theme and application background brushes at runtime.</summary>
public sealed class AppearanceService
{
    public void Apply(AppSettings settings)
    {
        var app = Application.Current;
        if (app is null) return;

        var dark = settings.Theme == "Dark" ||
                   (settings.Theme == "System" && IsWindowsDarkMode());
        ApplicationThemeManager.Apply(dark ? ApplicationTheme.Dark : ApplicationTheme.Light);

        var baseColor = ParseColor(dark ? "#151A18" : "#F4F6F5", Colors.White);
        SetBrush("WindowBackgroundBrush", new SolidColorBrush(baseColor));
        SetBrush("PrimaryTextBrush", new SolidColorBrush(ParseColor(dark ? "#F0F5F1" : "#17211D", Colors.Black)));
        SetBrush("SecondaryTextBrush", new SolidColorBrush(ParseColor(dark ? "#A1B0A7" : "#68756E", Colors.Gray)));
        SetBrush("AccentBrush", new SolidColorBrush(ParseColor(dark ? "#65B995" : "#2E7D5B", Colors.SeaGreen)));

        var panelColor = ParseColor(settings.OverlayColor, baseColor);
        panelColor.A = (byte)Math.Round(Math.Clamp(settings.OverlayOpacity, 0, 100) * 255.0 / 100.0);
        var surface = new SolidColorBrush(panelColor);
        surface.Freeze();
        SetBrush("SurfaceBrush", surface);

        var alternateColor = dark ? ParseColor("#202824", Colors.DarkSlateGray) : ParseColor("#F8FAF8", Colors.WhiteSmoke);
        alternateColor.A = (byte)Math.Round(Math.Clamp(settings.OverlayOpacity, 0, 100) * 0.88 * 255.0 / 100.0);
        var alternate = new SolidColorBrush(alternateColor);
        alternate.Freeze();
        SetBrush("AlternateRowBrush", alternate);
        SetBrush("WindowOverlayBrush", Brushes.Transparent);

        var imagePath = settings.BackgroundImagePath;
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
        {
            SetBrush("WindowImageBrush", new SolidColorBrush(baseColor));
            return;
        }

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(Path.GetFullPath(imagePath), UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();

            var imageBrush = new ImageBrush(bitmap) { AlignmentX = AlignmentX.Center, AlignmentY = AlignmentY.Center };
            if (settings.TileBackground)
            {
                var scale = Math.Clamp(settings.TextureScale, 5, 150) / 100.0;
                imageBrush.Stretch = Stretch.None;
                imageBrush.TileMode = TileMode.Tile;
                imageBrush.ViewportUnits = BrushMappingMode.Absolute;
                imageBrush.Viewport = new Rect(0, 0, Math.Max(1, bitmap.PixelWidth * scale), Math.Max(1, bitmap.PixelHeight * scale));
            }
            else
            {
                imageBrush.Stretch = Stretch.UniformToFill;
                imageBrush.TileMode = TileMode.None;
            }
            imageBrush.Freeze();
            SetBrush("WindowImageBrush", imageBrush);
        }
        catch (Exception)
        {
            SetBrush("WindowImageBrush", new SolidColorBrush(baseColor));
        }
    }

    private static void SetBrush(string key, Brush brush)
    {
        if (Application.Current is { } app) app.Resources[key] = brush;
    }

    private static Color ParseColor(string? value, Color fallback)
    {
        try { return (Color)ColorConverter.ConvertFromString(value ?? string.Empty); }
        catch { return fallback; }
    }

    private static bool IsWindowsDarkMode()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        catch { return false; }
    }
}
