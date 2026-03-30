# Real Estate Agency - Project Overview

**Course Project on OOP | WPF | MVVM | SQL Server | Entity Framework Core**

**Version: 1.0**  
**Last Updated: 2026**

---

## PROJECT NAME
Real Estate Agency Desktop Application

## PLATFORM
- WPF (Windows Presentation Foundation)
- .NET 6 or .NET 8
- C# programming language

## ARCHITECTURE PATTERN
MVVM (Model-View-ViewModel)

## DATABASE
- SQL Server (LocalDB)
- Entity Framework Core (Code First approach)

## PROJECT DESCRIPTION
Desktop application for real estate agency that automates property sales processes. The system includes three user roles (Administrator, Realtor, Client) with different access levels and functionality.

## KEY FEATURES
- Three-tier user system with role-based access
- Property catalog with image carousel
- Advanced filtering and search
- Appointment calendar system
- Favorites for authorized users
- Reviews and ratings
- Statistics and charts
- Multi-language support (Russian/English)
- Theme switching (Light/Dark)
- Image storage in database

---

# User Roles and Permissions

## ADMINISTRATOR ROLE

| Permission | Description |
|------------|-------------|
| User Management | Create, edit, delete realtor accounts |
| Review Moderation | Approve/reject all reviews |
| Notifications | Send email/SMS to clients |
| Database Access | Direct table view/edit |
| Statistics | View all realtor performance data |

## REALTOR ROLE

| Permission | Description |
|------------|-------------|
| Property Management | Add/edit/delete properties |
| Photo Upload | Multiple images per property |
| Appointment Processing | View and confirm client requests |
| Schedule Management | Set available time slots |
| Personal Statistics | View own performance metrics |

## CLIENT ROLE

| Permission | Description |
|------------|-------------|
| Browse Catalog | View all active properties |
| Search & Filter | Advanced filtering options |
| Favorites | Save properties to favorites |
| Appointments | Schedule property viewings |
| Reviews | Submit ratings and comments |

---

# Functional Requirements

## ADMINISTRATOR FUNCTIONS

### User Management
- Add new realtor accounts with login, password, contact information
- Edit existing realtor information
- Deactivate/delete realtor accounts
- Reset passwords for users
- View list of all registered users

### Review Moderation
- View all submitted reviews in queue
- Read review content and ratings
- Approve reviews for publication
- Reject reviews with reason
- Delete inappropriate reviews

### Notification System
- Compose notification messages
- Select recipient group
- Choose notification type (email/SMS emulation)
- Use message templates
- View notification history

### Database Management
- View all database tables in grid format
- Edit records directly
- Export data to JSON/CSV formats
- Import data from files

### Statistics Dashboard
- Total number of properties in system
- Properties sold per realtor (chart)
- Number of viewings per period (chart)
- Average rating per realtor
- Revenue statistics

## REALTOR FUNCTIONS

### Property Management

| Field | Type | Required | Validation |
|-------|------|----------|------------|
| Title | String(200) | Yes | Max 200 chars |
| Description | String(MAX) | Yes | Max 5000 chars |
| Price | Decimal(18,2) | Yes | > 0 |
| Area | Float | Yes | 10-1000 m² |
| Rooms | Int | Yes | 1-10+ |
| City | String(100) | Yes | - |
| Address | String(300) | Yes | - |
| PropertyType | Enum | Yes | House/Apartment/Complex |
| Photos | Byte[] | Yes | Min 1, Max 20 |

### Appointment Processing
- List of all appointment requests
- Filter by status (New/In Progress/Confirmed/Rejected/Completed)
- Change application status
- Add comments to status changes
- Send confirmation to client

### Schedule Management
- Select date from calendar
- Define time slots (e.g., 10:00-10:30)
- Mark slots as available/unavailable
- View weekly/monthly calendar

### Personal Statistics
- Total active properties
- Properties sold this month/total
- Number of viewings scheduled
- Conversion rate (views to sales)
- Client rating average

## CLIENT FUNCTIONS

### Registration and Authorization
- Register new account (login, password, email, name, phone)
- Login to system
- Manage personal profile
- Change password

### Property Catalog
- Grid/list view with property cards
- Pagination (20/50/100 per page)
- Property card: image, price, area, rooms, location

### Search and Filtering

| Filter Type | Options |
|-------------|---------|
| Price Range | From/To inputs |
| Area Range | From/To inputs |
| City | Dropdown |
| Rooms | 1/2/3/4/5+ |
| Property Type | House/Apartment/Complex |
| Year Built | Range selector |
| Renovation | Yes/No/Any |
| Mortgage | Yes/No/Any |

### Property Details
- Image carousel with navigation
- Full property information
- Realtor contact information
- Schedule viewing button
- Add to favorites button

### Favorites System
- Grid of favorited properties
- Remove from favorites
- Sort by date/price
- Requires authorization

### Appointment System
- Select date from calendar
- View available time slots
- Submit request with comment
- View appointment history and status

### Reviews System
- Star rating (1-5)
- Review text (optional, max 1000 chars)
- Submit for moderation
- View approved reviews

---

# Non-Functional Requirements

## PERFORMANCE REQUIREMENTS

| Metric | Target |
|--------|--------|
| Application startup | < 3 seconds |
| Login authentication | < 2 seconds |
| Property list loading | < 2 seconds |
| Property detail loading | < 1 second |
| Search/filter results | < 1.5 seconds |
| Image carousel navigation | < 0.5 seconds |
| Memory usage | < 200MB typical |
| Max properties in DB | 10,000 |
| Max registered users | 1,000 |

## USABILITY REQUIREMENTS
- Intuitive navigation
- Consistent design across all screens
- Readable fonts (min 12pt)
- Adequate color contrast (WCAG AA)
- Responsive to window resizing
- Keyboard navigation support
- Loading indicators for async operations
- Error messages in plain language

## RELIABILITY REQUIREMENTS
- Application uptime: 99% during business hours
- Graceful degradation on errors
- Auto-save for forms in progress
- Transaction support for critical operations
- Data validation on input
- Error logging to file
- No application crashes

## SECURITY REQUIREMENTS
- Password hashing using BCrypt
- Minimum password length: 6 characters
- Role-based access control
- Parameterized SQL queries (prevent SQL injection)
- Input validation and sanitization
- No sensitive data in logs

## MAINTAINABILITY REQUIREMENTS
- MVVM pattern strictly followed
- Separation of concerns
- DRY principle
- Meaningful variable and method names
- Code comments for complex logic
- XML comments for public methods
- No hardcoded strings (use resources)

## COMPATIBILITY REQUIREMENTS

