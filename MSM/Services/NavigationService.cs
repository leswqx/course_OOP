using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using MSM.Services.Interfaces;
using MSM.ViewModels;

namespace MSM.Services;

public partial class NavigationService : ObservableObject, INavigationService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private IServiceScope? _currentScope;

    private readonly Stack<(Type VmType, object? Param)> _history = new();
    private object? _currentParam;

    [ObservableProperty] private ViewModelBase? _currentViewModel;
    [ObservableProperty] private bool _canGoBack;

    public NavigationService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void NavigateTo<TViewModel>(object? parameter = null) where TViewModel : ViewModelBase
    {
        // сохранить текущий в историю с его параметром
        if (CurrentViewModel != null)
            _history.Push((CurrentViewModel.GetType(), _currentParam));
        _currentParam = parameter;

        _currentScope?.Dispose();
        _currentScope = _scopeFactory.CreateScope();

        var vm = _currentScope.ServiceProvider.GetRequiredService<TViewModel>();
        CurrentViewModel = vm;
        vm.OnNavigatedTo(parameter);
        CanGoBack = _history.Count > 0;
    }

    public void GoBack()
    {
        if (_history.Count == 0) return;

        var (vmType, param) = _history.Pop();

        _currentScope?.Dispose();
        _currentScope = _scopeFactory.CreateScope();

        var vm = (ViewModelBase)_currentScope.ServiceProvider.GetRequiredService(vmType);
        CurrentViewModel = vm;
        vm.OnNavigatedTo(param);
        CanGoBack = _history.Count > 0;
    }
}
