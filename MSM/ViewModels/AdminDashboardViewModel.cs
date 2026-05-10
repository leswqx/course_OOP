using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
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

public class UserRowViewModel
{
    public int Id { get; }
    public string FullName { get; }
    public string Login { get; }
    public string Email { get; }
    public string RoleDisplay { get; }
    public string Role { get; }
    public string Phone { get; }
    public string CreatedAt { get; }
    public bool IsBlocked { get; }
    public bool CanPromote => Role == "client" && !IsBlocked;
    public bool CanDemote  => Role == "realtor" && !IsBlocked;
    public string BlockLabel => IsBlocked ? "🔓 Разблокировать" : "🔒 Заблокировать";
    public string BlockedBadge => IsBlocked ? "ЗАБЛОКИРОВАН" : "";
    public bool ShowBlockedBadge => IsBlocked;

    public UserRowViewModel(User u)
    {
        Id = u.Id; FullName = u.FullName; Login = u.Login; Email = u.Email;
        Role = u.Role; Phone = u.Phone ?? "—";
        IsBlocked = u.IsBlocked;
        RoleDisplay = u.Role switch { "admin" => "Администратор", "realtor" => "Риелтор", _ => "Клиент" };
        CreatedAt = u.CreatedAt.ToString("dd.MM.yyyy");
    }
}

public class ReviewRowViewModel
{
    public int Id { get; }
    public string Author { get; }
    public string RealtorName { get; }
    public string Rating { get; }
    public string? Comment { get; }
    public string Date { get; }

    public ReviewRowViewModel(Review r)
    {
        Id = r.Id;
        Author      = r.User?.FullName   ?? "—";
        RealtorName = r.Realtor?.FullName ?? "—";
        Rating  = new string('★', r.Rating) + new string('☆', 5 - r.Rating);
        Comment = r.Comment;
        Date    = r.CreatedAt.ToString("dd.MM.yyyy");
    }
}

public class RealtorSummaryRow
{
    public int    Id            { get; init; }
    public string Name          { get; init; } = "";
    public int    TotalProps    { get; init; }
    public int    ActiveProps   { get; init; }
    public int    SoldProps     { get; init; }
    public int    TotalAppt     { get; init; }
    public int    CompletedAppt { get; init; }
    public int    PendingAppt   { get; init; }
    public string Rating        { get; init; } = "—";
    public double RatingValue   { get; init; }
    public string Color         { get; init; } = "#D4A5A5";
    public int    Score         { get; init; }
    public int    Rank          { get; init; }
    public string ScoreColor    { get; init; } = "#7A7A7A";
    public string RevenueText   { get; init; } = "—";
    public double ScoreBarWidth => Score * 1.8;
    public string RankEmoji     => Rank switch { 1 => "🥇", 2 => "🥈", 3 => "🥉", _ => $"#{Rank}" };
    public string TrendArrow    { get; init; } = "→";
    public string TrendColor    { get; init; } = "#9E9E9E";
}

public class AlertItem
{
    public string RealtorName { get; init; } = "";
    public string AlertText   { get; init; } = "";
    public string AlertColor  { get; init; } = "#EF5350";
    public string Icon        { get; init; } = "⚠";
    public string Severity    { get; init; } = "Внимание";
}

public class DetailReviewRow
{
    public string ClientName  { get; init; } = "";
    public string Stars       { get; init; } = "";
    public string Comment     { get; init; } = "";
    public string Date        { get; init; } = "";
    public int    RatingValue { get; init; }
    public string StarColor   => RatingValue >= 4 ? "#7CB342" : RatingValue >= 3 ? "#FF8F00" : "#EF5350";
}

public class DetailPropertyRow
{
    public string Title       { get; init; } = "";
    public string StatusLabel { get; init; } = "";
    public string StatusColor { get; init; } = "#9E9E9E";
    public string Price       { get; init; } = "";
    public string City        { get; init; } = "";
}

// Панель администратора: пользователи · модерация · статистика
public partial class AdminDashboardViewModel : ViewModelBase
{
    private readonly IReviewService _reviewService;
    private readonly INavigationService _navigationService;
    private readonly AppDbContext _context;