| Component | Requirement |
|-----------|-------------|
| Operating System | Windows 10 (1903+), Windows 11 |
| Database | SQL Server 2019+, LocalDB |
| IDE | Visual Studio 2022 |
| Framework | .NET 6 or .NET 8 |

---

# Database Schema

## DATABASE STRUCTURE

**Database Name:** RealEstateDb  
**Tables:** 7 main tables  
**Relationships:** One-to-Many, Many-to-Many

## TABLE: Users

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Auto-increment ID |
| Login | nvarchar(50) | Unique, Not Null | User login name |
| PasswordHash | nvarchar(255) | Not Null | BCrypt hashed password |
| Email | nvarchar(100) | Not Null | User email |
| Role | nvarchar(20) | Not Null | admin/realtor/client |
| FullName | nvarchar(100) | Not Null | Full name |
| Phone | nvarchar(20) | Null | Contact phone |
| CreatedAt | datetime | Not Null | Account creation date |

## TABLE: Properties

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Auto-increment ID |
| Title | nvarchar(200) | Not Null | Property title |
| Description | nvarchar(max) | Not Null | Full description |
| Price | decimal(18,2) | Not Null | Price in rubles |
| Area | float | Not Null | Area in m² |
| Rooms | int | Not Null | Number of rooms |
| City | nvarchar(100) | Not Null | City name |
| District | nvarchar(100) | Null | District |
| Address | nvarchar(300) | Not Null | Full address |
| PropertyType | nvarchar(20) | Not Null | house/apartment/complex |
| Floor | int | Null | Floor number |
| TotalFloors | int | Null | Total floors |
| YearBuilt | int | Null | Year built |
| HasRepair | bit | Not Null | Has renovation |
| MortgageAvailable | bit | Not Null | Mortgage available |
| Status | nvarchar(20) | Not Null | active/sold/hidden |
| RealtorId | int | FK, Not Null | Reference to Users |
| CreatedAt | datetime | Not Null | Creation date |
| UpdatedAt | datetime | Not Null | Last update |

## TABLE: PropertyImages

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Auto-increment ID |
| PropertyId | int | FK, Not Null | Reference to Properties |
| ImageData | varbinary(max) | Not Null | Binary image data |
| FileName | nvarchar(255) | Not Null | Original filename |
| SortOrder | int | Not Null | Display order |
| IsMain | bit | Not Null | Is main image |

## TABLE: Appointments

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Auto-increment ID |
| PropertyId | int | FK, Not Null | Reference to Properties |
| ClientId | int | FK, Not Null | Reference to Users (client) |
| RealtorId | int | FK, Not Null | Reference to Users (realtor) |
| SlotStart | datetime | Not Null | Start time |
| SlotEnd | datetime | Not Null | End time |
| Status | nvarchar(20) | Not Null | new/confirmed/cancelled/completed |
| Comment | nvarchar(500) | Null | Client comment |
| CreatedAt | datetime | Not Null | Request date |

## TABLE: RealtorSchedule

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Auto-increment ID |
| RealtorId | int | FK, Not Null | Reference to Users |
| SlotStart | datetime | Not Null | Slot start |
| SlotEnd | datetime | Not Null | Slot end |
| IsAvailable | bit | Not Null | Available for booking |

## TABLE: Favorites

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Auto-increment ID |
| UserId | int | FK, Not Null | Reference to Users |
| PropertyId | int | FK, Not Null | Reference to Properties |
| AddedAt | datetime | Not Null | When favorited |

**Constraints:**
- Unique: UserId + PropertyId (prevent duplicates)

## TABLE: Reviews

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int | PK, Identity | Auto-increment ID |
| UserId | int | FK, Not Null | Reference to Users |
| PropertyId | int | FK, Null | Reference to Properties |
| RealtorId | int | FK, Null | Reference to Users |
| Rating | int | Not Null | 1-5 stars |
| Comment | nvarchar(1000) | Null | Review text |
| IsApproved | bit | Not Null | Moderation status |
| CreatedAt | datetime | Not Null | Submission date |

## DATABASE CREATION SCRIPT

```sql
CREATE DATABASE RealEstateDb;
GO

USE RealEstateDb;
GO

CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Login NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    Role NVARCHAR(20) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Phone NVARCHAR(20),
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
);

CREATE TABLE Properties (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    Area FLOAT NOT NULL,
    Rooms INT NOT NULL,
    City NVARCHAR(100) NOT NULL,
    District NVARCHAR(100),
    Address NVARCHAR(300) NOT NULL,
    PropertyType NVARCHAR(20) NOT NULL,
    Floor INT,
    TotalFloors INT,
    YearBuilt INT,
    HasRepair BIT NOT NULL DEFAULT 0,
    MortgageAvailable BIT NOT NULL DEFAULT 0,
    Status NVARCHAR(20) NOT NULL DEFAULT 'active',
    RealtorId INT NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Properties_Users FOREIGN KEY (RealtorId) REFERENCES Users(Id),
    CONSTRAINT CHK_Price CHECK (Price > 0),
    CONSTRAINT CHK_Area CHECK (Area > 10 AND Area < 1000)
);

CREATE TABLE PropertyImages (
    Id INT PRIMARY KEY IDENTITY(1,1),
    PropertyId INT NOT NULL,
    ImageData VARBINARY(MAX) NOT NULL,
    FileName NVARCHAR(255) NOT NULL,
    SortOrder INT NOT NULL DEFAULT 0,
    IsMain BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_PropertyImages_Properties FOREIGN KEY (PropertyId) 
        REFERENCES Properties(Id) ON DELETE CASCADE
);

CREATE TABLE Appointments (
    Id INT PRIMARY KEY IDENTITY(1,1),
    PropertyId INT NOT NULL,
    ClientId INT NOT NULL,
    RealtorId INT NOT NULL,
    SlotStart DATETIME NOT NULL,
    SlotEnd DATETIME NOT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'new',
    Comment NVARCHAR(500),
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Appointments_Properties FOREIGN KEY (PropertyId) REFERENCES Properties(Id),
    CONSTRAINT FK_Appointments_Clients FOREIGN KEY (ClientId) REFERENCES Users(Id),
    CONSTRAINT FK_Appointments_Realtors FOREIGN KEY (RealtorId) REFERENCES Users(Id),
    CONSTRAINT CHK_SlotTime CHECK (SlotEnd > SlotStart)
);

CREATE TABLE RealtorSchedule (
    Id INT PRIMARY KEY IDENTITY(1,1),
    RealtorId INT NOT NULL,
    SlotStart DATETIME NOT NULL,
    SlotEnd DATETIME NOT NULL,
    IsAvailable BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_RealtorSchedule_Users FOREIGN KEY (RealtorId) REFERENCES Users(Id),
    CONSTRAINT CHK_ScheduleTime CHECK (SlotEnd > SlotStart)
);

CREATE TABLE Favorites (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    PropertyId INT NOT NULL,
    AddedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Favorites_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_Favorites_Properties FOREIGN KEY (PropertyId) REFERENCES Properties(Id),
    CONSTRAINT UQ_Favorites_UserProperty UNIQUE (UserId, PropertyId)
);

CREATE TABLE Reviews (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    PropertyId INT,
    RealtorId INT,
    Rating INT NOT NULL,
    Comment NVARCHAR(1000),
    IsApproved BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Reviews_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_Reviews_Properties FOREIGN KEY (PropertyId) REFERENCES Properties(Id),
    CONSTRAINT FK_Reviews_Realtors FOREIGN KEY (RealtorId) REFERENCES Users(Id),
    CONSTRAINT CHK_Rating CHECK (Rating >= 1 AND Rating <= 5),
    CONSTRAINT CHK_ReviewTarget CHECK (PropertyId IS NOT NULL OR RealtorId IS NOT NULL)
);


---

## ФАЙЛ 6: `06_TECHNICAL_STACK.md`

```markdown
# Technical Stack

