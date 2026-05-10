using System.Collections.ObjectModel;
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
    private readonly IAppointmentService _appointmentService;
    private readonly IReviewService _reviewService;

    // Profile
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

    // Tabs
    [ObservableProperty] private bool _showTab0 = true;
    [ObservableProperty] private bool _showTab1;

    // Appointments
    [ObservableProperty] private ObservableCollection<AppointmentRowViewModel> _appointments = new();
    [ObservableProperty] private bool _isApptLoading;
    [ObservableProperty] private bool _isApptEmpty;

    // Review form
    [ObservableProperty] private bool _isReviewFormVisible;
    [ObservableProperty] private int _reviewTargetAppointmentId;
    [ObservableProperty] private int _reviewTargetRealtorId;
    [ObservableProperty] private int _reviewRating = 5;
    [ObservableProperty] private string _reviewComment = "";
    [ObservableProperty] private bool _isSubmittingReview;
    [ObservableProperty] private bool _reviewSuccess;
    [ObservableProperty] private string? _reviewError;

    public ClientProfileViewModel(
        IAuthService authService,
        INavigationService navigationService,
        IAppointmentService appointmentService,
        IReviewService reviewService)
    {
        _authService = authService;
        _navigationService = navigationService;
        _appointmentService = appointmentService;
        _reviewService = reviewService;
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
        ShowTab0 = true;
        ShowTab1 = false;
    }

    [RelayCommand]
    private void SetTab0()
    {
        ShowTab0 = true;
        ShowTab1 = false;
    }

    [RelayCommand]
    private void SetTab1()
    {
        ShowTab0 = false;
        ShowTab1 = true;
        _ = LoadAppointmentsAsync();
    }

    private async Task LoadAppointmentsAsync()
    {
        if (Session.CurrentUser == null) return;
        IsApptLoading = true;
        try
        {
            var items = await _appointmentService.GetByClientIdAsync(Session.CurrentUser.Id);
            var myReviews = await _reviewService.GetUserReviewsAsync(Session.CurrentUser.Id);
            var reviewedRealtorIds = myReviews.Where(r => r.RealtorId.HasValue)
                                               .Select(r => r.RealtorId!.Value).ToHashSet();
            Appointments.Clear();
            foreach (var a in items)
                Appointments.Add(new AppointmentRowViewModel(a, reviewedRealtorIds.Contains(a.RealtorId)));
            IsApptEmpty = Appointments.Count == 0;
        }
        catch { IsApptEmpty = true; }
        finally { IsApptLoading = false; }
    }

    [RelayCommand]
    private async Task CancelAppointmentAsync(int appointmentId)
    {
        var result = System.Windows.MessageBox.Show(
            "Вы уверены, что хотите отменить запись?",
            "Подтверждение отмены",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);
        if (result != System.Windows.MessageBoxResult.Yes) return;

        try { await _appointmentService.UpdateStatusAsync(appointmentId, "cancelled"); }
        catch { }
        await LoadAppointmentsAsync();
    }

    [RelayCommand]
    private void OpenReviewForm(AppointmentRowViewModel row)
    {
        ReviewTargetAppointmentId = row.Id;
        ReviewTargetRealtorId = row.RealtorId;
        ReviewRating = 5;
        ReviewComment = "";
        ReviewError = null;
        ReviewSuccess = false;
        IsReviewFormVisible = true;
    }

    [RelayCommand]
    private void CloseReviewForm() { IsReviewFormVisible = false; ReviewSuccess = false; }

    [RelayCommand]
    private async Task SubmitReviewAsync()
    {
        if (Session.CurrentUser == null) return;
        ReviewError = null;
        if (ReviewRating < 1 || ReviewRating > 5)
        { ReviewError = "Оценка должна быть от 1 до 5."; return; }

        IsSubmittingReview = true;
        try
        {
            await _reviewService.CreateAsync(
                userId: Session.CurrentUser.Id,
                propertyId: null,
                realtorId: ReviewTargetRealtorId,
                rating: ReviewRating,
                comment: string.IsNullOrWhiteSpace(ReviewComment) ? null : ReviewComment.Trim());
            ReviewSuccess = true;
            IsReviewFormVisible = false;
        }
        catch (Exception ex) { ReviewError = $"Ошибка: {ex.Message}"; }
        finally { IsSubmittingReview = false; }
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
    private void GoToRealtorProfile(AppointmentRowViewModel row) =>
        _navigationService.NavigateTo<RealtorProfileViewModel>(row.RealtorId);

    [RelayCommand]
    private void GoBack() => _navigationService.NavigateTo<PropertyListViewModel>();
}
