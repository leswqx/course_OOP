# WPF MVVM Skill

## CommunityToolkit.Mvvm Patterns

### ViewModel Base
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public partial class PropertyViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title;
    
    [ObservableProperty]
    private decimal _price;
    
    [ObservableProperty]
    private bool _isLoading;
    
    [RelayCommand]
    private async Task SaveAsync()
    {
        // Save logic
    }
    
    [RelayCommand]
    private void Cancel()
    {
        // Cancel logic
    }
}

