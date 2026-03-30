using System.Windows;
using MSM.ViewModels;
using MSM.Views;

namespace MSM;

public partial class MainWindow : Window
{
    private readonly LoginViewModel _loginViewModel;

    public MainWindow(LoginViewModel loginViewModel)
    {
        InitializeComponent();

        _loginViewModel = loginViewModel;
        _loginViewModel.LoginSuccessful += OnLoginSuccessful;
        _loginViewModel.NavigationToRegister += OnNavigationToRegister;

        // Устанавливаем LoginView как содержимое
        var loginView = new LoginView(_loginViewModel);
        MainContent.Content = loginView;
    }

    private void OnLoginSuccessful(string role)
    {
        MessageBox.Show($"Вход выполнен!\nРоль: {GetRoleName(role)}", "Успех");
    }

    private void OnNavigationToRegister()
    {
        MessageBox.Show("Регистрация будет реализована позже");
    }

    private static string GetRoleName(string role)
    {
        return role switch
        {
            "admin" => "Администратор",
            "realtor" => "Риелтор",
            "client" => "Клиент",
            _ => role
        };
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _loginViewModel.LoginSuccessful -= OnLoginSuccessful;
        _loginViewModel.NavigationToRegister -= OnNavigationToRegister;
    }
}
