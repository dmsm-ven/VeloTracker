using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VeloTracker.Models;
namespace VeloTracker.Services;

public sealed record ColorPreset(string Name, string Hex);

/// <summary>Observable background layers: overlay, image, opacity and texture settings.</summary>
public sealed partial class AppBackgroundService : ObservableObject
{
    public static readonly ColorPreset[] Presets = [
        new("Чёрный", "#000000"), new("Тёмно-серый", "#1E1E1E"), new("Тёмно-синий", "#0B1633"),
        new("Тёмно-фиолетовый", "#1A1033"), new("Белый", "#FFFFFF")];
    [ObservableProperty] private ImageSource? image;
    [ObservableProperty] private Brush? imageBrush;
    [ObservableProperty] private bool tile;
    [ObservableProperty] private double imageOpacity;
    [ObservableProperty] private Brush overlayBrush = Brushes.Black;
    public bool HasImage => Image is not null;
    partial void OnImageChanged(ImageSource? value) => OnPropertyChanged(nameof(HasImage));
    public void Apply(AppSettings settings)
    {
        ImageOpacity = Math.Clamp(settings.OverlayOpacity, 0, 100) / 100.0;
        OverlayBrush = ParseBrush(settings.OverlayColor); Tile = settings.TileBackground;
        Image = LoadImage(settings.BackgroundImagePath); ImageBrush = CreateBrush(Image, Tile, settings.TextureScale);
    }
    private static Brush? CreateBrush(ImageSource? source, bool tile, int scalePercent)
    {
        if (source is not BitmapSource bitmap) return null;
        var brush = new ImageBrush(bitmap);
        if (tile) { var scale=Math.Clamp(scalePercent,5,150)/100.0;
            var width=bitmap.PixelWidth*96.0/bitmap.DpiX*scale; var height=bitmap.PixelHeight*96.0/bitmap.DpiY*scale;
            brush.TileMode=TileMode.Tile; brush.Stretch=Stretch.Fill; brush.ViewportUnits=BrushMappingMode.Absolute;
            brush.Viewport=new Rect(0,0,Math.Max(1,width),Math.Max(1,height)); }
        else brush.Stretch=Stretch.UniformToFill; brush.Freeze(); return brush;
    }
    private static ImageSource? LoadImage(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)||!File.Exists(path)) return null;
        try { var bitmap=new BitmapImage(); bitmap.BeginInit(); bitmap.CacheOption=BitmapCacheOption.OnLoad;
            bitmap.CreateOptions=BitmapCreateOptions.IgnoreImageCache; bitmap.UriSource=new Uri(Path.GetFullPath(path),UriKind.Absolute);
            bitmap.EndInit(); bitmap.Freeze(); return bitmap; } catch(Exception) { return null; }
    }
    private static Brush ParseBrush(string? hex)
    {
        try { var brush=new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex ?? "#000000")); brush.Freeze(); return brush; }
        catch(FormatException) { return Brushes.Black; }
    }
}