## DEVELOPMENT TOOLS

| Tool | Purpose | Version |
|------|---------|---------|
| Visual Studio 2022 | IDE | Community or higher |
| SQL Server Management Studio | Database management | Latest |
| SQL Server Express LocalDB | Database | v15.0+ |
| Git | Version control | Latest |

## PROGRAMMING LANGUAGES AND FRAMEWORKS

| Component | Technology | Version |
|-----------|------------|---------|
| Language | C# | 10.0 or 11.0 |
| Framework | .NET | 6.0 or 8.0 |
| UI Framework | WPF | Latest |
| Markup | XAML | 2009 |
| ORM | Entity Framework Core | 8.0 |

## NUGET PACKAGES

```xml
<!-- MVVM Framework -->
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />

<!-- Entity Framework Core -->
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0" />

<!-- Password Hashing -->
<PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />

<!-- Charts -->
<PackageReference Include="LiveCharts.Wpf" Version="0.9.7" />

<!-- UI Controls -->
<PackageReference Include="MahApps.Metro" Version="2.4.10" />

<!-- Notifications -->
<PackageReference Include="Notification.Wpf" Version="7.0.0" />

<!-- JSON -->
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />

<!-- Dependency Injection -->
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />

<!-- Validation -->
<PackageReference Include="FluentValidation" Version="11.9.0" />

<!-- Configuration -->
<PackageReference Include="System.Configuration.ConfigurationManager" Version="8.0.0" />

---

# Project Architecture
ФАЙЛ 7: 07_PROJECT_ARCHITECTURE.md
## SOLUTION STRUCTURE
RealEstateAgency.sln
│
├── RealEstateAgency.Core/
│ ├── Models/
│ │ ├── User.cs
│ │ ├── Property.cs
│ │ ├── PropertyImage.cs
│ │ ├── Appointment.cs
│ │ ├── RealtorSchedule.cs
│ │ ├── Favorite.cs
│ │ └── Review.cs
│ ├── Interfaces/
│ │ ├── IRepository.cs
│ │ └── IUnitOfWork.cs
│ ├── Common/
│ │ ├── BaseEntity.cs
│ │ └── Result.cs
│ └── Exceptions/
│ └── BusinessException.cs
│
├── RealEstateAgency.Data/
│ ├── Context/
│ │ └── AppDbContext.cs
│ ├── Repositories/
│ │ ├── Repository.cs
│ │ └── UnitOfWork.cs
│ ├── Configurations/
│ │ └── *.cs
│ └── Migrations/
│ └── *.cs
│
├── RealEstateAgency.Services/
│ ├── Interfaces/
│ │ ├── IAuthService.cs
│ │ ├── IPropertyService.cs
│ │ ├── IAppointmentService.cs
│ │ ├── IFavoriteService.cs
│ │ ├── IReviewService.cs
│ │ └── IStatisticsService.cs
│ ├── AuthService.cs
│ ├── PropertyService.cs
│ ├── AppointmentService.cs
│ ├── FavoriteService.cs
│ ├── ReviewService.cs
│ ├── StatisticsService.cs
│ └── NotificationService.cs
│
└── RealEstateAgency.Wpf/
├── App.xaml
├── App.xaml.cs
├── MainWindow.xaml
├── MainWindow.xaml.cs
├── Views/
│ ├── LoginView.xaml
│ ├── RegisterView.xaml
│ ├── PropertyListView.xaml
│ ├── PropertyDetailView.xaml
│ ├── FilterWindow.xaml
│ ├── AppointmentCalendarView.xaml
│ ├── FavoritesView.xaml
│ ├── ProfileView.xaml
│ ├── AdminPanelView.xaml
│ ├── RealtorDashboardView.xaml
│ └── StatisticsView.xaml
├── ViewModels/
│ ├── ViewModelBase.cs
│ ├── LoginViewModel.cs
│ ├── RegisterViewModel.cs
│ ├── PropertyListViewModel.cs
│ ├── PropertyDetailViewModel.cs
│ ├── FilterViewModel.cs
│ ├── AppointmentCalendarViewModel.cs
│ ├── FavoritesViewModel.cs
│ ├── ProfileViewModel.cs
│ ├── AdminPanelViewModel.cs
│ ├── RealtorDashboardViewModel.cs
│ └── StatisticsViewModel.cs
├── Commands/
│ ├── RelayCommand.cs
│ └── AsyncRelayCommand.cs
├── Converters/
│ ├── PriceToStringConverter.cs
│ ├── BoolToVisibilityConverter.cs
│ ├── DateTimeToStringConverter.cs
│ ├── RatingToStarsConverter.cs
│ └── InverseBoolConverter.cs
├── Controls/
│ ├── PropertyCardControl.xaml
│ ├── PropertyCardControl.xaml.cs
│ ├── ImageCarouselControl.xaml
│ ├── ImageCarouselControl.xaml.cs
│ └── RatingControl.xaml
├── Resources/
│ ├── Styles/
│ │ ├── Buttons.xaml
│ │ ├── TextBoxes.xaml
│ │ └── Common.xaml
│ ├── Templates/
│ │ └── DataTemplates.xaml
│ ├── Languages/
│ │ ├── lang.ru.xaml
│ │ └── lang.en.xaml
│ ├── Themes/
│ │ ├── LightTheme.xaml
│ │ └── DarkTheme.xaml
│ └── Icons/
│ └── *.xaml
├── Helpers/
│ ├── DialogService.cs
│ ├── NavigationService.cs
│ ├── ThemeManager.cs
│ └── LanguageManager.cs
└── Models/
└── Session.cs


## LAYER DESCRIPTIONS

| Layer | Project | Responsibility |
|-------|---------|----------------|
| Core | RealEstateAgency.Core | Entities, interfaces, base classes |
| Data | RealEstateAgency.Data | Database access, EF Core, repositories |
| Services | RealEstateAgency.Services | Business logic, validation |
| Presentation | RealEstateAgency.Wpf | UI, ViewModels, Views |

## DEPENDENCY INJECTION SETUP

**File: App.xaml.cs**

```csharp
using System;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RealEstateAgency.Data.Context;
using RealEstateAgency.Data.Repositories;
using RealEstateAgency.Core.Interfaces;
using RealEstateAgency.Services;
using RealEstateAgency.Services.Interfaces;

