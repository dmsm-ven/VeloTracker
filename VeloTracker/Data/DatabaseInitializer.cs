using Dapper;
using Microsoft.Data.Sqlite;
using VeloTracker.Services;

namespace VeloTracker.Data;

public sealed class DatabaseInitializer(AppPaths paths)
{
    private sealed class ColumnInfo { public string name { get; set; } = string.Empty; }
    public async Task InitializeAsync()
    {
        await using var connection = new SqliteConnection($"Data Source={paths.DatabasePath};Cache=Shared");
        await connection.OpenAsync();
        await connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS BikeEvents (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                EventDate TEXT NOT NULL,
                Category TEXT NOT NULL,
                EventType TEXT NOT NULL,
                Title TEXT NOT NULL,
                ItemName TEXT NULL,
                DistanceKm REAL NULL CHECK (DistanceKm IS NULL OR DistanceKm >= 0),
                AverageTimeMinutes INTEGER NULL CHECK (AverageTimeMinutes IS NULL OR AverageTimeMinutes > 0),
                Cost NUMERIC NULL CHECK (Cost IS NULL OR Cost >= 0),
                Notes TEXT NULL,
                CreatedAtUtc TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_BikeEvents_EventDate ON BikeEvents(EventDate DESC);
            """);

        // Existing databases are upgraded in place without losing prior events.
        // PRAGMA table_info returns rows, so read column names via a tiny row model.
        var schema = await connection.QueryAsync<ColumnInfo>("PRAGMA table_info(BikeEvents);");
        if (!schema.Any(column => column.name == "AverageTimeMinutes"))
            await connection.ExecuteAsync("ALTER TABLE BikeEvents ADD COLUMN AverageTimeMinutes INTEGER NULL CHECK (AverageTimeMinutes IS NULL OR AverageTimeMinutes > 0);");
    }
}
