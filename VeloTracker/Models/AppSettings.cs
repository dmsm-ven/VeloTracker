namespace VeloTracker.Models;

public sealed class AppSettings
{
    public string Theme { get; set; } = "System";
    public string? BackgroundImagePath { get; set; }
    public int OverlayOpacity { get; set; } = 85;
    public string OverlayColor { get; set; } = "#F4F6F5";
    public bool TileBackground { get; set; }
    public int TextureScale { get; set; } = 100;
}
