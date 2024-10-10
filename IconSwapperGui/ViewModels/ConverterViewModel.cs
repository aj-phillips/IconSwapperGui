using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using IconSwapperGui.Commands;
using IconSwapperGui.Commands.Converter;
using IconSwapperGui.Commands.Swapper;
using IconSwapperGui.Interfaces;
using IconSwapperGui.Models;

namespace IconSwapperGui.ViewModels
{
    public class ConverterViewModel : ViewModel, IIconViewModel, INotifyPropertyChanged, IDisposable
    {
        public ISettingsService SettingsService { get; set; }
        private readonly IIconService _iconService;
        public IDialogService DialogService { get; set; }

        private ObservableCollection<Icon> _icons;
        private ObservableCollection<Icon> _filteredIcons;
        private string _filterString;
        private FileSystemWatcher _iconsDirectoryWatcher;
        
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

        public ConverterViewModel(IIconService iconService, ISettingsService settingsService,
            IDialogService dialogService)
        {
            Icons = new ObservableCollection<Icon>();
            FilteredIcons = new ObservableCollection<Icon>();

            _iconService = iconService;
            SettingsService = settingsService;
            DialogService = dialogService;

            ConvertIconCommand = new ConvertIconCommand(this, null!, x => true);
            ChooseIconFolderCommand = new ChooseIconFolderCommand<ConverterViewModel>(this, null!, x => true);

            LoadPreviousIcons();
        }

        private void SetupIconsDirectoryWatcher()
        {
            if (string.IsNullOrEmpty(IconsFolderPath)) return;

            _iconsDirectoryWatcher?.Dispose();
            _iconsDirectoryWatcher = new FileSystemWatcher(IconsFolderPath)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName
            };
            _iconsDirectoryWatcher.Created += OnIconsDirectoryChanged;
            _iconsDirectoryWatcher.Deleted += OnIconsDirectoryChanged;
            _iconsDirectoryWatcher.Renamed += OnIconsDirectoryRenamed;
            _iconsDirectoryWatcher.EnableRaisingEvents = true;
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
            if (!string.IsNullOrEmpty(folderPath))
            {
                var icons = _iconService.GetPngIcons(folderPath);
                
                Icons.Clear();
                
                foreach (var icon in icons)
                {
                    if (Icons.Any(x => x.Path == icon.Path)) continue;
                    Icons.Add(icon);
                }

                FilterIcons();
            }

            UpdateConvertButtonEnabledState();
        }

        public void FilterIcons()
        {
            if (string.IsNullOrEmpty(_filterString))
            {
                FilteredIcons = new ObservableCollection<Icon>(Icons);
            }
            else
            {
                var filtered = Icons
                    .Where(icon => icon.Name.Contains(_filterString, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                FilteredIcons = new ObservableCollection<Icon>(filtered);
            }
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
            _icons.Clear();
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
            _iconsDirectoryWatcher?.Dispose();
        }
    }
}