using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MSM.Services.Interfaces;

namespace MSM.ViewModels;

// ViewModel формы регистрации нового клиента.
public partial class RegisterViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;
    private readonly INotificationService _notificationService;

    [ObservableProperty] private string _login = "";
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private string _confirmPassword = "";
    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string _fullName = "";
    [ObservableProperty] private string _phone = "";
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isLoading;

    public RegisterViewModel(IAuthService authService, INavigationService navigationService,
        INotificationService notificationService)
    {
        _authService = authService;
        _navigationService = navigationService;
        _notificationService = notificationService;
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

        if (!Regex.IsMatch(Login.Trim(), @"^[a-zA-Z0-9_]{3,30}$"))
        {
            ErrorMessage = "Логин: только латинские буквы, цифры и «_», от 3 до 30 символов.";
            return;
        }

        if (!Regex.IsMatch(Email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            ErrorMessage = "Введите корректный e-mail адрес.";
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

        if (!string.IsNullOrWhiteSpace(Phone) &&
            !Regex.IsMatch(Phone.Trim(), @"^\+375\s?\(?(29|33|44|25|17)\)?\s?\d{3}[-\s]?\d{2}[-\s]?\d{2}$"))
        {
            ErrorMessage = "Телефон: +375 (XX) XXX-XX-XX, где XX — 17, 25, 29, 33 или 44.";
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

            // Приветственное письмо — fire-and-forget, не блокируем навигацию
            _ = _notificationService.SendWelcomeEmailAsync(user);

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
