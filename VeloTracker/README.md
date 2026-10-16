# 🚲 VeloTracker

Локальный WPF-журнал обслуживания велосипеда, покупок и установки деталей, а также ежедневного пробега. C# / .NET 10.

## Возможности
- События обслуживания: смазка цепи, покрышка, цепь, тормозные колодки, камера.
- Покупки и замена деталей: маячок, задний переключатель, трещотка, кассета, педали.
- Запись пройденного за день расстояния в километрах.
- Дата, модель/бренд/артикул, примечание и стоимость.
- История событий, удаление записей, общий счётчик поездок и суммарного пробега.
- SQLite + Dapper; параметризованные SQL-запросы.
- MVVM через CommunityToolkit.Mvvm, DI через `IHost`, single-instance Mutex.
- Настройки темы Light/Dark/System, фонового изображения, цвета/непрозрачности подложки, замощения и масштаба текстуры.
- Иконки Fluent System Icons через WPF-UI и тематические символы для типов событий.

## Требования
- Windows 10/11
- .NET 10 SDK
- Visual Studio 2026 с workload «.NET Desktop Development» либо `dotnet` CLI

## Сборка
Из папки репозитория:

```powershell
dotnet restore .\VeloTracker.slnx
dotnet build .\VeloTracker.slnx -c Debug
dotnet run --project .\VeloTracker\VeloTracker.csproj
```

## Хранение данных
- Настройки: `%AppData%\VeloTracker\settings.json`
- База событий и логи: `%LocalAppData%\VeloTracker\`
- Пользовательские данные не сохраняются рядом с EXE.
