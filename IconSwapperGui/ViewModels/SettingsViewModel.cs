using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using IconSwapperGui.Commands;
using IconSwapperGui.Commands.Converter;
using IconSwapperGui.Commands.Settings;
using IconSwapperGui.Commands.Swapper;
using IconSwapperGui.Interfaces;
using IconSwapperGui.Models;
using IconSwapperGui.Services;
using Microsoft.WindowsAPICodePack.Shell.Interop;
using Application = System.Windows.Application;

namespace IconSwapperGui.ViewModels;

public class SettingsViewModel : ViewModel
{
    public ISettingsService SettingsService { get; set; }
    
    private bool? _isDarkModeEnabled;
    public bool? IsDarkModeEnabled
    {
        get => _isDarkModeEnabled;
        set
        {
            if (_isDarkModeEnabled != value)
            {
                _isDarkModeEnabled = value;
                OnPropertyChanged();
                ApplyTheme();
            }
        }
    }
    
    public RelayCommand ToggleDarkModeCommand { get; }

    public SettingsViewModel(ISettingsService settingsService)
    {
        SettingsService = settingsService;
        _isDarkModeEnabled = SettingsService?.GetSettings()?.EnableDarkMode;

        ToggleDarkModeCommand = new ToggleDarkModeCommand(this, null!, x => true);
        
        ApplyTheme();
    }
    
    public void ToggleDarkMode()
    {
        if (IsDarkModeEnabled.HasValue)
        {
            SettingsService.SaveEnableDarkMode(IsDarkModeEnabled.Value);
            ApplyTheme();
        }
    }
    
    private void ApplyTheme()
    {
        var themeUri = IsDarkModeEnabled == true
            ? new Uri("pack://application:,,,/Themes/DarkTheme.xaml")
            : new Uri("pack://application:,,,/Themes/LightTheme.xaml");

        // Remove previous theme dictionaries
        Application.Current.Resources.MergedDictionaries.Clear();

        // Add the selected theme dictionary
        ResourceDictionary theme = new ResourceDictionary { Source = themeUri };
        Application.Current.Resources.MergedDictionaries.Add(theme);
    }
}