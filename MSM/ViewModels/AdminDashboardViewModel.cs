using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.EntityFrameworkCore;
using MSM.Data.Context;
using MSM.Helpers;
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
    public string BlockLabel => IsBlocked ? L.Get("Admin.Unblock", "Разблокировать") : L.Get("Admin.Block", "Заблокировать");
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
    public string SeverityKey { get; init; } = "warning";
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

public class AdminPropertyRow
{
    public int    Id              { get; init; }
    public string Title           { get; init; } = "";
    public string City            { get; init; } = "";
    public string Price           { get; init; } = "";
    public string StatusRaw       { get; init; } = "";
    public string StatusLabel     { get; init; } = "";
    public string StatusColor     { get; init; } = "#9E9E9E";
    public string RealtorName     { get; init; } = "";
    public string PropertyType    { get; init; } = "";
    public string CreatedAt       { get; init; } = "";
    public bool   CanToggleHide   => StatusRaw != "sold";
    public string ToggleHideLabel => StatusRaw == "hidden" ? "Показать" : "Скрыть";
}

public partial class AdminDashboardViewModel : ViewModelBase
{
    private readonly IReviewService _reviewService;
    private readonly INavigationService _navigationService;
    private readonly AppDbContext _context;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowTab0), nameof(ShowTab1), nameof(ShowTab2), nameof(ShowTab3), nameof(ShowTab4), nameof(ShowTab5))]
    private int _selectedTab;
    public bool ShowTab0 => SelectedTab == 0;
    public bool ShowTab1 => SelectedTab == 1;
    public bool ShowTab2 => SelectedTab == 2;
    public bool ShowTab3 => SelectedTab == 3;
    public bool ShowTab4 => SelectedTab == 4;
    public bool ShowTab5 => SelectedTab == 5;

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

    [ObservableProperty] private ObservableCollection<UserRowViewModel> _users = new();
    [ObservableProperty] private bool _isUsersLoading;
    [ObservableProperty] private string? _userActionMessage;
    [ObservableProperty] private string _userSearch = "";
    [ObservableProperty] private string _userRoleFilter = "all";

    [ObservableProperty] private ObservableCollection<ReviewRowViewModel> _pendingReviews = new();
    [ObservableProperty] private bool _isReviewsLoading;
    [ObservableProperty] private bool _noReviews;
    [ObservableProperty] private int _pendingReviewsCount;
    [ObservableProperty] private string _reviewRealtorFilter = "";
    public ObservableCollection<string> ReviewRealtorNames { get; } = new();

    [ObservableProperty] private int _statTotalProps;
    [ObservableProperty] private int _statActiveProps;
    [ObservableProperty] private int _statSoldProps;
    [ObservableProperty] private int _statTotalUsers;
    [ObservableProperty] private int _statRealtors;
    [ObservableProperty] private string _statRevenueText = "—";
    [ObservableProperty] private string _statAvgDealText = "—";
    [ObservableProperty] private int _statNewClients;
    [ObservableProperty] private int _statPendingAppts;
    [ObservableProperty] private ISeries[] _agencySalesSeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _agencySalesXAxes = Array.Empty<Axis>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsChartPeriod6m), nameof(IsChartPeriod1y), nameof(IsChartPeriodAll))]
    private string _agencyChartPeriod = "6m";
    public bool IsChartPeriod6m  => AgencyChartPeriod == "6m";
    public bool IsChartPeriod1y  => AgencyChartPeriod == "1y";
    public bool IsChartPeriodAll => AgencyChartPeriod == "all";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPeriodAll), nameof(IsPeriodMonth),
                              nameof(IsPeriodLastMonth), nameof(IsPeriodQuarter), nameof(IsPeriodYear))]
    private string _selectedPeriod = "all";
    public bool IsPeriodAll       => SelectedPeriod == "all";
    public bool IsPeriodMonth     => SelectedPeriod == "month";
    public bool IsPeriodLastMonth => SelectedPeriod == "lastmonth";
    public bool IsPeriodQuarter   => SelectedPeriod == "quarter";
    public bool IsPeriodYear      => SelectedPeriod == "year";

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
    [ObservableProperty] private ISeries[] _detailApptSeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _detailApptXAxes = Array.Empty<Axis>();

    [ObservableProperty] private int _teamAvgScore;
    [ObservableProperty] private string _teamAvgColor = "#9E9E9E";

    private List<AlertItem> _allAlerts = new();
    public int AllAlertsCount => _allAlerts.Count;
    [ObservableProperty] private ObservableCollection<AlertItem> _alerts = new();
    [ObservableProperty] private bool _hasAlerts;
    [ObservableProperty] private bool _isAlertsExpanded = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAlertAll), nameof(IsAlertInfo), nameof(IsAlertWarning), nameof(IsAlertCritical))]
    private string _alertSeverityFilter = "all";
    public bool IsAlertAll      => AlertSeverityFilter == "all";
    public bool IsAlertInfo     => AlertSeverityFilter == "info";
    public bool IsAlertWarning  => AlertSeverityFilter == "warning";
    public bool IsAlertCritical => AlertSeverityFilter == "critical";

    [ObservableProperty] private string _alertRealtorFilter = "";
    public ObservableCollection<string> AlertRealtorNames { get; } = new();

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
    [ObservableProperty] private bool _hasMoreDetailReviews;
    private const int DetailReviewPageSize = 5;
    private int _detailReviewRealtorId;
    private int _detailReviewPage;
    private int _detailReviewTotal;

    private static readonly string[] _palette =
        { "#4A9061", "#7CB342", "#42A5F5", "#FF8F00", "#AB47BC", "#26A69A", "#EF5350", "#66BB6A", "#FFA726", "#5C6BC0" };

    private readonly INotificationService _notificationService;
    private readonly bool[] _tabLoaded = new bool[6];

    [ObservableProperty] private string _mailSubject = "";
    [ObservableProperty] private string _mailBody = "";
    [ObservableProperty] private string _mailRecipients = "clients";
    [ObservableProperty] private string? _mailResult;
    [ObservableProperty] private bool _mailSuccess;
    [ObservableProperty] private bool _isSendingMail;

    private bool _suppressReviewReload;
    private bool _suppressAdminPropReload;
    private readonly SemaphoreSlim _reviewSem    = new(1, 1);
    private readonly SemaphoreSlim _adminPropSem = new(1, 1);

    [ObservableProperty] private ObservableCollection<AdminPropertyRow> _adminProperties = new();
    [ObservableProperty] private bool _isAdminPropsLoading;
    [ObservableProperty] private string _adminPropSearch = "";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAdminPropFilterAll), nameof(IsAdminPropFilterActive),
                              nameof(IsAdminPropFilterHidden), nameof(IsAdminPropFilterSold))]
    private string _adminPropStatusFilter = "all";
    [ObservableProperty] private string _adminPropRealtorFilter = "";
    [ObservableProperty] private string? _adminPropActionMessage;
    [ObservableProperty] private int _adminPropsCount;
    public bool IsAdminPropFilterAll    => AdminPropStatusFilter == "all";
    public bool IsAdminPropFilterActive => AdminPropStatusFilter == "active";
    public bool IsAdminPropFilterHidden => AdminPropStatusFilter == "hidden";
    public bool IsAdminPropFilterSold   => AdminPropStatusFilter == "sold";
    public ObservableCollection<string> AdminRealtorNames { get; } = new();
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
        Array.Clear(_tabLoaded, 0, _tabLoaded.Length);
        await Task.Yield();
        try
        {
            PendingReviewsCount = await _context.Reviews.CountAsync(r => !r.IsApproved);
            await EnsureTabLoadedAsync(SelectedTab);
        }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }
    }

    private async Task EnsureTabLoadedAsync(int tab)
    {
        switch (tab)
        {
            case 0:
                if (!_tabLoaded[0]) { await LoadUsersAsync(); _tabLoaded[0] = true; }
                break;
            case 1:
                await LoadReviewsAsync(); _tabLoaded[1] = true;
                break;
            case 2: case 4:
                if (!_tabLoaded[2]) { await LoadStatsAsync(); _tabLoaded[2] = _tabLoaded[4] = true; }
                break;
            case 5:
                if (!_tabLoaded[5]) { await LoadAdminPropertiesAsync(); _tabLoaded[5] = true; }
                break;
        }
    }

    [RelayCommand] private async Task SetTab0Async() { SelectedTab = 0; await EnsureTabLoadedAsync(0); }
    [RelayCommand] private async Task SetTab1Async() { SelectedTab = 1; await EnsureTabLoadedAsync(1); }
    [RelayCommand] private async Task SetTab2Async() { SelectedTab = 2; await EnsureTabLoadedAsync(2); }
    [RelayCommand] private void SetTab3() { SelectedTab = 3; }
    [RelayCommand] private async Task SetTab4Async() { SelectedTab = 4; await EnsureTabLoadedAsync(4); }
    [RelayCommand] private async Task SetTab5Async() { SelectedTab = 5; await EnsureTabLoadedAsync(5); }

    [RelayCommand] private void SetDetailTab0() => DetailTab = 0;
    [RelayCommand] private void SetDetailTab1() => DetailTab = 1;
    [RelayCommand] private void SetDetailTab2() => DetailTab = 2;

    [RelayCommand] private async Task SetPeriodAllAsync()       { SelectedPeriod = "all";       await LoadStatsAsync(); }
    [RelayCommand] private async Task SetPeriodMonthAsync()     { SelectedPeriod = "month";     await LoadStatsAsync(); }
    [RelayCommand] private async Task SetPeriodLastMonthAsync() { SelectedPeriod = "lastmonth"; await LoadStatsAsync(); }
    [RelayCommand] private async Task SetPeriodQuarterAsync()   { SelectedPeriod = "quarter";   await LoadStatsAsync(); }
    [RelayCommand] private async Task SetPeriodYearAsync()      { SelectedPeriod = "year";      await LoadStatsAsync(); }

    [RelayCommand] private async Task SetChartPeriod6mAsync()  { AgencyChartPeriod = "6m";  await BuildAgencyChartAsync(); }
    [RelayCommand] private async Task SetChartPeriod1yAsync()  { AgencyChartPeriod = "1y";  await BuildAgencyChartAsync(); }
    [RelayCommand] private async Task SetChartPeriodAllAsync() { AgencyChartPeriod = "all"; await BuildAgencyChartAsync(); }

    private async Task BuildAgencyChartAsync()
    {
        List<DateTime> months;
        if (AgencyChartPeriod == "1y")
            months = Enumerable.Range(0, 12).Select(i => DateTime.Today.AddMonths(-11 + i)).ToList();
        else if (AgencyChartPeriod == "all")
        {
            var firstDate = await _context.Properties
                .Where(p => p.Status == "sold")
                .Select(p => (DateTime?)p.UpdatedAt)
                .MinAsync();
            var first = firstDate ?? DateTime.Today;
            int totalMonths = Math.Max(1, (int)((DateTime.Today - first).TotalDays / 30));
            totalMonths = Math.Min(totalMonths, 24);
            months = Enumerable.Range(0, totalMonths).Select(i => DateTime.Today.AddMonths(-totalMonths + 1 + i)).ToList();
        }
        else
            months = Enumerable.Range(0, 6).Select(i => DateTime.Today.AddMonths(-5 + i)).ToList();

        var rangeFrom = months.First();
        var rangeTo   = months.Last().AddMonths(1);
        var soldDates = await _context.Properties
            .Where(p => p.Status == "sold" && p.UpdatedAt >= rangeFrom && p.UpdatedAt < rangeTo)
            .Select(p => new { p.UpdatedAt.Year, p.UpdatedAt.Month })
            .ToListAsync();

        var counts = months.Select(m =>
            soldDates.Count(p => p.Year == m.Year && p.Month == m.Month)).ToArray();
        AgencySalesSeries = new ISeries[]
        {
            new ColumnSeries<int> { Values = counts, Name = L.Get("Status.Sold", "Продано"), Fill = new SolidColorPaint(SKColor.Parse("#7CB342")) }
        };
        AgencySalesXAxes = new Axis[] { new Axis { Labels = months.Select(m => m.ToString("MMM yy")).ToArray() } };
    }

    private async Task LoadUsersAsync()
    {
        IsUsersLoading = true;
        try
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(UserSearch))
            {
                var q = UserSearch.Trim().ToLower();
                query = query.Where(u =>
                    u.FullName.ToLower().Contains(q) ||
                    u.Login.ToLower().Contains(q) ||
                    u.Email.ToLower().Contains(q));
            }

            query = UserRoleFilter switch
            {
                "realtor" => query.Where(u => u.Role == "realtor"),
                "client"  => query.Where(u => u.Role == "client"),
                "blocked" => query.Where(u => u.IsBlocked),
                _         => query
            };

            var users = await query
                .OrderBy(u => u.IsBlocked).ThenBy(u => u.Role).ThenBy(u => u.FullName)
                .ToListAsync();
            Users.Clear();
            foreach (var u in users) Users.Add(new UserRowViewModel(u));
        }
        finally { IsUsersLoading = false; }
    }

    [RelayCommand]
    private async Task SearchUsersAsync() => await LoadUsersAsync();

    [RelayCommand]
    private async Task FilterAllAsync()     { UserRoleFilter = "all";     await LoadUsersAsync(); }
    [RelayCommand]
    private async Task FilterRealtorAsync() { UserRoleFilter = "realtor"; await LoadUsersAsync(); }
    [RelayCommand]
    private async Task FilterClientAsync()  { UserRoleFilter = "client";  await LoadUsersAsync(); }
    [RelayCommand]
    private async Task FilterBlockedAsync() { UserRoleFilter = "blocked"; await LoadUsersAsync(); }

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

        string warningText = "";
        if (newRole == "client" && user.Role == "realtor")
        {
            var activeProps = await _context.Properties
                .CountAsync(p => p.RealtorId == userId && p.Status == "active");
            var activeAppts = await _context.Appointments
                .CountAsync(a => a.RealtorId == userId && (a.Status == AppointmentStatus.New || a.Status == AppointmentStatus.Confirmed));

            if (activeProps > 0 || activeAppts > 0)
            {
                var parts = new List<string>();
                if (activeProps > 0) parts.Add($"активных объектов: {activeProps}");
                if (activeAppts > 0) parts.Add($"предстоящих записей на просмотр: {activeAppts}");
                warningText = $"\n\n⚠ У риелтора есть {string.Join(", ", parts)}.\nВсе активные объекты будут скрыты автоматически.";
            }
        }

        var result = System.Windows.MessageBox.Show(
            $"Изменить роль пользователя «{user.FullName}» на «{roleLabel}»?{warningText}",
            "Подтверждение смены роли",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);
        if (result != System.Windows.MessageBoxResult.Yes) return;

        user.Role = newRole;

        if (newRole == "client")
        {
            var propsToHide = await _context.Properties
                .Where(p => p.RealtorId == userId && p.Status == "active")
                .ToListAsync();
            foreach (var p in propsToHide)
                p.Status = "hidden";
        }

        await _context.SaveChangesAsync();
        UserActionMessage = $"Роль {user.FullName} изменена на «{roleLabel}»";

        _ = _notificationService.SendEmailAsync(user.Email, user.FullName,
            "Изменение роли в системе",
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

    private async Task LoadReviewsAsync()
    {
        IsReviewsLoading = true;
        try
        {
            var realtorNames = await _context.Reviews
                .Where(r => !r.IsApproved && r.Realtor != null)
                .Select(r => r.Realtor!.FullName)
                .Distinct()
                .OrderBy(n => n)
                .ToListAsync();

            _suppressReviewReload = true;
            ReviewRealtorNames.Clear();
            ReviewRealtorNames.Add("");
            foreach (var name in realtorNames) ReviewRealtorNames.Add(name);
            ReviewRealtorFilter = "";
            _suppressReviewReload = false;

            await RefreshReviewsAsync();
        }
        finally { IsReviewsLoading = false; }
    }

    private async Task RefreshReviewsAsync()
    {
        await _reviewSem.WaitAsync();
        try
        {
            var query = _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Realtor)
                .Where(r => !r.IsApproved)
                .AsQueryable();

            if (!string.IsNullOrEmpty(ReviewRealtorFilter))
                query = query.Where(r => r.Realtor != null && r.Realtor.FullName == ReviewRealtorFilter);

            var reviews = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
            PendingReviewsCount = reviews.Count;
            PendingReviews.Clear();
            foreach (var r in reviews) PendingReviews.Add(new ReviewRowViewModel(r));
            NoReviews = PendingReviews.Count == 0;
        }
        finally { _reviewSem.Release(); }
    }

    partial void OnReviewRealtorFilterChanged(string value) { if (!_suppressReviewReload) _ = RefreshReviewsAsync(); }

    [RelayCommand]
    private async Task ApproveReviewAsync(int id)
    {
        var result = System.Windows.MessageBox.Show(
            "Одобрить этот отзыв? Он станет виден всем пользователям.",
            "Подтверждение одобрения",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);
        if (result != System.Windows.MessageBoxResult.Yes) return;
        await _reviewService.ApproveAsync(id);
        await LoadReviewsAsync();
    }

    [RelayCommand]
    private async Task RejectReviewAsync(int id)
    {
        var result = System.Windows.MessageBox.Show(
            "Отклонить и удалить этот отзыв? Действие необратимо.",
            "Подтверждение отклонения",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);
        if (result != System.Windows.MessageBoxResult.Yes) return;
        await _reviewService.RejectAsync(id);
        await LoadReviewsAsync();
    }

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
            var from = GetPeriodStart();
            var to   = GetPeriodEnd();

            StatActiveProps = await _context.Properties.CountAsync(p => p.Status == "active");
            StatTotalProps  = await _context.Properties.CountAsync(p => p.Status == "active");
            StatSoldProps   = await _context.Properties.CountAsync(p => p.Status == "sold");
            StatTotalUsers  = await _context.Users.CountAsync();
            StatRealtors    = await _context.Users.CountAsync(u => u.Role == "realtor");

            var soldQ = _context.Properties.Where(p => p.Status == "sold");
            if (from.HasValue) soldQ = soldQ.Where(p => p.UpdatedAt >= from.Value);
            if (to.HasValue)   soldQ = soldQ.Where(p => p.UpdatedAt <  to.Value);
            var soldCount   = await soldQ.CountAsync();
            decimal revenue = soldCount > 0 ? await soldQ.SumAsync(p => p.Price) : 0;
            StatRevenueText = soldCount > 0 ? FormatMoney(revenue) : "—";
            StatAvgDealText = soldCount > 0 ? FormatMoney(revenue / soldCount) : "—";

            var clientQ = _context.Users.Where(u => u.Role == "client");
            if (from.HasValue) clientQ = clientQ.Where(u => u.CreatedAt >= from.Value);
            if (to.HasValue)   clientQ = clientQ.Where(u => u.CreatedAt <  to.Value);
            StatNewClients = await clientQ.CountAsync();

            var pendingQ = _context.Appointments.Where(a => a.Status == AppointmentStatus.New);
            if (from.HasValue) pendingQ = pendingQ.Where(a => a.SlotStart >= from.Value);
            if (to.HasValue)   pendingQ = pendingQ.Where(a => a.SlotStart <  to.Value);
            StatPendingAppts = await pendingQ.CountAsync();

            await BuildAgencyChartAsync();

            var realtors = await _context.Users.Where(u => u.Role == "realtor").ToListAsync();

            var propStats = await _context.Properties
                .GroupBy(p => p.RealtorId)
                .Select(g => new
                {
                    RealtorId = g.Key,
                    Total     = g.Count(),
                    Active    = g.Count(p => p.Status == "active"),
                    Sold      = g.Count(p => p.Status == "sold"),
                    Revenue   = g.Sum(p => p.Status == "sold" ? p.Price : 0m)
                })
                .ToListAsync();

            var apptQ = _context.Appointments.AsQueryable();
            if (from.HasValue) apptQ = apptQ.Where(a => a.SlotStart >= from.Value);
            if (to.HasValue)   apptQ = apptQ.Where(a => a.SlotStart <  to.Value);
            var periodApptStats = await apptQ
                .GroupBy(a => new { a.RealtorId, a.Status })
                .Select(g => new { g.Key.RealtorId, g.Key.Status, Count = g.Count() })
                .ToListAsync();

            var allApptStats = await _context.Appointments
                .GroupBy(a => new { a.RealtorId, a.Status })
                .Select(g => new { g.Key.RealtorId, g.Key.Status, Count = g.Count() })
                .ToListAsync();

            var thisMonthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var lastMonthStart = thisMonthStart.AddMonths(-1);
            var trendStats = await _context.Appointments
                .Where(a => a.Status == AppointmentStatus.Completed && a.SlotStart >= lastMonthStart)
                .GroupBy(a => new { a.RealtorId, IsThisMonth = a.SlotStart >= thisMonthStart })
                .Select(g => new { g.Key.RealtorId, g.Key.IsThisMonth, Count = g.Count() })
                .ToListAsync();

            var staleThreshold = DateTime.Today.AddDays(-30);
            var staleStats = await _context.Properties
                .Where(p => p.Status == "active" && p.CreatedAt < staleThreshold)
                .GroupBy(p => p.RealtorId)
                .Select(g => new { RealtorId = g.Key, Count = g.Count() })
                .ToListAsync();

            var ratingStats = await _context.Reviews
                .Where(r => r.IsApproved)
                .GroupBy(r => r.RealtorId)
                .Select(g => new { RealtorId = g.Key, AvgRating = g.Average(r => (double)r.Rating) })
                .ToListAsync();

            var rawList = new List<RealtorSummaryRow>();
            for (int i = 0; i < realtors.Count; i++)
            {
                var r      = realtors[i];
                var ps     = propStats.FirstOrDefault(x => x.RealtorId == r.Id);
                var rating = ratingStats.FirstOrDefault(x => x.RealtorId == r.Id)?.AvgRating ?? 0;
                var color  = _palette[i % _palette.Length];

                int total  = ps?.Total  ?? 0;
                int active = ps?.Active ?? 0;
                int sold   = ps?.Sold   ?? 0;

                int totalAppt    = periodApptStats.Where(x => x.RealtorId == r.Id).Sum(x => x.Count);
                int completed    = periodApptStats.FirstOrDefault(x => x.RealtorId == r.Id && x.Status == AppointmentStatus.Completed)?.Count ?? 0;
                int pending      = periodApptStats.FirstOrDefault(x => x.RealtorId == r.Id && x.Status == AppointmentStatus.New)?.Count ?? 0;
                int score        = CalcScore(rating, sold, total, completed, totalAppt, active);
                decimal rRevenue = ps?.Revenue ?? 0;

                int thisM  = trendStats.FirstOrDefault(x => x.RealtorId == r.Id && x.IsThisMonth)?.Count  ?? 0;
                int lastM  = trendStats.FirstOrDefault(x => x.RealtorId == r.Id && !x.IsThisMonth)?.Count ?? 0;
                string trend = thisM > lastM ? "↑" : thisM < lastM ? "↓" : "→";

                rawList.Add(new RealtorSummaryRow
                {
                    Id            = r.Id,
                    Name          = r.FullName,
                    TotalProps    = total,
                    ActiveProps   = active,
                    SoldProps     = sold,
                    TotalAppt     = totalAppt,
                    CompletedAppt = completed,
                    PendingAppt   = pending,
                    Rating        = rating > 0 ? $"{rating:F1} ★" : "—",
                    RatingValue   = rating,
                    Color         = color,
                    Score         = score,
                    ScoreColor    = ScoreColor(score),
                    RevenueText   = sold > 0 ? FormatMoney(rRevenue) : "—",
                    TrendArrow    = trend,
                    TrendColor    = trend == "↑" ? "#7CB342" : trend == "↓" ? "#EF5350" : "#9E9E9E"
                });
            }

            _allRealtorSummary = rawList
                .OrderByDescending(r => r.Score)
                .Select((src, i) => new RealtorSummaryRow
                {
                    Id = src.Id, Name = src.Name, TotalProps = src.TotalProps, ActiveProps = src.ActiveProps,
                    SoldProps = src.SoldProps, TotalAppt = src.TotalAppt, CompletedAppt = src.CompletedAppt,
                    PendingAppt = src.PendingAppt, Rating = src.Rating, RatingValue = src.RatingValue,
                    Color = src.Color, Score = src.Score, ScoreColor = src.ScoreColor, RevenueText = src.RevenueText,
                    TrendArrow = src.TrendArrow, TrendColor = src.TrendColor, Rank = i + 1
                })
                .ToList();
            _showAllRealtors = false;
            ApplyRealtorSummaryLimit();

            if (rawList.Count > 0)
            {
                TeamAvgScore = (int)rawList.Average(r => r.Score);
                TeamAvgColor = ScoreColor(TeamAvgScore);
            }

            _allAlerts.Clear();
            foreach (var row in rawList)
            {
                var allAppt   = allApptStats.Where(x => x.RealtorId == row.Id).ToList();
                int allCompl  = allAppt.FirstOrDefault(x => x.Status == AppointmentStatus.Completed)?.Count ?? 0;
                int allCancel = allAppt.FirstOrDefault(x => x.Status == AppointmentStatus.Cancelled)?.Count  ?? 0;
                int allTotal  = allAppt.Sum(x => x.Count);
                int staleCount = staleStats.FirstOrDefault(x => x.RealtorId == row.Id)?.Count ?? 0;

                if (row.TotalProps == 0)
                    _allAlerts.Add(new AlertItem { RealtorName = row.Name, AlertText = L.Get("Alert.NoProps"),  Icon = "📭", AlertColor = "#9E9E9E", Severity = L.Get("Alert.SeverityInfo"),     SeverityKey = "info" });
                else if (row.ActiveProps == 0)
                    _allAlerts.Add(new AlertItem { RealtorName = row.Name, AlertText = L.Get("Alert.NoActive"), Icon = "⚠",  AlertColor = "#FF8F00", Severity = L.Get("Alert.SeverityWarning"),  SeverityKey = "warning" });

                if (row.SoldProps == 0)
                    _allAlerts.Add(new AlertItem { RealtorName = row.Name, AlertText = L.Get("Alert.NoSales"),  Icon = "📉", AlertColor = "#FF8F00", Severity = L.Get("Alert.SeverityWarning"),  SeverityKey = "warning" });

                if (row.RatingValue > 0 && row.RatingValue < 3.5)
                    _allAlerts.Add(new AlertItem { RealtorName = row.Name, AlertText = $"{L.Get("Alert.LowRating")} ({row.Rating})",   Icon = "⭐", AlertColor = "#EF5350", Severity = L.Get("Alert.SeverityCritical"), SeverityKey = "critical" });

                if (row.Score < 30 && row.TotalProps > 0)
                    _allAlerts.Add(new AlertItem { RealtorName = row.Name, AlertText = $"{L.Get("Alert.LowScore")} ({row.Score}/100)", Icon = "🚨", AlertColor = "#EF5350", Severity = L.Get("Alert.SeverityCritical"), SeverityKey = "critical" });

                if (allTotal >= 3 && allCompl == 0)
                    _allAlerts.Add(new AlertItem { RealtorName = row.Name, AlertText = L.Get("Alert.NoCompleted"),  Icon = "📋", AlertColor = "#FF8F00", Severity = L.Get("Alert.SeverityWarning"),  SeverityKey = "warning" });

                if (allTotal >= 5 && (double)allCancel / allTotal > 0.5)
                    _allAlerts.Add(new AlertItem { RealtorName = row.Name, AlertText = $"{L.Get("Alert.HighCancels")} ({allCancel * 100 / allTotal}%)", Icon = "❌", AlertColor = "#EF5350", Severity = L.Get("Alert.SeverityCritical"), SeverityKey = "critical" });

                if (staleCount > 0)
                    _allAlerts.Add(new AlertItem { RealtorName = row.Name, AlertText = $"{staleCount} {L.Get("Alert.StaleProps")}", Icon = "🕐", AlertColor = "#FF8F00", Severity = L.Get("Alert.SeverityWarning"),  SeverityKey = "warning" });
            }
            HasAlerts = _allAlerts.Count > 0;

            AlertRealtorNames.Clear();
            AlertRealtorNames.Add("");
            foreach (var n in _allAlerts.Select(a => a.RealtorName).Distinct().OrderBy(x => x))
                AlertRealtorNames.Add(n);
            AlertRealtorFilter = "";
            AlertSeverityFilter = "all";
            ApplyAlertFilters();
        }
        catch {  }
    }

    private void ApplyAlertFilters()
    {
        Alerts.Clear();
        foreach (var a in _allAlerts)
        {
            if (AlertSeverityFilter != "all" && a.SeverityKey != AlertSeverityFilter) continue;
            if (!string.IsNullOrEmpty(AlertRealtorFilter) && a.RealtorName != AlertRealtorFilter) continue;
            Alerts.Add(a);
        }
        OnPropertyChanged(nameof(AllAlertsCount));
    }

    partial void OnAlertSeverityFilterChanged(string value) => ApplyAlertFilters();
    partial void OnAlertRealtorFilterChanged(string value)  => ApplyAlertFilters();

    [RelayCommand] private void ToggleAlertsExpanded() => IsAlertsExpanded = !IsAlertsExpanded;
    [RelayCommand] private void FilterAlertAll()      => AlertSeverityFilter = "all";
    [RelayCommand] private void FilterAlertInfo()     => AlertSeverityFilter = "info";
    [RelayCommand] private void FilterAlertWarning()  => AlertSeverityFilter = "warning";
    [RelayCommand] private void FilterAlertCritical() => AlertSeverityFilter = "critical";

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

            DetailTotal  = await _context.Properties.CountAsync(p => p.RealtorId == row.Id);
            DetailActive = await _context.Properties.CountAsync(p => p.RealtorId == row.Id && p.Status == "active");
            DetailSold   = await _context.Properties.CountAsync(p => p.RealtorId == row.Id && p.Status == "sold");

            var from = GetPeriodStart();
            var to   = GetPeriodEnd();
            var apptQ = _context.Appointments.Where(a => a.RealtorId == row.Id);
            if (from.HasValue) apptQ = apptQ.Where(a => a.SlotStart >= from.Value);
            if (to.HasValue)   apptQ = apptQ.Where(a => a.SlotStart <  to.Value);
            DetailTotalAppt     = await apptQ.CountAsync();
            DetailCompletedAppt = await apptQ.CountAsync(a => a.Status == AppointmentStatus.Completed);

            var avgRating = await _context.Reviews
                .Where(r => r.RealtorId == row.Id && r.IsApproved)
                .Select(r => (double?)r.Rating)
                .AverageAsync();
            DetailRatingText = avgRating.HasValue ? $"{avgRating.Value:F1} ★" : "—";

            decimal rRevenue = DetailSold > 0
                ? await _context.Properties.Where(p => p.RealtorId == row.Id && p.Status == "sold").SumAsync(p => p.Price)
                : 0;
            DetailRevenueText = DetailSold > 0 ? FormatMoney(rRevenue) : "—";
            DetailAvgDealText = DetailSold > 0 ? FormatMoney(rRevenue / DetailSold) : "—";

            var months = Enumerable.Range(0, 6).Select(i => DateTime.Today.AddMonths(-5 + i)).ToList();
            var chartFrom = months.First();
            var apptDates = await _context.Appointments
                .Where(a => a.RealtorId == row.Id && a.SlotStart >= chartFrom)
                .Select(a => new { a.SlotStart.Year, a.SlotStart.Month })
                .ToListAsync();
            var counts = months.Select(m => apptDates.Count(a => a.Year == m.Year && a.Month == m.Month)).ToArray();
            DetailApptSeries = new ISeries[]
            {
                new ColumnSeries<int> { Values = counts, Name = "Записи", Fill = new SolidColorPaint(SKColor.Parse(row.Color)) }
            };
            DetailApptXAxes = new Axis[] { new Axis { Labels = months.Select(m => m.ToString("MMM")).ToArray() } };

            _detailReviewRealtorId = row.Id;
            _detailReviewTotal = await _context.Reviews.CountAsync(r => r.RealtorId == row.Id && r.IsApproved);
            _detailReviewPage  = 0;
            DetailReviews.Clear();
            await AppendDetailReviewPageAsync();

            var propRows = await _context.Properties
                .Where(p => p.RealtorId == row.Id)
                .OrderBy(p => p.Status)
                .Select(p => new { p.Title, p.Status, p.Price, p.City })
                .ToListAsync();
            DetailProperties.Clear();
            foreach (var p in propRows)
                DetailProperties.Add(new DetailPropertyRow
                {
                    Title       = p.Title,
                    StatusLabel = p.Status switch { "active" => "Активен", "sold" => "Продан", "hidden" => "Скрыт", _ => p.Status },
                    StatusColor = p.Status switch { "active" => "#7CB342", "sold" => "#F4B942", _ => "#9E9E9E" },
                    Price       = $"{p.Price:N0} BYN",
                    City        = p.City
                });
            NoDetailProperties = DetailProperties.Count == 0;
        }
        catch {  }
    }

    [RelayCommand]
    private void GoBack() => _navigationService.GoBack();

    private async Task AppendDetailReviewPageAsync()
    {
        var items = await _context.Reviews
            .Include(r => r.User)
            .Where(r => r.RealtorId == _detailReviewRealtorId && r.IsApproved)
            .OrderByDescending(r => r.CreatedAt)
            .Skip(_detailReviewPage * DetailReviewPageSize)
            .Take(DetailReviewPageSize)
            .ToListAsync();
        foreach (var rev in items)
            DetailReviews.Add(new DetailReviewRow
            {
                ClientName  = rev.User?.FullName ?? "—",
                Stars       = new string('★', rev.Rating) + new string('☆', 5 - rev.Rating),
                Comment     = string.IsNullOrWhiteSpace(rev.Comment) ? "Без комментария" : rev.Comment,
                Date        = rev.CreatedAt.ToString("dd.MM.yyyy"),
                RatingValue = rev.Rating
            });
        _detailReviewPage++;
        HasMoreDetailReviews = DetailReviews.Count < _detailReviewTotal;
        NoDetailReviews = _detailReviewTotal == 0;
    }

    [RelayCommand]
    private async Task LoadMoreDetailReviewsAsync() => await AppendDetailReviewPageAsync();

    private async Task LoadAdminPropertiesAsync()
    {
        IsAdminPropsLoading = true;
        try
        {
            var realtorNames = await _context.Properties
                .Where(p => p.Realtor != null)
                .Select(p => p.Realtor!.FullName)
                .Distinct()
                .OrderBy(n => n)
                .ToListAsync();

            _suppressAdminPropReload = true;
            AdminRealtorNames.Clear();
            AdminRealtorNames.Add("");
            foreach (var name in realtorNames) AdminRealtorNames.Add(name);
            AdminPropRealtorFilter = "";
            _suppressAdminPropReload = false;

            await RefreshAdminPropertiesAsync();
        }
        finally { IsAdminPropsLoading = false; }
    }

    private async Task RefreshAdminPropertiesAsync()
    {
        await _adminPropSem.WaitAsync();
        try
        {
            var query = _context.Properties.Include(p => p.Realtor).AsQueryable();

            if (!string.IsNullOrWhiteSpace(AdminPropSearch))
            {
                var q = AdminPropSearch.Trim().ToLower();
                query = query.Where(p =>
                    p.Title.ToLower().Contains(q) ||
                    p.City.ToLower().Contains(q) ||
                    (p.Realtor != null && p.Realtor.FullName.ToLower().Contains(q)));
            }

            query = AdminPropStatusFilter switch
            {
                "active" => query.Where(p => p.Status == "active"),
                "hidden" => query.Where(p => p.Status == "hidden"),
                "sold"   => query.Where(p => p.Status == "sold"),
                _        => query
            };

            if (!string.IsNullOrEmpty(AdminPropRealtorFilter))
                query = query.Where(p => p.Realtor != null && p.Realtor.FullName == AdminPropRealtorFilter);

            var props = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
            AdminProperties.Clear();
            foreach (var p in props)
                AdminProperties.Add(new AdminPropertyRow
                {
                    Id           = p.Id,
                    Title        = p.Title,
                    City         = p.City,
                    Price        = $"{p.Price:N0} BYN",
                    StatusRaw    = p.Status,
                    StatusLabel  = p.Status switch { "active" => "Активен", "sold" => "Продан", "hidden" => "Скрыт", _ => p.Status },
                    StatusColor  = p.Status switch { "active" => "#7CB342", "sold" => "#F4B942", "hidden" => "#9E9E9E", _ => "#9E9E9E" },
                    RealtorName  = p.Realtor?.FullName ?? "—",
                    PropertyType = p.PropertyType switch { "apartment" => "Квартира", "house" => "Дом", "complex" => "Комплекс", _ => p.PropertyType },
                    CreatedAt    = p.CreatedAt.ToString("dd.MM.yyyy"),
                });
            AdminPropsCount = AdminProperties.Count;
        }
        finally { _adminPropSem.Release(); }
    }

    partial void OnAdminPropRealtorFilterChanged(string value) { if (!_suppressAdminPropReload) _ = RefreshAdminPropertiesAsync(); }

    [RelayCommand] private async Task SearchAdminPropsAsync()  => await RefreshAdminPropertiesAsync();
    [RelayCommand] private async Task FilterAdminAllAsync()    { AdminPropStatusFilter = "all";    await RefreshAdminPropertiesAsync(); }
    [RelayCommand] private async Task FilterAdminActiveAsync() { AdminPropStatusFilter = "active"; await RefreshAdminPropertiesAsync(); }
    [RelayCommand] private async Task FilterAdminHiddenAsync() { AdminPropStatusFilter = "hidden"; await RefreshAdminPropertiesAsync(); }
    [RelayCommand] private async Task FilterAdminSoldAsync()   { AdminPropStatusFilter = "sold";   await RefreshAdminPropertiesAsync(); }

    [RelayCommand]
    private void OpenAdminPropertyDetail(AdminPropertyRow row) =>
        _navigationService.NavigateTo<PropertyDetailViewModel>(row.Id);

    [RelayCommand]
    private async Task ToggleAdminPropertyStatusAsync(AdminPropertyRow row)
    {
        var newStatus = row.StatusRaw == "hidden" ? "active" : "hidden";
        var label = newStatus == "hidden" ? "скрыть" : "сделать активным";
        var result = System.Windows.MessageBox.Show(
            $"{(newStatus == "hidden" ? "Скрыть" : "Показать")} объект «{row.Title}»?",
            "Подтверждение",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);
        if (result != System.Windows.MessageBoxResult.Yes) return;

        var prop = await _context.Properties.FindAsync(row.Id);
        if (prop == null) return;
        prop.Status    = newStatus;
        prop.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        AdminPropActionMessage = newStatus == "hidden"
            ? $"Объект «{row.Title}» скрыт из каталога."
            : $"Объект «{row.Title}» снова активен в каталоге.";
        await RefreshAdminPropertiesAsync();
    }

    [RelayCommand]
    private async Task DeleteAdminPropertyAsync(AdminPropertyRow row)
    {
        var hasActive = await _context.Appointments.AnyAsync(a =>
            a.PropertyId == row.Id && (a.Status == AppointmentStatus.New || a.Status == AppointmentStatus.Confirmed));
        if (hasActive)
        {
            System.Windows.MessageBox.Show(
                "Нельзя удалить объект с активными записями клиентов.\nСначала отмените или завершите все записи.",
                "Удаление невозможно",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        var result = System.Windows.MessageBox.Show(
            $"Удалить объект «{row.Title}»?\nЭто действие необратимо, все данные объекта будут удалены.",
            "Подтверждение удаления",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);
        if (result != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            var favorites    = await _context.Favorites.Where(f => f.PropertyId == row.Id).ToListAsync();
            var reviews      = await _context.Reviews.Where(r => r.PropertyId == row.Id).ToListAsync();
            var appointments = await _context.Appointments.Where(a => a.PropertyId == row.Id).ToListAsync();
            var priceHist    = await _context.PriceHistories.Where(ph => ph.PropertyId == row.Id).ToListAsync();
            var images       = await _context.PropertyImages.Where(i => i.PropertyId == row.Id).ToListAsync();

            _context.Favorites.RemoveRange(favorites);
            _context.Reviews.RemoveRange(reviews);
            _context.Appointments.RemoveRange(appointments);
            _context.PriceHistories.RemoveRange(priceHist);
            _context.PropertyImages.RemoveRange(images);

            var prop = await _context.Properties.FindAsync(row.Id);
            if (prop != null) _context.Properties.Remove(prop);

            await _context.SaveChangesAsync();
            AdminPropActionMessage = $"Объект «{row.Title}» удалён.";
            await LoadAdminPropertiesAsync();
            await LoadStatsAsync();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Ошибка при удалении: {ex.Message}",
                "Ошибка",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

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

    // Мой профиль 
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
        if (string.IsNullOrWhiteSpace(ProfileEmail))
        { ProfileResult = "E-mail не может быть пустым."; ProfileSuccess = false; return; }
        if (!Regex.IsMatch(ProfileEmail.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        { ProfileResult = "Введите корректный e-mail адрес."; ProfileSuccess = false; return; }

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
