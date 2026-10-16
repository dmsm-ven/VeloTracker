namespace VeloTracker.Models;

public sealed class AppSettings
{
    public string Theme { get; set; } = "Dark";
    public string? BackgroundImagePath { get; set; }
    public int OverlayOpacity { get; set; } = 40;
    public string OverlayColor { get; set; } = "#000000";
    public bool TileBackground { get; set; }
    public int TextureScale { get; set; } = 100;
}
