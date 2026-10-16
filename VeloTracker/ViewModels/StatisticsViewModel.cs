using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VeloTracker.Data;

namespace VeloTracker.ViewModels;

public partial class StatisticsViewModel(EventRepository repository) : ObservableObject
{
    [ObservableProperty] private int rideCount;
    [ObservableProperty] private double totalDistanceKm;
    [ObservableProperty] private double averageRideMinutes;
    [ObservableProperty] private double averageSpeedKmh;
    [ObservableProperty] private int maintenanceCount;
    [ObservableProperty] private int partCount;
    [ObservableProperty] private decimal totalPartCost;
    [ObservableProperty] private string statusMessage = "Загрузка статистики…";

    public string AverageRideMinutesLabel => AverageRideMinutes > 0 ? $"{AverageRideMinutes:N0} мин" : "Нет данных";
    public string AverageSpeedLabel => AverageSpeedKmh > 0 ? $"{AverageSpeedKmh:N1} км/ч" : "Нет данных";

    partial void OnAverageRideMinutesChanged(double value) => OnPropertyChanged(nameof(AverageRideMinutesLabel));
    partial void OnAverageSpeedKmhChanged(double value) => OnPropertyChanged(nameof(AverageSpeedLabel));

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            var stats = await repository.GetStatisticsAsync();
            RideCount = stats.RideCount;
            TotalDistanceKm = stats.TotalDistanceKm;
            AverageRideMinutes = stats.AverageRideMinutes;
            AverageSpeedKmh = stats.AverageSpeedKmh;
            MaintenanceCount = stats.MaintenanceCount;
            PartCount = stats.PartCount;
            TotalPartCost = stats.TotalPartCost;
            StatusMessage = "Статистика обновлена";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Не удалось загрузить статистику: {ex.Message}";
        }
    }
}
