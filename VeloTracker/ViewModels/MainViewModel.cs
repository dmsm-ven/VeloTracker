using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VeloTracker.Data;
using VeloTracker.Models;
using VeloTracker.Services;
using VeloTracker.Views;
using Wpf.Ui.Controls;

namespace VeloTracker.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly EventRepository _repository;
    private readonly IServiceProvider _services;
    public AppBackgroundService Background { get; }

    public ObservableCollection<EventCategoryOption> Categories { get; } = new()
    {
        new("Ride", "Километраж", SymbolRegular.VehicleBicycle24),
        new("Maintenance", "Обслуживание", SymbolRegular.WrenchScrewdriver24),
        new("Part", "Детали", SymbolRegular.ShoppingBag24)
    };

    private readonly List<EventOption> _allOptions = new()
    {
        // Обслуживание велосипеда
        new("LubricantChanged", "Замена смазки цепи", SymbolRegular.Drop24, "Maintenance"),
        new("TireChanged", "Замена покрышки", SymbolRegular.VehicleBicycle24, "Maintenance"),
        new("ChainChanged", "Замена цепи", SymbolRegular.Link24, "Maintenance"),
        new("BrakePadsChanged", "Замена тормозных колодок", SymbolRegular.ShieldCheckmark24, "Maintenance"),
        new("TubeChanged", "Замена камеры", SymbolRegular.Circle24, "Maintenance"),

        // Запасные части и компоненты
        new("PurchasedBeacon", "Покупка маячка", SymbolRegular.Location24, "Part"),
        new("RearDerailleurChanged", "Задний переключатель", SymbolRegular.WrenchScrewdriver24, "Part"),
        new("FrontDerailleurChanged", "Передний переключатель", SymbolRegular.WrenchScrewdriver24, "Part"),
        new("FreewheelChanged", "Трещотка", SymbolRegular.DriveTrain24, "Part"),
        new("CassetteChanged", "Кассета", SymbolRegular.Settings24, "Part"),
        new("ChainPartChanged", "Цепь", SymbolRegular.Link24, "Part"),
        new("CranksetChanged", "Система шатунов", SymbolRegular.Settings24, "Part"),
        new("ChainringChanged", "Передняя звезда", SymbolRegular.Settings24, "Part"),
        new("BottomBracketChanged", "Каретка", SymbolRegular.Settings24, "Part"),
        new("PedalsChanged", "Педали", SymbolRegular.VehicleBicycle24, "Part"),
        new("RearHubChanged", "Задняя втулка", SymbolRegular.Settings24, "Part"),
        new("FrontHubChanged", "Передняя втулка", SymbolRegular.Settings24, "Part"),
        new("RearWheelChanged", "Заднее колесо", SymbolRegular.VehicleBicycle24, "Part"),
        new("FrontWheelChanged", "Переднее колесо", SymbolRegular.VehicleBicycle24, "Part"),
        new("RimChanged", "Обод", SymbolRegular.Circle24, "Part"),
        new("SpokesChanged", "Спицы", SymbolRegular.Settings24, "Part"),
        new("TirePartChanged", "Покрышка", SymbolRegular.Circle24, "Part"),
        new("TubePartChanged", "Камера", SymbolRegular.Circle24, "Part"),
        new("SaddleChanged", "Седло", SymbolRegular.VehicleBicycle24, "Part"),
        new("SeatpostChanged", "Подседельный штырь", SymbolRegular.WrenchScrewdriver24, "Part"),
        new("HandlebarChanged", "Руль", SymbolRegular.VehicleBicycle24, "Part"),
        new("GripsChanged", "Грипсы / рукоятки", SymbolRegular.VehicleBicycle24, "Part"),
        new("StemChanged", "Вынос руля", SymbolRegular.WrenchScrewdriver24, "Part"),
        new("HeadsetChanged", "Рулевая колонка", SymbolRegular.Settings24, "Part"),
        new("BrakeRotorChanged", "Тормозной ротор / диск", SymbolRegular.Circle24, "Part"),
        new("BrakeCaliperChanged", "Тормозной калипер", SymbolRegular.ShieldCheckmark24, "Part"),
        new("BrakeLeverChanged", "Тормозная ручка", SymbolRegular.WrenchScrewdriver24, "Part"),
        new("BrakeCableChanged", "Тормозной трос / гидролиния", SymbolRegular.Link24, "Part"),
        new("ShifterChanged", "Манетка переключения передач", SymbolRegular.Settings24, "Part"),
        new("GearCableChanged", "Трос переключения передач", SymbolRegular.Link24, "Part"),
        new("AxleChanged", "Ось колеса", SymbolRegular.Settings24, "Part"),
        new("QuickReleaseChanged", "Эксцентрик / зажим колеса", SymbolRegular.Settings24, "Part"),
        new("MudguardChanged", "Крылья", SymbolRegular.VehicleBicycle24, "Part"),
        new("KickstandChanged", "Подножка", SymbolRegular.VehicleBicycle24, "Part"),
        new("BellChanged", "Звонок", SymbolRegular.VehicleBicycle24, "Part"),
        new("LightChanged", "Фара / задний фонарь", SymbolRegular.Location24, "Part"),
        new("OtherPart", "Другая деталь", SymbolRegular.Toolbox24, "Part")
    };

    public ObservableCollection<EventOption> AvailableOptions { get; } = new();
    public ObservableCollection<BikeEvent> Events { get; } = new();
    public ObservableCollection<BikeEvent> RideEvents { get; } = new();
    public ObservableCollection<BikeEvent> MaintenanceEvents { get; } = new();
    public ObservableCollection<BikeEvent> PartEvents { get; } = new();

    [ObservableProperty] private double totalDistanceKm;
    [ObservableProperty] private int totalRideCount;
    [ObservableProperty] private EventCategoryOption? selectedCategory;
    [ObservableProperty] private EventOption? selectedOption;
    [ObservableProperty] private DateTime selectedDate = DateTime.Today;
    [ObservableProperty] private string notes = string.Empty;
    [ObservableProperty] private string itemName = string.Empty;
    [ObservableProperty] private string distanceKmText = "20";
    [ObservableProperty] private string averageTimeMinutesText = string.Empty;
    [ObservableProperty] private string costText = string.Empty;
    [ObservableProperty] private string statusMessage = "Готово к работе";
    [ObservableProperty] private bool isBusy;

    public bool IsRideEvent => SelectedCategory?.Key == "Ride";
    public bool IsPartEvent => SelectedCategory?.Key == "Part";
    public bool IsMaintenanceEvent => SelectedCategory?.Key == "Maintenance";
    public bool ShowsSubtype => !IsRideEvent;
    public string ActiveViewTitle => IsRideEvent ? "Километраж" : IsMaintenanceEvent ? "Обслуживание" : "Детали";
    public string HistorySummary => $"Записей в списке: {Events.Count} · Поездок всего: {TotalRideCount} · Пробег всего: {TotalDistanceKm:N1} км";

    partial void OnTotalDistanceKmChanged(double value) => OnPropertyChanged(nameof(HistorySummary));
    partial void OnTotalRideCountChanged(int value) => OnPropertyChanged(nameof(HistorySummary));

    public MainViewModel(EventRepository repository, IServiceProvider services, AppearanceService appearance)
    {
        _repository = repository;
        _services = services;
        Background = appearance.Background;
        SelectedCategory = Categories[0]; // Километраж — наиболее частый сценарий и стартовый экран.
        RefreshOptions();
    }

    partial void OnSelectedCategoryChanged(EventCategoryOption? value)
    {
        OnPropertyChanged(nameof(IsRideEvent));
        OnPropertyChanged(nameof(IsPartEvent));
        OnPropertyChanged(nameof(IsMaintenanceEvent));
        OnPropertyChanged(nameof(ShowsSubtype));
        OnPropertyChanged(nameof(ActiveViewTitle));
        RefreshOptions();
    }

    private void RefreshOptions()
    {
        AvailableOptions.Clear();
        if (SelectedCategory is null) { SelectedOption = null; return; }
        foreach (var option in _allOptions.Where(x => x.Category == SelectedCategory.Key)) AvailableOptions.Add(option);
        SelectedOption = AvailableOptions.FirstOrDefault();
    }

    [RelayCommand]
    private void SelectView(string? categoryKey)
    {
        if (string.IsNullOrWhiteSpace(categoryKey)) return;
        var category = Categories.FirstOrDefault(x => x.Key == categoryKey);
        if (category is not null) SelectedCategory = category;
    }

    [RelayCommand]
    private async Task AddEventAsync()
    {
        if (IsBusy || SelectedCategory is null || (!IsRideEvent && SelectedOption is null)) return;
        double? distance = null;
        int? averageTimeMinutes = null;
        decimal? cost = null;

        if (IsRideEvent)
        {
            if (!double.TryParse(DistanceKmText, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed) ||
                !double.IsFinite(parsed) || parsed <= 0 || parsed > 1000)
            {
                StatusMessage = "Укажи корректный пробег больше 0 и не более 1000 км.";
                return;
            }
            distance = parsed;
            if (!string.IsNullOrWhiteSpace(AverageTimeMinutesText))
            {
                if (!int.TryParse(AverageTimeMinutesText, NumberStyles.Integer, CultureInfo.CurrentCulture, out var parsedMinutes) || parsedMinutes <= 0 || parsedMinutes > 1440)
                {
                    StatusMessage = "Среднее время должно быть целым числом минут от 1 до 1440.";
                    return;
                }
                averageTimeMinutes = parsedMinutes;
            }
        }
        else if (IsPartEvent && !string.IsNullOrWhiteSpace(CostText))
        {
            if (!decimal.TryParse(CostText, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsedCost) || parsedCost < 0)
            {
                StatusMessage = "Проверь стоимость: нужно неотрицательное число.";
                return;
            }
            cost = parsedCost;
        }

        var chosen = SelectedOption;
        var title = IsRideEvent ? "Пройденное расстояние" : chosen!.Label;
        var icon = IsRideEvent ? SymbolRegular.VehicleBicycle24 : chosen!.Icon;
        IsBusy = true;
        try
        {
            var item = new BikeEvent
            {
                EventDate = SelectedDate.Date,
                Category = SelectedCategory.Key,
                EventType = IsRideEvent ? "RideDistance" : chosen!.Key,
                Title = title,
                ItemName = IsPartEvent && !string.IsNullOrWhiteSpace(ItemName) ? ItemName.Trim() : null,
                DistanceKm = distance,
                AverageTimeMinutes = averageTimeMinutes,
                Cost = cost,
                Notes = !IsRideEvent && !string.IsNullOrWhiteSpace(Notes) ? Notes.Trim() : null,
                CreatedAtUtc = DateTime.UtcNow,
                Icon = icon
            };
            await _repository.AddAsync(item);
            StatusMessage = IsRideEvent ? $"Добавлено: {distance:N1} км" : $"Добавлено: {title}";
            Notes = string.Empty;
            ItemName = string.Empty;
            CostText = string.Empty;
            if (IsRideEvent) { DistanceKmText = string.Empty; AverageTimeMinutesText = string.Empty; }
            await LoadEventsAsync();
        }
        catch (Exception ex) { StatusMessage = $"Не удалось сохранить событие: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task DeleteEventAsync(BikeEvent? item)
    {
        if (item is null) return;
        if (System.Windows.MessageBox.Show($"Удалить событие «{item.Title}»?", "Подтверждение",
            System.Windows.MessageBoxButton.YesNo,
            MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes) return;
        try
        {
            await _repository.DeleteAsync(item.Id);
            await LoadEventsAsync();
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
                RideEvents.Clear();
                MaintenanceEvents.Clear();
                PartEvents.Clear();
                foreach (var item in items)
                {
                    item.Icon = _allOptions.FirstOrDefault(x => x.Key == item.EventType)?.Icon ??
                                (item.Category == "Ride" ? SymbolRegular.VehicleBicycle24 : SymbolRegular.Wrench24);
                    Events.Add(item);
                    switch (item.Category)
                    {
                        case "Ride": RideEvents.Add(item); break;
                        case "Maintenance": MaintenanceEvents.Add(item); break;
                        case "Part": PartEvents.Add(item); break;
                    }
                }
                TotalRideCount = summary.RideCount;
                TotalDistanceKm = summary.TotalDistanceKm;
                OnPropertyChanged(nameof(HistorySummary));
            });
        }
        catch (Exception ex) { StatusMessage = $"Ошибка чтения истории: {ex.Message}"; }
    }

    [RelayCommand]
    private void OpenStatistics()
    {
        if (_services.GetService(typeof(StatisticsWindow)) is not StatisticsWindow window) return;
        window.Owner = Application.Current.MainWindow;
        window.ShowDialog();
    }

    [RelayCommand]
    private void OpenSettings()
    {
        if (_services.GetService(typeof(SettingsWindow)) is not SettingsWindow window) return;
        window.Owner = Application.Current.MainWindow;
        window.ShowDialog();
    }
}
