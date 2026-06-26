using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MSM.Helpers;
using MSM.Models;
using MSM.Models.Entities;
using MSM.Services.Interfaces;

namespace MSM.ViewModels;

public class AppointmentRowViewModel
{
    public int Id { get; }
    public int RealtorId { get; }
    public int PropertyId { get; }
    public string PropertyTitle { get; }
    public string DateTime { get; }
    public string StatusDisplay { get; }
    public string StatusColor { get; }
    public string StatusBgColor { get; }
    public string? Comment { get; }
    public bool CanReview  { get; }
    public bool CanCancel  { get; }
    public string RealtorName { get; }
    public AppointmentStatus RawStatus { get; }
    public bool IsCompletedAndReviewed => RawStatus == AppointmentStatus.Completed && !CanReview;
    public bool ShowReviewHint => RawStatus is AppointmentStatus.New or AppointmentStatus.Confirmed;

    public AppointmentRowViewModel(Appointment a, bool alreadyReviewed = false)
    {
        Id = a.Id;
        RealtorId = a.RealtorId;
        PropertyId = a.PropertyId;
        PropertyTitle = a.Property?.Title ?? "—";
        RealtorName   = a.Realtor?.FullName ?? "—";
        RawStatus = a.Status;
        DateTime = $"{a.SlotStart:dd.MM.yyyy}  {a.SlotStart:HH:mm}–{a.SlotEnd:HH:mm}";
        Comment   = a.Comment;
        CanReview = a.Status == AppointmentStatus.Completed && !alreadyReviewed;
        CanCancel = a.Status is AppointmentStatus.New or AppointmentStatus.Confirmed;
        (StatusDisplay, StatusColor, StatusBgColor) = a.Status switch
        {
            AppointmentStatus.New       => (L.Get("Appt.StatusNew",       "Новая"),        "#7A7A7A", "#F0F0F0"),
            AppointmentStatus.Confirmed => (L.Get("Appt.StatusConfirmed", "Подтверждена"), "#2E7D32", "#E4F4E8"),
            AppointmentStatus.Cancelled => (L.Get("Appt.StatusCancelled", "Отменена"),     "#EF5350", "#FFEBEE"),
            AppointmentStatus.Completed => (L.Get("Appt.StatusCompleted", "Завершена"),    "#27563A", "#D3EEDb"),
            _                           => (a.Status.ToString(),                            "#7A7A7A", "#F0F0F0")
        };
    }
}

public partial class MyAppointmentsViewModel : ViewModelBase
{
    private readonly IAppointmentService _appointmentService;
    private readonly IReviewService _reviewService;
    private readonly INavigationService _navigationService;

    [ObservableProperty] private ObservableCollection<AppointmentRowViewModel> _appointments = new();
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isEmpty;
    [ObservableProperty] private string? _errorMessage;

    [ObservableProperty] private bool _isReviewFormVisible;
    [ObservableProperty] private int _reviewTargetAppointmentId;
    [ObservableProperty] private int _reviewTargetRealtorId;
    [ObservableProperty] private int _reviewRating = 5;
    [ObservableProperty] private string _reviewComment = "";
    [ObservableProperty] private bool _isSubmittingReview;
    [ObservableProperty] private bool _reviewSuccess;
    [ObservableProperty] private string? _reviewError;

    public MyAppointmentsViewModel(
        IAppointmentService appointmentService,
        IReviewService reviewService,
        INavigationService navigationService)
    {
        _appointmentService = appointmentService;
        _reviewService = reviewService;
        _navigationService = navigationService;
    }

    public override void OnNavigatedTo(object? parameter)
    {
        ReviewSuccess = false;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (Session.CurrentUser == null) return;
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var items = await _appointmentService.GetByClientIdAsync(Session.CurrentUser.Id);
            var myReviews = await _reviewService.GetUserReviewsAsync(Session.CurrentUser.Id);
            var reviewedAppointmentIds = myReviews.Where(r => r.AppointmentId.HasValue)
                                                   .Select(r => r.AppointmentId!.Value).ToHashSet();
            Appointments.Clear();
            foreach (var a in items)
                Appointments.Add(new AppointmentRowViewModel(a, reviewedAppointmentIds.Contains(a.Id)));
            IsEmpty = Appointments.Count == 0;
        }
        catch (ObjectDisposedException) { }
        catch (Exception ex) { ErrorMessage = $"Ошибка: {ex.Message}"; }
        finally { IsLoading = false; }
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
    private void CloseReviewForm()
    {
        IsReviewFormVisible = false;
        ReviewSuccess = false;
    }

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
                userId:        Session.CurrentUser.Id,
                propertyId:    null,
                realtorId:     ReviewTargetRealtorId,
                rating:        ReviewRating,
                comment:       string.IsNullOrWhiteSpace(ReviewComment) ? null : ReviewComment.Trim(),
                appointmentId: ReviewTargetAppointmentId);
            ReviewSuccess = true;
            IsReviewFormVisible = false;
        }
        catch (Exception ex) { ReviewError = $"Ошибка: {ex.Message}"; }
        finally { IsSubmittingReview = false; }
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

        try { await _appointmentService.UpdateStatusAsync(appointmentId, AppointmentStatus.Cancelled); }
        catch (Exception ex)
        {
            ErrorMessage = $"Не удалось отменить запись: {ex.Message}";
            return;
        }
        await LoadAsync();
    }

    [RelayCommand]
    private void GoToRealtorProfile(AppointmentRowViewModel row) =>
        _navigationService.NavigateTo<RealtorProfileViewModel>(row.RealtorId);

    [RelayCommand]
    private void OpenPropertyDetail(AppointmentRowViewModel row) =>
        _navigationService.NavigateTo<PropertyDetailViewModel>(row.PropertyId);

    [RelayCommand]
    private void GoBack() => _navigationService.NavigateTo<PropertyListViewModel>();
}
