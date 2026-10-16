using System.Windows;
using System.Windows.Media;
using VeloTracker.Models;
namespace VeloTracker.Services;

public sealed class AppearanceService(AppBackgroundService background)
{
    public AppBackgroundService Background { get; } = background;
    public void Apply(AppSettings settings)
    {
        if (Application.Current is null) return;
        ThemeHelper.Apply(settings.Theme);
        var dark=!string.Equals(settings.Theme,"Light",StringComparison.OrdinalIgnoreCase);
        var baseColor=ParseColor(dark?"#151A18":"#F4F6F5",Colors.White);
        SetBrush("WindowBackgroundBrush",new SolidColorBrush(baseColor));
        SetBrush("PrimaryTextBrush",new SolidColorBrush(ParseColor(dark?"#F0F5F1":"#17211D",Colors.Black)));
        SetBrush("SecondaryTextBrush",new SolidColorBrush(ParseColor(dark?"#A1B0A7":"#68756E",Colors.Gray)));
        SetBrush("AccentBrush",new SolidColorBrush(ParseColor(dark?"#65B995":"#2E7D5B",Colors.SeaGreen)));
        var surfaceColor=ParseColor(dark?"#1A211D":"#FFFFFF",Colors.White); surfaceColor.A=224;
        var surface=new SolidColorBrush(surfaceColor); surface.Freeze(); SetBrush("SurfaceBrush",surface);
        var alternateColor=ParseColor(dark?"#202824":"#F8FAF8",Colors.WhiteSmoke); alternateColor.A=218;
        var alternate=new SolidColorBrush(alternateColor); alternate.Freeze(); SetBrush("AlternateRowBrush",alternate);
        Background.Apply(settings);
    }
    private static void SetBrush(string key,Brush brush) { if(Application.Current is { } app) app.Resources[key]=brush; }
    private static Color ParseColor(string? value,Color fallback) { try{return (Color)ColorConverter.ConvertFromString(value ?? string.Empty);}catch{return fallback;} }
}
