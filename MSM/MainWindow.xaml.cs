using System.Windows;
using MSM.Services.Interfaces;

namespace MSM;

public partial class MainWindow : Window
{
    public MainWindow(INavigationService navigationService)
    {
        InitializeComponent();
        DataContext = navigationService;
    }
}
