using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using MSM.Data.Context;
using MSM.Models;
using MSM.Models.Entities;
using MSM.Services.Interfaces;
using SkiaSharp;

namespace MSM.ViewModels;

public class CalendarDayVm
{
    public int      Day             { get; init; }
    public DateTime Date            { get; init; }
    public bool     IsCurrentMonth  { get; init; }
    public bool     IsToday         { get; init; }
    public bool     IsEmpty         { get; init; }
    public bool     IsBlocked       { get; init; }
    public int      AppointmentCount { get; init; }
    public string   DotColor        { get; init; } = "Transparent";
    public string   DayForeground   { get; init; } = "#222222";
}

public class CalendarApptRow
{
    public string ClientName    { get; init; } = "";
    public string PropertyTitle { get; init; } = "";
    public string Time          { get; init; } = "";
    public string Status        { get; init; } = "";
    public string StatusColor   { get; init; } = "#7A7A7A";
}

public class RealtorAppointmentRow
{
    public int Id { get; }
    public string ClientName { get; }
    public string? ClientPhone { get; }
    public string ClientEmail { get; }
    public string PropertyTitle { get; }
    public string DateTime { get; }
    public System.DateTime SlotStart { get; }
    public string Status { get; }
    public string StatusRaw { get; }
    public string StatusColor { get; }
    public bool CanConfirm { get; }
    public bool CanCancel { get; }
    public bool CanComplete { get; }
    public bool HasClientPhone { get; }

    public RealtorAppointmentRow(Appointment a)
    {
        Id = a.Id;
        ClientName  = a.Client?.FullName ?? "—";
        ClientPhone = a.Client?.Phone;
        ClientEmail = a.Client?.Email ?? "—";
        HasClientPhone = !string.IsNullOrEmpty(ClientPhone);
        PropertyTitle = a.Property?.Title ?? "—";
        SlotStart = a.SlotStart;
        DateTime = $"{a.SlotStart:dd.MM.yyyy  HH:mm}–{a.SlotEnd:HH:mm}";
        StatusRaw   = a.Status;
        CanConfirm  = a.Status == "new";
        CanCancel   = a.Status is "new" or "confirmed";
        CanComplete = a.Status == "confirmed";
        (Status, StatusColor) = a.Status switch
        {
            "new"       => ("Новая",        "#7A7A7A"),
            "confirmed" => ("Подтверждена", "#7CB342"),
            "cancelled" => ("Отменена",     "#EF5350"),
            "completed" => ("Завершена ✓",  "#4A9061"),
            _           => (a.Status,       "#7A7A7A")
        };
    }
}

public class ImagePreviewVm
{
    public int? ExistingId { get; init; }
    public string FileName { get; init; } = "";
    public byte[] Data { get; init; } = Array.Empty<byte>();
    public bool IsMain { get; set; }
}

// Панель риелтора: мои объекты · записи клиентов · статистика
public partial class RealtorDashboardViewModel : ViewModelBase
{
    private readonly IPropertyService _propertyService;
    private readonly IAppointmentService _appointmentService;
    private readonly IReviewService _reviewService;
    private readonly INavigationService _navigationService;
    private readonly AppDbContext _context;

    private readonly List<(byte[] Data, string FileName, bool IsMain)> _pendingImages = new();

