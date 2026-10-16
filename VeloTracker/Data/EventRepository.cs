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
        public int? AverageTimeMinutes { get; set; }
        public decimal? Cost { get; set; }
        public string? Notes { get; set; }
        public string CreatedAtUtc { get; set; } = "";
    }

    public async Task<IReadOnlyList<BikeEvent>> GetRecentAsync(int limit = 500)
    {
        await using var connection = CreateConnection();
        var rows = await connection.QueryAsync<EventRow>("""
            SELECT Id, EventDate, Category, EventType, Title, ItemName, DistanceKm, AverageTimeMinutes, Cost, Notes, CreatedAtUtc
            FROM BikeEvents ORDER BY EventDate DESC, Id DESC LIMIT @Limit;
            """, new { Limit = Math.Clamp(limit, 1, 5000) });
        return rows.Select(row => new BikeEvent
        {
            Id = row.Id,
            EventDate = DateTime.TryParse(row.EventDate, null, System.Globalization.DateTimeStyles.RoundtripKind, out var date) ? date : DateTime.MinValue,
            Category = row.Category, EventType = row.EventType, Title = row.Title, ItemName = row.ItemName,
            DistanceKm = row.DistanceKm, AverageTimeMinutes = row.AverageTimeMinutes, Cost = row.Cost, Notes = row.Notes,
            CreatedAtUtc = DateTime.TryParse(row.CreatedAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out var created) ? created : DateTime.UtcNow
        }).ToArray();
    }

    public async Task AddAsync(BikeEvent item)
    {
        const string sql = """
            INSERT INTO BikeEvents (EventDate, Category, EventType, Title, ItemName, DistanceKm, AverageTimeMinutes, Cost, Notes, CreatedAtUtc)
            VALUES (@EventDate, @Category, @EventType, @Title, @ItemName, @DistanceKm, @AverageTimeMinutes, @Cost, @Notes, @CreatedAtUtc);
            """;
        await using var connection = CreateConnection();
        await connection.OpenAsync();
        await connection.ExecuteAsync(sql, new
        {
            EventDate = item.EventDate.ToString("O"), item.Category, item.EventType, item.Title, item.ItemName,
            item.DistanceKm, item.AverageTimeMinutes, item.Cost, item.Notes, CreatedAtUtc = item.CreatedAtUtc.ToString("O")
        });
    }

    private sealed class RideSummaryRow
    {
        public int RideCount { get; set; }
        public double TotalDistanceKm { get; set; }
    }

    public async Task<(int RideCount, double TotalDistanceKm)> GetRideSummaryAsync()
    {
        await using var connection = CreateConnection();
        var result = await connection.QuerySingleAsync<RideSummaryRow>("""
            SELECT COUNT(*) AS RideCount, COALESCE(SUM(DistanceKm), 0) AS TotalDistanceKm
            FROM BikeEvents WHERE Category = 'Ride' AND DistanceKm IS NOT NULL;
            """);
        return (result.RideCount, result.TotalDistanceKm);
    }


    public async Task<RideStatistics> GetStatisticsAsync()
    {
        await using var connection = CreateConnection();
        return await connection.QuerySingleAsync<RideStatistics>("""
            SELECT
                COALESCE(SUM(CASE WHEN Category = 'Ride' THEN 1 ELSE 0 END), 0) AS RideCount,
                COALESCE(SUM(CASE WHEN Category = 'Ride' THEN DistanceKm ELSE 0 END), 0) AS TotalDistanceKm,
                COALESCE(AVG(CASE WHEN Category = 'Ride' THEN AverageTimeMinutes END), 0) AS AverageRideMinutes,
                COALESCE(SUM(CASE WHEN Category = 'Maintenance' THEN 1 ELSE 0 END), 0) AS MaintenanceCount,
                COALESCE(SUM(CASE WHEN Category = 'Part' THEN 1 ELSE 0 END), 0) AS PartCount,
                COALESCE(SUM(CASE WHEN Category = 'Part' THEN Cost ELSE 0 END), 0) AS TotalPartCost,
                COALESCE(SUM(CASE WHEN Category = 'Ride' AND AverageTimeMinutes > 0 THEN DistanceKm ELSE 0 END), 0) AS TimedRideDistanceKm,
                COALESCE(SUM(CASE WHEN Category = 'Ride' AND AverageTimeMinutes > 0 THEN AverageTimeMinutes ELSE 0 END), 0) AS TimedRideMinutes
            FROM BikeEvents;
            """);
    }

    public sealed class RideStatistics
    {
        public int RideCount { get; set; }
        public double TotalDistanceKm { get; set; }
        public double AverageRideMinutes { get; set; }
        public int MaintenanceCount { get; set; }
        public int PartCount { get; set; }
        public decimal TotalPartCost { get; set; }
        public double TimedRideDistanceKm { get; set; }
        public double TimedRideMinutes { get; set; }
        public double AverageSpeedKmh => TimedRideMinutes > 0 ? TimedRideDistanceKm / (TimedRideMinutes / 60d) : 0;
    }

    public async Task DeleteAsync(long id)
    {
        await using var connection = CreateConnection();
        await connection.ExecuteAsync("DELETE FROM BikeEvents WHERE Id = @Id", new { Id = id });
    }
}