namespace RealEstateAgency.Wpf
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }
        
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            var services = new ServiceCollection();
            ConfigureServices(services);
            
            ServiceProvider = services.BuildServiceProvider();
            
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        
        private void ConfigureServices(IServiceCollection services)
        {
            // Database Context
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    System.Configuration.ConfigurationManager
                        .ConnectionStrings["RealEstateConnection"]
                        .ConnectionString));
            
            // Repositories
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            
            // Services
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IPropertyService, PropertyService>();
            services.AddScoped<IAppointmentService, AppointmentService>();
            services.AddScoped<IFavoriteService, FavoriteService>();
            services.AddScoped<IReviewService, ReviewService>();
            services.AddScoped<IStatisticsService, StatisticsService>();
            
            // Views
            services.AddScoped<MainWindow>();
            
            // Helpers
            services.AddSingleton<Helpers.DialogService>();
            services.AddSingleton<Helpers.NavigationService>();
        }
    }
}

PROJECT REFERENCES
RealEstateAgency.Wpf
    ├── RealEstateAgency.Core
    ├── RealEstateAgency.Data
    └── RealEstateAgency.Services

RealEstateAgency.Data
    └── RealEstateAgency.Core

RealEstateAgency.Services
    ├── RealEstateAgency.Core
    └── RealEstateAgency.Data

RealEstateAgency.Core
    └── (no dependencies)

    
---

## ФАЙЛ 8: `08_MVVM_STRUCTURE.md`

```markdown
# MVVM Structure

## MODEL-VIEW-VIEWMODEL PATTERN

| Component | Location | Responsibility |
|-----------|----------|----------------|
| Model | Core/Services | Business entities, data access |
| View | Wpf/Views | XAML UI, visual elements |
| ViewModel | Wpf/ViewModels | Mediator, commands, data binding |

## DATA BINDING TYPES

| Type | Syntax | Use Case |
|------|--------|----------|
| One-Way | `{Binding Property}` | Display data |
| Two-Way | `{Binding Property, Mode=TwoWay}` | Input fields |
| One-Time | `{Binding Property, Mode=OneTime}` | Static data |

## COMMANDS

### RelayCommand Implementation

```csharp
using System;
using System.Windows.Input;

namespace RealEstateAgency.Wpf.Commands
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;
        
        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }
        
        public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) != false;
        
        public void Execute(object parameter) => _execute(parameter);
        
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}

Using CommunityToolkit.Mvvm (Recommended)
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public partial class PropertyViewModel : ObservableObject
{
    [ObservableProperty]
    private string _searchQuery;
    
    [RelayCommand]
    private void Search()
    {
        // Synchronous command
    }
    
    [RelayCommand]
    private async Task LoadPropertiesAsync()
    {
        // Async command
        await Task.Delay(100);
    }
}

CONVERTERS
PriceToStringConverter.csusing System;
using System.Globalization;
using System.Windows.Data;

namespace RealEstateAgency.Wpf.Converters
{
    public class PriceToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal price)
            {
                return price.ToString("N0", culture) + " ₽";
            }
            return "0 ₽";
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && decimal.TryParse(str.Replace(" ₽", ""), NumberStyles.Number, culture, out var result))
            {
                return result;
            }
            return 0m;
        }
    }
}

BoolToVisibilityConverter.cs
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RealEstateAgency.Wpf.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }
}VIEWMODEL EXAMPLE
PropertyListViewModel.cs

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RealEstateAgency.Core.Models;
using RealEstateAgency.Services.Interfaces;

namespace RealEstateAgency.Wpf.ViewModels
{
    public partial class PropertyListViewModel : ViewModelBase
    {
        private readonly IPropertyService _propertyService;
        
        [ObservableProperty]
        private ObservableCollection<Property> _properties;
        
        [ObservableProperty]
        private string _searchQuery;
        
        [ObservableProperty]
        private decimal? _minPrice;
        
        [ObservableProperty]
        private decimal? _maxPrice;
        
        [ObservableProperty]
        private bool _isLoading;
        
        public PropertyListViewModel(IPropertyService propertyService)
        {
            _propertyService = propertyService;
            _properties = new ObservableCollection<Property>();
        }
        
        public override async void OnNavigatedTo(object parameter)
        {
            await LoadPropertiesCommand.ExecuteAsync(null);
        }
        
        [RelayCommand]
        private async Task LoadPropertiesAsync()
        {
            IsLoading = true;
            var properties = await _propertyService.GetFilteredAsync(
                MinPrice, MaxPrice, null, null, null, SearchQuery, null);
            
            Properties.Clear();
            foreach (var prop in properties)
            {
                Properties.Add(prop);
            }
            IsLoading = false;
        }
    }
}

SESSION SERVICE
Session.cs

using RealEstateAgency.Core.Models;

namespace RealEstateAgency.Wpf.Models
{
    public static class Session
    {
        public static User? CurrentUser { get; set; }
        
        public static bool IsLoggedIn => CurrentUser != null;
        public static bool IsAdmin => CurrentUser?.Role == "admin";
        public static bool IsRealtor => CurrentUser?.Role == "realtor";
        public static bool IsClient => CurrentUser?.Role == "client";
    }
}


---

## ФАЙЛ 9: `09_DESIGN_SPECIFICATIONS.md`

