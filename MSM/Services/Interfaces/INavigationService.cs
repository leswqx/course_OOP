using System.ComponentModel;
using MSM.ViewModels;

namespace MSM.Services.Interfaces;

public interface INavigationService : INotifyPropertyChanged
{
    ViewModelBase? CurrentViewModel { get; }
    bool CanGoBack { get; }
    bool CanGoForward { get; }
    void NavigateTo<TViewModel>(object? parameter = null) where TViewModel : ViewModelBase;
    void GoBack();
    void GoForward();
    void GoHome();
}
