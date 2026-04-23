using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MSM.Models;
using MSM.Services.Interfaces;

namespace MSM.ViewModels;

// ViewModel каталога: поиск, фильтры по цене/площади/комнатам/городу/типу/ипотеке/ремонту.
public partial class PropertyListViewModel : ViewModelBase
{
    private readonly IPropertyService _propertyService;
    private readonly INavigationService _navigationService;

    [ObservableProperty] private ObservableCollection<PropertyCardViewModel> _properties = new();
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasNoResults;
    [ObservableProperty] private string? _errorMessage;

    // Фильтры
    [ObservableProperty] private bool _isFilterVisible;
    [ObservableProperty] private string _filterMinPrice = "";
    [ObservableProperty] private string _filterMaxPrice = "";
    [ObservableProperty] private string _filterMinArea = "";
    [ObservableProperty] private string _filterMaxArea = "";
    [ObservableProperty] private string _filterRooms = "";
    [ObservableProperty] private string _filterCity = "";
    [ObservableProperty] private string _filterType = "";
    [ObservableProperty] private bool _filterMortgage;
    [ObservableProperty] private bool _filterRenovation;

    // Список городов для ComboBox (загружается из БД)
    public ObservableCollection<string> Cities { get; } = new();

    public string UserFullName => Session.CurrentUser?.FullName ?? "";
    public string UserRoleDisplay => Session.CurrentUser?.Role switch
    {
        "admin"   => "Администратор",
        "realtor" => "Риелтор",
        "client"  => "Клиент",
        _         => ""
    };
    public bool IsClient  => Session.IsClient;
    public bool IsRealtor => Session.CurrentUser?.Role == "realtor";
    public bool IsAdmin   => Session.CurrentUser?.Role == "admin";

    public PropertyListViewModel(IPropertyService propertyService, INavigationService navigationService)
    {
        _propertyService = propertyService;
        _navigationService = navigationService;
    }

    public override async void OnNavigatedTo(object? parameter)
    {
        await Task.Yield(); // дать UI отрисоваться перед загрузкой
        await LoadCitiesAsync();
        await LoadPropertiesAsync();
    }

    // Загружает города для выпадающего списка
    private async Task LoadCitiesAsync()
    {
        try
        {
            var cities = await _propertyService.GetDistinctCitiesAsync();
            Cities.Clear();
            foreach (var c in cities) Cities.Add(c);
        }
        catch { /* не критично — ComboBox просто будет пустым */ }
    }

    // Применяет все активные фильтры и поиск
    [RelayCommand]
    private async Task LoadPropertiesAsync()
    {
        IsLoading = true;
        HasNoResults = false;
        ErrorMessage = null;

        try
        {
            var items = await _propertyService.GetFilteredAsync(
                minPrice:     ParseDecimal(FilterMinPrice),
                maxPrice:     ParseDecimal(FilterMaxPrice),
                minArea:      ParseDouble(FilterMinArea),
                maxArea:      ParseDouble(FilterMaxArea),
                rooms:        ParseInt(FilterRooms),
                city:         NullIfEmpty(FilterCity),
                propertyType: NullIfEmpty(FilterType),
                searchQuery:  NullIfEmpty(SearchQuery),
                hasMortgage:  FilterMortgage ? true : null,
                hasRenovation: FilterRenovation ? true : null);

            Properties.Clear();
            foreach (var p in items)
                Properties.Add(new PropertyCardViewModel(p));

            HasNoResults = Properties.Count == 0;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка загрузки: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ToggleFilter() => IsFilterVisible = !IsFilterVisible;

    // Сбрасывает все фильтры и перезагружает список
    [RelayCommand]
    private void ResetFilters()
    {
        FilterMinPrice = FilterMaxPrice = "";
        FilterMinArea  = FilterMaxArea  = "";
        FilterRooms    = FilterCity     = FilterType = "";
        FilterMortgage = FilterRenovation = false;
        _ = LoadPropertiesAsync();
    }

    [RelayCommand]
    private void OpenDetail(PropertyCardViewModel card)
    {
        _navigationService.NavigateTo<PropertyDetailViewModel>(card.Id);
    }

    [RelayCommand]
    private void GoToProfile() => _navigationService.NavigateTo<ClientProfileViewModel>();

    [RelayCommand]
    private void GoToFavorites() => _navigationService.NavigateTo<FavoritesViewModel>();

    [RelayCommand]
    private void GoToMyAppointments() => _navigationService.NavigateTo<MyAppointmentsViewModel>();

    [RelayCommand]
    private void GoToRealtorDashboard() => _navigationService.NavigateTo<RealtorDashboardViewModel>();

    [RelayCommand]
    private void GoToAdminDashboard() => _navigationService.NavigateTo<AdminDashboardViewModel>();

    [RelayCommand]
    private void Logout()
    {
        Session.Logout();
        _navigationService.NavigateTo<LoginViewModel>();
    }

    // Вспомогательные методы парсинга
    private static decimal? ParseDecimal(string s) =>
        decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;

    private static double? ParseDouble(string s) =>
        double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;

    private static int? ParseInt(string s) =>
        int.TryParse(s, out var v) ? v : null;

    private static string? NullIfEmpty(string s) =>
        string.IsNullOrWhiteSpace(s) ? null : s;
}
