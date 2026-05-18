using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using MSM.Models;
using MSM.Services.Interfaces;

namespace MSM.ViewModels;

public partial class AboutViewModel : ViewModelBase
{
    private const string MapUrl = "https://yandex.com/maps/157/minsk/house/Zk4YcwFkT0IHQFtpfXR5eH5nYw==/?ll=27.565197%2C53.889810&pt=27.565197%2C53.889810%2Cpm2rdm&z=18.05";

    private readonly INavigationService _nav;

    public bool IsAdmin => Session.IsAdmin;

    public AboutViewModel(INavigationService nav) => _nav = nav;

    [RelayCommand]
    private void OpenMap() =>
        Process.Start(new ProcessStartInfo(MapUrl) { UseShellExecute = true });

    [RelayCommand] 
    private void GoToAdminProfile() =>
        _nav.NavigateTo<AdminDashboardViewModel>("profile");
}
