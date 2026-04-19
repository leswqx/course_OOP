using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using MSM.Services.Interfaces;
using MSM.ViewModels;

namespace MSM.Services;

public partial class NavigationService : ObservableObject, INavigationService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private IServiceScope? _currentScope;

    [ObservableProperty]
    private ViewModelBase? _currentViewModel;

    public NavigationService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void NavigateTo<TViewModel>(object? parameter = null) where TViewModel : ViewModelBase
    {
        _currentScope?.Dispose();
        _currentScope = _scopeFactory.CreateScope();

        var vm = _currentScope.ServiceProvider.GetRequiredService<TViewModel>();
        CurrentViewModel = vm;
        vm.OnNavigatedTo(parameter);
    }
}