    // ===== Вкладки =====
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowTab0), nameof(ShowTab1), nameof(ShowTab2), nameof(ShowTab3))]
    private int _selectedTab;
    public bool ShowTab0 => SelectedTab == 0;
    public bool ShowTab1 => SelectedTab == 1;
    public bool ShowTab2 => SelectedTab == 2;
    public bool ShowTab3 => SelectedTab == 3;

    // ===== Фильтр объектов =====
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredMyProperties), nameof(IsPropFilterAll), nameof(IsPropFilterActive), nameof(IsPropFilterHidden), nameof(IsPropFilterSold))]
    private string _propStatusFilter = "all";
    public bool IsPropFilterAll    => PropStatusFilter == "all";
    public bool IsPropFilterActive => PropStatusFilter == "active";
    public bool IsPropFilterHidden => PropStatusFilter == "hidden";
    public bool IsPropFilterSold   => PropStatusFilter == "sold";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredMyProperties))]
    private string _myPropertiesSearch = "";

    public IEnumerable<PropertyCardViewModel> FilteredMyProperties
    {
        get
        {
            IEnumerable<PropertyCardViewModel> result = PropStatusFilter switch
            {
                "active" => MyProperties.Where(p => p.Status == "active"),
                "hidden" => MyProperties.Where(p => p.Status == "hidden"),
                "sold"   => MyProperties.Where(p => p.Status == "sold"),
                _        => MyProperties
            };
            if (!string.IsNullOrWhiteSpace(MyPropertiesSearch))
            {
                var q = MyPropertiesSearch.Trim();
                result = result.Where(p =>
                    p.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    p.City.Contains(q, StringComparison.OrdinalIgnoreCase));
            }
            return result;
        }
    }
    [RelayCommand] private void SetPropFilterAll()    => PropStatusFilter = "all";
    [RelayCommand] private void SetPropFilterActive() => PropStatusFilter = "active";
    [RelayCommand] private void SetPropFilterHidden() => PropStatusFilter = "hidden";
    [RelayCommand] private void SetPropFilterSold()   => PropStatusFilter = "sold";
    [RelayCommand] private void ClearMySearch()       => MyPropertiesSearch = "";

    // ===== Профиль риелтора =====
    [ObservableProperty] private string _realtorName = "";
    [ObservableProperty] private string _realtorPhone = "";
    [ObservableProperty] private string _realtorEmail = "";
    [ObservableProperty] private string _realtorDescription = "";

    // ===== Мои объекты =====
    [ObservableProperty] private ObservableCollection<PropertyCardViewModel> _myProperties = new();
    [ObservableProperty] private bool _isPropsLoading;

    // ===== Форма добавления объекта =====
    [ObservableProperty] private bool _isFormVisible;
    [ObservableProperty] private bool _formValidated;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TitleInvalid))]
    private string _formTitle = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DescriptionInvalid))]
    private string _formDescription = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PriceInvalid))]
    private string _formPrice = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AreaInvalid))]
    private string _formArea = "";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RoomsInvalid))]
    private string _formRooms = "";
    [ObservableProperty] private string _formBathrooms = "";
    [ObservableProperty] private string _formFloor = "";
    [ObservableProperty] private string _formTotalFloors = "";
    [ObservableProperty] private string _formYearBuilt = "";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CityInvalid))]
    private string _formCity = "";
    [ObservableProperty] private string _formDistrict = "";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AddressInvalid))]
    private string _formAddress = "";
    [ObservableProperty] private string _formType = "apartment";
    [ObservableProperty] private bool _formHasRepair;
    [ObservableProperty] private bool _formMortgage;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TitleInvalid), nameof(DescriptionInvalid), nameof(PriceInvalid),
                              nameof(AreaInvalid), nameof(RoomsInvalid), nameof(CityInvalid), nameof(AddressInvalid))]
    private string? _formError;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private bool _formSuccess;
    [ObservableProperty] private ObservableCollection<ImagePreviewVm> _formImages = new();

    public bool TitleInvalid       => FormValidated && string.IsNullOrWhiteSpace(FormTitle);
    public bool DescriptionInvalid => FormValidated && string.IsNullOrWhiteSpace(FormDescription);
    public bool PriceInvalid       => FormValidated && string.IsNullOrWhiteSpace(FormPrice);
    public bool AreaInvalid    => FormValidated && string.IsNullOrWhiteSpace(FormArea);
    public bool RoomsInvalid   => FormValidated && string.IsNullOrWhiteSpace(FormRooms);
    public bool CityInvalid    => FormValidated && string.IsNullOrWhiteSpace(FormCity);
    public bool AddressInvalid => FormValidated && string.IsNullOrWhiteSpace(FormAddress);

    // ===== Форма редактирования объекта =====
    [ObservableProperty] private bool _isEditFormVisible;
    [ObservableProperty] private int _editPropertyId;
    [ObservableProperty] private string _editTitle = "";
    [ObservableProperty] private string _editDescription = "";
    [ObservableProperty] private string _editPrice = "";
    [ObservableProperty] private string _editArea = "";
    [ObservableProperty] private string _editRooms = "";
    [ObservableProperty] private string _editBathrooms = "";
    [ObservableProperty] private string _editFloor = "";
    [ObservableProperty] private string _editTotalFloors = "";
    [ObservableProperty] private string _editYearBuilt = "";
    [ObservableProperty] private string _editCity = "";
    [ObservableProperty] private string _editDistrict = "";
    [ObservableProperty] private string _editAddress = "";
    [ObservableProperty] private string _editType = "apartment";
    [ObservableProperty] private bool _editHasRepair;
    [ObservableProperty] private bool _editMortgage;
    [ObservableProperty] private string? _editError;
    [ObservableProperty] private bool _isEditSaving;
    [ObservableProperty] private ObservableCollection<ImagePreviewVm> _editImages = new();

    // ===== Записи клиентов =====
    [ObservableProperty] private ObservableCollection<RealtorAppointmentRow> _appointments = new();
    [ObservableProperty] private bool _isApptLoading;
    [ObservableProperty] private bool _noAppointments;
    [ObservableProperty] private int _newAppointmentsCount;
    [ObservableProperty] private string _apptFilterProperty = "";
    [ObservableProperty] private string _apptFilterStatus = "";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsApptDateAll), nameof(IsApptDateToday), nameof(IsApptDateWeek), nameof(IsApptDateMonth))]
    private string _apptDateFilter = "";
    public bool IsApptDateAll   => ApptDateFilter == "";
    public bool IsApptDateToday => ApptDateFilter == "today";
    public bool IsApptDateWeek  => ApptDateFilter == "week";
    public bool IsApptDateMonth => ApptDateFilter == "month";
    public ObservableCollection<string> AppointmentPropertyTitles { get; } = new();
    private List<RealtorAppointmentRow> _allAppointmentRows = new();

    // ===== Статистика =====
    [ObservableProperty] private int _statTotal;
    [ObservableProperty] private int _statActive;
    [ObservableProperty] private int _statSold;
    [ObservableProperty] private double _statRating;
    [ObservableProperty] private int _statCompletedAppt;
    [ObservableProperty] private int _statTotalAppt;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatScoreLabel), nameof(StatScoreBarWidth))]
    private int _statScore;
    [ObservableProperty] private string _statScoreColor = "#7A7A7A";
    [ObservableProperty] private ISeries[] _statusSeries = Array.Empty<ISeries>();
    [ObservableProperty] private ISeries[] _ratingSeriesData = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _ratingXAxes = Array.Empty<Axis>();

    public string StatScoreLabel    => StatScore switch { >= 70 => "Отличный результат! 🏆", >= 40 => "Хороший показатель 👍", _ => "Нужно работать над показателями" };
    public double StatScoreBarWidth => StatScore * 1.8;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPeriodAll), nameof(IsPeriodMonth), nameof(IsPeriodQuarter), nameof(IsPeriodYear))]
    private string _selectedStatPeriod = "all";
    public bool IsPeriodAll     => SelectedStatPeriod == "all";
    public bool IsPeriodMonth   => SelectedStatPeriod == "month";
    public bool IsPeriodQuarter => SelectedStatPeriod == "quarter";
    public bool IsPeriodYear    => SelectedStatPeriod == "year";

    // ===== Календарь расписания =====
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCalendarClosed))]
    private bool _showCalendarOverlay;
    public bool IsCalendarClosed => !ShowCalendarOverlay;
    [ObservableProperty] private int  _calendarYear  = DateTime.Today.Year;
    [ObservableProperty] private int  _calendarMonth = DateTime.Today.Month;
    [ObservableProperty] private ObservableCollection<CalendarDayVm> _calendarDays = new();
    [ObservableProperty] private ObservableCollection<CalendarApptRow> _calendarDayAppointments = new();
    [ObservableProperty] private string _calendarSelectedDateLabel = "";
    [ObservableProperty] private bool   _calendarDayHasAppts;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalendarHasDaySelected))]
    [NotifyPropertyChangedFor(nameof(IsSelectedDayBlocked))]
    [NotifyPropertyChangedFor(nameof(CalendarBlockLabel))]
    private CalendarDayVm? _calendarSelectedDay;

    public bool CalendarHasDaySelected => CalendarSelectedDay != null;

    public bool IsSelectedDayBlocked =>
        CalendarSelectedDay != null && _calendarBlockedDates.Contains(CalendarSelectedDay.Date.Date);

    public string CalendarBlockLabel =>
        IsSelectedDayBlocked ? "Разблокировать день" : "Заблокировать день";

    public string CalendarMonthLabel =>
        new DateTime(CalendarYear, CalendarMonth, 1).ToString("MMMM yyyy", new System.Globalization.CultureInfo("ru-RU"));

    private List<Appointment> _calendarAllAppts = new();
    private readonly HashSet<DateTime> _calendarBlockedDates = new();

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

    public override async void OnNavigatedTo(object? parameter)
    {
        if (parameter is string s)
        {
            if (s == "stats")   SelectedTab = 2;
            if (s == "profile") SelectedTab = 3;
        }
        LoadProfile();
        LoadProfileTab();
        await Task.Yield();
        await LoadPropertiesAsync();
        await LoadAppointmentsAsync();
        await LoadStatsAsync();
    }

    private void LoadProfile()
    {
        var u = Session.CurrentUser;
        if (u == null) return;
        RealtorName        = u.FullName;
        RealtorPhone       = u.Phone ?? "—";
        RealtorEmail       = u.Email;
        RealtorDescription = string.IsNullOrWhiteSpace(u.Description) ? "Описание не заполнено." : u.Description;
    }

    // ===== Вкладки =====
    [RelayCommand] private void SetTab0() => SelectedTab = 0;
    [RelayCommand] private void SetTab1() => SelectedTab = 1;
    [RelayCommand] private void SetTab2() => SelectedTab = 2;
    [RelayCommand] private void SetTab3() => SelectedTab = 3;

    // ===== Фильтры статистики =====
    [RelayCommand] private async Task SetStatPeriodAllAsync()     { SelectedStatPeriod = "all";     await LoadStatsAsync(); }
    [RelayCommand] private async Task SetStatPeriodMonthAsync()   { SelectedStatPeriod = "month";   await LoadStatsAsync(); }
    [RelayCommand] private async Task SetStatPeriodQuarterAsync() { SelectedStatPeriod = "quarter"; await LoadStatsAsync(); }
    [RelayCommand] private async Task SetStatPeriodYearAsync()    { SelectedStatPeriod = "year";    await LoadStatsAsync(); }

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
            OnPropertyChanged(nameof(FilteredMyProperties));
        }
        finally { IsPropsLoading = false; }
    }

    [RelayCommand]
    private void OpenDetail(PropertyCardViewModel card) =>
        _navigationService.NavigateTo<PropertyDetailViewModel>(card.Id);

    [RelayCommand]
    private async Task DeletePropertyAsync(PropertyCardViewModel card)
    {
        // Проверяем активные записи
        var appts = await _appointmentService.GetByRealtorIdAsync(Session.CurrentUser!.Id);
        var hasActive = appts.Any(a => a.PropertyId == card.Id && a.Status is "new" or "confirmed");
        if (hasActive)
        {
            System.Windows.MessageBox.Show(
                "Нельзя удалить объект с активными записями клиентов (новые или подтверждённые). Сначала отмените или завершите записи.",
                "Удаление невозможно",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        var result = System.Windows.MessageBox.Show(
            $"Удалить объект «{card.Title}»?\nЭто действие необратимо, все фотографии будут удалены.",
            "Подтверждение удаления",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);
        if (result != System.Windows.MessageBoxResult.Yes) return;

        await _propertyService.DeleteAsync(card.Id);
        await LoadPropertiesAsync();
        await LoadStatsAsync();
    }

    [RelayCommand]
    private async Task OpenEditFormAsync(PropertyCardViewModel card)
    {
        var property = await _propertyService.GetByIdAsync(card.Id);
        if (property == null) return;

        EditPropertyId   = property.Id;
        EditTitle        = property.Title;
        EditDescription  = property.Description;
        EditPrice        = property.Price.ToString(CultureInfo.InvariantCulture);
        EditArea         = property.Area.ToString(CultureInfo.InvariantCulture);
        EditRooms        = property.Rooms.ToString();
        EditBathrooms    = property.Bathrooms?.ToString() ?? "";
        EditFloor        = property.Floor?.ToString() ?? "";
        EditTotalFloors  = property.TotalFloors?.ToString() ?? "";
        EditYearBuilt    = property.YearBuilt?.ToString() ?? "";
        EditCity         = property.City;
        EditDistrict     = property.District ?? "";
        EditAddress      = property.Address;
        EditType         = property.PropertyType;
        EditHasRepair    = property.HasRepair;
        EditMortgage     = property.MortgageAvailable;
        EditError        = null;

        EditImages.Clear();
        if (property.Images != null)
        {
            foreach (var img in property.Images.OrderByDescending(i => i.IsMain).ThenBy(i => i.SortOrder))
                EditImages.Add(new ImagePreviewVm { ExistingId = img.Id, FileName = img.FileName, Data = img.ImageData, IsMain = img.IsMain });
        }

        IsEditFormVisible = true;
    }

    [RelayCommand]
    private void CloseEditForm() => IsEditFormVisible = false;

    [RelayCommand]
    private void AddEditImage()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Выберите изображение",
            Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
            Multiselect = true
        };
        if (dlg.ShowDialog() != true) return;

        foreach (var file in dlg.FileNames)
        {
            try
            {
                var bytes = File.ReadAllBytes(file);
                var name = Path.GetFileName(file);
                bool isMain = EditImages.Count == 0;
                EditImages.Add(new ImagePreviewVm { FileName = name, Data = bytes, IsMain = isMain });
            }
            catch { }
        }
    }

    [RelayCommand]
    private void RemoveEditImage(ImagePreviewVm img) => EditImages.Remove(img);

    [RelayCommand]
    private async Task SaveEditAsync()
    {
        EditError = null;
        if (string.IsNullOrWhiteSpace(EditTitle) || string.IsNullOrWhiteSpace(EditDescription)
            || string.IsNullOrWhiteSpace(EditPrice) || string.IsNullOrWhiteSpace(EditArea)
            || string.IsNullOrWhiteSpace(EditRooms) || string.IsNullOrWhiteSpace(EditCity)
            || string.IsNullOrWhiteSpace(EditAddress))
        { EditError = "Заполните все обязательные поля."; return; }

        var priceStr = EditPrice.Replace(',', '.').Replace(" ", "");
        if (!decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) || price <= 0)
        { EditError = "Укажите корректную цену."; return; }

        var areaStr = EditArea.Replace(',', '.').Replace(" ", "");
        if (!double.TryParse(areaStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var area) || area <= 0)
        { EditError = "Укажите корректную площадь."; return; }

        if (!int.TryParse(EditRooms.Trim(), out var rooms) || rooms <= 0)
        { EditError = "Укажите количество комнат."; return; }

        IsEditSaving = true;
        try
        {
            await _propertyService.UpdatePropertyDetailsAsync(
                EditPropertyId,
                EditTitle.Trim(), EditDescription.Trim(), price, area, rooms,
                int.TryParse(EditBathrooms.Trim(), out var baths) ? baths : null,
                int.TryParse(EditFloor.Trim(), out var f) ? f : null,
                int.TryParse(EditTotalFloors.Trim(), out var tf) ? tf : null,
                int.TryParse(EditYearBuilt.Trim(), out var yb) ? yb : null,
                EditCity.Trim(),
                string.IsNullOrWhiteSpace(EditDistrict) ? null : EditDistrict.Trim(),
                EditAddress.Trim(),
                EditType, EditHasRepair, EditMortgage);

            // Удаляем из БД фото, которые пользователь убрал
            var keepIds = EditImages.Where(i => i.ExistingId.HasValue).Select(i => i.ExistingId!.Value).ToHashSet();
            var dbImages = await _context.Set<MSM.Models.Entities.PropertyImage>()
                .Where(i => i.PropertyId == EditPropertyId).ToListAsync();
            foreach (var old in dbImages.Where(i => !keepIds.Contains(i.Id)))
                _context.Set<MSM.Models.Entities.PropertyImage>().Remove(old);

            // Добавляем новые фото
            bool anyMain = dbImages.Any(i => i.IsMain && keepIds.Contains(i.Id));
            int sortIdx = dbImages.Count(i => keepIds.Contains(i.Id));
            foreach (var newImg in EditImages.Where(i => !i.ExistingId.HasValue))
            {
                _context.Set<MSM.Models.Entities.PropertyImage>().Add(new MSM.Models.Entities.PropertyImage
                {
                    PropertyId = EditPropertyId,
                    ImageData  = newImg.Data,
                    FileName   = newImg.FileName,
                    SortOrder  = sortIdx++,
                    IsMain     = !anyMain
                });
                anyMain = true;
            }
            await _context.SaveChangesAsync();

            IsEditFormVisible = false;
            await LoadPropertiesAsync();
            await LoadStatsAsync();
        }
        catch (Exception ex) { EditError = $"Ошибка: {ex.Message}"; }
        finally { IsEditSaving = false; }
    }

    [RelayCommand]
    private async Task SetSoldAsync(PropertyCardViewModel card)
    {
        var result = System.Windows.MessageBox.Show(
            $"Пометить объект «{card.Title}» как «Продан»?\nПосле этого запись на просмотр будет недоступна. Вы сможете вернуть объект в продажу через кнопку «↩ В продажу».",
            "Подтверждение продажи",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);
        if (result != System.Windows.MessageBoxResult.Yes) return;

        await _propertyService.UpdateStatusAsync(card.Id, "sold");
        await LoadPropertiesAsync();
        await LoadStatsAsync();
    }

    [RelayCommand]
    private async Task SetActiveFromSoldAsync(PropertyCardViewModel card)
    {
        var result = System.Windows.MessageBox.Show(
            $"Вернуть объект «{card.Title}» в активные продажи?\nОн снова появится в каталоге для клиентов.",
            "Подтверждение",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);
        if (result != System.Windows.MessageBoxResult.Yes) return;

        await _propertyService.UpdateStatusAsync(card.Id, "active");
        await LoadPropertiesAsync();
        await LoadStatsAsync();
    }

    [RelayCommand]
    private async Task ToggleHiddenAsync(PropertyCardViewModel card)
    {
        var newStatus = card.Status == "hidden" ? "active" : "hidden";
        await _propertyService.UpdateStatusAsync(card.Id, newStatus);
        await LoadPropertiesAsync();
        await LoadStatsAsync();
    }

    [RelayCommand]
    private void ToggleForm()
    {
        IsFormVisible = !IsFormVisible;
        FormError = null;
        FormValidated = false;
        FormSuccess = false;
    }

    // Загрузить изображение через диалог
    [RelayCommand]
    private void AddImage()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Выберите изображение",
            Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
            Multiselect = true
        };
        if (dlg.ShowDialog() != true) return;

        foreach (var file in dlg.FileNames)
        {
            try
            {
                var bytes = File.ReadAllBytes(file);
                var name = Path.GetFileName(file);
                bool isMain = _pendingImages.Count == 0;
                _pendingImages.Add((Data: bytes, FileName: name, IsMain: isMain));
                FormImages.Add(new ImagePreviewVm { FileName = name, Data = bytes, IsMain = isMain });
            }
            catch { /* пропустить повреждённый файл */ }
        }
    }

    [RelayCommand]
    private void RemoveImage(ImagePreviewVm img)
    {
        int idx = FormImages.IndexOf(img);
        FormImages.Remove(img);
        if (idx >= 0 && idx < _pendingImages.Count)
            _pendingImages.RemoveAt(idx);

        // пересчитать IsMain
        if (_pendingImages.Count > 0 && !_pendingImages.Any(x => x.IsMain))
        {
            var first = _pendingImages[0];
            _pendingImages[0] = (first.Data, first.FileName, true);
            if (FormImages.Count > 0) FormImages[0].IsMain = true;
        }
    }

    [RelayCommand]
    private async Task SavePropertyAsync()
    {
        FormValidated = true;
        FormError = null;
        if (string.IsNullOrWhiteSpace(FormTitle) || string.IsNullOrWhiteSpace(FormDescription)
            || string.IsNullOrWhiteSpace(FormPrice) || string.IsNullOrWhiteSpace(FormArea)
            || string.IsNullOrWhiteSpace(FormRooms) || string.IsNullOrWhiteSpace(FormCity)
            || string.IsNullOrWhiteSpace(FormAddress))
        { FormError = "Заполните все обязательные поля."; return; }

        // принимаем и запятую, и точку как разделитель дробной части
        var priceStr = FormPrice.Replace(',', '.').Replace(" ", "");
        if (!decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) || price <= 0)
        { FormError = "Укажите корректную цену (например: 4500000)."; return; }

        var areaStr = FormArea.Replace(',', '.').Replace(" ", "");
        if (!double.TryParse(areaStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var area) || area <= 0)
        { FormError = "Укажите корректную площадь (например: 78.5 или 78,5)."; return; }

        if (!int.TryParse(FormRooms.Trim(), out var rooms) || rooms <= 0)
        { FormError = "Укажите количество комнат (целое число)."; return; }

        IsSaving = true;
        try
        {
            var property = new Property
            {
                Title = FormTitle.Trim(), Description = FormDescription.Trim(),
                Price = price, Area = area, Rooms = rooms,
                Bathrooms   = int.TryParse(FormBathrooms.Trim(),   out var baths) ? baths : null,
                Floor       = int.TryParse(FormFloor.Trim(),       out var f)  ? f  : null,
                TotalFloors = int.TryParse(FormTotalFloors.Trim(),  out var tf) ? tf : null,
                YearBuilt   = int.TryParse(FormYearBuilt.Trim(),   out var yb) ? yb : null,
                City = FormCity.Trim(), Address = FormAddress.Trim(),
                PropertyType = FormType, HasRepair = FormHasRepair,
                MortgageAvailable = FormMortgage,
                RealtorId = Session.CurrentUser!.Id,
            };
            await _propertyService.AddAsync(property, _pendingImages,
                string.IsNullOrWhiteSpace(FormDistrict) ? null : FormDistrict.Trim());
            IsFormVisible = false;
            ClearForm();
            FormSuccess = true;
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
            _allAppointmentRows = items.Select(a => new RealtorAppointmentRow(a)).ToList();
            NewAppointmentsCount = _allAppointmentRows.Count(r => r.StatusRaw == "new");

            // Заполняем список уникальных объектов для фильтра
            AppointmentPropertyTitles.Clear();
            AppointmentPropertyTitles.Add("");
            foreach (var title in _allAppointmentRows.Select(r => r.PropertyTitle).Distinct().OrderBy(t => t))
                AppointmentPropertyTitles.Add(title);

            ApplyAppointmentFilter();
        }
        finally { IsApptLoading = false; }
    }

    private void ApplyAppointmentFilter()
    {
        var filtered = _allAppointmentRows.AsEnumerable();
        if (!string.IsNullOrEmpty(ApptFilterProperty))
            filtered = filtered.Where(a => a.PropertyTitle == ApptFilterProperty);
        if (!string.IsNullOrEmpty(ApptFilterStatus))
            filtered = filtered.Where(a => a.StatusRaw == ApptFilterStatus);
        filtered = ApptDateFilter switch
        {
            "today" => filtered.Where(a => a.SlotStart.Date == System.DateTime.Today),
            "week"  => filtered.Where(a => a.SlotStart.Date >= System.DateTime.Today.AddDays(-7)),
            "month" => filtered.Where(a => a.SlotStart.Date >= new System.DateTime(System.DateTime.Today.Year, System.DateTime.Today.Month, 1)),
            _       => filtered
        };

        Appointments.Clear();
        foreach (var a in filtered) Appointments.Add(a);
        NoAppointments = Appointments.Count == 0;
    }

    partial void OnApptFilterPropertyChanged(string value) => ApplyAppointmentFilter();
    partial void OnApptFilterStatusChanged(string value)   => ApplyAppointmentFilter();
    partial void OnApptDateFilterChanged(string value)     => ApplyAppointmentFilter();

    [RelayCommand] private void SetApptDateAll()   => ApptDateFilter = "";
    [RelayCommand] private void SetApptDateToday() => ApptDateFilter = "today";
    [RelayCommand] private void SetApptDateWeek()  => ApptDateFilter = "week";
    [RelayCommand] private void SetApptDateMonth() => ApptDateFilter = "month";

    [RelayCommand]
    private void ResetApptFilter()
    {
        ApptFilterProperty = "";
        ApptFilterStatus   = "";
        ApptDateFilter     = "";
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
    private DateTime? GetStatPeriodStart() => SelectedStatPeriod switch
    {
        "month"   => new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
        "quarter" => DateTime.Today.AddMonths(-3),
        "year"    => new DateTime(DateTime.Today.Year, 1, 1),
        _         => null
    };

    private static int CalcRealtorScore(double rating, int sold, int total, int completedAppt, int totalAppt, int active)
    {
        double r = (rating / 5.0) * 35;
        double s = total > 0     ? ((double)sold          / total)     * 30 : 0;
        double a = totalAppt > 0 ? ((double)completedAppt / totalAppt) * 20 : 0;
        double p = Math.Min(active / 5.0, 1.0) * 15;
        return (int)Math.Round(r + s + a + p);
    }

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

            var start = GetStatPeriodStart();
            var periodAppts = start.HasValue
                ? appts.Where(a => a.SlotStart >= start.Value).ToList()
                : appts;
            StatTotalAppt     = periodAppts.Count;
            StatCompletedAppt = periodAppts.Count(a => a.Status == "completed");

            StatRating = await _reviewService.GetAverageRatingAsync(realtorId: Session.CurrentUser.Id);

            var allCompleted = appts.Count(a => a.Status == "completed");
            var score = CalcRealtorScore(StatRating, StatSold, StatTotal, allCompleted, appts.Count, StatActive);
            StatScore      = score;
            StatScoreColor = score >= 70 ? "#7CB342" : score >= 40 ? "#FF8F00" : "#EF5350";

            StatusSeries = new ISeries[]
            {
                new PieSeries<int> { Values = new[]{ StatActive }, Name = "Активные",
                    Fill = new SolidColorPaint(SKColor.Parse("#27563A")) },
                new PieSeries<int> { Values = new[]{ StatSold },   Name = "Проданные",
                    Fill = new SolidColorPaint(SKColor.Parse("#F4B942")) },
                new PieSeries<int> { Values = new[]{ props.Count(p=>p.Status=="hidden") }, Name = "Скрытые",
                    Fill = new SolidColorPaint(SKColor.Parse("#9E9E9E")) },
            };

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
                    Fill = new SolidColorPaint(SKColor.Parse("#4A9061"))
                }
            };
            RatingXAxes = new Axis[]
            {
                new Axis { Labels = months.Select(m => m.ToString("MMM")).ToArray() }
            };
        }
        catch { /* статистика некритична */ }
    }

    // ===== Профиль =====
    [ObservableProperty] private string _profileFullName = "";
    [ObservableProperty] private string _profileEmail = "";
    [ObservableProperty] private string _profilePhone = "";
    [ObservableProperty] private string _profileDescription = "";
    [ObservableProperty] private byte[]? _profileAvatar;
    [ObservableProperty] private string _profileOldPassword = "";
    [ObservableProperty] private string _profileNewPassword = "";
    [ObservableProperty] private string _profileConfirmPassword = "";
    [ObservableProperty] private string? _profileResult;
    [ObservableProperty] private bool _profileSuccess;
    [ObservableProperty] private string? _passwordResult;
    [ObservableProperty] private bool _passwordSuccess;
    [ObservableProperty] private bool _isProfileSaving;

    public string ProfileLogin => Session.CurrentUser?.Login ?? "";

    private void LoadProfileTab()
    {
        var u = Session.CurrentUser;
        if (u == null) return;
        ProfileFullName     = u.FullName;
        ProfileEmail        = u.Email;
        ProfilePhone        = u.Phone ?? "";
        ProfileDescription  = u.Description ?? "";
        ProfileAvatar       = u.AvatarPhoto;
        ProfileResult       = null;
        PasswordResult      = null;
    }

    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        var user = Session.CurrentUser;
        if (user == null) return;
        if (string.IsNullOrWhiteSpace(ProfileFullName))
        { ProfileResult = "Имя не может быть пустым."; ProfileSuccess = false; return; }

        IsProfileSaving = true;
        ProfileResult = null;
        try
        {
            var authService = MSM.App.ServiceProvider.GetRequiredService<IAuthService>();
            user.FullName    = ProfileFullName.Trim();
            user.Email       = ProfileEmail.Trim();
            user.Phone       = string.IsNullOrWhiteSpace(ProfilePhone) ? null : ProfilePhone.Trim();
            user.Description = string.IsNullOrWhiteSpace(ProfileDescription) ? null : ProfileDescription.Trim();
            user.AvatarPhoto = ProfileAvatar;
            await authService.UpdateProfileAsync(user);
            // обновить шапку профиля
            RealtorName        = user.FullName;
            RealtorEmail       = user.Email;
            RealtorPhone       = user.Phone ?? "—";
            RealtorDescription = user.Description ?? "Описание не заполнено.";
            ProfileResult  = "Профиль сохранён!";
            ProfileSuccess = true;
        }
        catch (Exception ex) { ProfileResult = $"Ошибка: {ex.Message}"; ProfileSuccess = false; }
        finally { IsProfileSaving = false; }
    }

    [RelayCommand]
    private async Task ChangeProfilePasswordAsync()
    {
        var user = Session.CurrentUser;
        if (user == null) return;
        if (string.IsNullOrWhiteSpace(ProfileOldPassword) || string.IsNullOrWhiteSpace(ProfileNewPassword))
        { PasswordResult = "Заполните все поля."; PasswordSuccess = false; return; }
        if (ProfileNewPassword != ProfileConfirmPassword)
        { PasswordResult = "Пароли не совпадают."; PasswordSuccess = false; return; }
        if (ProfileNewPassword.Length < 6)
        { PasswordResult = "Минимум 6 символов."; PasswordSuccess = false; return; }

        IsProfileSaving = true;
        PasswordResult = null;
        try
        {
            var authService = MSM.App.ServiceProvider.GetRequiredService<IAuthService>();
            var ok = await authService.ChangePasswordAsync(user, ProfileOldPassword, ProfileNewPassword);
            if (ok) { PasswordResult = "Пароль изменён!"; PasswordSuccess = true; ProfileOldPassword = ProfileNewPassword = ProfileConfirmPassword = ""; }
            else    { PasswordResult = "Неверный текущий пароль."; PasswordSuccess = false; }
        }
        catch (Exception ex) { PasswordResult = $"Ошибка: {ex.Message}"; PasswordSuccess = false; }
        finally { IsProfileSaving = false; }
    }

    [RelayCommand]
    private void UploadProfileAvatar()
    {
        var dlg = new OpenFileDialog { Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp", Title = "Фото профиля" };
        if (dlg.ShowDialog() == true) ProfileAvatar = System.IO.File.ReadAllBytes(dlg.FileName);
    }

    [RelayCommand]
    private void RemoveProfileAvatar() => ProfileAvatar = null;

    [RelayCommand]
    private void GoBack() => _navigationService.GoBack();

    // ===== Календарь =====
    [RelayCommand]
    private async Task OpenCalendarAsync()
    {
        var user = Session.CurrentUser;
        if (user == null) return;
        _calendarAllAppts = await _context.Appointments
            .Include(a => a.Client)
            .Include(a => a.Property)
            .Where(a => a.RealtorId == user.Id)
            .ToListAsync();

        var blocked = await _appointmentService.GetBlockedSchedulesAsync(user.Id);
        _calendarBlockedDates.Clear();
        foreach (var s in blocked)
        {
            for (var d = s.SlotStart.Date; d <= s.SlotEnd.Date; d = d.AddDays(1))
                _calendarBlockedDates.Add(d);
        }

        CalendarYear  = DateTime.Today.Year;
        CalendarMonth = DateTime.Today.Month;
        CalendarSelectedDay        = null;
        BuildCalendarDays();
        CalendarDayAppointments.Clear();
        CalendarDayHasAppts        = false;
        CalendarSelectedDateLabel  = "";
        ShowCalendarOverlay        = true;
    }

    [RelayCommand]
    private void CloseCalendar() => ShowCalendarOverlay = false;

    [RelayCommand]
    private void CalendarPrev()
    {
        if (CalendarMonth == 1) { CalendarMonth = 12; CalendarYear--; }
        else CalendarMonth--;
        OnPropertyChanged(nameof(CalendarMonthLabel));
        BuildCalendarDays();
    }

    [RelayCommand]
    private void CalendarNext()
    {
        if (CalendarMonth == 12) { CalendarMonth = 1; CalendarYear++; }
        else CalendarMonth++;
        OnPropertyChanged(nameof(CalendarMonthLabel));
        BuildCalendarDays();
    }

    [RelayCommand]
    private void SelectCalendarDay(CalendarDayVm day)
    {
        if (day.IsEmpty || !day.IsCurrentMonth) return;
        CalendarSelectedDay = day;
        CalendarSelectedDateLabel = day.Date.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("ru-RU"));
        var dayAppts = _calendarAllAppts
            .Where(a => a.SlotStart.Date == day.Date)
            .OrderBy(a => a.SlotStart)
            .ToList();
        CalendarDayAppointments.Clear();
        foreach (var a in dayAppts)
        {
            var (status, color) = a.Status switch
            {
                "new"       => ("Новая",        "#7A7A7A"),
                "confirmed" => ("Подтверждена", "#7CB342"),
                "cancelled" => ("Отменена",     "#EF5350"),
                "completed" => ("Завершена",    "#D4A5A5"),
                _           => (a.Status,       "#9E9E9E")
            };
            CalendarDayAppointments.Add(new CalendarApptRow
            {
                ClientName    = a.Client?.FullName ?? "—",
                PropertyTitle = a.Property?.Title  ?? "—",
                Time          = $"{a.SlotStart:HH:mm}–{a.SlotEnd:HH:mm}",
                Status        = status,
                StatusColor   = color
            });
        }
        CalendarDayHasAppts = CalendarDayAppointments.Count > 0;
    }

    private void BuildCalendarDays()
    {
        CalendarDays.Clear();
        var firstDay = new DateTime(CalendarYear, CalendarMonth, 1);
        int startDow = ((int)firstDay.DayOfWeek + 6) % 7; // Mon=0

        for (int i = 0; i < startDow; i++)
            CalendarDays.Add(new CalendarDayVm { IsEmpty = true });

        int daysInMonth = System.DateTime.DaysInMonth(CalendarYear, CalendarMonth);
        for (int d = 1; d <= daysInMonth; d++)
        {
            var date      = new DateTime(CalendarYear, CalendarMonth, d);
            bool isBlocked = _calendarBlockedDates.Contains(date);
            int count     = _calendarAllAppts.Count(a => a.SlotStart.Date == date);
            string dot    = isBlocked  ? "#9E9E9E"
                          : count == 0 ? "Transparent"
                          : count >= 4 ? "#EF5350"
                          : count >= 2 ? "#FF8F00"
                          : "#7CB342";
            CalendarDays.Add(new CalendarDayVm
            {
                Day              = d,
                Date             = date,
                IsCurrentMonth   = true,
                IsToday          = date == DateTime.Today,
                IsBlocked        = isBlocked,
                AppointmentCount = count,
                DotColor         = dot,
                DayForeground    = isBlocked ? "#9E9E9E" : "#222222"
            });
        }

        while (CalendarDays.Count % 7 != 0)
            CalendarDays.Add(new CalendarDayVm { IsEmpty = true });
    }

    [RelayCommand]
    private async Task ToggleBlockDayAsync()
    {
        var day  = CalendarSelectedDay;
        var user = Session.CurrentUser;
        if (day == null || user == null) return;

        var date = day.Date.Date;
        var slotStart = date;
        var slotEnd   = date.AddDays(1).AddSeconds(-1);

        if (_calendarBlockedDates.Contains(date))
        {
            // Remove block — delete all RealtorSchedule entries for this date
            var toRemove = await _context.RealtorSchedules
                .Where(s => s.RealtorId == user.Id && !s.IsAvailable
                         && s.SlotStart.Date == date)
                .ToListAsync();
            _context.RealtorSchedules.RemoveRange(toRemove);
            await _context.SaveChangesAsync();
            _calendarBlockedDates.Remove(date);
        }
        else
        {
            // Add block
            _context.RealtorSchedules.Add(new RealtorSchedule
            {
                RealtorId  = user.Id,
                SlotStart  = slotStart,
                SlotEnd    = slotEnd,
                IsAvailable = false
            });
            await _context.SaveChangesAsync();
            _calendarBlockedDates.Add(date);
        }

        CalendarSelectedDay = null;
        BuildCalendarDays();
        CalendarDayAppointments.Clear();
        CalendarDayHasAppts       = false;
        CalendarSelectedDateLabel = "";
    }

    private void ClearForm()
    {
        FormValidated = false;
        FormTitle = FormDescription = FormPrice = FormArea = FormRooms = FormBathrooms = "";
        FormFloor = FormTotalFloors = FormYearBuilt = FormCity = FormDistrict = FormAddress = "";
        FormType = "apartment"; FormHasRepair = FormMortgage = false;
        FormImages.Clear();
        _pendingImages.Clear();
    }
}
