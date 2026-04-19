using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MSM.Models;
using MSM.Models.Entities;
using MSM.Services.Interfaces;

namespace MSM.ViewModels;

// Строка записи для риелтора
public class RealtorAppointmentRow
{
    public int Id { get; }
    public string ClientName { get; }
    public string PropertyTitle { get; }
    public string DateTime { get; }
    public string Status { get; }
    public string StatusColor { get; }

    public RealtorAppointmentRow(Appointment a)
    {
        Id = a.Id;
        ClientName = a.Client?.FullName ?? "—";
        PropertyTitle = a.Property?.Title ?? "—";
        DateTime = $"{a.SlotStart:dd.MM.yyyy HH:mm}";
        (Status, StatusColor) = a.Status switch
        {
            "new"       => ("Новая",       "#7A7A7A"),
            "confirmed" => ("Подтверждена","#7CB342"),
            "cancelled" => ("Отменена",    "#EF5350"),
            "completed" => ("Завершена",   "#D4A5A5"),
            _           => (a.Status,      "#7A7A7A")
        };
    }
}

// Панель риелтора: мои объекты + записи клиентов + форма добавления объекта
public partial class RealtorDashboardViewModel : ViewModelBase
{
    private readonly IPropertyService _propertyService;
    private readonly IAppointmentService _appointmentService;
    private readonly INavigationService _navigationService;

    // --- Вкладки ---
    [ObservableProperty] private int _selectedTab; // 0 = объекты, 1 = записи

    // --- Мои объекты ---
    [ObservableProperty] private ObservableCollection<PropertyCardViewModel> _myProperties = new();
    [ObservableProperty] private bool _isPropsLoading;

    // --- Форма добавления объекта ---
    [ObservableProperty] private bool _isFormVisible;
    [ObservableProperty] private string _formTitle = "";
    [ObservableProperty] private string _formDescription = "";
    [ObservableProperty] private string _formPrice = "";
    [ObservableProperty] private string _formArea = "";
    [ObservableProperty] private string _formRooms = "";
    [ObservableProperty] private string _formFloor = "";
    [ObservableProperty] private string _formTotalFloors = "";
    [ObservableProperty] private string _formYearBuilt = "";
    [ObservableProperty] private string _formCity = "";
    [ObservableProperty] private string _formAddress = "";
    [ObservableProperty] private string _formType = "apartment";
    [ObservableProperty] private bool _formHasRepair;
    [ObservableProperty] private bool _formMortgage;
    [ObservableProperty] private string? _formError;
    [ObservableProperty] private bool _isSaving;

    // --- Записи клиентов ---
    [ObservableProperty] private ObservableCollection<RealtorAppointmentRow> _appointments = new();
    [ObservableProperty] private bool _isApptLoading;
    [ObservableProperty] private string? _apptError;

    public RealtorDashboardViewModel(
        IPropertyService propertyService,
        IAppointmentService appointmentService,
        INavigationService navigationService)
    {
        _propertyService = propertyService;
        _appointmentService = appointmentService;
        _navigationService = navigationService;
    }

    public override void OnNavigatedTo(object? parameter)
    {
        _ = LoadPropertiesAsync();
        _ = LoadAppointmentsAsync();
    }

    private async Task LoadPropertiesAsync()
    {
        if (Session.CurrentUser == null) return;
        IsPropsLoading = true;
        try
        {
            var items = await _propertyService.GetRealtorPropertiesAsync(Session.CurrentUser.Id);
            MyProperties.Clear();
            foreach (var p in items) MyProperties.Add(new PropertyCardViewModel(p));
        }
        finally { IsPropsLoading = false; }
    }

    private async Task LoadAppointmentsAsync()
    {
        if (Session.CurrentUser == null) return;
        IsApptLoading = true;
        ApptError = null;
        try
        {
            var items = await _appointmentService.GetByRealtorIdAsync(Session.CurrentUser.Id);
            Appointments.Clear();
            foreach (var a in items) Appointments.Add(new RealtorAppointmentRow(a));
        }
        catch (Exception ex) { ApptError = ex.Message; }
        finally { IsApptLoading = false; }
    }

    [RelayCommand]
    private void OpenDetail(PropertyCardViewModel card) =>
        _navigationService.NavigateTo<PropertyDetailViewModel>(card.Id);

    [RelayCommand]
    private void ToggleForm()
    {
        IsFormVisible = !IsFormVisible;
        FormError = null;
    }

    [RelayCommand]
    private async Task SavePropertyAsync()
    {
        FormError = null;
        if (string.IsNullOrWhiteSpace(FormTitle) || string.IsNullOrWhiteSpace(FormDescription)
            || string.IsNullOrWhiteSpace(FormPrice) || string.IsNullOrWhiteSpace(FormArea)
            || string.IsNullOrWhiteSpace(FormRooms) || string.IsNullOrWhiteSpace(FormCity)
            || string.IsNullOrWhiteSpace(FormAddress))
        {
            FormError = "Заполните все обязательные поля.";
            return;
        }

        if (!decimal.TryParse(FormPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) || price <= 0)
        { FormError = "Укажите корректную цену."; return; }

        if (!double.TryParse(FormArea, NumberStyles.Any, CultureInfo.InvariantCulture, out var area) || area <= 0)
        { FormError = "Укажите корректную площадь."; return; }

        if (!int.TryParse(FormRooms, out var rooms) || rooms <= 0)
        { FormError = "Укажите количество комнат."; return; }

        IsSaving = true;
        try
        {
            var property = new Property
            {
                Title = FormTitle.Trim(),
                Description = FormDescription.Trim(),
                Price = price,
                Area = area,
                Rooms = rooms,
                Floor = int.TryParse(FormFloor, out var f) ? f : null,
                TotalFloors = int.TryParse(FormTotalFloors, out var tf) ? tf : null,
                YearBuilt = int.TryParse(FormYearBuilt, out var yb) ? yb : null,
                City = FormCity.Trim(),
                Address = FormAddress.Trim(),
                PropertyType = FormType,
                HasRepair = FormHasRepair,
                MortgageAvailable = FormMortgage,
                RealtorId = Session.CurrentUser!.Id,
            };

            await _propertyService.AddAsync(property, Array.Empty<(byte[], string, bool)>());
            IsFormVisible = false;
            ClearForm();
            await LoadPropertiesAsync();
        }
        catch (Exception ex) { FormError = $"Ошибка: {ex.Message}"; }
        finally { IsSaving = false; }
    }

    [RelayCommand]
    private async Task ConfirmAppointmentAsync(int id)
    {
        try { await _appointmentService.UpdateStatusAsync(id, "confirmed"); await LoadAppointmentsAsync(); }
        catch (Exception ex) { ApptError = ex.Message; }
    }

    [RelayCommand]
    private async Task CancelAppointmentAsync(int id)
    {
        try { await _appointmentService.UpdateStatusAsync(id, "cancelled"); await LoadAppointmentsAsync(); }
        catch (Exception ex) { ApptError = ex.Message; }
    }

    [RelayCommand]
    private void GoBack() => _navigationService.NavigateTo<PropertyListViewModel>();

    private void ClearForm()
    {
        FormTitle = FormDescription = FormPrice = FormArea = FormRooms = "";
        FormFloor = FormTotalFloors = FormYearBuilt = FormCity = FormAddress = "";
        FormType = "apartment"; FormHasRepair = FormMortgage = false;
    }
}
