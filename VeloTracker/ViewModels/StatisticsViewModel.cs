using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VeloTracker.Services;

namespace VeloTracker.ViewModels;

public partial class StatisticsViewModel : ObservableObject
{
    private readonly StatisticsService _statisticsService;
    private int _chartLoadVersion;

    [ObservableProperty] private int rideCount;
    [ObservableProperty] private double totalDistanceKm;
    [ObservableProperty] private double averageRideMinutes;
    [ObservableProperty] private double averageSpeedKmh;
    [ObservableProperty] private int maintenanceCount;
    [ObservableProperty] private int partCount;
    [ObservableProperty] private decimal totalPartCost;
    [ObservableProperty] private string statusMessage = "Загрузка статистики…";

    [ObservableProperty] private string selectedPeriodKey = "Month";
    [ObservableProperty] private double[] chartValues = [];
    [ObservableProperty] private string[] chartLabels = [];
    [ObservableProperty] private string[] chartToolTips = [];
    [ObservableProperty] private double chartWidth = 900;
    [ObservableProperty] private int daysWithRides;
    [ObservableProperty] private double periodDistanceKm;
    [ObservableProperty] private double averageDistancePerRideKm;
    [ObservableProperty] private string periodDescription = string.Empty;

    public IReadOnlyList<StatisticsPeriodOption> PeriodOptions { get; } =
    [
        new("Week", "Неделя"),
        new("Month", "Текущий месяц"),
        new("Year", "Текущий год"),
        new("All", "Все данные")
    ];

    public string AverageRideMinutesLabel => AverageRideMinutes > 0 ? $"{AverageRideMinutes:N0} мин" : "Нет данных";
    public string AverageSpeedLabel => AverageSpeedKmh > 0 ? $"{AverageSpeedKmh:N1} км/ч" : "Нет данных";
    public string DaysWithRidesLabel => DaysWithRides.ToString("N0");
    public string PeriodDistanceLabel => $"{PeriodDistanceKm:N1} км";
    public string AverageDistancePerRideLabel => AverageDistancePerRideKm > 0 ? $"{AverageDistancePerRideKm:N1} км" : "Нет данных";

    public StatisticsViewModel(StatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
        _ = LoadAsync();
    }

    partial void OnAverageRideMinutesChanged(double value) => OnPropertyChanged(nameof(AverageRideMinutesLabel));
    partial void OnAverageSpeedKmhChanged(double value) => OnPropertyChanged(nameof(AverageSpeedLabel));
    partial void OnDaysWithRidesChanged(int value) => OnPropertyChanged(nameof(DaysWithRidesLabel));
    partial void OnPeriodDistanceKmChanged(double value) => OnPropertyChanged(nameof(PeriodDistanceLabel));
    partial void OnAverageDistancePerRideKmChanged(double value) => OnPropertyChanged(nameof(AverageDistancePerRideLabel));

    partial void OnSelectedPeriodKeyChanged(string value)
    {
        _ = LoadChartAsync();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            var stats = await _statisticsService.GetOverallStatisticsAsync();
            RideCount = stats.RideCount;
            TotalDistanceKm = stats.TotalDistanceKm;
            AverageRideMinutes = stats.AverageRideMinutes;
            AverageSpeedKmh = stats.AverageSpeedKmh;
            MaintenanceCount = stats.MaintenanceCount;
            PartCount = stats.PartCount;
            TotalPartCost = stats.TotalPartCost;
            await LoadChartAsync();
            StatusMessage = "Статистика обновлена";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Не удалось загрузить статистику: {ex.Message}";
        }
    }

    private async Task LoadChartAsync()
    {
        var version = Interlocked.Increment(ref _chartLoadVersion);
        try
        {
            var result = await _statisticsService.GetRideChartStatisticsAsync(SelectedPeriodKey);
            if (version != _chartLoadVersion) return;

            var columnWidth = SelectedPeriodKey switch
            {
                "Week" => 76d,
                "Month" => 28d,
                _ => 23d
            };

            ChartValues = result.Points.Select(point => point.DistanceKm).ToArray();
            ChartLabels = result.Points.Select(point => SelectedPeriodKey switch
            {
                "Week" => point.Date.ToString("dd.MM"),
                "Month" => point.Date.ToString("dd"),
                "Year" => point.Date.ToString("dd.MM"),
                _ => point.Date.ToString("dd.MM.yy")
            }).ToArray();
            ChartToolTips = result.Points
                .Select(point => $"{point.Date:dd.MM.yyyy}: {point.DistanceKm:N1} км")
                .ToArray();
            ChartWidth = Math.Max(760d, 78d + result.Points.Count * columnWidth);

            DaysWithRides = result.DaysWithRides;
            PeriodDistanceKm = result.TotalDistanceKm;
            AverageDistancePerRideKm = result.AverageDistancePerRideKm;
            PeriodDescription = $"{result.StartDate:dd.MM.yyyy} — {result.EndDate:dd.MM.yyyy}";
            OnPropertyChanged(nameof(DaysWithRidesLabel));
            OnPropertyChanged(nameof(PeriodDistanceLabel));
            OnPropertyChanged(nameof(AverageDistancePerRideLabel));
        }
        catch (Exception ex)
        {
            if (version == _chartLoadVersion)
                StatusMessage = $"Не удалось загрузить график: {ex.Message}";
        }
    }
}

public sealed record StatisticsPeriodOption(string Key, string Label);