```markdown
# Design Specifications

## COLOR PALETTE

### Light Theme

| Color | Hex Code | Usage |
|-------|----------|-------|
| Primary | #D4A5A5 | Buttons, accents |
| Primary Hover | #C49595 | Button hover |
| Background | #F5E6E6 | Main background |
| Surface | #FFFFFF | Cards, panels |
| Text Primary | #4A4A4A | Main text |
| Text Secondary | #7A7A7A | Secondary text |
| Accent | #8B6F6F | Secondary buttons |
| Success | #7CB342 | Success messages |
| Warning | #FFA726 | Warning messages |
| Error | #EF5350 | Error messages |

### Dark Theme

| Color | Hex Code | Usage |
|-------|----------|-------|
| Background | #1E1E1E | Main background |
| Surface | #2D2D2D | Cards, panels |
| Text Primary | #E0E0E0 | Main text |
| Text Secondary | #A0A0A0 | Secondary text |

## TYPOGRAPHY

| Element | Font Size | Weight |
|---------|-----------|--------|
| Heading 1 | 28pt | Bold |
| Heading 2 | 20pt | SemiBold |
| Heading 3 | 16pt | SemiBold |
| Body Large | 14pt | Regular |
| Body | 12pt | Regular |
| Body Small | 10pt | Regular |
| Caption | 9pt | Light |

**Font Family:** Segoe UI (primary), Arial (fallback)

## SPACING AND LAYOUT

| Spacing Type | Value |
|--------------|-------|
| Small | 8px |
| Medium | 16px |
| Large | 24px |
| XL | 32px |

## COMPONENT SPECIFICATIONS

### Buttons

| Property | Primary | Secondary |
|----------|---------|-----------|
| Background | #D4A5A5 | Transparent |
| Text Color | White | #8B6F6F |
| Height | 40px | 40px |
| Min Width | 120px | 120px |
| Border Radius | 4px | 4px |
| Border | None | 2px solid #8B6F6F |

### Input Fields

| Property | Value |
|----------|-------|
| Height | 40px |
| Border | 1px solid #CCCCCC |
| Border Radius | 4px |
| Padding | 8px 12px |
| Focus Border | 2px solid #D4A5A5 |
| Error Border | 2px solid #EF5350 |

### Cards

| Property | Value |
|----------|-------|
| Width | 300px (grid) |
| Min Height | 350px |
| Background | White |
| Border Radius | 8px |
| Shadow | 0 2px 8px rgba(0,0,0,0.1) |
| Padding | 16px |

## ANIMATIONS

| Animation | Duration | Easing |
|-----------|----------|--------|
| Page Navigation | 300ms | EaseInOut |
| Button Hover | 200ms | EaseOut |
| Card Hover | 250ms | EaseOut |
| Loading Spinner | 1000ms | Linear |
| Image Carousel | 400ms | EaseInOut |

## RESOURCE DICTIONARIES

### LightTheme.xaml

```xaml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    
    <Color x:Key="PrimaryColor">#D4A5A5</Color>
    <Color x:Key="BackgroundColor">#F5E6E6</Color>
    <Color x:Key="SurfaceColor">#FFFFFF</Color>
    <Color x:Key="TextPrimaryColor">#4A4A4A</Color>
    
    <SolidColorBrush x:Key="PrimaryBrush" Color="{StaticResource PrimaryColor}"/>
    <SolidColorBrush x:Key="BackgroundBrush" Color="{StaticResource BackgroundColor}"/>
    <SolidColorBrush x:Key="SurfaceBrush" Color="{StaticResource SurfaceColor}"/>
    <SolidColorBrush x:Key="TextPrimaryBrush" Color="{StaticResource TextPrimaryColor}"/>
    
</ResourceDictionary>

DarkTheme.xaml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    
    <Color x:Key="PrimaryColor">#D4A5A5</Color>
    <Color x:Key="BackgroundColor">#1E1E1E</Color>
    <Color x:Key="SurfaceColor">#2D2D2D</Color>
    <Color x:Key="TextPrimaryColor">#E0E0E0</Color>
    
    <SolidColorBrush x:Key="PrimaryBrush" Color="{StaticResource PrimaryColor}"/>
    <SolidColorBrush x:Key="BackgroundBrush" Color="{StaticResource BackgroundColor}"/>
    <SolidColorBrush x:Key="SurfaceBrush" Color="{StaticResource SurfaceColor}"/>
    <SolidColorBrush x:Key="TextPrimaryBrush" Color="{StaticResource TextPrimaryColor}"/>
    
</ResourceDictionary>


---

## ФАЙЛ 10: `10_DEVELOPMENT_PLAN_PART1.md`

```markdown
# Development Plan - Part 1 (Phases 1-7)

## PHASE 1: PROJECT SETUP (Days 1-2)

### Step 1.1: Create Solution and Projects

**Tasks:**
1. Open Visual Studio 2022
2. Create new WPF Application (.NET 6 or 8)
3. Name: RealEstateAgency.Wpf
4. Add Class Library projects: Core, Data, Services
5. Set WPF as startup project

**Verification:**
```bash
dotnet build RealEstateAgency.sln

Expected: Build succeeded, 0 errors
Step 1.2: Configure Project References
Tasks:
Wpf → Core, Data, Services
Data → Core
Services → Core, Data
Step 1.3: Install NuGet Packages
Tasks:

Install-Package CommunityToolkit.Mvvm -Version 8.2.2
Install-Package Microsoft.EntityFrameworkCore.SqlServer -Version 8.0.0
Install-Package Microsoft.EntityFrameworkCore.Tools -Version 8.0.0
Install-Package BCrypt.Net-Next -Version 4.0.3
Install-Package LiveCharts.Wpf -Version 0.9.7
Install-Package MahApps.Metro -Version 2.4.10
Install-Package Notification.Wpf -Version 7.0.0
Install-Package Newtonsoft.Json -Version 13.0.3
Install-Package Microsoft.Extensions.DependencyInjection -Version 8.0.0
Install-Package FluentValidation -Version 11.9.0
Install-Package System.Configuration.ConfigurationManager -Version 8.0.0

Step 1.4: Create Folder Structure
Tasks:
Create all folders as shown in Architecture section
PHASE 1 CHECKLIST:
Solution created with 4 projects
Project references configured
All NuGet packages installed
Folder structure created
Solution builds without errors
PHASE 2: DATABASE AND ENTITIES (Days 2-4)
Step 2.1: Create Entity Classes
Tasks:
Create all model classes in Core/Models/
Verification:
dotnet build RealEstateAgency.Core

Step 2.2: Create DbContext
Tasks:
Create AppDbContext.cs in Data/Context/
Step 2.3: Configure Connection String
Tasks:
Add connection string to App.config
Step 2.4: Create Initial Migration
Tasks:Add-Migration InitialCreate -Context AppDbContext

Step 2.5: Update Database
Tasks:Update-Database -Context AppDbContext


