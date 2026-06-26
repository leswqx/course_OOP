using Microsoft.EntityFrameworkCore;
using MSM.Models.Entities;

namespace MSM.Data.Context;

public class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<RealtorSchedule> RealtorSchedules => Set<RealtorSchedule>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<PriceHistory> PriceHistories => Set<PriceHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Login).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Login).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Role).HasMaxLength(20).IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Phone).HasMaxLength(20);
        });

        modelBuilder.Entity<Property>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.Price).IsRequired();
            entity.Property(e => e.Area).IsRequired();
            entity.Property(e => e.Rooms).IsRequired();
            entity.Property(e => e.City).HasMaxLength(100).IsRequired();
            entity.Property(e => e.District).HasMaxLength(100);
            entity.Property(e => e.Address).HasMaxLength(300).IsRequired();
            entity.Property(e => e.PropertyType).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired().HasDefaultValue("active");
            entity.HasOne(e => e.Realtor)
                .WithMany(e => e.Properties)
                .HasForeignKey(e => e.RealtorId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.Price);
            entity.HasIndex(e => e.City);
        });

        modelBuilder.Entity<PropertyImage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ImageData).IsRequired();
            entity.Property(e => e.FileName).HasMaxLength(255).IsRequired();
            entity.HasOne(e => e.Property)
                .WithMany(e => e.Images)
                .HasForeignKey(e => e.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status)
                .HasMaxLength(20).IsRequired().HasDefaultValueSql("'new'")
                .HasConversion(
                    v => v.ToString().ToLower(),
                    v => Enum.Parse<AppointmentStatus>(v, true));
            entity.Property(e => e.Comment).HasMaxLength(500);

            entity.HasIndex(e => new { e.RealtorId, e.SlotStart })
                .HasFilter("[Status] != 'cancelled'")
                .IsUnique()
                .HasDatabaseName("IX_Appointments_RealtorId_SlotStart_Active");
            entity.HasOne(e => e.Property)
                .WithMany(e => e.Appointments)
                .HasForeignKey(e => e.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Client)
                .WithMany(e => e.ClientAppointments)
                .HasForeignKey(e => e.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Realtor)
                .WithMany(e => e.RealtorAppointments)
                .HasForeignKey(e => e.RealtorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RealtorSchedule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.IsAvailable).HasDefaultValue(true);
            entity.HasOne(e => e.Realtor)
                .WithMany(e => e.Schedules)
                .HasForeignKey(e => e.RealtorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Favorite>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.PropertyId }).IsUnique();
            entity.HasOne(e => e.User)
                .WithMany(e => e.Favorites)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Property)
                .WithMany(e => e.Favorites)
                .HasForeignKey(e => e.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PriceHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Price).IsRequired();
            entity.HasOne(e => e.Property)
                .WithMany()
                .HasForeignKey(e => e.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.PropertyId);
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.IsApproved).HasDefaultValue(false);

            entity.HasIndex(e => e.AppointmentId)
                .IsUnique()
                .HasFilter("[AppointmentId] IS NOT NULL")
                .HasDatabaseName("IX_Reviews_AppointmentId_Unique");
            entity.HasOne(e => e.User)
                .WithMany(e => e.Reviews)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Property)
                .WithMany(e => e.Reviews)
                .HasForeignKey(e => e.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Realtor)
                .WithMany()
                .HasForeignKey(e => e.RealtorId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer(
                System.Configuration.ConfigurationManager.ConnectionStrings["RealEstateConnection"]?.ConnectionString
                ?? "Server=(localdb)\\mssqllocaldb;Database=RealEstateDb;Trusted_Connection=True;MultipleActiveResultSets=true;Encrypt=False");
        }
    }
}
