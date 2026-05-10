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
    private readonly Stack<(Type VmType, object? Param)> _forwardHistory = new();
    private object? _currentParam;

    [ObservableProperty] private ViewModelBase? _currentViewModel;
    [ObservableProperty] private bool _canGoBack;
    [ObservableProperty] private bool _canGoForward;
    [ObservableProperty] private bool _showGlobalNav;

    public NavigationService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void NavigateTo<TViewModel>(object? parameter = null) where TViewModel : ViewModelBase
    {
        if (CurrentViewModel != null)
            _history.Push((CurrentViewModel.GetType(), _currentParam));
        _forwardHistory.Clear();
        _currentParam = parameter;

        _currentScope?.Dispose();
        _currentScope = _scopeFactory.CreateScope();

        var vm = _currentScope.ServiceProvider.GetRequiredService<TViewModel>();
        vm.OnNavigatedTo(parameter);
        CurrentViewModel = vm;
        CanGoBack    = _history.Count > 0;
        CanGoForward = false;
        ShowGlobalNav = false;
    }

    public void GoBack()
    {
        if (_history.Count == 0) return;

        if (CurrentViewModel != null)
            _forwardHistory.Push((CurrentViewModel.GetType(), _currentParam));

        var (vmType, param) = _history.Pop();
        _currentParam = param;

        _currentScope?.Dispose();
        _currentScope = _scopeFactory.CreateScope();

        var vm = (ViewModelBase)_currentScope.ServiceProvider.GetRequiredService(vmType);
        vm.OnNavigatedTo(param);
        CurrentViewModel = vm;
        CanGoBack    = _history.Count > 0;
        CanGoForward = _forwardHistory.Count > 0;
        ShowGlobalNav = false;
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    public void GoForward()
    {
        if (_forwardHistory.Count == 0) return;

        if (CurrentViewModel != null)
            _history.Push((CurrentViewModel.GetType(), _currentParam));

        var (vmType, param) = _forwardHistory.Pop();
        _currentParam = param;

        _currentScope?.Dispose();
        _currentScope = _scopeFactory.CreateScope();

        var vm = (ViewModelBase)_currentScope.ServiceProvider.GetRequiredService(vmType);
        vm.OnNavigatedTo(param);
        CurrentViewModel = vm;
        CanGoBack    = _history.Count > 0;
        CanGoForward = _forwardHistory.Count > 0;
        ShowGlobalNav = false;
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    public void GoHome()
    {
        _history.Clear();
        _forwardHistory.Clear();
        _currentParam = null;

        _currentScope?.Dispose();
        _currentScope = _scopeFactory.CreateScope();

        var vm = _currentScope.ServiceProvider.GetRequiredService<LandingViewModel>();
        vm.OnNavigatedTo(null);
        CurrentViewModel = vm;
        CanGoBack    = false;
        CanGoForward = false;
        ShowGlobalNav = false;
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void NavigateBack() => GoBack();
}
