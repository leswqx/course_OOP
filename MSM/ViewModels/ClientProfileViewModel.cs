using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using MSM.Models;
using MSM.Services.Interfaces;

namespace MSM.ViewModels;

public partial class ClientProfileViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;

    [ObservableProperty] private string _fullName = "";
    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string _phone = "";
    [ObservableProperty] private byte[]? _avatarPhoto;

    [ObservableProperty] private string _oldPassword = "";
    [ObservableProperty] private string _newPassword = "";
    [ObservableProperty] private string _confirmPassword = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasProfileResult))]
    private string? _profileResult;

    [ObservableProperty] private bool _profileSuccess;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPasswordResult))]
    private string? _passwordResult;

    [ObservableProperty] private bool _passwordSuccess;
    [ObservableProperty] private bool _isSaving;

    public bool HasProfileResult => ProfileResult != null;
    public bool HasPasswordResult => PasswordResult != null;

    public string Login => Session.CurrentUser?.Login ?? "";

    public ClientProfileViewModel(IAuthService authService, INavigationService navigationService)
    {
        _authService = authService;
        _navigationService = navigationService;
    }

    public override void OnNavigatedTo(object? parameter)
    {
        var user = Session.CurrentUser;
        if (user == null) return;
        FullName = user.FullName;
        Email = user.Email;
        Phone = user.Phone ?? "";
        AvatarPhoto = user.AvatarPhoto;
        ProfileResult = null;
        PasswordResult = null;
    }

    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        var user = Session.CurrentUser;
        if (user == null) return;

        if (string.IsNullOrWhiteSpace(FullName))
        {
            ProfileResult = "Имя не может быть пустым.";
            ProfileSuccess = false;
            return;
        }

        IsSaving = true;
        ProfileResult = null;
        try
        {
            user.FullName = FullName.Trim();
            user.Email = Email.Trim();
            user.Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim();
            user.AvatarPhoto = AvatarPhoto;
            await _authService.UpdateProfileAsync(user);
            ProfileResult = "Профиль сохранён!";
            ProfileSuccess = true;
        }
        catch (Exception ex)
        {
            ProfileResult = $"Ошибка: {ex.Message}";
            ProfileSuccess = false;
        }
        finally { IsSaving = false; }
    }

    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        var user = Session.CurrentUser;
        if (user == null) return;

        if (string.IsNullOrWhiteSpace(OldPassword) || string.IsNullOrWhiteSpace(NewPassword))
        { PasswordResult = "Заполните все поля."; PasswordSuccess = false; return; }

        if (NewPassword != ConfirmPassword)
        { PasswordResult = "Пароли не совпадают."; PasswordSuccess = false; return; }

        if (NewPassword.Length < 6)
        { PasswordResult = "Минимум 6 символов."; PasswordSuccess = false; return; }

        IsSaving = true;
        PasswordResult = null;
        try
        {
            var ok = await _authService.ChangePasswordAsync(user, OldPassword, NewPassword);
            if (ok)
            {
                PasswordResult = "Пароль изменён!";
                PasswordSuccess = true;
                OldPassword = NewPassword = ConfirmPassword = "";
            }
            else
            {
                PasswordResult = "Неверный текущий пароль.";
                PasswordSuccess = false;
            }
        }
        catch (Exception ex) { PasswordResult = $"Ошибка: {ex.Message}"; PasswordSuccess = false; }
        finally { IsSaving = false; }
    }

    [RelayCommand]
    private void UploadAvatar()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp",
            Title = "Выберите фото профиля"
        };
        if (dlg.ShowDialog() != true) return;
        AvatarPhoto = File.ReadAllBytes(dlg.FileName);
    }

    [RelayCommand]
    private void RemoveAvatar() => AvatarPhoto = null;

    [RelayCommand]
    private void GoBack() => _navigationService.NavigateTo<PropertyListViewModel>();
}
