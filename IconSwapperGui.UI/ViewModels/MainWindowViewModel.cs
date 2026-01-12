using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IconSwapperGui.Core.Config;
using IconSwapperGui.Core.Interfaces;

namespace IconSwapperGui.UI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;

    [ObservableProperty] private string _applicationName = AppInfo.ApplicationName;
    [ObservableProperty] private string _applicationVersion = AppInfo.GetApplicationVersion();
    [ObservableProperty] private string _pageTitle = "Swapper";

    public MainWindowViewModel(INavigationService navigationService, NotificationPanelViewModel notificationPanel)
    {
        _navigationService = navigationService;
        NotificationPanel = notificationPanel;

        _navigationService.CurrentViewChanged += OnCurrentViewChanged;

        _navigationService.NavigateTo<SwapperViewModel>();
    }

    public object? CurrentView => _navigationService.CurrentView;

    public NotificationPanelViewModel NotificationPanel { get; }

    [RelayCommand]
    private void NavigateToSwapper()
    {
        NavigateTo<SwapperViewModel>("Swapper");
    }

    [RelayCommand]
    private void NavigateToConverter()
    {
        NavigateTo<ConverterViewModel>("Converter");
    }

    [RelayCommand]
    private void NavigateToPixelArtEditor()
    {
        NavigateTo<PixelArtEditorViewModel>("Pixel Art Editor");
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        NavigateTo<SettingsViewModel>("Settings");
    }

    private void NavigateTo<TViewModel>(string title) where TViewModel : class
    {
        _navigationService.NavigateTo<TViewModel>();
        PageTitle = title;

        OnPropertyChanged("PageTitle");
    }

    private void OnCurrentViewChanged()
    {
        OnPropertyChanged(nameof(CurrentView));
    }
}