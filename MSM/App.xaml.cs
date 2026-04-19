using System.Configuration;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MSM.Data;
using MSM.Data.Context;
using MSM.Data.Repositories;
using MSM.Services;
using MSM.Services.Interfaces;
using MSM.ViewModels;

namespace MSM;

public partial class App : Application
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();

        await InitializeDatabaseAsync();

        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();

        ServiceProvider.GetRequiredService<INavigationService>().NavigateTo<LoginViewModel>();
    }

    private static async Task InitializeDatabaseAsync()
    {
        using var scope = ServiceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
        await DbInitializer.InitializeAsync(context);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Database
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                ConfigurationManager.ConnectionStrings["RealEstateConnection"]?.ConnectionString
                ?? "Server=(localdb)\\mssqllocaldb;Database=RealEstateDb;Trusted_Connection=True;MultipleActiveResultSets=true;Encrypt=False"));

        // Repositories
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPropertyService, PropertyService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IFavoriteService, FavoriteService>();
        services.AddScoped<IReviewService, ReviewService>();

        // Navigation (singleton — хранит CurrentViewModel, создаёт DI-скоуп при каждом переходе)
        services.AddSingleton<INavigationService, NavigationService>();

        // ViewModels (scoped — живут в рамках одного DI-скоупа, создаваемого NavigationService)
        services.AddScoped<LoginViewModel>();
        services.AddScoped<RegisterViewModel>();
        services.AddScoped<HomeViewModel>();
        services.AddScoped<PropertyListViewModel>();
        services.AddScoped<PropertyDetailViewModel>();
        services.AddScoped<FavoritesViewModel>();
        services.AddScoped<MyAppointmentsViewModel>();
        services.AddScoped<RealtorDashboardViewModel>();
        services.AddScoped<AdminDashboardViewModel>();
        services.AddScoped<RealtorProfileViewModel>();

        // Main window (singleton — одно окно на всё приложение)
        services.AddSingleton<MainWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        if (ServiceProvider is IDisposable disposable)
            disposable.Dispose();
    }
}
