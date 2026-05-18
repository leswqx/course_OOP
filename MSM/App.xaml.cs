using System.Configuration;
using System.Windows;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
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

        DispatcherUnhandledException += (_, ex) =>
        {
            MessageBox.Show(
                $"Ошибка: {ex.Exception.Message}\n\n{ex.Exception.StackTrace}",
                "Необработанное исключение",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            ex.Handled = true;
        };

        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();

        await InitializeDatabaseAsync();

        LiveCharts.Configure(config => config
            .AddSkiaSharp()
            .AddDefaultMappers()
            .AddLightTheme());

        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();

        ServiceProvider.GetRequiredService<INavigationService>().NavigateTo<LandingViewModel>();
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
        services.AddSingleton<INotificationService, EmailNotificationService>();

        // Navigation (singleton — хранит CurrentViewModel, создаёт DI-скоуп при каждом переходе)
        services.AddSingleton<INavigationService, NavigationService>();

        // Глобальный хедер (singleton — живёт всё время приложения)
        services.AddSingleton<HeaderViewModel>();

        // ViewModels (scoped — живут в рамках одного DI-скоупа, создаваемого NavigationService)
        services.AddScoped<LandingViewModel>();
        services.AddScoped<LoginViewModel>();
        services.AddScoped<RegisterViewModel>();
        services.AddScoped<PropertyListViewModel>();
        services.AddScoped<PropertyDetailViewModel>();
        services.AddScoped<FavoritesViewModel>();
        services.AddScoped<MyAppointmentsViewModel>();
        services.AddScoped<RealtorDashboardViewModel>();
        services.AddScoped<AdminDashboardViewModel>();
        services.AddScoped<RealtorProfileViewModel>();
        services.AddScoped<ClientProfileViewModel>();
        services.AddScoped<AboutViewModel>();

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
