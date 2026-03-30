# MSM — WPF Application (.NET 8)

## Project Overview

**MSM** — это десктопное приложение на **WPF (Windows Presentation Foundation)**, разработанное в рамках курсовой работы по объектно-ориентированному программированию (ООП).

**Название**: MSM — аббревиатура от **«Мой Квадратный Метр»** (агентство недвижимости).

### Technology Stack
- **Framework**: .NET 8.0 Windows
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Language**: C# с включёнными nullable reference types
- **IDE**: JetBrains Rider (присутствует `.idea`)

### Project Structure
```
Course_OOP/
├── Course_OOP.sln          # Файл решения Visual Studio/Rider
├── global.json             # Конфигурация версии .NET SDK (8.0.0)
├── .gitignore              # Правила игнорирования для артефактов сборки
└── MSM/
    ├── MSM.csproj          # Файл проекта (WinExe, net8.0-windows, UseWPF=true)
    ├── App.xaml(.cs)       # Точка входа приложения
    ├── MainWindow.xaml(.cs)# Главное окно приложения
    └── AssemblyInfo.cs     # Информация о теме сборки
```

## Building and Running

### Prerequisites
- .NET SDK 8.0 или новее (указано в `global.json`)

### Commands

| Действие | Команда |
|----------|---------|
| Восстановить зависимости | `dotnet restore` |
| Сборка (Debug) | `dotnet build --configuration Debug` |
| Сборка (Release) | `dotnet build --configuration Release` |
| Запуск | `dotnet run --project MSM\MSM.csproj` |
| Очистка | `dotnet clean` |

### Using Visual Studio / Rider
1. Откройте `Course_OOP.sln` в вашей IDE
2. Сборка: `Ctrl+Shift+B` (VS) или `Ctrl+F9` (Rider)
3. Запуск: `F5` или `Ctrl+F5`

## Development Conventions

### Code Style
- **Nullable reference types**: Включено (`<Nullable>enable</Nullable>`)
- **Implicit usings**: Включено (`<ImplicitUsings>enable</ImplicitUsings>`)
- **Target platform**: Windows-only (`net8.0-windows`)

### Project Configuration
- Output type: `WinExe` (Windows-приложение без консоли)
- Platform target: `Any CPU`

### Git Ignore Rules
Игнорируются:
- `bin/` — Скомпилированные бинарные файлы
- `obj/` — Промежуточные файлы сборки
- `packages/` — NuGet пакеты
- `_ReSharper.Caches/` — Кэш ReSharper
- `riderModule.iml` — Файл модуля Rider

## Current State

Проект представляет собой **каркас WPF-приложения** с:
- `App.xaml` с `StartupUri="MainWindow.xaml"`
- Пустым `MainWindow` с чистым `Grid`
- Без дополнительных зависимостей или пользовательских контролов

Это стартовая точка для разработки курсового проекта по агентству недвижимости.

## Expected Features (Inferred)

Для приложения агентства недвижимости ожидаемая функциональность может включать:
- Управление объектами недвижимости (квартиры, дома, коммерческая недвижимость)
- CRUD-операции с объектами и клиентами
- Поиск и фильтрация объектов
- Управление сделками/арендой
- Отчётность и статистика
