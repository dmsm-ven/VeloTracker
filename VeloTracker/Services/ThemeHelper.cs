using Wpf.Ui.Appearance;
namespace VeloTracker.Services;

/// <summary>Applies WPF-UI's global theme so windows and controls change together.</summary>
public static class ThemeHelper
{
    public static readonly string[] Themes = ["Dark", "Light"];
    public static void Apply(string? theme) => ApplicationThemeManager.Apply(
        string.Equals(theme, "Light", StringComparison.OrdinalIgnoreCase) ? ApplicationTheme.Light : ApplicationTheme.Dark);
}
