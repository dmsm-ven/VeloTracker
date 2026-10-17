using VeloTracker.Data;

namespace VeloTracker.Services;

public sealed class StatisticsService(EventRepository repository)
{
    public Task<EventRepository.RideStatistics> GetOverallStatisticsAsync() => repository.GetStatisticsAsync();

    public async Task<RideChartStatistics> GetRideChartStatisticsAsync(string periodKey)
    {
        var rides = await repository.GetRideEntriesAsync();
        var today = DateTime.Today;

        var (startDate, endDate) = periodKey switch
        {
            "Week" => GetCurrentWeek(today),
            "Year" => (new DateTime(today.Year, 1, 1), new DateTime(today.Year, 12, 31)),
            "All" => GetAllTimeRange(rides, today),
            _ => (new DateTime(today.Year, today.Month, 1), new DateTime(today.Year, today.Month, 1).AddMonths(1).AddDays(-1))
        };

        var periodRides = rides
            .Where(ride => ride.Date >= startDate && ride.Date <= endDate)
            .ToArray();

        var dailyDistance = periodRides
            .GroupBy(ride => ride.Date)
            .ToDictionary(group => group.Key, group => group.Sum(ride => ride.DistanceKm));

        var points = new List<DailyRideDistance>();
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            points.Add(new DailyRideDistance(date, dailyDistance.GetValueOrDefault(date, 0d)));
        }

        var totalDistance = periodRides.Sum(ride => ride.DistanceKm);
        var rideCount = periodRides.Length;
        var daysWithRides = dailyDistance.Count;

        return new RideChartStatistics(
            startDate,
            endDate,
            points,
            daysWithRides,
            rideCount,
            totalDistance,
            rideCount > 0 ? totalDistance / rideCount : 0d);
    }

    private static (DateTime Start, DateTime End) GetCurrentWeek(DateTime today)
    {
        var offset = ((int)today.DayOfWeek + 6) % 7;
        var monday = today.AddDays(-offset).Date;
        return (monday, monday.AddDays(6));
    }

    private static (DateTime Start, DateTime End) GetAllTimeRange(
        IReadOnlyList<EventRepository.RideEntry> rides,
        DateTime today)
    {
        if (rides.Count == 0)
            return (today, today);

        var start = rides.Min(ride => ride.Date).Date;
        var latestRide = rides.Max(ride => ride.Date).Date;
        return (start, latestRide > today ? latestRide : today);
    }
}

public sealed record DailyRideDistance(DateTime Date, double DistanceKm);

public sealed record RideChartStatistics(
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlyList<DailyRideDistance> Points,
    int DaysWithRides,
    int RideCount,
    double TotalDistanceKm,
    double AverageDistancePerRideKm);
