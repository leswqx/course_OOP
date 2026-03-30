using CommunityToolkit.Mvvm.ComponentModel;

namespace MSM.ViewModels;

/// <summary>
/// Базовый класс для всех ViewModel
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
    public virtual void OnNavigatedTo(object? parameter)
    {
    }

    public virtual void OnNavigatedFrom()
    {
    }
}
