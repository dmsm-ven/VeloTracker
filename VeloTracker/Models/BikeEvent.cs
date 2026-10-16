namespace VeloTracker.Models;

public sealed class BikeEvent
{
    public long Id { get; set; }
    public DateTime EventDate { get; set; } = DateTime.Now;
    public string Category { get; set; } = "Maintenance";
    public string EventType { get; set; } = "LubricantChanged";
    public string Title { get; set; } = "Смазка заменена";
    public string? ItemName { get; set; }
    public double? DistanceKm { get; set; }
    public decimal? Cost { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string Icon { get; set; } = "🔧";
}

public sealed record EventOption(string Key, string Label, string Icon, string Category);
