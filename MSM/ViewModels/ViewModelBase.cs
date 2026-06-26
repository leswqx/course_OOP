using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MSM.Helpers;

namespace MSM.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{

    public string ThemeIcon => ThemeManager.IsDark ? "☀" : "🌙";

    public string LanguageIcon => LanguageManager.IsEnglish ? "RU" : "EN";

    [RelayCommand]
    private void ToggleTheme()
    {
        ThemeManager.Toggle();
        OnPropertyChanged(nameof(ThemeIcon));
    }

    [RelayCommand]
    private void ToggleLanguage()
    {
        LanguageManager.Toggle();
        OnPropertyChanged(nameof(LanguageIcon));
    }

    public virtual void OnNavigatedTo(object? parameter) { }
    public virtual void OnNavigatedFrom() { }
}
