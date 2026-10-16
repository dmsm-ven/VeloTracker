using Dapper;
using Microsoft.Data.Sqlite;
using VeloTracker.Models;
using VeloTracker.Services;

namespace VeloTracker.Data;

public sealed class EventRepository(AppPaths paths)
{
    private SqliteConnection CreateConnection() => new($"Data Source={paths.DatabasePath};Cache=Shared");

    private sealed class EventRow
    {
        public long Id { get; set; }
        public string EventDate { get; set; } = "";
        public string Category { get; set; } = "";
        public string EventType { get; set; } = "";
        public string Title { get; set; } = "";
        public string? ItemName { get; set; }
        public double? DistanceKm { get; set; }
        public decimal? Cost { get; set; }
        public string? Notes { get; set; }
        public string CreatedAtUtc { get; set; } = "";
    }

    public async Task<IReadOnlyList<BikeEvent>> GetRecentAsync(int limit = 500)
    {
        await using var connection = CreateConnection();
        var rows = await connection.QueryAsync<EventRow>("""
            SELECT Id, EventDate, Category, EventType, Title, ItemName, DistanceKm, Cost, Notes, CreatedAtUtc
            FROM BikeEvents ORDER BY EventDate DESC, Id DESC LIMIT @Limit;
            """, new { Limit = Math.Clamp(limit, 1, 5000) });
        return rows.Select(row => new BikeEvent
        {
            Id = row.Id,
            EventDate = DateTime.TryParse(row.EventDate, null, System.Globalization.DateTimeStyles.RoundtripKind, out var date) ? date : DateTime.MinValue,
            Category = row.Category, EventType = row.EventType, Title = row.Title, ItemName = row.ItemName,
            DistanceKm = row.DistanceKm, Cost = row.Cost, Notes = row.Notes,
            CreatedAtUtc = DateTime.TryParse(row.CreatedAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out var created) ? created : DateTime.UtcNow
        }).ToArray();
    }

    public async Task AddAsync(BikeEvent item)
    {
        const string sql = """
            INSERT INTO BikeEvents (EventDate, Category, EventType, Title, ItemName, DistanceKm, Cost, Notes, CreatedAtUtc)
            VALUES (@EventDate, @Category, @EventType, @Title, @ItemName, @DistanceKm, @Cost, @Notes, @CreatedAtUtc);
            """;
        await using var connection = CreateConnection();
        await connection.OpenAsync();
        await connection.ExecuteAsync(sql, new
        {
            EventDate = item.EventDate.ToString("O"), item.Category, item.EventType, item.Title, item.ItemName,
            item.DistanceKm, item.Cost, item.Notes, CreatedAtUtc = item.CreatedAtUtc.ToString("O")
        });
    }

    public async Task DeleteAsync(long id)
    {
        await using var connection = CreateConnection();
        await connection.ExecuteAsync("DELETE FROM BikeEvents WHERE Id = @Id", new { Id = id });
    }
}
