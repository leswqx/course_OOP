using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MSM.Models;
using MSM.Services.Interfaces;

namespace MSM.ViewModels;

public partial class LandingViewModel : ViewModelBase
{
    private readonly INavigationService _nav;
    private bool _isDarkTheme;

    [ObservableProperty] private string _selectedPropertyType = "Квартира";
    [ObservableProperty] private string _selectedRooms = "Любое";
    [ObservableProperty] private string _maxPrice = "";
    [ObservableProperty] private string _currentLanguage = "RU";
    [ObservableProperty] private string _themeIcon = "🌙";

    public List<string> PropertyTypes { get; } = ["Квартира", "Дом", "Коммерческая"];
    public List<string> RoomOptions { get; } = ["Любое", "1", "2", "3", "4+"];

    public LandingViewModel(INavigationService nav)
    {
        _nav = nav;
    }

    [RelayCommand]
    private void ShowProperties()
    {
        if (Session.CurrentUser != null)
            _nav.NavigateTo<PropertyListViewModel>();
        else
            _nav.NavigateTo<LoginViewModel>();
    }

    [RelayCommand]
    private void GoToLogin() => _nav.NavigateTo<LoginViewModel>();

    [RelayCommand]
    private void SwitchTheme()
    {
        _isDarkTheme = !_isDarkTheme;
        ThemeIcon = _isDarkTheme ? "☀" : "🌙";

        var dicts = Application.Current.Resources.MergedDictionaries;
        var themeDict = dicts.FirstOrDefault(d => d.Source?.ToString().Contains("Theme") == true);
        if (themeDict != null) dicts.Remove(themeDict);
        dicts.Insert(0, new ResourceDictionary
        {
            Source = new Uri(_isDarkTheme
                ? "Resources/Themes/DarkTheme.xaml"
                : "Resources/Themes/LightTheme.xaml", UriKind.Relative)
        });
    }

    [RelayCommand]
    private void SwitchLanguage()
    {
        CurrentLanguage = CurrentLanguage == "RU" ? "EN" : "RU";

        var dicts = Application.Current.Resources.MergedDictionaries;
        var langDict = dicts.FirstOrDefault(d => d.Source?.ToString().Contains("lang.") == true);
        if (langDict != null) dicts.Remove(langDict);
        dicts.Add(new ResourceDictionary
        {
            Source = new Uri(CurrentLanguage == "RU"
                ? "Resources/Languages/lang.ru.xaml"
                : "Resources/Languages/lang.en.xaml", UriKind.Relative)
        });
    }
}
