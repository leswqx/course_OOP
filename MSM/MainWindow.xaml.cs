using System.Windows;
using System.Windows.Media.Imaging;
using MSM.Services.Interfaces;

namespace MSM;

public partial class MainWindow : Window
{
    public MainWindow(INavigationService navigationService)
    {
        InitializeComponent();
        DataContext = navigationService;

        try
        {
            Icon = BitmapFrame.Create(
                new Uri("pack://application:,,,/Resources/Images/icon.ico"),
                BitmapCreateOptions.None,
                BitmapCacheOption.OnLoad);
        }
        catch { }
    }
}