    // ===== Вкладки =====
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowTab0), nameof(ShowTab1), nameof(ShowTab2), nameof(ShowTab3), nameof(ShowTab4))]
    private int _selectedTab;
    public bool ShowTab0 => SelectedTab == 0;
    public bool ShowTab1 => SelectedTab == 1;
    public bool ShowTab2 => SelectedTab == 2;
    public bool ShowTab3 => SelectedTab == 3;
    public bool ShowTab4 => SelectedTab == 4;

    // ===== Профиль =====
    [ObservableProperty] private string _profileFullName = "";
    [ObservableProperty] private string _profileEmail = "";
    [ObservableProperty] private string _profilePhone = "";
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

    // ===== Пользователи =====
    [ObservableProperty] private ObservableCollection<UserRowViewModel> _users = new();
    [ObservableProperty] private bool _isUsersLoading;
    [ObservableProperty] private string? _userActionMessage;
    [ObservableProperty] private string _userSearch = "";
    [ObservableProperty] private string _userRoleFilter = "all"; // all, realtor, client, blocked

    private List<UserRowViewModel> _allUsers = new();

    // ===== Отзывы =====
    [ObservableProperty] private ObservableCollection<ReviewRowViewModel> _pendingReviews = new();
    [ObservableProperty] private bool _isReviewsLoading;
    [ObservableProperty] private bool _noReviews;
    [ObservableProperty] private int _pendingReviewsCount;
    [ObservableProperty] private string _reviewRealtorFilter = "";
    private List<ReviewRowViewModel> _allPendingReviews = new();
    public ObservableCollection<string> ReviewRealtorNames { get; } = new();

    // ===== Статистика =====
    [ObservableProperty] private int _statTotalProps;
    [ObservableProperty] private int _statActiveProps;
    [ObservableProperty] private int _statSoldProps;
    [ObservableProperty] private int _statTotalUsers;
    [ObservableProperty] private int _statRealtors;
    [ObservableProperty] private string _statRevenueText = "—";
    [ObservableProperty] private string _statAvgDealText = "—";
    [ObservableProperty] private int _statNewClients;
    [ObservableProperty] private int _statPendingAppts;
    [ObservableProperty] private ISeries[] _propStatusSeries = Array.Empty<ISeries>();
    [ObservableProperty] private ISeries[] _agencySalesSeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _agencySalesXAxes = Array.Empty<Axis>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsChartPeriod6m), nameof(IsChartPeriod1y), nameof(IsChartPeriodAll))]
    private string _agencyChartPeriod = "6m";
    public bool IsChartPeriod6m  => AgencyChartPeriod == "6m";
    public bool IsChartPeriod1y  => AgencyChartPeriod == "1y";
    public bool IsChartPeriodAll => AgencyChartPeriod == "all";

    // Период фильтра
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPeriodAll), nameof(IsPeriodMonth),
                              nameof(IsPeriodLastMonth), nameof(IsPeriodQuarter), nameof(IsPeriodYear))]
    private string _selectedPeriod = "all";
    public bool IsPeriodAll       => SelectedPeriod == "all";
    public bool IsPeriodMonth     => SelectedPeriod == "month";
    public bool IsPeriodLastMonth => SelectedPeriod == "lastmonth";
    public bool IsPeriodQuarter   => SelectedPeriod == "quarter";
    public bool IsPeriodYear      => SelectedPeriod == "year";

    // ===== Рейтинг + детальный риелтор =====
    [ObservableProperty] private ObservableCollection<RealtorSummaryRow> _realtorSummary = new();
    private List<RealtorSummaryRow> _allRealtorSummary = new();
    private bool _showAllRealtors;
    [ObservableProperty] private bool _showMoreRealtorsButton;
    [ObservableProperty] private string _showMoreRealtorsLabel = "";
    [ObservableProperty] private bool _hasSelectedRealtor;
    [ObservableProperty] private string _detailRealtorName = "";
    [ObservableProperty] private string _detailColor = "#D4A5A5";
    [ObservableProperty] private int _detailScore;
    [ObservableProperty] private string _detailScoreColor = "#7A7A7A";
    [ObservableProperty] private int _detailTotal;
    [ObservableProperty] private int _detailActive;
    [ObservableProperty] private int _detailSold;
    [ObservableProperty] private int _detailTotalAppt;
    [ObservableProperty] private int _detailCompletedAppt;
    [ObservableProperty] private string _detailRatingText = "—";
    [ObservableProperty] private string _detailRevenueText = "—";
    [ObservableProperty] private string _detailAvgDealText = "—";
    [ObservableProperty] private ISeries[] _detailStatusSeries = Array.Empty<ISeries>();
    [ObservableProperty] private ISeries[] _detailApptSeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _detailApptXAxes = Array.Empty<Axis>();

    // ===== Среднее по команде =====
    [ObservableProperty] private int _teamAvgScore;
    [ObservableProperty] private string _teamAvgColor = "#9E9E9E";

    // ===== Тревожные сигналы =====
    [ObservableProperty] private ObservableCollection<AlertItem> _alerts = new();
    [ObservableProperty] private bool _hasAlerts;

    // ===== Вкладки детальной панели =====
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowDetailTab0), nameof(ShowDetailTab1), nameof(ShowDetailTab2))]
    private int _detailTab;
    public bool ShowDetailTab0 => DetailTab == 0;
    public bool ShowDetailTab1 => DetailTab == 1;
    public bool ShowDetailTab2 => DetailTab == 2;

    [ObservableProperty] private ObservableCollection<DetailReviewRow> _detailReviews = new();
    [ObservableProperty] private ObservableCollection<DetailPropertyRow> _detailProperties = new();
    [ObservableProperty] private bool _noDetailReviews;
    [ObservableProperty] private bool _noDetailProperties;

    private static readonly string[] _palette =
        { "#4A9061", "#7CB342", "#42A5F5", "#FF8F00", "#AB47BC", "#26A69A", "#EF5350", "#66BB6A", "#FFA726", "#5C6BC0" };

    private readonly INotificationService _notificationService;

    // ===== Рассылка =====
    [ObservableProperty] private string _mailSubject = "";
    [ObservableProperty] private string _mailBody = "";
    [ObservableProperty] private string _mailRecipients = "clients"; // all, clients, realtors
    [ObservableProperty] private string? _mailResult;
    [ObservableProperty] private bool _mailSuccess;
    [ObservableProperty] private bool _isSendingMail;

    public AdminDashboardViewModel(
        IReviewService reviewService,
        INavigationService navigationService,
        INotificationService notificationService,
        AppDbContext context)
    {
        _reviewService = reviewService;
        _navigationService = navigationService;
        _notificationService = notificationService;
        _context = context;
    }

    public override async void OnNavigatedTo(object? parameter)
    {
        if (parameter is string s && s == "profile")
            SelectedTab = 3;
        LoadProfileTab();
        await Task.Yield();
        await LoadUsersAsync();
        await LoadReviewsAsync();
        await LoadStatsAsync();
    }

    // ===== Вкладки =====
    [RelayCommand] private void SetTab0() => SelectedTab = 0;
    [RelayCommand] private void SetTab1() => SelectedTab = 1;
    [RelayCommand] private void SetTab2() => SelectedTab = 2;
    [RelayCommand] private void SetTab3() => SelectedTab = 3;
    [RelayCommand] private void SetTab4() => SelectedTab = 4;

    [RelayCommand] private void SetDetailTab0() => DetailTab = 0;
    [RelayCommand] private void SetDetailTab1() => DetailTab = 1;
    [RelayCommand] private void SetDetailTab2() => DetailTab = 2;

    // ===== Период статистики =====
    [RelayCommand] private async Task SetPeriodAllAsync()       { SelectedPeriod = "all";       await LoadStatsAsync(); }
    [RelayCommand] private async Task SetPeriodMonthAsync()     { SelectedPeriod = "month";     await LoadStatsAsync(); }
    [RelayCommand] private async Task SetPeriodLastMonthAsync() { SelectedPeriod = "lastmonth"; await LoadStatsAsync(); }
    [RelayCommand] private async Task SetPeriodQuarterAsync()   { SelectedPeriod = "quarter";   await LoadStatsAsync(); }
    [RelayCommand] private async Task SetPeriodYearAsync()      { SelectedPeriod = "year";      await LoadStatsAsync(); }

    [RelayCommand] private async Task SetChartPeriod6mAsync()  { AgencyChartPeriod = "6m";  await RebuildAgencyChartAsync(); }
    [RelayCommand] private async Task SetChartPeriod1yAsync()  { AgencyChartPeriod = "1y";  await RebuildAgencyChartAsync(); }
    [RelayCommand] private async Task SetChartPeriodAllAsync() { AgencyChartPeriod = "all"; await RebuildAgencyChartAsync(); }

    private async Task RebuildAgencyChartAsync()
    {
        var props = await _context.Properties.ToListAsync();
        BuildAgencyChart(props);
    }

    private void BuildAgencyChart(List<Property> props)
    {
        List<DateTime> months;
        if (AgencyChartPeriod == "1y")
            months = Enumerable.Range(0, 12).Select(i => DateTime.Today.AddMonths(-11 + i)).ToList();
        else if (AgencyChartPeriod == "all")
        {
            var first = props.Where(p => p.Status == "sold").Select(p => p.UpdatedAt).DefaultIfEmpty(DateTime.Today).Min();
            int totalMonths = Math.Max(1, (int)((DateTime.Today - first).TotalDays / 30));
            totalMonths = Math.Min(totalMonths, 24);
            months = Enumerable.Range(0, totalMonths).Select(i => DateTime.Today.AddMonths(-totalMonths + 1 + i)).ToList();
        }
        else
            months = Enumerable.Range(0, 6).Select(i => DateTime.Today.AddMonths(-5 + i)).ToList();

        var counts = months.Select(m =>
            props.Count(p => p.Status == "sold" && p.UpdatedAt.Year == m.Year && p.UpdatedAt.Month == m.Month)).ToArray();
        AgencySalesSeries = new ISeries[]
        {
            new ColumnSeries<int> { Values = counts, Name = "Продано", Fill = new SolidColorPaint(SKColor.Parse("#7CB342")) }
        };
        AgencySalesXAxes = new Axis[] { new Axis { Labels = months.Select(m => m.ToString("MMM yy")).ToArray() } };
    }

    // ===== Пользователи =====
    private async Task LoadUsersAsync()
    {
        IsUsersLoading = true;
        try
        {
            var users = await _context.Users
                .OrderBy(u => u.IsBlocked).ThenBy(u => u.Role).ThenBy(u => u.FullName)
                .ToListAsync();
            _allUsers = users.Select(u => new UserRowViewModel(u)).ToList();
            ApplyUserFilter();
        }
        finally { IsUsersLoading = false; }
    }

    private void ApplyUserFilter()
    {
        var filtered = _allUsers.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(UserSearch))
        {
            var q = UserSearch.Trim().ToLower();
            filtered = filtered.Where(u =>
                u.FullName.ToLower().Contains(q) ||
                u.Login.ToLower().Contains(q) ||
                u.Email.ToLower().Contains(q));
        }

        filtered = UserRoleFilter switch
        {
            "realtor"  => filtered.Where(u => u.Role == "realtor"),
            "client"   => filtered.Where(u => u.Role == "client"),
            "blocked"  => filtered.Where(u => u.IsBlocked),
            _          => filtered
        };

        Users.Clear();
        foreach (var u in filtered) Users.Add(u);
    }

    [RelayCommand]
    private void SearchUsers() => ApplyUserFilter();

    [RelayCommand]
    private void FilterAll()     { UserRoleFilter = "all";     ApplyUserFilter(); }
    [RelayCommand]
    private void FilterRealtor() { UserRoleFilter = "realtor"; ApplyUserFilter(); }
    [RelayCommand]
    private void FilterClient()  { UserRoleFilter = "client";  ApplyUserFilter(); }
    [RelayCommand]
    private void FilterBlocked() { UserRoleFilter = "blocked"; ApplyUserFilter(); }

    [RelayCommand]
    private async Task PromoteToRealtorAsync(int userId) => await ChangeRoleAsync(userId, "realtor");

    [RelayCommand]
    private async Task DemoteToClientAsync(int userId) => await ChangeRoleAsync(userId, "client");

    private async Task ChangeRoleAsync(int userId, string newRole)
    {
        UserActionMessage = null;
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return;

        var roleLabel = newRole == "realtor" ? "Риелтор" : "Клиент";
        var result = System.Windows.MessageBox.Show(
            $"Изменить роль пользователя «{user.FullName}» на «{roleLabel}»?",
            "Подтверждение смены роли",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);
        if (result != System.Windows.MessageBoxResult.Yes) return;

        user.Role = newRole;
        await _context.SaveChangesAsync();
        UserActionMessage = $"Роль {user.FullName} изменена на «{roleLabel}»";

        // Уведомляем пользователя по email
        _ = _notificationService.SendEmailAsync(user.Email, user.FullName,
            $"Изменение роли в {(_notificationService is MSM.Services.EmailNotificationService ? "системе" : "системе")}",
            $"<p>Здравствуйте, <strong>{user.FullName}</strong>!</p>" +
            $"<p>Ваша роль в системе изменена на <strong>«{roleLabel}»</strong>.</p>" +
            $"<p>Для применения изменений перезайдите в систему.</p>");

        await LoadUsersAsync();
        await LoadStatsAsync();
    }

    [RelayCommand]
    private async Task ToggleBlockAsync(int userId)
    {
        UserActionMessage = null;
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return;

        var action = user.IsBlocked ? "разблокировать" : "заблокировать";
        var result = System.Windows.MessageBox.Show(
            $"Вы уверены, что хотите {action} пользователя «{user.FullName}»?",
            "Подтверждение",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);
        if (result != System.Windows.MessageBoxResult.Yes) return;

        user.IsBlocked = !user.IsBlocked;
        await _context.SaveChangesAsync();
        UserActionMessage = user.IsBlocked
            ? $"Пользователь {user.FullName} заблокирован."
            : $"Пользователь {user.FullName} разблокирован.";
        await LoadUsersAsync();
    }

    // ===== Отзывы =====
    private async Task LoadReviewsAsync()
    {
        IsReviewsLoading = true;
        try
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Realtor)
                .Where(r => !r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            PendingReviewsCount = reviews.Count;

            _allPendingReviews = reviews.Select(r => new ReviewRowViewModel(r)).ToList();

            // Заполняем список имён риелторов для фильтра
            ReviewRealtorNames.Clear();
            ReviewRealtorNames.Add("");
            foreach (var name in reviews
                .Where(r => r.Realtor != null)
                .Select(r => r.Realtor!.FullName)
                .Distinct().OrderBy(n => n))
                ReviewRealtorNames.Add(name);

            ApplyReviewFilter();
        }
        finally { IsReviewsLoading = false; }
    }

    private void ApplyReviewFilter()
    {
        var filtered = string.IsNullOrEmpty(ReviewRealtorFilter)
            ? _allPendingReviews
            : _allPendingReviews.Where(r => r.RealtorName == ReviewRealtorFilter).ToList();

        PendingReviews.Clear();
        foreach (var r in filtered) PendingReviews.Add(r);
        NoReviews = PendingReviews.Count == 0;
    }

    partial void OnReviewRealtorFilterChanged(string value) => ApplyReviewFilter();

    [RelayCommand]
    private async Task ApproveReviewAsync(int id)
    {
        await _reviewService.ApproveAsync(id);
        await LoadReviewsAsync();
    }

    [RelayCommand]
    private async Task RejectReviewAsync(int id)
    {
        await _reviewService.RejectAsync(id);
        await LoadReviewsAsync();
    }

    // ===== Статистика =====
    private DateTime? GetPeriodStart() => SelectedPeriod switch
    {
        "month"     => new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
        "lastmonth" => new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1),
        "quarter"   => DateTime.Today.AddMonths(-3),
        "year"      => new DateTime(DateTime.Today.Year, 1, 1),
        _           => null
    };

    private DateTime? GetPeriodEnd() => SelectedPeriod == "lastmonth"
        ? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)
        : null;

    private static int CalcScore(double rating, int sold, int total, int completedAppt, int totalAppt, int active)
    {
        double r = (rating / 5.0) * 35;
        double s = total > 0      ? ((double)sold          / total)     * 30 : 0;
        double a = totalAppt > 0  ? ((double)completedAppt / totalAppt) * 20 : 0;
        double p = Math.Min(active / 5.0, 1.0) * 15;
        return (int)Math.Round(r + s + a + p);
    }

    private static string ScoreColor(int score) => score switch
    {
        >= 70 => "#7CB342",
        >= 40 => "#FF8F00",
        _     => "#EF5350"
    };

    private static string FormatMoney(decimal amount) =>
        amount >= 1_000_000 ? $"{amount / 1_000_000:F1} млн BYN" : $"{amount:N0} BYN";

    private async Task LoadStatsAsync()
    {
        try
        {
            var props    = await _context.Properties.ToListAsync();
            var users    = await _context.Users.ToListAsync();
            var allAppts = await _context.Appointments.ToListAsync();
            var realtors = users.Where(u => u.Role == "realtor").ToList();

            // Глобальные счётчики — всегда за всё время
            StatTotalProps  = props.Count;
            StatActiveProps = props.Count(p => p.Status == "active");
            StatSoldProps   = props.Count(p => p.Status == "sold");
            StatTotalUsers  = users.Count;
            StatRealtors    = realtors.Count;

            PropStatusSeries = new ISeries[]
            {
                new PieSeries<int> { Values = new[]{ StatActiveProps },                         Name = "Активные",
                    Fill = new SolidColorPaint(SKColor.Parse("#27563A")) },
                new PieSeries<int> { Values = new[]{ StatSoldProps },                           Name = "Проданные",
                    Fill = new SolidColorPaint(SKColor.Parse("#F4B942")) },
                new PieSeries<int> { Values = new[]{ props.Count(p => p.Status == "hidden") },  Name = "Скрытые",
                    Fill = new SolidColorPaint(SKColor.Parse("#9E9E9E")) },
            };

            // Записи за выбранный период
            var from = GetPeriodStart();
            var to   = GetPeriodEnd();
            var periodAppts = allAppts
                .Where(a => (from == null || a.SlotStart >= from) && (to == null || a.SlotStart < to))
                .ToList();

            // Выручка и средний чек за период (используем UpdatedAt как дату продажи)
            var soldProps = props.Where(p => p.Status == "sold"
                && (from == null || p.UpdatedAt >= from)
                && (to   == null || p.UpdatedAt <  to)).ToList();
            decimal totalRevenue = soldProps.Sum(p => p.Price);
            StatRevenueText = soldProps.Count > 0 ? FormatMoney(totalRevenue) : "—";
            StatAvgDealText = soldProps.Count > 0 ? FormatMoney(totalRevenue / soldProps.Count) : "—";

            // Новые клиенты за период
            StatNewClients = users.Count(u => u.Role == "client"
                && (from == null || u.CreatedAt >= from)
                && (to   == null || u.CreatedAt <  to));

            // Всего новых (ожидающих подтверждения) записей
            StatPendingAppts = periodAppts.Count(a => a.Status == "new");

            // График продаж агентства
            BuildAgencyChart(props);

            var thisMonthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var lastMonthStart = thisMonthStart.AddMonths(-1);
            var staleThreshold = DateTime.Today.AddDays(-30); // объект активен > 30 дней

            var rawList = new List<RealtorSummaryRow>();
            for (int i = 0; i < realtors.Count; i++)
            {
                var r      = realtors[i];
                var rProps = props.Where(p => p.RealtorId == r.Id).ToList();
                var rAppts = periodAppts.Where(a => a.RealtorId == r.Id).ToList();
                var rating = await _reviewService.GetAverageRatingAsync(realtorId: r.Id);
                var color  = _palette[i % _palette.Length];
                int active = rProps.Count(p => p.Status == "active");
                int sold   = rProps.Count(p => p.Status == "sold");
                int completed = rAppts.Count(a => a.Status == "completed");
                int pending   = rAppts.Count(a => a.Status == "new");
                int score  = CalcScore(rating, sold, rProps.Count, completed, rAppts.Count, active);

                // Выручка риелтора (все проданные объекты за всё время)
                decimal rRevenue = rProps.Where(p => p.Status == "sold").Sum(p => p.Price);
                string revenueText = sold > 0 ? FormatMoney(rRevenue) : "—";

                // Тренд: завершённые записи этого месяца vs прошлого
                int thisM = allAppts.Count(a => a.RealtorId == r.Id && a.Status == "completed" && a.SlotStart >= thisMonthStart);
                int lastM = allAppts.Count(a => a.RealtorId == r.Id && a.Status == "completed" && a.SlotStart >= lastMonthStart && a.SlotStart < thisMonthStart);
                string trend      = thisM > lastM ? "↑" : thisM < lastM ? "↓" : "→";
                string trendColor = trend == "↑" ? "#7CB342" : trend == "↓" ? "#EF5350" : "#9E9E9E";

                rawList.Add(new RealtorSummaryRow
                {
                    Id            = r.Id,
                    Name          = r.FullName,
                    TotalProps    = rProps.Count,
                    ActiveProps   = active,
                    SoldProps     = sold,
                    TotalAppt     = rAppts.Count,
                    CompletedAppt = completed,
                    PendingAppt   = pending,
                    Rating        = rating > 0 ? $"{rating:F1} ★" : "—",
                    RatingValue   = rating,
                    Color         = color,
                    Score         = score,
                    ScoreColor    = ScoreColor(score),
                    RevenueText   = revenueText,
                    TrendArrow    = trend,
                    TrendColor    = trendColor
                });
            }

            // Сортируем по Score и назначаем ранги
            var sorted = rawList.OrderByDescending(r => r.Score).ToList();
            _allRealtorSummary = sorted.Select((src, i) => new RealtorSummaryRow
            {
                Id            = src.Id,
                Name          = src.Name,
                TotalProps    = src.TotalProps,
                ActiveProps   = src.ActiveProps,
                SoldProps     = src.SoldProps,
                TotalAppt     = src.TotalAppt,
                CompletedAppt = src.CompletedAppt,
                PendingAppt   = src.PendingAppt,
                Rating        = src.Rating,
                RatingValue   = src.RatingValue,
                Color         = src.Color,
                Score         = src.Score,
                ScoreColor    = src.ScoreColor,
                RevenueText   = src.RevenueText,
                TrendArrow    = src.TrendArrow,
                TrendColor    = src.TrendColor,
                Rank          = i + 1
            }).ToList();
            _showAllRealtors = false;
            ApplyRealtorSummaryLimit();

            // Среднее по команде
            if (rawList.Count > 0)
            {
                TeamAvgScore = (int)rawList.Average(r => r.Score);
                TeamAvgColor = ScoreColor(TeamAvgScore);
            }

            // Тревожные сигналы (всегда за всё время)
            Alerts.Clear();
            foreach (var row in rawList)
            {
                var allR      = allAppts.Where(a => a.RealtorId == row.Id).ToList();
                int allCompl  = allR.Count(a => a.Status == "completed");
                int allCancel = allR.Count(a => a.Status == "cancelled");
                var rProps    = props.Where(p => p.RealtorId == row.Id).ToList();
                int staleCount = rProps.Count(p => p.Status == "active" && p.CreatedAt < staleThreshold);

                if (row.TotalProps == 0)
                    Alerts.Add(new AlertItem { RealtorName = row.Name, AlertText = "нет объектов в системе",             Icon = "📭", AlertColor = "#9E9E9E", Severity = "Инфо" });
                else if (row.ActiveProps == 0)
                    Alerts.Add(new AlertItem { RealtorName = row.Name, AlertText = "нет активных объектов",              Icon = "⚠",  AlertColor = "#FF8F00", Severity = "Внимание" });

                if (row.SoldProps == 0)
                    Alerts.Add(new AlertItem { RealtorName = row.Name, AlertText = "ни одной продажи за всё время",      Icon = "📉", AlertColor = "#FF8F00", Severity = "Внимание" });

                if (row.RatingValue > 0 && row.RatingValue < 3.5)
                    Alerts.Add(new AlertItem { RealtorName = row.Name, AlertText = $"низкий рейтинг ({row.Rating})",    Icon = "⭐", AlertColor = "#EF5350", Severity = "Критично" });

                if (row.Score < 30 && row.TotalProps > 0)
                    Alerts.Add(new AlertItem { RealtorName = row.Name, AlertText = $"критически низкий балл ({row.Score}/100)", Icon = "🚨", AlertColor = "#EF5350", Severity = "Критично" });

                if (allR.Count >= 3 && allCompl == 0)
                    Alerts.Add(new AlertItem { RealtorName = row.Name, AlertText = "есть записи, но ни одной завершённой", Icon = "📋", AlertColor = "#FF8F00", Severity = "Внимание" });

                if (allR.Count >= 5 && (double)allCancel / allR.Count > 0.5)
                    Alerts.Add(new AlertItem { RealtorName = row.Name, AlertText = $"высокий % отмен ({allCancel * 100 / allR.Count}%)", Icon = "❌", AlertColor = "#EF5350", Severity = "Критично" });

                if (staleCount > 0)
                    Alerts.Add(new AlertItem { RealtorName = row.Name, AlertText = $"{staleCount} объект(ов) без продажи >30 дней", Icon = "🕐", AlertColor = "#FF8F00", Severity = "Внимание" });
            }
            HasAlerts = Alerts.Count > 0;
        }
        catch { /* некритично */ }
    }

    private void ApplyRealtorSummaryLimit()
    {
        const int top = 5;
        var visible = _showAllRealtors ? _allRealtorSummary : _allRealtorSummary.Take(top).ToList();
        RealtorSummary.Clear();
        foreach (var r in visible) RealtorSummary.Add(r);

        ShowMoreRealtorsButton = _allRealtorSummary.Count > top;
        int rest = _allRealtorSummary.Count - top;
        ShowMoreRealtorsLabel = _showAllRealtors
            ? "Свернуть ▲"
            : $"Показать всех  (+{rest}) ▼";
    }

    [RelayCommand]
    private void ToggleShowAllRealtors()
    {
        _showAllRealtors = !_showAllRealtors;
        ApplyRealtorSummaryLimit();
    }

    [RelayCommand]
    private async Task SelectRealtorAsync(RealtorSummaryRow row)
    {
        DetailTab          = 0;
        HasSelectedRealtor = true;
        DetailRealtorName  = row.Name;
        DetailColor        = row.Color;
        DetailScore        = row.Score;
        DetailScoreColor   = row.ScoreColor;
        try
        {
            var rProps = await _context.Properties.Where(p => p.RealtorId == row.Id).ToListAsync();

            // Записи за выбранный период
            var from = GetPeriodStart();
            var to   = GetPeriodEnd();
            var rAppts = await _context.Appointments
                .Where(a => a.RealtorId == row.Id
                    && (from == null || a.SlotStart >= from)
                    && (to   == null || a.SlotStart <  to))
                .ToListAsync();

            var rating = await _reviewService.GetAverageRatingAsync(realtorId: row.Id);

            DetailTotal         = rProps.Count;
            DetailActive        = rProps.Count(p => p.Status == "active");
            DetailSold          = rProps.Count(p => p.Status == "sold");
            DetailTotalAppt     = rAppts.Count;
            DetailCompletedAppt = rAppts.Count(a => a.Status == "completed");
            DetailRatingText    = rating > 0 ? $"{rating:F1} ★" : "—";

            // Финансовые показатели риелтора (все проданные объекты, не зависит от периода)
            var soldByRealtor = rProps.Where(p => p.Status == "sold").ToList();
            decimal rRevenue = soldByRealtor.Sum(p => p.Price);
            DetailRevenueText = soldByRealtor.Count > 0 ? FormatMoney(rRevenue) : "—";
            DetailAvgDealText = soldByRealtor.Count > 0 ? FormatMoney(rRevenue / soldByRealtor.Count) : "—";

            DetailStatusSeries = new ISeries[]
            {
                new PieSeries<int> { Values = new[]{ DetailActive }, Name = "Активные",
                    Fill = new SolidColorPaint(SKColor.Parse("#27563A")) },
                new PieSeries<int> { Values = new[]{ DetailSold }, Name = "Проданные",
                    Fill = new SolidColorPaint(SKColor.Parse("#F4B942")) },
                new PieSeries<int> { Values = new[]{ rProps.Count(p => p.Status == "hidden") }, Name = "Скрытые",
                    Fill = new SolidColorPaint(SKColor.Parse("#9E9E9E")) },
            };

            // Записи по последним 6 месяцам (для детального графика всегда показываем динамику)
            var months   = Enumerable.Range(0, 6).Select(i => DateTime.Today.AddMonths(-5 + i)).ToList();
            var allRAppts = await _context.Appointments.Where(a => a.RealtorId == row.Id).ToListAsync();
            var counts   = months.Select(m =>
                allRAppts.Count(a => a.SlotStart.Year == m.Year && a.SlotStart.Month == m.Month)).ToArray();
            DetailApptSeries = new ISeries[]
            {
                new ColumnSeries<int>
                {
                    Values = counts, Name = "Записи",
                    Fill   = new SolidColorPaint(SKColor.Parse(row.Color))
                }
            };
            DetailApptXAxes = new Axis[] { new Axis { Labels = months.Select(m => m.ToString("MMM")).ToArray() } };

            // Отзывы
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.RealtorId == row.Id && r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            DetailReviews.Clear();
            foreach (var rev in reviews)
                DetailReviews.Add(new DetailReviewRow
                {
                    ClientName  = rev.User?.FullName ?? "—",
                    Stars       = new string('★', rev.Rating) + new string('☆', 5 - rev.Rating),
                    Comment     = string.IsNullOrWhiteSpace(rev.Comment) ? "Без комментария" : rev.Comment,
                    Date        = rev.CreatedAt.ToString("dd.MM.yyyy"),
                    RatingValue = rev.Rating
                });
            NoDetailReviews = DetailReviews.Count == 0;

            // Объекты
            var allRPropsDetail = await _context.Properties
                .Where(p => p.RealtorId == row.Id)
                .OrderBy(p => p.Status)
                .ToListAsync();
            DetailProperties.Clear();
            foreach (var prop in allRPropsDetail)
                DetailProperties.Add(new DetailPropertyRow
                {
                    Title       = prop.Title,
                    StatusLabel = prop.Status switch { "active" => "Активен", "sold" => "Продан", "hidden" => "Скрыт", _ => prop.Status },
                    StatusColor = prop.Status switch { "active" => "#7CB342", "sold" => "#F4B942", _ => "#9E9E9E" },
                    Price       = $"{prop.Price:N0} BYN",
                    City        = prop.City
                });
            NoDetailProperties = DetailProperties.Count == 0;
        }
        catch { /* некритично */ }
    }

    [RelayCommand]
    private void GoBack() => _navigationService.GoBack();

    // ===== Рассылка =====
    [RelayCommand] private void MailToAll()      { MailRecipients = "all";      }
    [RelayCommand] private void MailToClients()  { MailRecipients = "clients";  }
    [RelayCommand] private void MailToRealtors() { MailRecipients = "realtors"; }

    [RelayCommand]
    private void UseTemplate(string templateKey)
    {
        (MailSubject, MailBody) = templateKey switch
        {
            "new_listings" => (
                "Новые объекты недвижимости в каталоге",
                "Уважаемый клиент!\n\nМы рады сообщить, что в нашем каталоге появились новые объекты недвижимости.\nПосетите наш сайт или откройте приложение, чтобы ознакомиться с актуальными предложениями.\n\nС уважением,\nКоманда MSM Недвижимость"),
            "price_drop" => (
                "Специальное предложение — снижение цен",
                "Уважаемый клиент!\n\nОбращаем ваше внимание, что на ряд объектов были снижены цены.\nЭто отличный момент, чтобы рассмотреть покупку недвижимости по выгодной стоимости.\n\nЗаходите в каталог и выбирайте!\n\nС уважением,\nКоманда MSM Недвижимость"),
            "open_showings" => (
                "Открытые показы — запишитесь уже сегодня",
                "Уважаемый клиент!\n\nПриглашаем вас на открытые показы объектов недвижимости.\nНаши риелторы готовы провести для вас экскурсию и ответить на все вопросы.\n\nЗаписаться можно прямо в приложении.\n\nС уважением,\nКоманда MSM Недвижимость"),
            _ => (MailSubject, MailBody)
        };
    }

    [RelayCommand]
    private async Task SendMailAsync()
    {
        if (string.IsNullOrWhiteSpace(MailSubject) || string.IsNullOrWhiteSpace(MailBody))
        { MailResult = "Заполните тему и текст письма."; MailSuccess = false; return; }

        IsSendingMail = true;
        MailResult = null;
        try
        {
            var query = _context.Users.Where(u => !u.IsBlocked && u.Email != null);
            query = MailRecipients switch
            {
                "clients"  => query.Where(u => u.Role == "client"),
                "realtors" => query.Where(u => u.Role == "realtor"),
                _          => query
            };
            var recipients = await query.ToListAsync();

            if (!recipients.Any())
            { MailResult = "Нет получателей."; MailSuccess = false; return; }

            await _notificationService.SendBulkEmailAsync(recipients, MailSubject, MailBody);
            MailResult  = $"Письмо отправлено {recipients.Count} получателям.";
            MailSuccess = true;
            MailSubject = MailBody = "";
        }
        catch (Exception ex) { MailResult = $"Ошибка отправки: {ex.Message}"; MailSuccess = false; }
        finally { IsSendingMail = false; }
    }

    // ===== Профиль =====
    private void LoadProfileTab()
    {
        var u = Session.CurrentUser;
        if (u == null) return;
        ProfileFullName    = u.FullName;
        ProfileEmail       = u.Email;
        ProfilePhone       = u.Phone ?? "";
        ProfileAvatar      = u.AvatarPhoto;
        ProfileResult      = null;
        PasswordResult     = null;
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
            user.AvatarPhoto = ProfileAvatar;
            await authService.UpdateProfileAsync(user);
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
        var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp" };
        if (dlg.ShowDialog() == true) ProfileAvatar = System.IO.File.ReadAllBytes(dlg.FileName);
    }

    [RelayCommand]
    private void RemoveProfileAvatar() => ProfileAvatar = null;
}
