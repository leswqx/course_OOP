using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MSM.Models;
using MSM.Services.Interfaces;

namespace MSM.ViewModels;

public partial class HeaderViewModel : ViewModelBase
{
    private readonly INavigationService _nav;
    private readonly IServiceScopeFactory _scopeFactory;

    public bool IsLoggedIn    => Session.CurrentUser != null;
    public bool IsNotLoggedIn => Session.CurrentUser == null;

    public bool IsClientUser  => Session.CurrentUser?.Role == "client";

    public bool CanGoBack    => _nav.CanGoBack;
    public bool CanGoForward => _nav.CanGoForward;

    public bool IsHeroMode => _nav.CurrentViewModel is LandingViewModel;

    [ObservableProperty] private int _favoritesCount;

    public HeaderViewModel(INavigationService nav, IServiceScopeFactory scopeFactory)
    {
        _nav = nav;
        _scopeFactory = scopeFactory;
        nav.PropertyChanged += (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(INavigationService.CanGoBack):
                    OnPropertyChanged(nameof(CanGoBack));
                    GoBackNavCommand.NotifyCanExecuteChanged();
                    break;
                case nameof(INavigationService.CanGoForward):
                    OnPropertyChanged(nameof(CanGoForward));
                    GoForwardNavCommand.NotifyCanExecuteChanged();
                    break;
                case nameof(INavigationService.CurrentViewModel):
                    OnPropertyChanged(nameof(IsLoggedIn));
                    OnPropertyChanged(nameof(IsNotLoggedIn));
                    OnPropertyChanged(nameof(IsClientUser));
                    OnPropertyChanged(nameof(IsHeroMode));
                    _ = RefreshFavoritesCountAsync();
                    break;
            }
        };
    }

    private async Task RefreshFavoritesCountAsync()
    {
        if (!IsClientUser || Session.CurrentUser == null) { FavoritesCount = 0; return; }
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var favoriteService = scope.ServiceProvider.GetRequiredService<IFavoriteService>();
            var favs = await favoriteService.GetUserFavoritesAsync(Session.CurrentUser.Id);
            FavoritesCount = favs.Count();
        }
        catch { FavoritesCount = 0; }
    }

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void GoBackNav() => _nav.GoBack();

    [RelayCommand(CanExecute = nameof(CanGoForward))]
    private void GoForwardNav() => _nav.GoForward();

    [RelayCommand]
    private void GoHome() => _nav.GoHome();

    [RelayCommand]
    private void GoToAbout() => _nav.NavigateTo<AboutViewModel>();

    [RelayCommand]
    private void GoToCatalog()
    {
        if (Session.CurrentUser != null)
            _nav.NavigateTo<PropertyListViewModel>();
        else
            _nav.NavigateTo<LoginViewModel>();
    }

    [RelayCommand]
    private void GoToFavorites()
    {
        if (Session.CurrentUser != null)
            _nav.NavigateTo<FavoritesViewModel>();
        else
            _nav.NavigateTo<LoginViewModel>();
    }

    [RelayCommand]
    private void GoToLogin() => _nav.NavigateTo<LoginViewModel>();

    [RelayCommand]
    private void GoToProfile()
    {
        if (Session.CurrentUser?.Role == "realtor")
            _nav.NavigateTo<RealtorDashboardViewModel>("profile");
        else if (Session.CurrentUser?.Role == "admin")
            _nav.NavigateTo<AdminDashboardViewModel>("profile");
        else if (Session.CurrentUser != null)
            _nav.NavigateTo<ClientProfileViewModel>();
        else
            _nav.NavigateTo<LoginViewModel>();
    }

    [RelayCommand]
    private void Logout()
    {
        Session.Logout();
        _nav.GoHome();
    }
}
