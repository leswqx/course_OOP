using CommunityToolkit.Mvvm.Input;
using MSM.Models;
using MSM.Services.Interfaces;

namespace MSM.ViewModels;

// Временный placeholder — будет заменён на AdminPanelViewModel / RealtorDashboardViewModel / PropertyListViewModel
public partial class HomeViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;

    public string WelcomeText =>
        $"Добро пожаловать, {Session.CurrentUser?.FullName}!  |  Роль: {GetRoleName(Session.CurrentUser?.Role)}";

    public HomeViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    [RelayCommand]
    private void Logout()
    {
        Session.Logout();
        _navigationService.NavigateTo<LoginViewModel>();
    }

    private static string GetRoleName(string? role) => role switch
    {
        "admin" => "Администратор",
        "realtor" => "Риелтор",
        "client" => "Клиент",
        _ => role ?? "Неизвестно"
    };
}