Step 2.6: Seed Initial Data
Tasks:
Create SeedData.cs with admin user
PHASE 2 CHECKLIST:
All entity classes created
DbContext configured
Connection string set up
Initial migration created
Database created
Tables visible in SSMS
Initial data seeded
PHASE 3: DATA ACCESS LAYER (Days 3-5)
Step 3.1: Create Repository Interface
Tasks:
Create IRepository.cs in Core/Interfaces/
Step 3.2: Create Unit of Work Interface
Tasks:
Create IUnitOfWork.cs in Core/Interfaces/
Step 3.3: Implement Generic Repository
Tasks:
Create Repository.cs in Data/Repositories/
Step 3.4: Implement Unit of Work
Tasks:
Create UnitOfWork.cs in Data/Repositories/
PHASE 3 CHECKLIST:
IRepository interface created
IUnitOfWork interface created
Repository<T> class implemented
UnitOfWork class implemented
Build successful
PHASE 4: SERVICE LAYER (Days 5-7)
Step 4.1: Create Service Interfaces
Tasks:
Create all service interfaces in Services/Interfaces/
Step 4.2: Implement AuthService
Tasks:
Create AuthService.cs with login/register functionality
Step 4.3: Implement PropertyService
Tasks:
Create PropertyService.cs with CRUD operations
PHASE 4 CHECKLIST:
All service interfaces created
AuthService implemented
PropertyService implemented
Password hashing works
Filtering logic works
Build successful
PHASE 5: MVVM FOUNDATION (Days 6-8)
Step 5.1: Create ViewModelBase
Tasks:
Create ViewModelBase.cs in Wpf/ViewModels/
Step 5.2: Setup Dependency Injection
Tasks:
Update App.xaml.cs with DI configuration
Step 5.3: Create Session Service
Tasks:
Create Session.cs for user state management
PHASE 5 CHECKLIST:
ViewModelBase created
DI configured in App.xaml.cs
Session service created
Can resolve ViewModels from DI
PHASE 6: AUTHENTICATION UI (Days 8-9)
Step 6.1: Create LoginViewModel
Tasks:
Create LoginViewModel.cs with login logic
Step 6.2: Create LoginView
Tasks:
Create LoginView.xaml with UI elements
PHASE 6 CHECKLIST:
LoginViewModel created
LoginView created
Login functionality works
Role-based navigation works
Error messages display correctly
PHASE 7: PROPERTY CATALOG (Days 9-12)
Step 7.1: Create PropertyCardControl
Tasks:
Create PropertyCardControl.xaml as UserControl
Step 7.2: Create PropertyListViewModel
Tasks:
Create PropertyListViewModel.cs with data loading
Step 7.3: Create PropertyListView
Tasks:
Create PropertyListView.xaml with ItemsSource binding
PHASE 7 CHECKLIST:
PropertyCardControl created
PropertyListViewModel created
PropertyListView created
Data binding works
Properties load from database


---

## ФАЙЛ 11: `11_DEVELOPMENT_PLAN_PART2.md`

