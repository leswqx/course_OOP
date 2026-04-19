using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.EntityFrameworkCore;
using MSM.Data.Context;
using MSM.Models;
using MSM.Models.Entities;
using MSM.Services.Interfaces;
using SkiaSharp;

namespace MSM.ViewModels;

public class RealtorAppointmentRow
{
    public int Id { get; }
    public string ClientName { get; }
    public string PropertyTitle { get; }
    public string DateTime { get; }
    public string Status { get; }
    public string StatusColor { get; }
    public bool CanAct { get; }

    public RealtorAppointmentRow(Appointment a)
    {
        Id = a.Id;
        ClientName = a.Client?.FullName ?? "—";
        PropertyTitle = a.Property?.Title ?? "—";
        DateTime = $"{a.SlotStart:dd.MM.yyyy  HH:mm}–{a.SlotEnd:HH:mm}";
        CanAct = a.Status == "new";
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

// Панель риелтора: мои объекты · записи клиентов · статистика
public partial class RealtorDashboardViewModel : ViewModelBase
{
    private readonly IPropertyService _propertyService;
    private readonly IAppointmentService _appointmentService;
    private readonly IReviewService _reviewService;
    private readonly INavigationService _navigationService;
    private readonly AppDbContext _context;

    // ===== Вкладки =====
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowTab0), nameof(ShowTab1), nameof(ShowTab2))]
    private int _selectedTab;
    public bool ShowTab0 => SelectedTab == 0;
    public bool ShowTab1 => SelectedTab == 1;
    public bool ShowTab2 => SelectedTab == 2;

    // ===== Мои объекты =====
    [ObservableProperty] private ObservableCollection<PropertyCardViewModel> _myProperties = new();
    [ObservableProperty] private bool _isPropsLoading;

    // ===== Форма добавления объекта =====
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

    // ===== Записи клиентов =====
    [ObservableProperty] private ObservableCollection<RealtorAppointmentRow> _appointments = new();
    [ObservableProperty] private bool _isApptLoading;
    [ObservableProperty] private bool _noAppointments;

    // ===== Статистика =====
    [ObservableProperty] private int _statTotal;
    [ObservableProperty] private int _statActive;
    [ObservableProperty] private int _statSold;
    [ObservableProperty] private double _statRating;
    [ObservableProperty] private int _statCompletedAppt;
    [ObservableProperty] private ISeries[] _statusSeries = Array.Empty<ISeries>();
    [ObservableProperty] private ISeries[] _ratingSeriesData = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _ratingXAxes = Array.Empty<Axis>();

    public RealtorDashboardViewModel(
        IPropertyService propertyService,
        IAppointmentService appointmentService,
        IReviewService reviewService,
        INavigationService navigationService,
        AppDbContext context)
    {
        _propertyService = propertyService;
        _appointmentService = appointmentService;
        _reviewService = reviewService;
        _navigationService = navigationService;
        _context = context;
    }

    public override void OnNavigatedTo(object? parameter)
    {
        _ = LoadPropertiesAsync();
        _ = LoadAppointmentsAsync();
        _ = LoadStatsAsync();
    }

    // ===== Вкладки =====
    [RelayCommand] private void SetTab0() => SelectedTab = 0;
    [RelayCommand] private void SetTab1() => SelectedTab = 1;
    [RelayCommand] private void SetTab2() => SelectedTab = 2;

    // ===== Объекты =====
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
        { FormError = "Заполните все обязательные поля."; return; }

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
                Title = FormTitle.Trim(), Description = FormDescription.Trim(),
                Price = price, Area = area, Rooms = rooms,
                Floor = int.TryParse(FormFloor, out var f) ? f : null,
                TotalFloors = int.TryParse(FormTotalFloors, out var tf) ? tf : null,
                YearBuilt = int.TryParse(FormYearBuilt, out var yb) ? yb : null,
                City = FormCity.Trim(), Address = FormAddress.Trim(),
                PropertyType = FormType, HasRepair = FormHasRepair,
                MortgageAvailable = FormMortgage,
                RealtorId = Session.CurrentUser!.Id,
            };
            await _propertyService.AddAsync(property, Array.Empty<(byte[], string, bool)>());
            IsFormVisible = false;
            ClearForm();
            await LoadPropertiesAsync();
            await LoadStatsAsync();
        }
        catch (Exception ex) { FormError = $"Ошибка: {ex.Message}"; }
        finally { IsSaving = false; }
    }

    // ===== Записи =====
    private async Task LoadAppointmentsAsync()
    {
        if (Session.CurrentUser == null) return;
        IsApptLoading = true;
        try
        {
            var items = await _appointmentService.GetByRealtorIdAsync(Session.CurrentUser.Id);
            Appointments.Clear();
            foreach (var a in items) Appointments.Add(new RealtorAppointmentRow(a));
            NoAppointments = Appointments.Count == 0;
        }
        finally { IsApptLoading = false; }
    }

    [RelayCommand]
    private async Task ConfirmAppointmentAsync(int id)
    {
        await _appointmentService.UpdateStatusAsync(id, "confirmed");
        await LoadAppointmentsAsync();
    }

    [RelayCommand]
    private async Task CancelAppointmentAsync(int id)
    {
        await _appointmentService.UpdateStatusAsync(id, "cancelled");
        await LoadAppointmentsAsync();
    }

    [RelayCommand]
    private async Task CompleteAppointmentAsync(int id)
    {
        await _appointmentService.UpdateStatusAsync(id, "completed");
        await LoadAppointmentsAsync();
    }

    // ===== Статистика =====
    private async Task LoadStatsAsync()
    {
        if (Session.CurrentUser == null) return;
        try
        {
            var props = await _context.Properties
                .Where(p => p.RealtorId == Session.CurrentUser.Id)
                .ToListAsync();

            StatTotal  = props.Count;
            StatActive = props.Count(p => p.Status == "active");
            StatSold   = props.Count(p => p.Status == "sold");

            var appts = await _context.Appointments
                .Where(a => a.RealtorId == Session.CurrentUser.Id)
                .ToListAsync();
            StatCompletedAppt = appts.Count(a => a.Status == "completed");

            StatRating = await _reviewService.GetAverageRatingAsync(realtorId: Session.CurrentUser.Id);

            // Pie: статусы объектов
            StatusSeries = new ISeries[]
            {
                new PieSeries<int> { Values = new[]{ StatActive }, Name = "Активные",
                    Fill = new SolidColorPaint(SKColor.Parse("#D4A5A5")) },
                new PieSeries<int> { Values = new[]{ StatSold },   Name = "Проданные",
                    Fill = new SolidColorPaint(SKColor.Parse("#7CB342")) },
                new PieSeries<int> { Values = new[]{ props.Count(p=>p.Status=="hidden") }, Name = "Скрытые",
                    Fill = new SolidColorPaint(SKColor.Parse("#AAAAAA")) },
            };

            // Bar: записи по месяцам (последние 6 месяцев)
            var months = Enumerable.Range(0, 6)
                .Select(i => System.DateTime.Today.AddMonths(-5 + i))
                .ToList();
            var counts = months.Select(m =>
                appts.Count(a => a.SlotStart.Year == m.Year && a.SlotStart.Month == m.Month)).ToArray();

            RatingSeriesData = new ISeries[]
            {
                new ColumnSeries<int>
                {
                    Values = counts, Name = "Записи",
                    Fill = new SolidColorPaint(SKColor.Parse("#D4A5A5"))
                }
            };
            RatingXAxes = new Axis[]
            {
                new Axis { Labels = months.Select(m => m.ToString("MMM")).ToArray() }
            };
        }
        catch { /* статистика некритична */ }
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
