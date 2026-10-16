using Wpf.Ui.Controls;

namespace VeloTracker.Models;

public sealed class BikeEvent
{
    public long Id { get; set; }
    public DateTime EventDate { get; set; } = DateTime.Now;
    public string Category { get; set; } = "Maintenance";
    public string EventType { get; set; } = "LubricantChanged";
    public string Title { get; set; } = "Замена смазки цепи";
    public string? ItemName { get; set; }
    public double? DistanceKm { get; set; }
    public decimal? Cost { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Presentation-only property; the icon is derived from EventType, not stored in SQLite.
    public SymbolRegular Icon { get; set; } = SymbolRegular.Wrench24;
}

public sealed record EventOption(string Key, string Label, SymbolRegular Icon, string Category);
public sealed record EventCategoryOption(string Key, string Label, SymbolRegular Icon);