```markdown
# Development Plan - Part 2 (Phases 8-14)

## PHASE 8: PROPERTY DETAILS (Days 12-14)

### Step 8.1: Create ImageCarouselControl

**Tasks:**
- Create carousel UserControl
- Implement navigation buttons
- Bind to property images

### Step 8.2: Create PropertyDetailViewModel

**Tasks:**
- Load property by ID
- Load all images
- Handle favorite toggle

### Step 8.3: Create PropertyDetailView

**Tasks:**
- Design detail layout
- Integrate image carousel
- Add action buttons

**PHASE 8 CHECKLIST:**
- [ ] ImageCarouselControl created
- [ ] PropertyDetailView created
- [ ] Image loading from DB works
- [ ] Navigation buttons work
- [ ] Favorite button works

---

## PHASE 9: FILTERS AND SEARCH (Days 14-16)

### Step 9.1: Create FilterViewModel

**Tasks:**
- Create filter properties
- Implement ApplyFilters command
- Implement ResetFilters command

### Step 9.2: Create Basic Filters

**Tasks:**
- Add filter controls to header
- Bind to PropertyListViewModel

### Step 9.3: Create Advanced Filter Window

**Tasks:**
- Create FilterWindow.xaml
- Add all filter options
- Add hot filter presets

**PHASE 9 CHECKLIST:**
- [ ] FilterViewModel created
- [ ] Basic filters in header
- [ ] Advanced filter window
- [ ] Hot filters presets
- [ ] Filtering works correctly

---

## PHASE 10: APPOINTMENT SYSTEM (Days 16-18)

### Step 10.1: Create AppointmentCalendar

**Tasks:**
- Calendar control for date selection
- Time slot selection
- Booking form
- Confirmation dialog

### Step 10.2: Create AppointmentService

**Tasks:**
- Implement CRUD for appointments
- Check slot availability
- Send notifications

### Step 10.3: Create Appointment Views

**Tasks:**
- Client booking view
- Realtor schedule view
- Appointment list view

**PHASE 10 CHECKLIST:**
- [ ] AppointmentCalendar created
- [ ] Schedule management works
- [ ] Booking flow complete
- [ ] Notifications implemented

---

## PHASE 11: USER ACCOUNTS (Days 18-20)

### Step 11.1: Create Profile Views

**Tasks:**
- ProfileView for clients
- RealtorDashboard for realtors
- AdminPanel for administrators

### Step 11.2: Implement Favorites

**Tasks:**
- FavoritesView
- Add/remove functionality
- Persist to database

### Step 11.3: Implement Admin Features

**Tasks:**
- User management
- Review moderation
- Statistics dashboard

**PHASE 11 CHECKLIST:**
- [ ] ProfileView created
- [ ] Favorites functionality works
- [ ] Admin panel complete
- [ ] Realtor dashboard complete
- [ ] All roles work correctly

---

## PHASE 12: LOCALIZATION AND THEMES (Days 20-21)

### Step 12.1: Create Language Resources

**Tasks:**
- Create lang.ru.xaml
- Create lang.en.xaml
- Add all UI strings

### Step 12.2: Create Theme Dictionaries

**Tasks:**
- Create LightTheme.xaml
- Create DarkTheme.xaml
- Define all colors and styles

### Step 12.3: Implement Switchers

**Tasks:**
- Language selector in header
- Theme toggle in header
- Persist user preferences

**PHASE 12 CHECKLIST:**
- [ ] Language resources created
- [ ] Language switcher works
- [ ] Light theme created
- [ ] Dark theme created
- [ ] Theme switcher works

---

## PHASE 13: STATISTICS AND CHARTS (Days 21-22)

### Step 13.1: Integrate LiveCharts

**Tasks:**
- Add LiveCharts package
- Create chart controls
- Bind data from StatisticsService

### Step 13.2: Create StatisticsViewModel

**Tasks:**
- Aggregate data for charts
- Implement filtering by date
- Calculate metrics

### Step 13.3: Create Statistics Views

**Tasks:**
- Admin statistics dashboard
- Realtor personal statistics
- Chart types: bar, line, pie

**PHASE 13 CHECKLIST:**
- [ ] LiveCharts integrated
- [ ] StatisticsViewModel created
- [ ] Charts display correctly
- [ ] Data updates in real-time

---

## PHASE 14: POLISH AND TESTING (Days 22-24)

### Step 14.1: Add Error Handling

**Tasks:**
- Try-catch blocks for all database operations
- User-friendly error messages
- Error logging to file

### Step 14.2: Add Animations

**Tasks:**
- Page transitions
- Button hover effects
- Loading animations

### Step 14.3: Final Testing

**Test Scenarios:**
1. Login with all three roles
2. Browse and filter properties
3. Add property to favorites
4. Schedule appointment
5. Submit review
6. View statistics
7. Switch language
8. Switch theme

### Step 14.4: Create Custom Assets

**Tasks:**
- Design application icon
- Create custom cursor
- Add splash screen

**PHASE 14 CHECKLIST:**
- [ ] Error handling complete
- [ ] All features tested
- [ ] No critical bugs
- [ ] Performance acceptable
- [ ] Documentation complete
- [ ] Custom icon and cursor added

---

ФАЙЛ 12: 12_VERIFICATION_CHECKLISTS.md

# Verification Checklists

## GENERAL CHECKLIST (Use after each phase)

### Compilation
- [ ] Project builds without errors
- [ ] No critical warnings
- [ ] All references resolved

### Functionality
- [ ] Features in this phase work correctly
- [ ] No runtime exceptions
- [ ] UI responds to user input
- [ ] Data saves correctly
- [ ] Data loads correctly

### Database
- [ ] Tables created/updated
- [ ] Relationships work
- [ ] Constraints enforced
- [ ] Indexes created

### Code Quality
- [ ] Follows MVVM pattern
- [ ] No code-behind in Views (except necessary)
- [ ] Proper error handling
- [ ] Meaningful variable names
- [ ] Comments for complex logic

### Testing
- [ ] Manual testing completed
- [ ] Edge cases tested
- [ ] Error scenarios tested
- [ ] Performance acceptable

---

## PRE-SUBMISSION CHECKLIST

### Functionality
- [ ] All three roles work (Admin, Realtor, Client)
- [ ] Login/Logout works
- [ ] Property CRUD operations work
- [ ] Search and filters work
- [ ] Appointments can be scheduled
- [ ] Favorites work
- [ ] Reviews can be submitted
- [ ] Statistics display correctly

### UI/UX
- [ ] All screens render correctly
- [ ] Responsive to window resize
- [ ] Language switching works
- [ ] Theme switching works
- [ ] No UI glitches
- [ ] Loading indicators show
- [ ] Error messages clear

### Database
- [ ] All tables created
- [ ] All relationships work
- [ ] Data persists after restart
- [ ] Images stored correctly
- [ ] No data loss

### Code
- [ ] MVVM pattern followed
- [ ] No hardcoded strings
- [ ] Proper separation of concerns
- [ ] Code documented
- [ ] No memory leaks

### Performance
- [ ] Startup time < 3 seconds
- [ ] Property list loads < 2 seconds
- [ ] No UI freezing
- [ ] Memory usage reasonable

### Security
- [ ] Passwords hashed
- [ ] SQL injection prevented
- [ ] Role-based access enforced
- [ ] No sensitive data in logs

---

## PHASE-SPECIFIC CHECKLISTS

### Phase 1-2: Setup & Database
- [ ] Solution structure correct
- [ ] All NuGet packages installed
- [ ] Database created successfully
- [ ] All 7 tables exist
- [ ] Seed data inserted

### Phase 3-4: Data Access & Services
- [ ] Repository pattern implemented
- [ ] Unit of Work working
- [ ] All services injectable
- [ ] Password hashing functional
- [ ] CRUD operations tested

### Phase 5-6: MVVM & Auth
- [ ] DI container configured
- [ ] Login works for all roles
- [ ] Role-based navigation works
- [ ] Session management works

### Phase 7-9: Catalog & Filters
- [ ] Property cards display correctly
- [ ] Image carousel works
- [ ] All filters functional
- [ ] Search returns correct results

### Phase 10-11: Appointments & Accounts
- [ ] Calendar displays correctly
- [ ] Booking flow complete
- [ ] Favorites persist to DB
- [ ] All role dashboards work

### Phase 12-14: Polish
- [ ] Both languages work
- [ ] Both themes work
- [ ] Charts display data
- [ ] No unhandled exceptions
- [ ] All documentation complete

---

ФАЙЛ 13: 13_TROUBLESHOOTING_GUIDE.md
# Troubleshooting Guide

## COMMON ERRORS AND SOLUTIONS

| Error | Cause | Solution |
|-------|-------|----------|
| "Type or namespace not found" | Missing using/package | Add using statement, install NuGet |
| "Cannot create instance" | DI not configured | Register in ConfigureServices |
| "BindingExpression path error" | Wrong property name | Check spelling, verify DataContext |
| "Database connection failed" | LocalDB not installed | Install SQL Server LocalDB |
| "Migration failed" | DbContext issue | Delete migrations, recreate |
| "XAML parsing failed" | Invalid XAML | Check syntax, verify resources |
| "NullReferenceException" | Null object | Check for null, initialize collections |

---

## DETAILED SOLUTIONS

### Error: "The type or namespace name could not be found"

**Causes:**
- Missing using statement
- NuGet package not installed
- Project reference missing

**Solutions:**
1. Add missing using statement at top of file
2. Install required NuGet package
3. Add project reference
4. Rebuild solution

---

### Error: "Cannot create instance of the type"

**Causes:**
- ViewModel has no parameterless constructor
- Dependency not registered in DI container
- Exception in constructor

**Solutions:**
1. Ensure ViewModel can be resolved from DI
2. Register all dependencies in ConfigureServices
3. Check constructor for exceptions
4. Use [ObservableObject] from CommunityToolkit

---

### Error: "BindingExpression path error"

**Causes:**
- Property name misspelled
- DataContext not set
- Property doesn't exist

**Solutions:**
1. Check property name spelling (case-sensitive)
2. Verify DataContext is set in View
3. Check Output window for binding errors
4. Use `{Binding Path=PropertyName, FallbackValue='default'}`

---

### Error: "Database connection failed"

**Causes:**
- LocalDB not installed
- Connection string incorrect
- Permissions issue

**Solutions:**
1. Install SQL Server Express LocalDB
2. Verify connection string in App.config
3. Run Visual Studio as Administrator
4. Check SQL Server service is running

---

### Error: "Migration failed"

**Causes:**
- DbContext configuration issue
- Model changed but migration not updated
- Database in use

**Solutions:**
1. Delete Migrations folder
2. Delete database
3. Add-Migration InitialCreate again
4. Update-Database
5. Close all connections to database

---

### Error: "XAML parsing failed"

**Causes:**
- Invalid XAML syntax
- Resource not found
- Namespace issue

**Solutions:**
1. Check XAML syntax (missing closing tags)
2. Verify resources are defined
3. Check namespace declarations
4. Rebuild project

---

### Error: "Object reference not set to an instance of an object"

**Causes:**
- Null reference
- Property not initialized
- Dependency not injected

**Solutions:**
1. Check for null before accessing properties
2. Initialize collections in constructor
3. Verify DI registration
4. Use null-conditional operator (?.)

---

## DEBUGGING TIPS

### Enable Detailed Output

**Steps:**
1. Tools → Options → Projects and Solutions → Build and Run
2. Set MSBuild project build output verbosity to "Detailed"

### Check Output Window

**Steps:**
1. View → Output
2. Select "Build" or "Debug" from dropdown
3. Look for errors and warnings

### Use Breakpoints

**Steps:**
1. Click left margin to set breakpoint
2. F5 to start debugging
3. F10 to step over
4. F11 to step into
5. Check variable values in Locals window

### XAML Debugging

**Steps:**
1. Debug → Windows → Live Visual Tree
2. Use Live Property Explorer
3. Check binding errors in Output window

---

## PERFORMANCE OPTIMIZATION

| Issue | Solution |
|-------|----------|
| Slow loading | Use async/await, implement pagination |
| High memory | Dispose DbContext, clear collections |
| UI freezing | Move operations to background threads |
| Large images | Compress images before storing |
| Slow queries | Add indexes, optimize LINQ |

---

## GIT WORKFLOW

```bash
# After completing each phase
git add .
git commit -m "Phase X: [Description]"
git push origin main

