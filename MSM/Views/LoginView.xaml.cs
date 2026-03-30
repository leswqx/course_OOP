using System.Windows.Controls;
using MSM.ViewModels;

namespace MSM.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }

    public LoginView(LoginViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private void PasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel viewModel)
        {
            viewModel.Password = PasswordBox.Password;
        }
    }
}
