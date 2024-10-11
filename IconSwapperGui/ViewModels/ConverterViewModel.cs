using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using IconSwapperGui.Commands;
using IconSwapperGui.Commands.Converter;
using IconSwapperGui.Commands.Swapper;
using IconSwapperGui.Interfaces;
using IconSwapperGui.Models;
using IconSwapperGui.Services;

namespace IconSwapperGui.ViewModels
{
    public class ConverterViewModel : ViewModel, IIconViewModel, INotifyPropertyChanged, IDisposable
    {
        public ISettingsService SettingsService { get; set; }
        private readonly IIconManagementService _iconManagementService;
        public IDialogService DialogService { get; set; }
        private IFileSystemWatcherService _fsWatcherService;

        private ObservableCollection<Icon> _icons;
        private ObservableCollection<Icon> _filteredIcons;
        private string _filterString;
        private bool _canConvertImages;
        private string _iconsFolderPath;

        public bool CanDeletePngImages { get; set; }

        public string IconsFolderPath
        {
            get => _iconsFolderPath;
            set
            {
                if (_iconsFolderPath != value)
                {
                    _iconsFolderPath = value;
                    OnPropertyChanged();
                    SetupIconsDirectoryWatcher();
                }
            }
        }

        public ObservableCollection<Icon> Icons
        {
            get => _icons;
            set
            {
                _icons = value;
                OnPropertyChanged();
                FilterIcons();
            }
        }

        public ObservableCollection<Icon> FilteredIcons
        {
            get => _filteredIcons;
            private set
            {
                _filteredIcons = value;
                OnPropertyChanged();
            }
        }

        public bool CanConvertImages
        {
            get => _canConvertImages;
            set => SetField(ref _canConvertImages, value);
        }

        public RelayCommand ConvertIconCommand { get; }
        public RelayCommand ChooseIconFolderCommand { get; }

        public ConverterViewModel(IIconManagementService iconManagementManagementService, ISettingsService settingsService,
            IDialogService dialogService,
            Func<string, Action<object, FileSystemEventArgs>, Action<object, RenamedEventArgs>,
                IFileSystemWatcherService> fileSystemWatcherServiceFactory)
        {
            _iconManagementService = iconManagementManagementService;
            SettingsService = settingsService;
            DialogService = dialogService;
            _fsWatcherService = fileSystemWatcherServiceFactory(SettingsService.GetConverterIconsLocation(),
                OnIconsDirectoryChanged, OnIconsDirectoryRenamed);

            Icons = new ObservableCollection<Icon>();
            FilteredIcons = new ObservableCollection<Icon>();

            ConvertIconCommand = new ConvertIconCommand(this, null!, x => true);
            ChooseIconFolderCommand = new ChooseIconFolderCommand<ConverterViewModel>(this, null!, x => true);

            LoadPreviousIcons();
        }

        private void SetupIconsDirectoryWatcher()
        {
            if (string.IsNullOrEmpty(IconsFolderPath)) return;

            _fsWatcherService?.Dispose();
            _fsWatcherService =
                new FileSystemWatcherService(IconsFolderPath, OnIconsDirectoryChanged, OnIconsDirectoryRenamed);
            _fsWatcherService.StartWatching();
        }

        private void OnIconsDirectoryChanged(object sender, FileSystemEventArgs e)
        {
            PopulateIconsList(IconsFolderPath);
        }

        private void OnIconsDirectoryRenamed(object sender, RenamedEventArgs e)
        {
            PopulateIconsList(IconsFolderPath);
        }

        private void LoadPreviousIcons()
        {
            IconsFolderPath = SettingsService.GetConverterIconsLocation();

            if (!string.IsNullOrEmpty(IconsFolderPath))
            {
                PopulateIconsList(IconsFolderPath);
                SetupIconsDirectoryWatcher();
            }
        }

        public void PopulateIconsList(string folderPath)
        {
            Icons = _iconManagementService.GetIcons(folderPath);
            FilterIcons();
            UpdateConvertButtonEnabledState();
        }

        public void FilterIcons()
        {
            FilteredIcons = _iconManagementService.FilterIcons(Icons, _filterString);
        }

        public string FilterString
        {
            get => _filterString;
            set
            {
                if (_filterString == value) return;
                _filterString = value;
                OnPropertyChanged();
                FilterIcons();
            }
        }

        public void RefreshGui()
        {
            Icons.Clear();
            PopulateIconsList(IconsFolderPath);
        }

        public void UpdateConvertButtonEnabledState()
        {
            CanConvertImages = Icons.Count > 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public void Dispose()
        {
            _fsWatcherService?.Dispose();
        }
    }
}