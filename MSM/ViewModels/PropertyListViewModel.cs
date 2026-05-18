using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
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
    private readonly IFavoriteService _favoriteService;

    private const int PageSize = 15;
    private int _totalCount;
    private HashSet<int> _favoriteIds = new();

    [ObservableProperty] private ObservableCollection<PropertyCardViewModel> _properties = new();
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasNoResults;
    [ObservableProperty] private int _resultCount;
    [ObservableProperty] private string? _errorMessage;

    public IEnumerable<PropertyCardViewModel> DisplayedProperties => Properties;
    public bool CanShowMore   => _totalCount > Properties.Count;
    public int  ShowMoreCount => Math.Max(0, _totalCount - Properties.Count);

    // Фильтры
    [ObservableProperty] private bool _isFilterVisible;
    [ObservableProperty] private string _filterMinPrice = "";
    [ObservableProperty] private string _filterMaxPrice = "";
    [ObservableProperty] private string _filterMinArea = "";
    [ObservableProperty] private string _filterMaxArea = "";
    [ObservableProperty] private string _filterMinRooms = "";
    [ObservableProperty] private string _filterMaxRooms = "";
    [ObservableProperty] private string _filterMinBathrooms = "";
    [ObservableProperty] private string _filterMaxBathrooms = "";
    [ObservableProperty] private string _filterCity = "";
    [ObservableProperty] private string _filterDistrict = "";
    [ObservableProperty] private string _filterType = "";
    [ObservableProperty] private bool _filterMortgage;
    [ObservableProperty] private bool _filterRenovation;
    [ObservableProperty] private string _filterSortBy = "date_desc";

    // Список городов и районов для ComboBox (загружается из БД, первый элемент — пустой «Все»)
    public ObservableCollection<string> Cities         { get; } = new();
    public ObservableCollection<string> FilteredCities { get; } = new();
    public ObservableCollection<string> Districts      { get; } = new();

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

    public PropertyListViewModel(IPropertyService propertyService, INavigationService navigationService, IFavoriteService favoriteService)
    {
        _propertyService = propertyService;
        _navigationService = navigationService;
        _favoriteService = favoriteService;
    }

    public override async void OnNavigatedTo(object? parameter)
    {
        if (parameter is LandingSearchParams p)
        {
            FilterType     = p.PropertyType ?? "";
            FilterMaxPrice = p.MaxPrice ?? "";
            FilterMinRooms = p.MinRooms ?? "";
            FilterMaxRooms = p.MaxRooms ?? "";
        }
        await Task.Yield();
        try
        {
            await LoadCitiesAsync();
            await LoadDistrictsAsync();
            await LoadPropertiesAsync();
        }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }
    }

    private async Task LoadCitiesAsync()
    {
        try
        {
            var cities = await _propertyService.GetDistinctCitiesAsync();
            Cities.Clear();
            Cities.Add("");
            foreach (var c in cities.OrderBy(c => c)) Cities.Add(c);
            RefreshFilteredCities();
        }
        catch { }
    }

    private void RefreshFilteredCities()
    {
        FilteredCities.Clear();
        FilteredCities.Add("");
        var q = FilterCity?.Trim() ?? "";
        foreach (var c in Cities.Skip(1))
        {
            if (string.IsNullOrEmpty(q) || c.Contains(q, StringComparison.OrdinalIgnoreCase))
                FilteredCities.Add(c);
        }
    }

    partial void OnFilterCityChanged(string value) => RefreshFilteredCities();

    private async Task LoadDistrictsAsync()
    {
        try
        {
            var districts = await _propertyService.GetDistinctDistrictsAsync();
            Districts.Clear();
            Districts.Add("");
            foreach (var d in districts) Districts.Add(d);
        }
        catch { }
    }

    // Применяет все активные фильтры и поиск (первая страница)
    [RelayCommand]
    private async Task LoadPropertiesAsync()
    {
        IsLoading = true;
        HasNoResults = false;
        ErrorMessage = null;

        try
        {
            _favoriteIds.Clear();
            if (Session.IsClient && Session.CurrentUser != null)
            {
                var favs = await _favoriteService.GetUserFavoritesAsync(Session.CurrentUser.Id);
                foreach (var f in favs) _favoriteIds.Add(f.Id);
            }

            var minPrice      = ParseDecimal(FilterMinPrice);
            var maxPrice      = ParseDecimal(FilterMaxPrice);
            var minArea       = ParseDouble(FilterMinArea);
            var maxArea       = ParseDouble(FilterMaxArea);
            var minRooms      = ParseInt(FilterMinRooms);
            var maxRooms      = ParseInt(FilterMaxRooms);
            var minBathrooms  = ParseInt(FilterMinBathrooms);
            var maxBathrooms  = ParseInt(FilterMaxBathrooms);
            var city          = NullIfEmpty(FilterCity);
            var district      = NullIfEmpty(FilterDistrict);
            var propertyType  = NullIfEmpty(FilterType);
            var searchQuery   = NullIfEmpty(SearchQuery);
            bool? hasMortgage  = FilterMortgage   ? true : null;
            bool? hasRenovation = FilterRenovation ? true : null;
            var sortBy        = NullIfEmpty(FilterSortBy);

            _totalCount = await _propertyService.GetFilteredCountAsync(
                minPrice, maxPrice, minArea, maxArea,
                minRooms, maxRooms, minBathrooms, maxBathrooms,
                city, district, propertyType, searchQuery,
                hasMortgage, hasRenovation);

            var items = await _propertyService.GetFilteredAsync(
                minPrice, maxPrice, minArea, maxArea,
                minRooms, maxRooms, minBathrooms, maxBathrooms,
                city, district, propertyType, searchQuery,
                hasMortgage, hasRenovation,
                sortBy, skip: 0, take: PageSize);

            Properties.Clear();
            foreach (var p in items)
                Properties.Add(new PropertyCardViewModel(p) { IsFavorite = _favoriteIds.Contains(p.Id) });

            ResultCount  = _totalCount;
            HasNoResults = _totalCount == 0;
            OnPropertyChanged(nameof(DisplayedProperties));
            OnPropertyChanged(nameof(CanShowMore));
            OnPropertyChanged(nameof(ShowMoreCount));
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
    private async Task ShowMoreAsync()
    {
        if (!CanShowMore) return;
        IsLoading = true;
        try
        {
            var items = await _propertyService.GetFilteredAsync(
                minPrice:      ParseDecimal(FilterMinPrice),
                maxPrice:      ParseDecimal(FilterMaxPrice),
                minArea:       ParseDouble(FilterMinArea),
                maxArea:       ParseDouble(FilterMaxArea),
                minRooms:      ParseInt(FilterMinRooms),
                maxRooms:      ParseInt(FilterMaxRooms),
                minBathrooms:  ParseInt(FilterMinBathrooms),
                maxBathrooms:  ParseInt(FilterMaxBathrooms),
                city:          NullIfEmpty(FilterCity),
                district:      NullIfEmpty(FilterDistrict),
                propertyType:  NullIfEmpty(FilterType),
                searchQuery:   NullIfEmpty(SearchQuery),
                hasMortgage:   FilterMortgage   ? true : null,
                hasRenovation: FilterRenovation  ? true : null,
                sortBy:        NullIfEmpty(FilterSortBy),
                skip:          Properties.Count,
                take:          PageSize);

            foreach (var p in items)
                Properties.Add(new PropertyCardViewModel(p) { IsFavorite = _favoriteIds.Contains(p.Id) });

            OnPropertyChanged(nameof(DisplayedProperties));
            OnPropertyChanged(nameof(CanShowMore));
            OnPropertyChanged(nameof(ShowMoreCount));
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
    private async Task ResetFiltersAsync()
    {
        FilterMinPrice = FilterMaxPrice = "";
        FilterMinArea  = FilterMaxArea  = "";
        FilterMinRooms = FilterMaxRooms = "";
        FilterMinBathrooms = FilterMaxBathrooms = "";
        FilterCity     = FilterDistrict = FilterType = "";
        FilterMortgage = FilterRenovation = false;
        FilterSortBy   = "date_desc";
        await LoadPropertiesAsync();
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
