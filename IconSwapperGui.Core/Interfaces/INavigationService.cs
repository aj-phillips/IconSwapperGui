namespace IconSwapperGui.Core.Interfaces;

public interface INavigationService
{
    object? CurrentView { get; }
    void NavigateTo<TViewModel>() where TViewModel : class;
    event Action? CurrentViewChanged;
}