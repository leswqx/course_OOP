using System.ComponentModel;
using MSM.ViewModels;

namespace MSM.Services.Interfaces;

public interface INavigationService : INotifyPropertyChanged
{
    ViewModelBase? CurrentViewModel { get; }
    void NavigateTo<TViewModel>(object? parameter = null) where TViewModel : ViewModelBase;
}
