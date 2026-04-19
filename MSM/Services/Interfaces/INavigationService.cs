using System.ComponentModel;
using MSM.ViewModels;

namespace MSM.Services.Interfaces;

public interface INavigationService : INotifyPropertyChanged
{
    ViewModelBase? CurrentViewModel { get; }
    bool CanGoBack { get; }
    void NavigateTo<TViewModel>(object? parameter = null) where TViewModel : ViewModelBase;
    void GoBack();
}
