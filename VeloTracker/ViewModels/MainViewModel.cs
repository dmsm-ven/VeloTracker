using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VeloTracker.Data;
using VeloTracker.Models;
using VeloTracker.Services;
using VeloTracker.Views;

namespace VeloTracker.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly EventRepository _repository;
    private readonly IServiceProvider _services;

    public ObservableCollection<EventOption> EventOptions { get; } = new()
    {
        new("LubricantChanged", "Замена смазки цепи", "🛢️", "Maintenance"),
        new("TireChanged", "Замена покрышки", "🚲", "Maintenance"),
        new("ChainChanged", "Замена цепи", "⛓️", "Maintenance"),
        new("BrakePadsChanged", "Замена тормозных колодок", "🛑", "Maintenance"),
        new("TubeChanged", "Замена камеры", "⭕", "Maintenance"),
        new("PurchasedBeacon", "Покупка маячка", "📡", "Part"),
        new("RearDerailleurChanged", "Замена заднего переключателя", "⚙️", "Part"),
        new("FreewheelChanged", "Замена трещотки", "🔩", "Part"),
        new("CassetteChanged", "Замена кассеты", "⚙️", "Part"),
        new("PedalsChanged", "Замена педалей", "🦶", "Part"),
        new("RideDistance", "Проехано за день", "📏", "Ride")
    };

    public ObservableCollection<BikeEvent> Events { get; } = new();

    [ObservableProperty] private double totalDistanceKm;
    [ObservableProperty] private int totalRideCount;

    public string HistorySummary => $"Записей в списке: {Events.Count} · Поездок всего: {TotalRideCount} · Пробег всего: {TotalDistanceKm:N1} км";

    partial void OnTotalDistanceKmChanged(double value) => OnPropertyChanged(nameof(HistorySummary));
    partial void OnTotalRideCountChanged(int value) => OnPropertyChanged(nameof(HistorySummary));

    [ObservableProperty] private EventOption? selectedOption;
    [ObservableProperty] private DateTime selectedDate = DateTime.Today;
    [ObservableProperty] private string notes = string.Empty;
    [ObservableProperty] private string itemName = string.Empty;
    [ObservableProperty] private string distanceKmText = "20";
    [ObservableProperty] private string costText = string.Empty;
    [ObservableProperty] private string statusMessage = "Готово к работе";
    [ObservableProperty] private bool isBusy;

    public bool IsRideEvent => SelectedOption?.Category == "Ride";

    public MainViewModel(EventRepository repository, IServiceProvider services)
    {
        _repository = repository;
        _services = services;
        SelectedOption = EventOptions[0];
    }

    partial void OnSelectedOptionChanged(EventOption? value)
    {
        OnPropertyChanged(nameof(IsRideEvent));
        if (value?.Category == "Ride" && string.IsNullOrWhiteSpace(DistanceKmText)) DistanceKmText = "20";
    }

    [RelayCommand]
    private async Task AddEventAsync()
    {
        if (IsBusy || SelectedOption is null) return;
        double? distance = null;
        decimal? cost = null;
        if (SelectedOption.Category == "Ride")
        {
            if (!double.TryParse(DistanceKmText, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed) ||
                !double.IsFinite(parsed) || parsed < 0 || parsed > 1000)
            {
                StatusMessage = "Укажи корректный пробег от 0 до 1000 км.";
                return;
            }
            distance = parsed;
        }
        if (!string.IsNullOrWhiteSpace(CostText))
        {
            if (!decimal.TryParse(CostText, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsedCost) || parsedCost < 0)
            {
                StatusMessage = "Проверь стоимость: нужно неотрицательное число.";
                return;
            }
            cost = parsedCost;
        }

        IsBusy = true;
        try
        {
            var item = new BikeEvent
            {
                EventDate = SelectedDate.Date,
                Category = SelectedOption.Category,
                EventType = SelectedOption.Key,
                Title = SelectedOption.Label,
                ItemName = string.IsNullOrWhiteSpace(ItemName) ? null : ItemName.Trim(),
                DistanceKm = distance,
                Cost = cost,
                Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
                CreatedAtUtc = DateTime.UtcNow,
                Icon = SelectedOption.Icon
            };
            await _repository.AddAsync(item);
            StatusMessage = $"Добавлено: {SelectedOption.Label}";
            Notes = string.Empty;
            ItemName = string.Empty;
            CostText = string.Empty;
            await LoadEventsAsync();
            OnPropertyChanged(nameof(HistorySummary));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Не удалось сохранить событие: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task DeleteEventAsync(BikeEvent? item)
    {
        if (item is null) return;
        if (MessageBox.Show($"Удалить событие «{item.Title}»?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try
        {
            await _repository.DeleteAsync(item.Id);
            await LoadEventsAsync();
            OnPropertyChanged(nameof(HistorySummary));
            StatusMessage = "Событие удалено";
        }
        catch (Exception ex) { StatusMessage = $"Ошибка удаления: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task LoadEventsAsync()
    {
        try
        {
            var items = await _repository.GetRecentAsync();
            var summary = await _repository.GetRideSummaryAsync();
            Application.Current.Dispatcher.Invoke(() =>
            {
                Events.Clear();
                foreach (var item in items)
                {
                    item.Icon = EventOptions.FirstOrDefault(x => x.Key == item.EventType)?.Icon ?? "📌";
                    Events.Add(item);
                }
                TotalRideCount = summary.RideCount;
                TotalDistanceKm = summary.TotalDistanceKm;
                OnPropertyChanged(nameof(HistorySummary));
            });
        }
        catch (Exception ex) { StatusMessage = $"Ошибка чтения истории: {ex.Message}"; }
    }

    [RelayCommand]
    private void OpenSettings()
    {
        if (_services.GetService(typeof(SettingsWindow)) is not SettingsWindow window) return;
        window.Owner = Application.Current.MainWindow;
        window.ShowDialog();
    }
}
