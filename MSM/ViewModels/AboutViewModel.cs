using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;

namespace MSM.ViewModels;

public partial class AboutViewModel : ViewModelBase
{
    private const string MapUrl = "https://yandex.com/maps/157/minsk/house/Zk4YcwFkT0IHQFtpfXR5eH5nYw==/?ll=27.565197%2C53.889810&pt=37.6178%2C55.7558%2Cpm2rdm&z=18.05";

    [RelayCommand]
    private void OpenMap() =>
        Process.Start(new ProcessStartInfo(MapUrl) { UseShellExecute = true });
}
