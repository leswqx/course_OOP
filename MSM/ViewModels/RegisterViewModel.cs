using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MSM.Services.Interfaces;

namespace MSM.ViewModels;

// ViewModel формы регистрации нового клиента.
public partial class RegisterViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;

    [ObservableProperty] private string _login = "";
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private string _confirmPassword = "";
    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string _fullName = "";
    [ObservableProperty] private string _phone = "";
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isLoading;

    public RegisterViewModel(IAuthService authService, INavigationService navigationService)
    {
        _authService = authService;
        _navigationService = navigationService;
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(Password)
            || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(FullName))
        {
            ErrorMessage = "Заполните все обязательные поля.";
            return;
        }

        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Пароли не совпадают.";
            return;
        }

        if (Password.Length < 6)
        {
            ErrorMessage = "Пароль должен содержать минимум 6 символов.";
            return;
        }

        IsLoading = true;
        try
        {
            var user = await _authService.RegisterAsync(
                Login.Trim(), Password, Email.Trim(),
                FullName.Trim(), string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim());

            if (user == null)
            {
                ErrorMessage = "Логин или e-mail уже занят.";
                return;
            }

            _navigationService.NavigateTo<LoginViewModel>();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void GoBack() => _navigationService.NavigateTo<LoginViewModel>();
}
