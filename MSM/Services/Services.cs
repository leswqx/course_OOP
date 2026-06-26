using Microsoft.EntityFrameworkCore;
using MSM.Data.Repositories;
using MSM.Models.Entities;
using MSM.Services.Interfaces;

namespace MSM.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<User?> LoginAsync(string login, string password)
    {
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Login == login);
        if (user == null)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        if (user.IsBlocked)
            return null;

        return user;
    }

    public async Task<string?> GetLoginErrorAsync(string login, string password)
    {
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Login == login);
        if (user == null) return "Неверный логин или пароль.";
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return "Неверный логин или пароль.";
        if (user.IsBlocked) return "Аккаунт заблокирован. Обратитесь к администратору.";
        return null;
    }

    public async Task<User?> RegisterAsync(string login, string password, string email, string fullName, string? phone = null)
    {
        if (await _unitOfWork.Users.AnyAsync(u => u.Login == login))
            return null;

        if (await _unitOfWork.Users.AnyAsync(u => u.Email == email))
            return null;

        var user = new User
        {
            Login = login,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Email = email,
            FullName = fullName,
            Phone = phone,
            Role = "client",
            CreatedAt = DateTime.Now
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return user;
    }

    public async Task<bool> ChangePasswordAsync(User user, string oldPassword, string newPassword)
    {
        if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash))
            return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task UpdateProfileAsync(User user)
    {
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();
    }
}

