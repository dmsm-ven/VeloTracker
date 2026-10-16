using Dapper;
using Microsoft.Data.Sqlite;
using VeloTracker.Services;

namespace VeloTracker.Data;

public sealed class DatabaseInitializer(AppPaths paths)
{
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
                Cost NUMERIC NULL CHECK (Cost IS NULL OR Cost >= 0),
                Notes TEXT NULL,
                CreatedAtUtc TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_BikeEvents_EventDate ON BikeEvents(EventDate DESC);
            """);
    }
}
