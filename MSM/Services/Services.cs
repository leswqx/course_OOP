using Microsoft.EntityFrameworkCore;
using MSM.Data.Repositories;
using MSM.Models.Entities;
using MSM.Services.Interfaces;

namespace MSM.Services;

/// <summary>
/// Сервис аутентификации
/// </summary>
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

        return user;
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

    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await _unitOfWork.Users.GetByIdAsync(id);
    }

    public async Task<User?> GetUserByLoginAsync(string login)
    {
        return await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Login == login);
    }
}

/// <summary>
/// Сервис управления недвижимостью
/// </summary>
public class PropertyService : IPropertyService
{
    private readonly IUnitOfWork _unitOfWork;

    public PropertyService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
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

    public async Task<IEnumerable<Property>> GetFilteredAsync(
        decimal? minPrice = null,
        decimal? maxPrice = null,
        double? minArea = null,
        double? maxArea = null,
        int? rooms = null,
        string? city = null,
        string? propertyType = null,
        string? searchQuery = null)
    {
        var query = _unitOfWork.Properties.Query()
            .Include(p => p.Realtor)
            .Include(p => p.Images)
            .Where(p => p.Status == "active");

        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice.Value);
        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice.Value);
        if (minArea.HasValue)
            query = query.Where(p => p.Area >= minArea.Value);
        if (maxArea.HasValue)
            query = query.Where(p => p.Area <= maxArea.Value);
        if (rooms.HasValue)
            query = query.Where(p => p.Rooms == rooms.Value);
        if (!string.IsNullOrEmpty(city))
            query = query.Where(p => p.City == city);
        if (!string.IsNullOrEmpty(propertyType))
            query = query.Where(p => p.PropertyType == propertyType);
        if (!string.IsNullOrEmpty(searchQuery))
            query = query.Where(p => p.Title.Contains(searchQuery) || p.Description.Contains(searchQuery));

        return await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
    }

    public async Task<Property> AddAsync(Property property, IEnumerable<(byte[] Data, string FileName, bool IsMain)> images)
    {
        property.CreatedAt = DateTime.Now;
        property.UpdatedAt = DateTime.Now;
        property.Status = "active";

        await _unitOfWork.Properties.AddAsync(property);
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

        return property;
    }

    public async Task UpdateAsync(Property property)
    {
        property.UpdatedAt = DateTime.Now;
        _unitOfWork.Properties.Update(property);
        await _unitOfWork.SaveChangesAsync();
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

    public async Task<IEnumerable<Property>> GetActivePropertiesAsync()
    {
        return await _unitOfWork.Properties.Query()
            .Include(p => p.Realtor)
            .Include(p => p.Images)
            .Where(p => p.Status == "active")
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }
}

/// <summary>
/// Сервис записей на просмотр
/// </summary>
public class AppointmentService : IAppointmentService
{
    private readonly IUnitOfWork _unitOfWork;

    public AppointmentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
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
            Status = "new",
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

    public async Task UpdateStatusAsync(int id, string status, string? comment = null)
    {
        var appointment = await _unitOfWork.Appointments.GetByIdAsync(id);
        if (appointment != null)
        {
            appointment.Status = status;
            _unitOfWork.Appointments.Update(appointment);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task<bool> IsSlotAvailableAsync(int realtorId, DateTime slotStart, DateTime slotEnd, int? excludeAppointmentId = null)
    {
        var query = _unitOfWork.Appointments.Query()
            .Where(a => a.RealtorId == realtorId)
            .Where(a => a.Status != "cancelled")
            .Where(a => a.SlotStart < slotEnd && a.SlotEnd > slotStart);

        if (excludeAppointmentId.HasValue)
            query = query.Where(a => a.Id != excludeAppointmentId.Value);

        return !await query.AnyAsync();
    }
}

/// <summary>
/// Сервис избранного
/// </summary>
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

/// <summary>
/// Сервис отзывов
/// </summary>
public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _unitOfWork;

    public ReviewService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Review> CreateAsync(int userId, int? propertyId, int? realtorId, int rating, string? comment = null)
    {
        var review = new Review
        {
            UserId = userId,
            PropertyId = propertyId,
            RealtorId = realtorId,
            Rating = rating,
            Comment = comment,
            IsApproved = false,
            CreatedAt = DateTime.Now
        };

        await _unitOfWork.Reviews.AddAsync(review);
        await _unitOfWork.SaveChangesAsync();

        return review;
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

        if (propertyId.HasValue)
            query = query.Where(r => r.PropertyId == propertyId.Value);
        if (realtorId.HasValue)
            query = query.Where(r => r.RealtorId == realtorId.Value);

        var reviews = await query.ToListAsync();
        return reviews.Any() ? reviews.Average(r => r.Rating) : 0;
    }
}
