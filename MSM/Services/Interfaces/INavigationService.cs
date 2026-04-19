using System.ComponentModel;
using MSM.ViewModels;

namespace MSM.Services.Interfaces;

public interface INavigationService : INotifyPropertyChanged
{
    ViewModelBase? CurrentViewModel { get; }
    void NavigateTo<TViewModel>() where TViewModel : ViewModelBase;
}