public class PropertyService : IPropertyService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    public PropertyService(IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task<IEnumerable<Property>> GetAllAsync()
    {
        return await _unitOfWork.Properties.Query()
            .Include(p => p.Realtor)
            .Include(p => p.Images)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Property?> GetByIdAsync(int id)
    {
        return await _unitOfWork.Properties.Query()
            .Include(p => p.Realtor)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    private static IQueryable<Property> ApplyFilters(
        IQueryable<Property> query,
        decimal? minPrice, decimal? maxPrice,
        double? minArea, double? maxArea,
        int? minRooms, int? maxRooms,
        int? minBathrooms, int? maxBathrooms,
        string? city, string? district, string? propertyType,
        string? searchQuery, bool? hasMortgage, bool? hasRenovation)
    {
        if (minPrice.HasValue)     query = query.Where(p => p.Price >= minPrice.Value);
        if (maxPrice.HasValue)     query = query.Where(p => p.Price <= maxPrice.Value);
        if (minArea.HasValue)      query = query.Where(p => p.Area >= minArea.Value);
        if (maxArea.HasValue)      query = query.Where(p => p.Area <= maxArea.Value);
        if (minRooms.HasValue)     query = query.Where(p => p.Rooms >= minRooms.Value);
        if (maxRooms.HasValue)     query = query.Where(p => p.Rooms <= maxRooms.Value);
        if (minBathrooms.HasValue) query = query.Where(p => p.Bathrooms.HasValue && p.Bathrooms >= minBathrooms.Value);
        if (maxBathrooms.HasValue) query = query.Where(p => p.Bathrooms.HasValue && p.Bathrooms <= maxBathrooms.Value);
        if (!string.IsNullOrEmpty(city))         query = query.Where(p => p.City == city);
        if (!string.IsNullOrEmpty(district))     query = query.Where(p => p.District != null && p.District.Contains(district));
        if (!string.IsNullOrEmpty(propertyType)) query = query.Where(p => p.PropertyType == propertyType);
        if (!string.IsNullOrEmpty(searchQuery))  query = query.Where(p => p.Title.Contains(searchQuery) || p.Description.Contains(searchQuery));
        if (hasMortgage == true)   query = query.Where(p => p.MortgageAvailable);
        if (hasRenovation == true) query = query.Where(p => p.HasRepair);
        return query;
    }

    public async Task<IEnumerable<Property>> GetFilteredAsync(
        decimal? minPrice = null,
        decimal? maxPrice = null,
        double? minArea = null,
        double? maxArea = null,
        int? minRooms = null,
        int? maxRooms = null,
        int? minBathrooms = null,
        int? maxBathrooms = null,
        string? city = null,
        string? district = null,
        string? propertyType = null,
        string? searchQuery = null,
        bool? hasMortgage = null,
        bool? hasRenovation = null,
        string? sortBy = null,
        int skip = 0,
        int take = 0)
    {
        var query = ApplyFilters(
            _unitOfWork.Properties.Query()
                .Include(p => p.Realtor)
                .Include(p => p.Images)
                .Where(p => p.Status == "active"),
            minPrice, maxPrice, minArea, maxArea, minRooms, maxRooms,
            minBathrooms, maxBathrooms, city, district, propertyType,
            searchQuery, hasMortgage, hasRenovation);

        IQueryable<Property> ordered = sortBy switch
        {
            "price_asc"  => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "area_asc"   => query.OrderBy(p => p.Area),
            "area_desc"  => query.OrderByDescending(p => p.Area),
            _            => query.OrderByDescending(p => p.CreatedAt)
        };

        if (skip > 0) ordered = ordered.Skip(skip);
        if (take > 0) ordered = ordered.Take(take);

        return await ordered.ToListAsync();
    }

    public async Task<int> GetFilteredCountAsync(
        decimal? minPrice = null,
        decimal? maxPrice = null,
        double? minArea = null,
        double? maxArea = null,
        int? minRooms = null,
        int? maxRooms = null,
        int? minBathrooms = null,
        int? maxBathrooms = null,
        string? city = null,
        string? district = null,
        string? propertyType = null,
        string? searchQuery = null,
        bool? hasMortgage = null,
        bool? hasRenovation = null)
    {
        var query = ApplyFilters(
            _unitOfWork.Properties.Query().Where(p => p.Status == "active"),
            minPrice, maxPrice, minArea, maxArea, minRooms, maxRooms,
            minBathrooms, maxBathrooms, city, district, propertyType,
            searchQuery, hasMortgage, hasRenovation);

        return await query.CountAsync();
    }

    public async Task<IEnumerable<string>> GetDistinctCitiesAsync()
    {
        return await _unitOfWork.Properties.Query()
            .Where(p => p.Status == "active")
            .Select(p => p.City)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetDistinctDistrictsAsync()
    {
        return await _unitOfWork.Properties.Query()
            .Where(p => p.Status == "active" && p.District != null && p.District != "")
            .Select(p => p.District!)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();
    }

    public async Task<Property> AddAsync(Property property, IEnumerable<(byte[] Data, string FileName, bool IsMain)> images,
        string? district = null)
    {
        property.District = string.IsNullOrWhiteSpace(district) ? null : district.Trim();
        property.CreatedAt = DateTime.Now;
        property.UpdatedAt = DateTime.Now;
        property.Status = "active";

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _unitOfWork.Properties.AddAsync(property);
            await _unitOfWork.SaveChangesAsync();

            await _unitOfWork.PriceHistories.AddAsync(new PriceHistory
            {
                PropertyId = property.Id,
                Price = property.Price,
                ChangedAt = property.CreatedAt
            });
            await _unitOfWork.SaveChangesAsync();

            var imageList = images.Select((img, index) => new PropertyImage
            {
                PropertyId = property.Id,
                ImageData = img.Data,
                FileName = img.FileName,
                IsMain = img.IsMain,
                SortOrder = index
            }).ToList();

            await _unitOfWork.PropertyImages.AddRangeAsync(imageList);
            await _unitOfWork.SaveChangesAsync();

            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        var activeCount = await _unitOfWork.Properties.Query()
            .CountAsync(p => p.Status == "active");
        if (activeCount > 0 && activeCount % 10 == 0)
        {
            var clients = await _unitOfWork.Users.Query()
                .Where(u => u.Role == "client" && !u.IsBlocked && u.Email != null)
                .ToListAsync();
            _ = _notificationService.SendNewListingsNotificationAsync(clients, activeCount);
        }

        return property;
    }

    public async Task DeleteAsync(int id)
    {
        var property = await _unitOfWork.Properties.GetByIdAsync(id);
        if (property != null)
        {
            _unitOfWork.Properties.Remove(property);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Property>> GetRealtorPropertiesAsync(int realtorId)
    {
        return await _unitOfWork.Properties.Query()
            .Include(p => p.Images)
            .Where(p => p.RealtorId == realtorId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task UpdateStatusAsync(int id, string status)
    {
        var property = await _unitOfWork.Properties.GetByIdAsync(id);
        if (property == null) return;
        property.Status = status;
        property.UpdatedAt = DateTime.Now;
        _unitOfWork.Properties.Update(property);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task UpdatePropertyDetailsAsync(int id, string title, string description, decimal price, double area,
        int rooms, int? bathrooms, int? floor, int? totalFloors, int? yearBuilt, string city, string? district, string address,
        string propertyType, bool hasRepair, bool mortgageAvailable)
    {
        var property = await _unitOfWork.Properties.GetByIdAsync(id);
        if (property == null) return;
        bool priceChanged = property.Price != price;
        property.Title = title;
        property.Description = description;
        property.Price = price;
        property.Area = area;
        property.Rooms = rooms;
        property.Bathrooms = bathrooms;
        property.Floor = floor;
        property.TotalFloors = totalFloors;
        property.YearBuilt = yearBuilt;
        property.City = city;
        property.District = string.IsNullOrWhiteSpace(district) ? null : district.Trim();
        property.Address = address;
        property.PropertyType = propertyType;
        property.HasRepair = hasRepair;
        property.MortgageAvailable = mortgageAvailable;
        property.UpdatedAt = DateTime.Now;
        _unitOfWork.Properties.Update(property);
        await _unitOfWork.SaveChangesAsync();

        if (priceChanged)
        {
            await _unitOfWork.PriceHistories.AddAsync(new PriceHistory
            {
                PropertyId = id,
                Price = price,
                ChangedAt = DateTime.Now
            });
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<PriceHistory>> GetPriceHistoryAsync(int propertyId)
    {
        var history = await _unitOfWork.PriceHistories.Query()
            .Where(ph => ph.PropertyId == propertyId)
            .OrderBy(ph => ph.ChangedAt)
            .ToListAsync();

        if (history.Count == 0)
        {
            var property = await _unitOfWork.Properties.GetByIdAsync(propertyId);
            if (property != null)
            {
                var initial = new PriceHistory
                {
                    PropertyId = propertyId,
                    Price      = property.Price,
                    ChangedAt  = property.CreatedAt
                };
                await _unitOfWork.PriceHistories.AddAsync(initial);
                await _unitOfWork.SaveChangesAsync();
                history = new List<PriceHistory> { initial };
            }
        }

        return history;
    }
}

public class AppointmentService : IAppointmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    public AppointmentService(IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task<BookingStatus> BookSlotAsync(int propertyId, int clientId, int realtorId, DateTime slotStart, DateTime slotEnd, string? comment = null)
    {
        await _unitOfWork.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            var duplicate = await _unitOfWork.Appointments.AnyAsync(a =>
                a.ClientId == clientId &&
                a.PropertyId == propertyId &&
                (a.Status == AppointmentStatus.New || a.Status == AppointmentStatus.Confirmed));
            if (duplicate)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return BookingStatus.AlreadyBooked;
            }

            var conflict = await _unitOfWork.Appointments.AnyAsync(a =>
                a.RealtorId == realtorId &&
                a.Status != AppointmentStatus.Cancelled &&
                a.SlotStart < slotEnd && a.SlotEnd > slotStart);
            if (conflict)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return BookingStatus.SlotTaken;
            }

            var appointment = new Appointment
            {
                PropertyId = propertyId,
                ClientId   = clientId,
                RealtorId  = realtorId,
                SlotStart  = slotStart,
                SlotEnd    = slotEnd,
                Comment    = comment,
                Status     = AppointmentStatus.New,
                CreatedAt  = DateTime.Now
            };
            await _unitOfWork.Appointments.AddAsync(appointment);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            return BookingStatus.Success;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<Appointment> CreateAsync(int propertyId, int clientId, int realtorId, DateTime slotStart, DateTime slotEnd, string? comment = null)
    {
        var appointment = new Appointment
        {
            PropertyId = propertyId,
            ClientId = clientId,
            RealtorId = realtorId,
            SlotStart = slotStart,
            SlotEnd = slotEnd,
            Comment = comment,
            Status = AppointmentStatus.New,
            CreatedAt = DateTime.Now
        };

        await _unitOfWork.Appointments.AddAsync(appointment);
        await _unitOfWork.SaveChangesAsync();

        return appointment;
    }

    public async Task<Appointment?> GetByIdAsync(int id)
    {
        return await _unitOfWork.Appointments.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Appointment>> GetByClientIdAsync(int clientId)
    {
        return await _unitOfWork.Appointments.Query()
            .Include(a => a.Property)
            .Include(a => a.Realtor)
            .Where(a => a.ClientId == clientId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Appointment>> GetByRealtorIdAsync(int realtorId)
    {
        return await _unitOfWork.Appointments.Query()
            .Include(a => a.Property)
            .Include(a => a.Client)
            .Where(a => a.RealtorId == realtorId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task UpdateStatusAsync(int id, AppointmentStatus status, string? comment = null)
    {
        var appointment = await _unitOfWork.Appointments.Query()
            .Include(a => a.Client)
            .Include(a => a.Property)
            .Include(a => a.Realtor)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment == null) return;

        appointment.Status = status;
        _unitOfWork.Appointments.Update(appointment);
        await _unitOfWork.SaveChangesAsync();

        if (appointment.Client != null && status is AppointmentStatus.Confirmed or AppointmentStatus.Cancelled or AppointmentStatus.Completed)
        {
            _ = _notificationService.SendAppointmentStatusChangedAsync(
                appointment.Client,
                appointment.Property?.Title ?? "—",
                appointment.Realtor?.FullName ?? "—",
                status,
                appointment.SlotStart);
        }
    }

    public async Task<bool> IsSlotAvailableAsync(int realtorId, DateTime slotStart, DateTime slotEnd, int? excludeAppointmentId = null)
    {
        var query = _unitOfWork.Appointments.Query()
            .Where(a => a.RealtorId == realtorId)
            .Where(a => a.Status != AppointmentStatus.Cancelled)
            .Where(a => a.SlotStart < slotEnd && a.SlotEnd > slotStart);

        if (excludeAppointmentId.HasValue)
            query = query.Where(a => a.Id != excludeAppointmentId.Value);

        if (await query.AnyAsync()) return false;

        var blocked = await _unitOfWork.RealtorSchedules.Query()
            .AnyAsync(s => s.RealtorId == realtorId && !s.IsAvailable
                        && s.SlotStart < slotEnd && s.SlotEnd > slotStart);
        return !blocked;
    }

    public async Task<IEnumerable<RealtorSchedule>> GetBlockedSchedulesAsync(int realtorId)
    {
        return await _unitOfWork.RealtorSchedules.Query()
            .Where(s => s.RealtorId == realtorId && !s.IsAvailable)
            .ToListAsync();
    }
}

public class FavoriteService : IFavoriteService
{
    private readonly IUnitOfWork _unitOfWork;

    public FavoriteService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<Property>> GetUserFavoritesAsync(int userId)
    {
        return await _unitOfWork.Favorites.Query()
            .Where(f => f.UserId == userId)
            .Include(f => f.Property)
            .ThenInclude(p => p!.Images)
            .Include(f => f.Property)
            .ThenInclude(p => p!.Realtor)
            .Select(f => f.Property!)
            .ToListAsync();
    }

    public async Task<IEnumerable<Property>> GetUserFavoritesPagedAsync(int userId, int skip, int take)
    {
        return await _unitOfWork.Favorites.Query()
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.AddedAt)
            .Include(f => f.Property)
            .ThenInclude(p => p!.Images)
            .Include(f => f.Property)
            .ThenInclude(p => p!.Realtor)
            .Skip(skip)
            .Take(take)
            .Select(f => f.Property!)
            .ToListAsync();
    }

    public async Task<int> GetUserFavoritesCountAsync(int userId)
    {
        return await _unitOfWork.Favorites.Query()
            .Where(f => f.UserId == userId)
            .CountAsync();
    }

    public async Task<bool> IsFavoriteAsync(int userId, int propertyId)
    {
        return await _unitOfWork.Favorites.AnyAsync(f => f.UserId == userId && f.PropertyId == propertyId);
    }

    public async Task AddToFavoritesAsync(int userId, int propertyId)
    {
        if (!await IsFavoriteAsync(userId, propertyId))
        {
            var favorite = new Favorite
            {
                UserId = userId,
                PropertyId = propertyId,
                AddedAt = DateTime.Now
            };

            await _unitOfWork.Favorites.AddAsync(favorite);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task RemoveFromFavoritesAsync(int userId, int propertyId)
    {
        var favorite = await _unitOfWork.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.PropertyId == propertyId);
        if (favorite != null)
        {
            _unitOfWork.Favorites.Remove(favorite);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _unitOfWork;

    public ReviewService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Review> CreateAsync(int userId, int? propertyId, int? realtorId, int rating, string? comment = null, int? appointmentId = null)
    {
        await _unitOfWork.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            if (appointmentId.HasValue && await HasReviewForAppointmentAsync(appointmentId.Value))
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new InvalidOperationException("Вы уже оставляли отзыв по этой записи.");
            }

            var review = new Review
            {
                UserId        = userId,
                PropertyId    = propertyId,
                RealtorId     = realtorId,
                AppointmentId = appointmentId,
                Rating        = rating,
                Comment       = comment,
                IsApproved    = false,
                CreatedAt     = DateTime.Now
            };

            await _unitOfWork.Reviews.AddAsync(review);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            return review;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<IEnumerable<Review>> GetUserReviewsAsync(int userId)
    {
        return await _unitOfWork.Reviews.Query()
            .Where(r => r.UserId == userId)
            .ToListAsync();
    }

    public async Task<bool> HasReviewForAppointmentAsync(int appointmentId)
    {
        return await _unitOfWork.Reviews.AnyAsync(r => r.AppointmentId == appointmentId);
    }

    public async Task<IEnumerable<Review>> GetPropertyReviewsAsync(int propertyId)
    {
        return await _unitOfWork.Reviews.Query()
            .Include(r => r.User)
            .Where(r => r.PropertyId == propertyId && r.IsApproved)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Review>> GetRealtorReviewsAsync(int realtorId)
    {
        return await _unitOfWork.Reviews.Query()
            .Include(r => r.User)
            .Where(r => r.RealtorId == realtorId && r.IsApproved)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task ApproveAsync(int id)
    {
        var review = await _unitOfWork.Reviews.GetByIdAsync(id);
        if (review != null)
        {
            review.IsApproved = true;
            _unitOfWork.Reviews.Update(review);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task RejectAsync(int id)
    {
        var review = await _unitOfWork.Reviews.GetByIdAsync(id);
        if (review != null)
        {
            _unitOfWork.Reviews.Remove(review);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task<double> GetAverageRatingAsync(int? propertyId = null, int? realtorId = null)
    {
        var query = _unitOfWork.Reviews.Query().Where(r => r.IsApproved);
        if (propertyId.HasValue) query = query.Where(r => r.PropertyId == propertyId.Value);
        if (realtorId.HasValue)  query = query.Where(r => r.RealtorId  == realtorId.Value);
        return await query.Select(r => (double?)r.Rating).AverageAsync() ?? 0;
    }
}