# Create branch for new feature
git checkout -b feature/phase-X

# After completion
git checkout main
git merge feature/phase-X
git push origin main


---

## ФАЙЛ 14: `14_FINAL_NOTES_RESOURCES.md`

```markdown
# Final Notes and Resources

## BEFORE SUBMISSION

### Clean Solution

**Steps:**
1. Build → Clean Solution
2. Delete bin and obj folders
3. Rebuild

### Remove Sensitive Data

**Tasks:**
- Remove real passwords
- Use placeholder connection strings
- Remove personal information

### Documentation

**Tasks:**
- README.md with setup instructions
- Comment complex code
- Document database schema

### Testing

**Tasks:**
- Test all features
- Test with different roles
- Test error scenarios
- Get second opinion

### Version Control

**Tasks:**
- Commit all changes
- Push to repository
- Tag release version

---

## USEFUL RESOURCES

### Documentation

| Resource | URL |
|----------|-----|
| WPF Documentation | https://docs.microsoft.com/dotnet/desktop/wpf/ |
| EF Core Docs | https://docs.microsoft.com/ef/core/ |
| CommunityToolkit.Mvvm | https://docs.microsoft.com/dotnet/communitytoolkit/mvvm/ |
| WPF Tutorial | https://www.wpftutorial.net/ |

### Downloads

| Tool | URL |
|------|-----|
| SSMS | https://aka.ms/ssmsfullsetup |
| Visual Studio | https://visualstudio.microsoft.com/vs/community/ |
| SQL Server Express | https://www.microsoft.com/sql-server/sql-server-downloads |

### NuGet Packages

| Package | URL |
|---------|-----|
| CommunityToolkit.Mvvm | https://www.nuget.org/packages/CommunityToolkit.Mvvm |
| Entity Framework Core | https://www.nuget.org/packages/Microsoft.EntityFrameworkCore |
| LiveCharts.Wpf | https://www.nuget.org/packages/LiveCharts.Wpf |
| MahApps.Metro | https://www.nuget.org/packages/MahApps.Metro |

---

## PROJECT TIMELINE

| Phase | Days | Total |
|-------|------|-------|
| Phase 1: Setup | 1-2 | 2 |
| Phase 2: Database | 2-4 | 4 |
| Phase 3: Data Access | 3-5 | 5 |
| Phase 4: Services | 5-7 | 7 |
| Phase 5: MVVM | 6-8 | 8 |
| Phase 6: Auth UI | 8-9 | 9 |
| Phase 7: Catalog | 9-12 | 12 |
| Phase 8: Details | 12-14 | 14 |
| Phase 9: Filters | 14-16 | 16 |
| Phase 10: Appointments | 16-18 | 18 |
| Phase 11: Accounts | 18-20 | 20 |
| Phase 12: Localization | 20-21 | 21 |
| Phase 13: Statistics | 21-22 | 22 |
| Phase 14: Polish | 22-24 | 24 |

**Estimated Total Development Time: 20-24 days**

---

## CONTACT AND SUPPORT

If you encounter issues:
1. Check this troubleshooting guide
2. Search error message on Stack Overflow
3. Check GitHub issues for relevant packages
4. Ask your instructor/mentor

---

**Good luck with your project! 🚀**

**END OF DOCUMENT**

ФАЙЛ 15: README.md
# Real Estate Agency - WPF Application

Desktop application for real estate agency built with WPF, MVVM, and Entity Framework Core.

## Features

- Three user roles: Administrator, Realtor, Client
- Property catalog with advanced filtering
- Image carousel for property photos
- Appointment scheduling system
- Favorites management
- Reviews and ratings
- Statistics and charts
- Bilingual interface (Russian/English)
- Light/Dark theme support

## Requirements

- Windows 10/11
- .NET 6.0 or .NET 8.0
- SQL Server Express LocalDB
- Visual Studio 2022

## Installation

1. Clone the repository
2. Open `RealEstateAgency.sln` in Visual Studio
3. Restore NuGet packages
4. Update database:
   ```powershell
   Update-Database

   Run the application
Default Credentials
Role
Login
Password
Admin
admin
admin123
Project Structure
12345
Technologies
WPF (.NET 6/8)
Entity Framework Core
MVVM Pattern
CommunityToolkit.Mvvm
SQL Server
LiveCharts