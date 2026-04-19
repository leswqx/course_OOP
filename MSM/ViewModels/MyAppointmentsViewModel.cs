using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MSM.Models;
using MSM.Models.Entities;
using MSM.Services.Interfaces;

namespace MSM.ViewModels;

// Строка в списке записей клиента
public class AppointmentRowViewModel
{
    public int Id { get; }
    public string PropertyTitle { get; }
    public string DateTime { get; }
    public string StatusDisplay { get; }
    public string StatusColor { get; }
    public string? Comment { get; }

    public AppointmentRowViewModel(Appointment a)
    {
        Id = a.Id;
        PropertyTitle = a.Property?.Title ?? "—";
        DateTime = $"{a.SlotStart:dd.MM.yyyy}  {a.SlotStart:HH:mm}–{a.SlotEnd:HH:mm}";
        Comment = a.Comment;
        (StatusDisplay, StatusColor) = a.Status switch
        {
            "new"       => ("Новая",       "#7A7A7A"),
            "confirmed" => ("Подтверждена","#7CB342"),
            "cancelled" => ("Отменена",    "#EF5350"),
            "completed" => ("Завершена",   "#D4A5A5"),
            _           => (a.Status,      "#7A7A7A")
        };
    }
}

// История записей текущего клиента
public partial class MyAppointmentsViewModel : ViewModelBase
{
    private readonly IAppointmentService _appointmentService;
    private readonly INavigationService _navigationService;

    [ObservableProperty] private ObservableCollection<AppointmentRowViewModel> _appointments = new();
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isEmpty;
    [ObservableProperty] private string? _errorMessage;

    public MyAppointmentsViewModel(IAppointmentService appointmentService, INavigationService navigationService)
    {
        _appointmentService = appointmentService;
        _navigationService = navigationService;
    }

    public override void OnNavigatedTo(object? parameter) => _ = LoadAsync();

    private async Task LoadAsync()
    {
        if (Session.CurrentUser == null) return;
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var items = await _appointmentService.GetByClientIdAsync(Session.CurrentUser.Id);
            Appointments.Clear();
            foreach (var a in items) Appointments.Add(new AppointmentRowViewModel(a));
            IsEmpty = Appointments.Count == 0;
        }
        catch (Exception ex) { ErrorMessage = $"Ошибка: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void GoBack() => _navigationService.NavigateTo<PropertyListViewModel>();
}
