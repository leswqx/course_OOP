using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MSM.Models;
using MSM.Services.Interfaces;

namespace MSM.ViewModels;

// ViewModel каталога объектов недвижимости.
// Загружает список из БД, поддерживает поиск по строке.
public partial class PropertyListViewModel : ViewModelBase
{
    private readonly IPropertyService _propertyService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private ObservableCollection<PropertyCardViewModel> _properties = new();

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasNoResults;

    [ObservableProperty]
    private string? _errorMessage;

    public string UserFullName => Session.CurrentUser?.FullName ?? "";
    public string UserRoleDisplay => Session.CurrentUser?.Role switch
    {
        "admin"   => "Администратор",
        "realtor" => "Риелтор",
        "client"  => "Клиент",
        _         => ""
    };

    public PropertyListViewModel(IPropertyService propertyService, INavigationService navigationService)
    {
        _propertyService = propertyService;
        _navigationService = navigationService;
    }

    // Вызывается NavigationService автоматически при переходе на эту страницу
    public override void OnNavigatedTo(object? parameter)
    {
        _ = LoadPropertiesAsync();
    }

    [RelayCommand]
    private async Task LoadPropertiesAsync()
    {
        IsLoading = true;
        HasNoResults = false;
        ErrorMessage = null;

        try
        {
            var items = await _propertyService.GetFilteredAsync(
                searchQuery: string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery);

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
    private void Logout()
    {
        Session.Logout();
        _navigationService.NavigateTo<LoginViewModel>();
    }
}
