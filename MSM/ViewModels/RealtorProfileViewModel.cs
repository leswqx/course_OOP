using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using MSM.Data.Context;
using MSM.Services.Interfaces;

namespace MSM.ViewModels;

public class ReviewDisplayRow
{
    public string Author { get; }
    public string Stars { get; }
    public string FilledStars { get; }
    public string EmptyStars  { get; }
    public string? Comment { get; }
    public string Date { get; }
    public int RatingValue { get; }
    public System.DateTime CreatedAt { get; }

    public ReviewDisplayRow(MSM.Models.Entities.Review r)
    {
        Author      = r.User?.FullName ?? "—";
        RatingValue = r.Rating;
        Stars       = new string('★', r.Rating) + new string('☆', 5 - r.Rating);
        FilledStars = new string('★', r.Rating);
        EmptyStars  = new string('☆', 5 - r.Rating);
        Comment     = r.Comment;
        CreatedAt   = r.CreatedAt;
        Date        = r.CreatedAt.ToString("dd.MM.yyyy");
    }
}

public partial class RealtorProfileViewModel : ViewModelBase
{
    private readonly AppDbContext _context;
    private readonly IReviewService _reviewService;
    private readonly INavigationService _navigationService;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _realtorName = "";
    [ObservableProperty] private string _realtorPhone = "";
    [ObservableProperty] private string _realtorEmail = "";
    [ObservableProperty] private string _realtorDescription = "";
    [ObservableProperty] private BitmapImage? _avatarImage;
    [ObservableProperty] private bool _hasAvatar;

    [ObservableProperty] private int _statProperties;
    [ObservableProperty] private int _statSold;
    [ObservableProperty] private double _statRating;
    [ObservableProperty] private string _statRatingStars = "☆☆☆☆☆";
    [ObservableProperty] private string _statFilledStars = "";
    [ObservableProperty] private string _statEmptyStars  = "☆☆☆☆☆";

    [ObservableProperty] private ObservableCollection<ReviewDisplayRow> _reviews = new();
    [ObservableProperty] private bool _noReviews;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSortDateDesc), nameof(IsSortDateAsc), nameof(IsSortRatingDesc), nameof(IsSortRatingAsc))]
    private string _sortMode = "date_desc";
    public bool IsSortDateDesc   => SortMode == "date_desc";
    public bool IsSortDateAsc    => SortMode == "date_asc";
    public bool IsSortRatingDesc => SortMode == "rating_desc";
    public bool IsSortRatingAsc  => SortMode == "rating_asc";

    private List<ReviewDisplayRow> _allReviews = new();
    private int _realtorId;

    public RealtorProfileViewModel(
        AppDbContext context,
        IReviewService reviewService,
        INavigationService navigationService)
    {
        _context = context;
        _reviewService = reviewService;
        _navigationService = navigationService;
    }

    public override void OnNavigatedTo(object? parameter)
    {
        if (parameter is int id) _realtorId = id;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var realtor = await _context.Users.FindAsync(_realtorId);
            if (realtor == null) return;

            RealtorName        = realtor.FullName;
            RealtorPhone       = realtor.Phone ?? "—";
            RealtorEmail       = realtor.Email;
            RealtorDescription = string.IsNullOrWhiteSpace(realtor.Description)
                ? "Описание не заполнено."
                : realtor.Description;

            if (realtor.AvatarPhoto?.Length > 0)
            {
                var ms = new System.IO.MemoryStream(realtor.AvatarPhoto);
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = ms;
                bmp.EndInit();
                AvatarImage = bmp;
                HasAvatar = true;
            }
            else
            {
                HasAvatar = false;
            }

            var props = await _context.Properties
                .Where(p => p.RealtorId == _realtorId)
                .ToListAsync();
            StatProperties = props.Count;
            StatSold       = props.Count(p => p.Status == "sold");

            StatRating = await _reviewService.GetAverageRatingAsync(realtorId: _realtorId);
            var rounded = (int)Math.Round(StatRating);
            StatRatingStars = new string('★', rounded) + new string('☆', 5 - rounded);
            StatFilledStars = new string('★', rounded);
            StatEmptyStars  = new string('☆', 5 - rounded);

            var reviewEntities = await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.RealtorId == _realtorId && r.IsApproved)
                .ToListAsync();

            _allReviews = reviewEntities.Select(r => new ReviewDisplayRow(r)).ToList();
            ApplySort();
            NoReviews = _allReviews.Count == 0;
        }
        finally { IsLoading = false; }
    }

    private void ApplySort()
    {
        var sorted = SortMode switch
        {
            "date_asc"    => _allReviews.OrderBy(r => r.CreatedAt),
            "rating_desc" => _allReviews.OrderByDescending(r => r.RatingValue),
            "rating_asc"  => _allReviews.OrderBy(r => r.RatingValue),
            _             => _allReviews.OrderByDescending(r => r.CreatedAt)
        };
        Reviews.Clear();
        foreach (var r in sorted) Reviews.Add(r);
    }

    [RelayCommand] private void SortDateDesc()   { SortMode = "date_desc";   ApplySort(); }
    [RelayCommand] private void SortDateAsc()    { SortMode = "date_asc";    ApplySort(); }
    [RelayCommand] private void SortRatingDesc() { SortMode = "rating_desc"; ApplySort(); }
    [RelayCommand] private void SortRatingAsc()  { SortMode = "rating_asc";  ApplySort(); }

    [RelayCommand]
    private void GoBack() => _navigationService.GoBack();
}
