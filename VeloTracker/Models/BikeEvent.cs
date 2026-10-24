using System.Globalization;
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
    public int? AverageTimeMinutes { get; set; }
    public decimal? Cost { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Presentation-only property; the icon is derived from EventType, not stored in SQLite.
    public SymbolRegular Icon { get; set; } = SymbolRegular.Wrench24;

    private static readonly CultureInfo RuCulture = CultureInfo.GetCultureInfo("ru-RU");

    // Presentation-only, computed (get-only, so not persisted or mapped from SQLite).
    public string DayOfWeekText
    {
        get
        {
            var name = EventDate.ToString("dddd", RuCulture);
            return name.Length == 0 ? name : char.ToUpper(name[0], RuCulture) + name[1..];
        }
    }

    public string DaysAgoText
    {
        get
        {
            var days = (DateTime.Today - EventDate.Date).Days;
            return days switch
            {
                0 => "Сегодня",
                1 => "Вчера",
                > 1 => $"{days} дн. назад",
                -1 => "Завтра",
                _ => $"Через {-days} дн."
            };
        }
    }
}

public sealed record EventOption(string Key, string Label, SymbolRegular Icon, string Category);
public sealed record EventCategoryOption(string Key, string Label, SymbolRegular Icon);
