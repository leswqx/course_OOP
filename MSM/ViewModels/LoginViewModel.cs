using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MSM.Models;
using MSM.Models.Entities;
using MSM.Services.Interfaces;

namespace MSM.ViewModels;

/// <summary>
/// ViewModel для авторизации
/// </summary>
public partial class LoginViewModel : ViewModelBase
{
    private readonly IAuthService _authService;

    [ObservableProperty]
    private string _login = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isLoading;

    public event Action<string>? LoginSuccessful;
    public event Action? NavigationToRegister;

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Login))
        {
            ErrorMessage = "Введите логин";
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Введите пароль";
            return;
        }

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var user = await _authService.LoginAsync(Login, Password);

            if (user != null)
            {
                Session.CurrentUser = user;
                LoginSuccessful?.Invoke(user.Role);
            }
            else
            {
                ErrorMessage = "Неверный логин или пароль";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void NavigateToRegister()
    {
        NavigationToRegister?.Invoke();
    }
}
