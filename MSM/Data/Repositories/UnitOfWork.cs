using Microsoft.EntityFrameworkCore.Storage;
using MSM.Data.Context;
using MSM.Models.Entities;

namespace MSM.Data.Repositories;

/// <summary>
/// Реализация Unit of Work
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;

    private IRepository<User>? _users;
    private IRepository<Property>? _properties;
    private IRepository<PropertyImage>? _propertyImages;
    private IRepository<Appointment>? _appointments;
    private IRepository<RealtorSchedule>? _realtorSchedules;
    private IRepository<Favorite>? _favorites;
    private IRepository<Review>? _reviews;
    private IRepository<PriceHistory>? _priceHistories;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public IRepository<User> Users => _users ??= new Repository<User>(_context);
    public IRepository<Property> Properties => _properties ??= new Repository<Property>(_context);
    public IRepository<PropertyImage> PropertyImages => _propertyImages ??= new Repository<PropertyImage>(_context);
    public IRepository<Appointment> Appointments => _appointments ??= new Repository<Appointment>(_context);
    public IRepository<RealtorSchedule> RealtorSchedules => _realtorSchedules ??= new Repository<RealtorSchedule>(_context);
    public IRepository<Favorite> Favorites => _favorites ??= new Repository<Favorite>(_context);
    public IRepository<Review> Reviews => _reviews ??= new Repository<Review>(_context);
    public IRepository<PriceHistory> PriceHistories => _priceHistories ??= new Repository<PriceHistory>(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
