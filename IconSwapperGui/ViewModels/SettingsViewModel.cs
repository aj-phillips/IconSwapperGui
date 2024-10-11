using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using IconSwapperGui.Commands;
using IconSwapperGui.Interfaces;
using IconSwapperGui.Models;
using IconSwapperGui.Services;
using Application = System.Windows.Application;

namespace IconSwapperGui.ViewModels
{
    public class SettingsViewModel : ViewModel
    {
        private const string StartupKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "IconSwapperGui";

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

        private bool? _isLaunchAtStartupEnabled;

        public bool? IsLaunchAtStartupEnabled
        {
            get => _isLaunchAtStartupEnabled;
            set
            {
                if (_isLaunchAtStartupEnabled != value)
                {
                    _isLaunchAtStartupEnabled = value;
                    OnPropertyChanged();
                    ToggleLaunchAtStartup();
                }
            }
        }

        public RelayCommand ToggleDarkModeCommand { get; }
        public RelayCommand ToggleLaunchAtStartupCommand { get; }

        public SettingsViewModel(ISettingsService settingsService)
        {
            SettingsService = settingsService;
            _isDarkModeEnabled = SettingsService?.GetSettingsFieldValue<bool>("EnableDarkMode");
            _isLaunchAtStartupEnabled = SettingsService?.GetSettingsFieldValue<bool>("EnableLaunchAtStartup");

            ToggleDarkModeCommand = new RelayCommand(param => ToggleDarkMode());
            ToggleLaunchAtStartupCommand = new RelayCommand(param => ToggleLaunchAtStartup());

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

        public void ToggleLaunchAtStartup()
        {
            if (IsLaunchAtStartupEnabled.HasValue)
            {
                SettingsService.SaveEnableLaunchAtStartup(IsLaunchAtStartupEnabled.Value);
                UpdateLaunchAtStartupRegistry(IsLaunchAtStartupEnabled.Value);
            }
        }

        private void UpdateLaunchAtStartupRegistry(bool enable)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(StartupKey, true))
            {
                if (key != null)
                {
                    if (enable)
                    {
                        string executablePath =
                            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IconSwapperGui.exe");
                        key.SetValue(AppName, $"\"{executablePath}\"");
                    }
                    else
                    {
                        key.DeleteValue(AppName, false);
                    }
                }
            }
        }

        private void ApplyTheme()
        {
            var themeUri = IsDarkModeEnabled == true
                ? new Uri("pack://application:,,,/Themes/DarkTheme.xaml")
                : new Uri("pack://application:,,,/Themes/LightTheme.xaml");

            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
        }
    }
}