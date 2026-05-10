using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using MSM.Models;
using MSM.Models.Entities;
using MSM.Services.Interfaces;
using SkiaSharp;

namespace MSM.ViewModels;

public class PriceHistoryRowVm
{
    public string Date        { get; init; } = "";
    public string Price       { get; init; } = "";
    public string Change      { get; init; } = "";
    public string ChangeColor { get; init; } = "#888888";
}

public class BookingSlotVm
{
    public string Time        { get; init; } = "";
    public bool   IsAvailable { get; init; }
}

public partial class PropertyDetailViewModel : ViewModelBase
{
    private readonly IPropertyService    _propertyService;
    private readonly IFavoriteService    _favoriteService;
    private readonly IAppointmentService _appointmentService;
    private readonly INavigationService  _navigationService;

    private List<BitmapImage>       _images          = new();
    private List<Appointment>       _realtorAppts    = new();
    private HashSet<DateTime>       _blockedDates    = new();

    [ObservableProperty] private Property?    _property;
    [ObservableProperty] private BitmapImage? _currentImage;
    [ObservableProperty] private bool         _isLoading;
    [ObservableProperty] private string?      _errorMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FavoriteButtonText))]
    private bool _isFavorite;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ImageCounter))]
    private int _currentImageIndex;

    // ===== Lightbox =====
    [ObservableProperty] private bool         _isPhotoViewerOpen;
    [ObservableProperty] private BitmapImage? _fullScreenImage;

    // ===== Price History =====
    [ObservableProperty] private ObservableCollection<PriceHistoryRowVm> _priceHistoryRows = new();
    [ObservableProperty] private ISeries[] _priceHistorySeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[]    _priceHistoryXAxes  = Array.Empty<Axis>();
    [ObservableProperty] private bool      _hasPriceHistory;
    [ObservableProperty] private bool      _hasPriceChart;

    // ===== Booking Calendar =====
    [ObservableProperty] private bool _showBookingCalendar;
    [ObservableProperty] private int  _bookCalYear  = DateTime.Today.Year;
    [ObservableProperty] private int  _bookCalMonth = DateTime.Today.Month;
    [ObservableProperty] private ObservableCollection<CalendarDayVm>  _bookCalDays = new();
    [ObservableProperty] private ObservableCollection<BookingSlotVm> _bookSlots    = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BookSlotSelected))]
    private string _bookSelectedSlot = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BookDaySelected), nameof(BookSelectedDateLabel))]
    private DateTime? _bookSelectedDate;

    [ObservableProperty] private string  _scheduleComment   = "";
    [ObservableProperty] private bool    _scheduleSuccess;
    [ObservableProperty] private bool    _scheduleSlotTaken;
    [ObservableProperty] private string? _scheduleErrorText;
    [ObservableProperty] private bool    _isSubmittingSchedule;

    public bool   BookDaySelected        => BookSelectedDate.HasValue;
    public bool   BookSlotSelected       => !string.IsNullOrEmpty(BookSelectedSlot);
    public string BookCalMonthLabel      => new DateTime(BookCalYear, BookCalMonth, 1).ToString("MMMM yyyy", new CultureInfo("ru-RU"));
    public string BookSelectedDateLabel  => BookSelectedDate?.ToString("dddd, dd MMMM", new CultureInfo("ru-RU")) ?? "";

    public string ImageCounter    => _images.Count > 0 ? $"{CurrentImageIndex + 1} / {_images.Count}" : "";
    public bool   HasMultipleImages => _images.Count > 1;
    public bool   HasImages         => _images.Count > 0;
    public bool   IsClient          => Session.IsClient;
    public bool   IsBookable        => Session.IsClient && Property?.Status == "active";

    public string FavoriteButtonText => IsFavorite ? "★  В избранном" : "☆  В избранное";

    public string PropertyTypeDisplay => Property?.PropertyType switch
    {
        "apartment" => "Квартира",
        "house"     => "Дом",
        "complex"   => "Комплекс",
        _           => Property?.PropertyType ?? ""
    };

    public string StatusDisplay => Property?.Status switch
    {
        "active" => "Активен",
        "sold"   => "Продан",
        "hidden" => "Скрыт",
        _        => ""
    };

    public string FloorInfo
    {
        get
        {
            if (Property == null) return "";
            if (Property.Floor.HasValue && Property.TotalFloors.HasValue)
                return $"{Property.Floor} / {Property.TotalFloors}";
            return Property.Floor.HasValue ? Property.Floor.ToString()! : "—";
        }
    }

    public PropertyDetailViewModel(
        IPropertyService    propertyService,
        IFavoriteService    favoriteService,
        IAppointmentService appointmentService,
        INavigationService  navigationService)
    {
        _propertyService    = propertyService;
        _favoriteService    = favoriteService;
        _appointmentService = appointmentService;
        _navigationService  = navigationService;
    }

    public override void OnNavigatedTo(object? parameter)
    {
        if (parameter is int id) _ = LoadAsync(id);
    }

    private async Task LoadAsync(int id)
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            Property = await _propertyService.GetByIdAsync(id);
            if (Property == null) { ErrorMessage = "Объект не найден"; return; }

            _images = (Property.Images?
                .OrderByDescending(i => i.IsMain).ThenBy(i => i.SortOrder)
                .Select(i => ToImage(i.ImageData)).OfType<BitmapImage>()
                .ToList()) ?? new();

            CurrentImageIndex = 0;
            CurrentImage = _images.FirstOrDefault();
            OnPropertyChanged(nameof(ImageCounter));
            OnPropertyChanged(nameof(HasMultipleImages));
            OnPropertyChanged(nameof(HasImages));
            OnPropertyChanged(nameof(PropertyTypeDisplay));
            OnPropertyChanged(nameof(StatusDisplay));
            OnPropertyChanged(nameof(FloorInfo));
            OnPropertyChanged(nameof(IsBookable));

            if (Session.IsClient && Session.CurrentUser != null)
                IsFavorite = await _favoriteService.IsFavoriteAsync(Session.CurrentUser.Id, id);

            await LoadPriceHistoryAsync(id);
        }
        catch (Exception ex) { ErrorMessage = $"Ошибка: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    private async Task LoadPriceHistoryAsync(int propertyId)
    {
        var history = (await _propertyService.GetPriceHistoryAsync(propertyId)).ToList();
        HasPriceHistory = history.Count > 0;
        HasPriceChart   = history.Count >= 1;

        PriceHistoryRows.Clear();
        for (int i = 0; i < history.Count; i++)
        {
            var h    = history[i];
            var prev = i > 0 ? history[i - 1].Price : h.Price;
            var diff = h.Price - prev;
            PriceHistoryRows.Add(new PriceHistoryRowVm
            {
                Date        = h.ChangedAt.ToString("dd.MM.yyyy HH:mm"),
                Price       = $"{h.Price:N0} BYN",
                Change      = i == 0 ? "Начальная цена"
                            : diff > 0 ? $"▲ +{diff:N0} BYN"
                            : diff < 0 ? $"▼ {diff:N0} BYN"
                            : "— без изменений",
                ChangeColor = i == 0 ? "#9E9E9E"
                            : diff > 0 ? "#EF5350"
                            : diff < 0 ? "#7CB342"
                            : "#9E9E9E"
            });
        }

        if (HasPriceChart)
        {
            var values = history.Select(h => (double)h.Price).ToArray();
            var labels = history.Select(h => h.ChangedAt.ToString("dd.MM.yy")).ToArray();
            PriceHistorySeries = new ISeries[]
            {
                new LineSeries<double>
                {
                    Values          = values,
                    Name            = "Цена",
                    Fill            = null,
                    Stroke          = new SolidColorPaint(SKColor.Parse("#4CAF50")) { StrokeThickness = 2 },
                    GeometryFill    = new SolidColorPaint(SKColor.Parse("#4CAF50")),
                    GeometryStroke  = new SolidColorPaint(SKColor.Parse("#4CAF50")),
                    GeometrySize    = 8
                }
            };
            PriceHistoryXAxes = new Axis[]
            {
                new Axis { Labels = labels, TextSize = 11 }
            };
        }
    }

    // ── Карусель ──────────────────────────────────────────────

    [RelayCommand]
    private void NextImage()
    {
        if (_images.Count == 0) return;
        CurrentImageIndex = (CurrentImageIndex + 1) % _images.Count;
        CurrentImage = _images[CurrentImageIndex];
        if (IsPhotoViewerOpen) FullScreenImage = CurrentImage;
    }

    [RelayCommand]
    private void PreviousImage()
    {
        if (_images.Count == 0) return;
        CurrentImageIndex = (CurrentImageIndex - 1 + _images.Count) % _images.Count;
        CurrentImage = _images[CurrentImageIndex];
        if (IsPhotoViewerOpen) FullScreenImage = CurrentImage;
    }

    [RelayCommand]
    private void OpenPhotoViewer()
    {
        if (CurrentImage == null) return;
        FullScreenImage   = CurrentImage;
        IsPhotoViewerOpen = true;
    }

    [RelayCommand]
    private void ClosePhotoViewer() => IsPhotoViewerOpen = false;

    [RelayCommand]
    private async Task ToggleFavoriteAsync()
    {
        if (Session.CurrentUser == null || Property == null) return;
        try
        {
            if (IsFavorite) await _favoriteService.RemoveFromFavoritesAsync(Session.CurrentUser.Id, Property.Id);
            else            await _favoriteService.AddToFavoritesAsync(Session.CurrentUser.Id, Property.Id);
            IsFavorite = !IsFavorite;
        }
        catch (Exception ex) { ErrorMessage = $"Ошибка: {ex.Message}"; }
    }

    // ── Booking Calendar ──────────────────────────────────────

    [RelayCommand]
    private async Task OpenBookingCalendarAsync()
    {
        if (Property == null) return;
        _realtorAppts = (await _appointmentService.GetByRealtorIdAsync(Property.RealtorId)).ToList();

        var schedules = await _appointmentService.GetBlockedSchedulesAsync(Property.RealtorId);
        _blockedDates.Clear();
        foreach (var s in schedules)
        {
            for (var d = s.SlotStart.Date; d <= s.SlotEnd.Date; d = d.AddDays(1))
                _blockedDates.Add(d);
        }
        BookCalYear   = DateTime.Today.Year;
        BookCalMonth  = DateTime.Today.Month;
        BookSelectedDate   = null;
        BookSelectedSlot   = "";
        ScheduleComment    = "";
        ScheduleSuccess    = ScheduleSlotTaken = false;
        ScheduleErrorText  = null;
        BookSlots.Clear();
        OnPropertyChanged(nameof(BookCalMonthLabel));
        BuildBookCalendar();
        ShowBookingCalendar = true;
    }

    [RelayCommand]
    private void CloseBookingCalendar()
    {
        ShowBookingCalendar = false;
        ScheduleSuccess     = false;
    }

    [RelayCommand]
    private void BookCalPrev()
    {
        if (BookCalMonth == 1) { BookCalMonth = 12; BookCalYear--; }
        else BookCalMonth--;
        BookSelectedDate = null;
        BookSelectedSlot = "";
        BookSlots.Clear();
        OnPropertyChanged(nameof(BookCalMonthLabel));
        BuildBookCalendar();
    }

    [RelayCommand]
    private void BookCalNext()
    {
        if (BookCalMonth == 12) { BookCalMonth = 1; BookCalYear++; }
        else BookCalMonth++;
        BookSelectedDate = null;
        BookSelectedSlot = "";
        BookSlots.Clear();
        OnPropertyChanged(nameof(BookCalMonthLabel));
        BuildBookCalendar();
    }

    [RelayCommand]
    private void SelectBookDay(CalendarDayVm day)
    {
        if (day.IsEmpty || !day.IsCurrentMonth) return;
        BookSelectedDate = day.Date;
        BookSelectedSlot = "";
        ScheduleErrorText = null;
        BuildBookCalendar(day.Date);
        BuildBookSlots(day.Date);
    }

    [RelayCommand]
    private void SelectBookSlot(string time)
    {
        BookSelectedSlot  = time;
        ScheduleErrorText = null;
    }

    [RelayCommand]
    private async Task SubmitAppointmentAsync()
    {
        if (Property == null || Session.CurrentUser == null || !BookSelectedDate.HasValue) return;
        if (string.IsNullOrEmpty(BookSelectedSlot)) return;
        if (!TimeSpan.TryParse(BookSelectedSlot, out var time)) return;

        var slotStart = BookSelectedDate.Value.Date + time;
        var slotEnd   = slotStart.AddHours(1);

        IsSubmittingSchedule = true;
        ScheduleSuccess = ScheduleSlotTaken = false;
        ScheduleErrorText = null;

        try
        {
            // Проверка дубля: активная запись на этот объект у текущего клиента
            var clientAppts = await _appointmentService.GetByClientIdAsync(Session.CurrentUser.Id);
            var duplicate   = clientAppts.Any(a =>
                a.PropertyId == Property.Id && a.Status is "new" or "confirmed");
            if (duplicate)
            {
                ScheduleErrorText = "У вас уже есть активная запись на этот объект.";
                return;
            }

            var available = await _appointmentService.IsSlotAvailableAsync(
                Property.RealtorId, slotStart, slotEnd);
            if (!available) { ScheduleSlotTaken = true; return; }

            await _appointmentService.CreateAsync(
                Property.Id, Session.CurrentUser.Id, Property.RealtorId,
                slotStart, slotEnd,
                string.IsNullOrWhiteSpace(ScheduleComment) ? null : ScheduleComment);

            ScheduleSuccess    = true;
            ScheduleComment    = "";
            BookSelectedSlot   = "";
        }
        catch (Exception ex) { ScheduleErrorText = $"Ошибка: {ex.Message}"; }
        finally { IsSubmittingSchedule = false; }
    }

    private void BuildBookCalendar(DateTime? selected = null)
    {
        BookCalDays.Clear();
        var firstDay = new DateTime(BookCalYear, BookCalMonth, 1);
        int startDow = ((int)firstDay.DayOfWeek + 6) % 7;

        for (int i = 0; i < startDow; i++)
            BookCalDays.Add(new CalendarDayVm { IsEmpty = true });

        int daysInMonth = DateTime.DaysInMonth(BookCalYear, BookCalMonth);
        for (int d = 1; d <= daysInMonth; d++)
        {
            var date   = new DateTime(BookCalYear, BookCalMonth, d);
            bool isPast = date.Date < DateTime.Today;
            bool isSelected = selected.HasValue && selected.Value.Date == date;

            bool isBlocked = _blockedDates.Contains(date.Date);
            // Доступен ли день: не заблокирован, хотя бы один слот (9-18) не занят и дата не в прошлом
            bool hasFreeSLot = !isPast && !isBlocked && Enumerable.Range(9, 10).Any(h =>
                !_realtorAppts.Any(a =>
                    a.SlotStart.Date == date &&
                    a.SlotStart.Hour == h &&
                    a.Status != "cancelled"));

            string dot = isPast        ? "#CCCCCC"
                       : isBlocked     ? "#9E9E9E"
                       : isSelected    ? "#42A5F5"
                       : hasFreeSLot   ? "#7CB342"
                       : "#EF5350";

            BookCalDays.Add(new CalendarDayVm
            {
                Day             = d,
                Date            = date,
                IsCurrentMonth  = true,
                IsToday         = date == DateTime.Today,
                IsEmpty         = false,
                AppointmentCount = isSelected ? 1 : (hasFreeSLot ? 0 : 1),
                DotColor        = dot
            });
        }

        while (BookCalDays.Count % 7 != 0)
            BookCalDays.Add(new CalendarDayVm { IsEmpty = true });
    }

    private void BuildBookSlots(DateTime date)
    {
        BookSlots.Clear();
        bool isDayBlocked = _blockedDates.Contains(date.Date);
        for (int h = 9; h <= 18; h++)
        {
            bool taken = isDayBlocked || _realtorAppts.Any(a =>
                a.SlotStart.Date == date && a.SlotStart.Hour == h && a.Status != "cancelled");
            BookSlots.Add(new BookingSlotVm
            {
                Time        = $"{h:D2}:00",
                IsAvailable = !taken && date.Date >= DateTime.Today
            });
        }
    }

    [RelayCommand]
    private void GoToRealtorProfile()
    {
        if (Property == null) return;
        _navigationService.NavigateTo<RealtorProfileViewModel>(Property.RealtorId);
    }

    [RelayCommand]
    private void GoBack() => _navigationService.GoBack();

    private static BitmapImage? ToImage(byte[]? data)
    {
        if (data == null || data.Length == 0) return null;
        try
        {
            var img = new BitmapImage();
            using var ms = new MemoryStream(data);
            img.BeginInit();
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.StreamSource = ms;
            img.EndInit();
            img.Freeze();
            return img;
        }
        catch { return null; }
    }
}
