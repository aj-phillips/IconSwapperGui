using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IconSwapperGui.Core.Interfaces;
using IconSwapperGui.Core.Models.Swapper;
using IconSwapperGui.Infrastructure.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using IconSwapperGui.Core.Models;
using IconSwapperGui.UI.Windows;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace IconSwapperGui.UI.ViewModels
{
    public partial class SwapperViewModel : ObservableObject
    {
        // Services
        private readonly INotificationService _notificationService;
        private readonly ISettingsService _settingsService;
        private readonly IShortcutService _shortcutService;
        private readonly IIconManagementService _iconManagementService;
        private readonly IIconHistoryService _iconHistoryService;
        private readonly IFolderManagementService _folderManagementService;
        private readonly ILoggingService _loggingService;
        private readonly ILnkSwapperService _lnkSwapperService;
        private readonly IUrlSwapperService _urlSwapperService;
        private readonly Func<string, IconVersionManagerViewModel> _iconVersionManagerViewModelFactory;

        // Properties
        private List<IFileSystemWatcherService>? _shortcutsDirectoryWatcherServices;
        private List<IFileSystemWatcherService>? _iconsDirectoryWatcherServices;
        private List<IFileSystemWatcherService>? _foldersDirectoryWatcherServices;
        private CancellationTokenSource? _filterCts;

        // Observable Properties
        [ObservableProperty] private ObservableCollection<Shortcut> _shortcuts;
        [ObservableProperty] private ObservableCollection<string> _shortcutsFolders;

        [ObservableProperty] private ObservableCollection<Icon> _icons;
        [ObservableProperty] private ObservableCollection<Icon> _filteredIcons;
        [ObservableProperty] private ObservableCollection<string> _iconsFolders;

        [ObservableProperty] private bool _isFolderTabSelected;
        [ObservableProperty] private ObservableCollection<FolderShortcut> _folders;
        [ObservableProperty] private ObservableCollection<string> _foldersFolders;

        [ObservableProperty] private string? _filterString;

        [ObservableProperty] private bool _canSwapIcons;
        [ObservableProperty] private bool _canSwapFolderIcons;
        [ObservableProperty] private bool _isTickVisible;

        [ObservableProperty] private FolderShortcut? _selectedFolder;
        [ObservableProperty] private Shortcut? _selectedShortcut;
        [ObservableProperty] private Icon? _selectedIcon;

        public SwapperViewModel(
            ISettingsService settingsService,
            INotificationService notificationService,
            IShortcutService shortcutService,
            IIconManagementService iconManagementService,
            IIconHistoryService iconHistoryService,
            IFolderManagementService folderManagementService,
            ILoggingService loggingService,
            ILnkSwapperService lnkSwapperService,
            IUrlSwapperService urlSwapperService,
            Func<string, IconVersionManagerViewModel> iconVersionManagerViewModelFactory)
        {
            _settingsService = settingsService;
            _notificationService = notificationService;
            _shortcutService = shortcutService;
            _iconManagementService = iconManagementService;
            _iconHistoryService = iconHistoryService;
            _folderManagementService = folderManagementService;
            _loggingService = loggingService;
            _lnkSwapperService = lnkSwapperService;
            _urlSwapperService = urlSwapperService;
            _iconVersionManagerViewModelFactory = iconVersionManagerViewModelFactory;

            _loggingService.LogInfo("SwapperViewModel initialising");

            Shortcuts = new ObservableCollection<Shortcut>();
            ShortcutsFolders = new ObservableCollection<string>();
            Icons = new ObservableCollection<Icon>();
            IconsFolders = new ObservableCollection<string>();
            Folders = new ObservableCollection<FolderShortcut>();
            FoldersFolders = new ObservableCollection<string>();
            FilteredIcons = new ObservableCollection<Icon>();

            InitializeAsync();

            _loggingService.LogInfo("SwapperViewModel initialised");
        }

        public void InitializeAsync()
        {
            try
            {
                LoadPreviousShortcuts();
                LoadPreviousIcons();
                LoadPreviousFolders();
                Application.Current.Dispatcher.Invoke(UpdateSwapButtonEnabledState);
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Error during async initialization", ex);
            }
        }

        public bool CanSwap => (!IsFolderTabSelected && CanSwapIcons) || (IsFolderTabSelected && CanSwapFolderIcons);

        private void UpdateSwapButtonEnabledState()
        {
            CanSwapIcons = SelectedShortcut != null && SelectedIcon != null;
            CanSwapFolderIcons = SelectedFolder != null && SelectedIcon != null;
        }

        partial void OnSelectedShortcutChanged(Shortcut? value)
        {
            UpdateSwapButtonEnabledState();

            if (DualSwapCommand is { } dual)
                dual.NotifyCanExecuteChanged();

            if (SwapCommand is { } swap)
                swap.NotifyCanExecuteChanged();

            if (ManageVersionsContextCommand is { } manage)
                manage.NotifyCanExecuteChanged();

            CommandManager.InvalidateRequerySuggested();
        }

        partial void OnIsFolderTabSelectedChanged(bool value)
        {
            UpdateSwapButtonEnabledState();

            if (DualSwapCommand is { } dual)
                dual.NotifyCanExecuteChanged();

            if (SwapCommand is { } swap)
                swap.NotifyCanExecuteChanged();

            if (SwapFolderIconCommand is { } swapFolder)
                swapFolder.NotifyCanExecuteChanged();

            if (ManageVersionsContextCommand is { } manage)
                manage.NotifyCanExecuteChanged();

            CommandManager.InvalidateRequerySuggested();
        }

        partial void OnSelectedIconChanged(Icon? value)
        {
            UpdateSwapButtonEnabledState();

            if (DualSwapCommand is { } dual)
                dual.NotifyCanExecuteChanged();

            if (SwapCommand is { } swap)
                swap.NotifyCanExecuteChanged();

            if (SwapFolderIconCommand is { } swapFolder)
                swapFolder.NotifyCanExecuteChanged();

            CommandManager.InvalidateRequerySuggested();
        }

        partial void OnSelectedFolderChanged(FolderShortcut? value)
        {
            UpdateSwapButtonEnabledState();

            if (DualSwapCommand is { } dual)
                dual.NotifyCanExecuteChanged();

            if (SwapFolderIconCommand is { } swapFolder)
                swapFolder.NotifyCanExecuteChanged();

            CommandManager.InvalidateRequerySuggested();
        }

        partial void OnFilterStringChanged(string? value)
        {
            try
            {
                _filterCts?.Cancel();
                _filterCts?.Dispose();
                _filterCts = new CancellationTokenSource();
                var token = _filterCts.Token;

                _ = DebounceFilterAsync(value, token);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error scheduling debounced filter: {value}", ex);
            }
        }

        private async Task DebounceFilterAsync(string? value, CancellationToken token)
        {
            try
            {
                await Task.Delay(300, token).ConfigureAwait(false);

                if (token.IsCancellationRequested)
                    return;

                InvokeOnUI(FilterIcons);
            }
            catch (OperationCanceledException)
            {
                // expected when debounce is cancelled
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error applying debounced filter: {value}", ex);
            }
        }

        private void InvokeOnUI(Action action)
        {
            try
            {
                var app = System.Windows.Application.Current;

                if (app == null || app.Dispatcher == null)
                {
                    action();
                    return;
                }

                if (app.Dispatcher.CheckAccess())
                {
                    action();
                }
                else
                {
                    app.Dispatcher.Invoke(action);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Failed to invoke action on UI thread", ex);
            }
        }

        public async Task RefreshGuiAsync()
        {
            _loggingService.LogInfo("Refreshing GUI (async), clearing icons/folders and repopulating");

            try
            {
                var prevSelectedApplicationPath = SelectedShortcut?.Path;
                var prevSelectedIconPath = SelectedIcon?.Path;
                var prevSelectedFolderPath = SelectedFolder?.Path;

                var shortcutsList = _settingsService.Settings.Application.ShortcutLocations;
                var iconsList = _settingsService.Settings.Application.IconLocations;
                var foldersList = _settingsService.Settings.Application.FolderShortcutLocations;

                var allShortcuts = new List<Shortcut>();

                try
                {
                    var publicApps = await Task.Run(() => _shortcutService.GetShortcuts(null))
                        .ConfigureAwait(false);

                    foreach (var a in publicApps)
                    {
                        if (allShortcuts.All(x => x.Path != a.Path))
                            allShortcuts.Add(a);
                    }

                    foreach (var folder in shortcutsList)
                    {
                        try
                        {
                            var appsFromFolder = _shortcutService.GetShortcuts(folder);

                            foreach (var a in appsFromFolder)
                            {
                                if (allShortcuts.All(x => x.Path != a.Path))
                                    allShortcuts.Add(a);
                            }
                        }
                        catch (Exception ex)
                        {
                            _loggingService.LogError($"Failed to load applications from folder: {folder}", ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _loggingService.LogError("Error aggregating applications for GUI refresh", ex);
                }

                InvokeOnUI(() =>
                {
                    Shortcuts.Clear();
                    Icons.Clear();
                    Folders.Clear();

                    if (shortcutsList.Any())
                    {
                        ShortcutsFolders = new ObservableCollection<string>(shortcutsList);
                        PopulateShortcutsFromLocations(ShortcutsFolders);
                        SetupShortcutsDirectoryWatcher();
                    }

                    if (iconsList.Any())
                    {
                        IconsFolders = new ObservableCollection<string>(iconsList);
                        PopulateIconsFromLocations(IconsFolders);
                        SetupIconsDirectoryWatcher();
                    }

                    if (foldersList.Any())
                    {
                        FoldersFolders = new ObservableCollection<string>(foldersList);
                        PopulateFoldersFromLocations(FoldersFolders);
                        SetupFoldersDirectoryWatcher();
                    }

                    if (prevSelectedApplicationPath != null)
                    {
                        SelectedShortcut =
                            Shortcuts.FirstOrDefault(app => app.Path == prevSelectedApplicationPath);
                    }

                    if (prevSelectedIconPath != null)
                    {
                        SelectedIcon = Icons.FirstOrDefault(icon => icon.Path == prevSelectedIconPath);
                    }

                    if (prevSelectedFolderPath != null)
                    {
                        SelectedFolder = Folders.FirstOrDefault(f => f.Path == prevSelectedFolderPath);
                    }

                    UpdateSwapButtonEnabledState();
                });
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Error refreshing GUI", ex);
            }
        }

        public void PopulateIconsFromLocations(IEnumerable<string>? folderPaths)
        {
            _loggingService.LogInfo("PopulateIconsFromLocations called");
            var supportedExtensions = new List<string> { ".ico" };

            try
            {
                foreach (var folderPath in folderPaths ?? Enumerable.Empty<string>())
                {
                    var icons = _iconManagementService.GetIcons(folderPath, supportedExtensions);

                    foreach (var icon in icons)
                    {
                        if (Icons.Any(x => x.Path == icon.Path)) continue;
                        Icons.Add(icon);
                    }
                }

                FilterIcons();
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Error populating icons from locations", ex);
            }
        }

        public void PopulateShortcutsFromLocations(IEnumerable<string>? folderPaths)
        {
            _loggingService.LogInfo("PopulateApplicationsFromLocations called");

            try
            {
                foreach (var folderPath in folderPaths ?? Enumerable.Empty<string>())
                {
                    var shortcuts = _shortcutService.GetShortcuts(folderPath);

                    foreach (var shortcut in shortcuts)
                    {
                        if (Shortcuts.Any(x => x.Path == shortcut.Path))
                            continue;

                        Shortcuts.Add(shortcut);
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Error populating applications from locations", ex);
            }
        }

        public void PopulateFoldersFromLocations(IEnumerable<string>? folderPaths)
        {
            _loggingService.LogInfo("PopulateFoldersFromLocations called");

            try
            {
                foreach (var folderPath in folderPaths ?? Enumerable.Empty<string>())
                {
                    var folders = _folderManagementService.GetFolders(folderPath);

                    foreach (var f in folders)
                    {
                        if (Folders.Any(x => x.Path == f.Path))
                            continue;

                        Folders.Add(f);
                    }
                }

                _loggingService.LogInfo($"Populated folders from locations (total: {Folders.Count})");
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Error populating folders from locations", ex);
            }
        }

        public void FilterIcons()
        {
            try
            {
                FilteredIcons ??= new ObservableCollection<Icon>();
                FilteredIcons.Clear();

                if (string.IsNullOrWhiteSpace(FilterString))
                {
                    foreach (var icon in Icons)
                        FilteredIcons.Add(icon);
                }
                else
                {
                    var filter = FilterString.Trim();
                    var filtered = Icons.Where(icon => icon.Name.Contains(filter, StringComparison.OrdinalIgnoreCase));

                    foreach (var icon in filtered)
                        FilteredIcons.Add(icon);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Error filtering icons", ex);
            }
        }

        private void ExecuteDualSwap()
        {
            try
            {
                if (IsFolderTabSelected)
                {
                    SwapFolderIconCommand?.Execute(null);
                }
                else
                {
                    SwapCommand?.Execute(null);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Error executing dual swap", ex);
            }
        }

        public Task<string?> GetCurrentIconPathAsync(string filePath)
        {
            return _iconManagementService.GetCurrentIconPathAsync(filePath);
        }

        public async Task ShowSuccessTick()
        {
            try
            {
                IsTickVisible = true;
                await Task.Delay(800).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Error showing success tick", ex);
            }
            finally
            {
                InvokeOnUI(() => { IsTickVisible = false; });
            }
        }

        public void ResetGui()
        {
            try
            {
                SelectedIcon = null;
                SelectedShortcut = null;
                SelectedFolder = null;
                FilterString = null;
                _ = RefreshGuiAsync();
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Error resetting GUI", ex);
            }
        }

        [RelayCommand]
        private Task ManageVersionsContext(object parameter)
        {
            if (parameter is not Shortcut shortcut)
                return Task.CompletedTask;

            return OpenVersionManagerAsync(shortcut);
        }

        private Task OpenVersionManagerAsync(Shortcut shortcut)
        {
            if (shortcut == null) return Task.CompletedTask;

            try
            {
                var viewModel = _iconVersionManagerViewModelFactory(shortcut.Path);

                viewModel.VersionReverted += _ => Application.Current.Dispatcher.Invoke(() =>
                {
                    LoadPreviousShortcuts();
                    ResetGui();
                });

                var window = new IconVersionManagerWindow
                    { Owner = Application.Current.MainWindow };

                viewModel.RequestClose += () => window.Close();
                window.DataContext = viewModel;

                var result = window.ShowDialog();

                if (result == true)
                    LoadPreviousShortcuts();
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Error opening version manager", ex);
                _notificationService.AddNotification("Icon Version Manager", "Failed to open version manager",
                    NotificationType.Error);
            }

            return Task.CompletedTask;
        }

        [RelayCommand]
        private async Task DeleteIconContext(object parameter)
        {
            if (parameter is not Icon icon)
                return;

            try
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete '{icon.Name}'? This action cannot be undone.",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;

                var success = await _iconManagementService.DeleteIconAsync(icon.Path);

                if (success)
                {
                    _notificationService.AddNotification("Icon Deleted", $"Successfully deleted '{icon.Name}'",
                        NotificationType.Success);

                    await RefreshGuiAsync();
                }
                else
                {
                    _notificationService.AddNotification("Delete Failed", $"Failed to delete '{icon.Name}'",
                        NotificationType.Error);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error deleting icon: {icon.Path}", ex);
                _notificationService.AddNotification("Error", "An error occurred while deleting the icon",
                    NotificationType.Error);
            }
        }

        [RelayCommand]
        private async Task RenameIconContext(object parameter)
        {
            if (parameter is not Icon icon)
                return;

            try
            {
                var window = new RenameIconWindow
                {
                    Owner = Application.Current.MainWindow
                };

                var viewModel = new RenameIconViewModel(icon.Name, _iconManagementService, _loggingService);
                viewModel.RequestClose += result =>
                {
                    window.DialogResult = result;
                    window.Close();
                };

                window.DataContext = viewModel;

                var dialogResult = window.ShowDialog();

                if (dialogResult == true && !string.IsNullOrWhiteSpace(viewModel.NewName))
                {
                    var success = await _iconManagementService.RenameIconAsync(icon.Path, viewModel.NewName);

                    if (success)
                    {
                        _notificationService.AddNotification("Icon Renamed", 
                            $"Successfully renamed '{icon.Name}' to '{viewModel.NewName}'",
                            NotificationType.Success);

                        await RefreshGuiAsync();
                    }
                    else
                    {
                        _notificationService.AddNotification("Rename Failed", 
                            $"Failed to rename '{icon.Name}'. The file may already exist or be in use.",
                            NotificationType.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Error renaming icon: {icon.Path}", ex);
                _notificationService.AddNotification("Error", "An error occurred while renaming the icon",
                    NotificationType.Error);
            }
        }

        [RelayCommand(CanExecute = nameof(CanSwap))]
        private void DualSwap()
        {
            ExecuteDualSwap();
        }

        private bool CanSwapExecute() => (SelectedShortcut != null && SelectedIcon != null) ||
                                         (SelectedFolder != null && SelectedIcon != null);

        [RelayCommand(CanExecute = nameof(CanSwapExecute))]
        private async Task Swap()
        {
            try
            {
                if (SelectedShortcut == null || SelectedIcon == null)
                {
                    MessageBox.Show("Please select an application and an icon to swap.",
                        "No Application or Icon Selected", MessageBoxButton.OK, MessageBoxImage.Warning);

                    return;
                }

                try
                {
                    if (_iconHistoryService != null)
                    {
                        var filePath = SelectedShortcut.Path;
                        string? currentIconPath = null;

                        try
                        {
                            currentIconPath = await GetCurrentIconPathAsync(filePath);
                        }
                        catch
                        {
                        }

                        if (!string.IsNullOrEmpty(currentIconPath))
                        {
                            try
                            {
                                if (File.Exists(currentIconPath))
                                {
                                    var ext = Path.GetExtension(currentIconPath)?.ToLowerInvariant();
                                    if (ext == ".exe" || ext == ".dll")
                                    {
                                        try
                                        {
                                            using var icon = System.Drawing.Icon.ExtractAssociatedIcon(currentIconPath);
                                            if (icon != null)
                                            {
                                                var tempIconPath = Path.Combine(Path.GetTempPath(),
                                                    Guid.NewGuid() + ".ico");
                                                using (var fs = new FileStream(tempIconPath, FileMode.Create,
                                                           FileAccess.Write))
                                                {
                                                    icon.Save(fs);
                                                }

                                                currentIconPath = tempIconPath;
                                            }
                                        }
                                        catch
                                        {
                                            // ignore
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                // ignore
                            }

                            var existing = await _iconHistoryService.GetHistoryAsync(filePath);
                            if (existing == null || existing.Versions.Count == 0)
                            {
                                try
                                {
                                    await _iconHistoryService.RecordIconChangeAsync(filePath, currentIconPath);
                                }
                                catch
                                {
                                    // ignore
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // ignore
                }

                var extension = Path.GetExtension(SelectedShortcut.Path)?.ToLowerInvariant();

                switch (extension)
                {
                    case ".lnk":
                        _lnkSwapperService.Swap(SelectedShortcut.Path,
                            SelectedIcon.Path, SelectedShortcut.Name);
                        break;
                    case ".url":
                        _urlSwapperService.Swap(SelectedShortcut.Path, SelectedIcon.Path);
                        break;
                }

                var capturedApplicationPath = SelectedShortcut?.Path;
                var capturedIconPath = SelectedIcon?.Path;

                try
                {
                    if (_iconHistoryService != null && !string.IsNullOrWhiteSpace(capturedApplicationPath))
                    {
                        await _iconHistoryService.RecordIconChangeAsync(capturedApplicationPath, capturedIconPath);
                    }
                }
                catch (Exception ex)
                {
                    _loggingService.LogError("Failed to record icon change history", ex);
                }

                await ShowSuccessTick();
                ResetGui();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while swapping the icon for {SelectedShortcut?.Name}: {ex.Message}",
                    "Error Swapping Icon", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand(CanExecute = nameof(CanSwapExecute))]
        private async Task SwapFolderIcon()
        {
            try
            {
                if (SelectedFolder == null || SelectedIcon == null)
                {
                    MessageBox.Show("Please select a folder and an icon to swap.", "No Folder or Icon Selected",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var success =
                    await _folderManagementService.ChangeFolderIconAsync(SelectedFolder.Path, SelectedIcon.Path);

                if (success)
                {
                    try
                    {
                        if (_iconHistoryService != null)
                        {
                            await _iconHistoryService.RecordIconChangeAsync(SelectedFolder.Path, SelectedIcon.Path);
                        }
                    }
                    catch
                    {
                        // ignore
                    }

                    await ShowSuccessTick();

                    await RefreshGuiAsync();
                }
                else
                {
                    MessageBox.Show("Failed to change folder icon", "Error", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Error swapping folder icon", ex);
                MessageBox.Show("Failed to change folder icon", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SetupIconsDirectoryWatcher()
        {
            try
            {
                if (_iconsDirectoryWatcherServices != null)
                {
                    foreach (var svc in _iconsDirectoryWatcherServices)
                        svc.Dispose();

                    _iconsDirectoryWatcherServices = null;
                }

                var locations = IconsFolders?.Where(p => !string.IsNullOrWhiteSpace(p)).ToList() ?? new List<string>();

                if (locations.Any())
                {
                    _iconsDirectoryWatcherServices = new List<IFileSystemWatcherService>();

                    foreach (var loc in locations)
                    {
                        if (!Directory.Exists(loc))
                            continue;

                        var svc = new FileSystemWatcherService(loc, OnIconsDirectoryChanged, OnIconsDirectoryRenamed);

                        svc.StartWatching();

                        _iconsDirectoryWatcherServices.Add(svc);
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Error setting up icons watcher", ex);
            }
        }

        private void SetupShortcutsDirectoryWatcher()
        {
            try
            {
                if (_shortcutsDirectoryWatcherServices != null)
                {
                    foreach (var svc in _shortcutsDirectoryWatcherServices)
                        svc.Dispose();

                    _shortcutsDirectoryWatcherServices = null;
                }

                var locations = ShortcutsFolders?.Where(p => !string.IsNullOrWhiteSpace(p)).ToList() ??
                                new List<string>();

                if (locations.Any())
                {
                    _shortcutsDirectoryWatcherServices = new List<IFileSystemWatcherService>();

                    foreach (var loc in locations)
                    {
                        if (!Directory.Exists(loc))
                            continue;

                        var svc = new FileSystemWatcherService(loc, OnShortcutsDirectoryChanged,
                            OnShortcutsDirectoryRenamed);

                        svc.StartWatching();

                        _shortcutsDirectoryWatcherServices.Add(svc);
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Error setting up shortcuts watcher", ex);
            }
        }

        private void SetupFoldersDirectoryWatcher()
        {
            try
            {
                if (_foldersDirectoryWatcherServices != null)
                {
                    foreach (var svc in _foldersDirectoryWatcherServices)
                        svc.Dispose();

                    _foldersDirectoryWatcherServices = null;
                }

                var locations = FoldersFolders?.Where(p => !string.IsNullOrWhiteSpace(p)).ToList() ??
                                new List<string>();

                if (locations.Any())
                {
                    _foldersDirectoryWatcherServices = new List<IFileSystemWatcherService>();

                    foreach (var loc in locations)
                    {
                        if (!Directory.Exists(loc))
                            continue;

                        var svc = new FileSystemWatcherService(loc, OnFoldersDirectoryChanged,
                            OnFoldersDirectoryRenamed);

                        svc.StartWatching();

                        _foldersDirectoryWatcherServices.Add(svc);
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Error setting up folders watcher", ex);
            }
        }

        private void SyncIconsFromLocations(IEnumerable<string>? folderPaths)
        {
            var supportedExtensions = new List<string> { ".ico" };
            var desired = new Dictionary<string, Icon>(StringComparer.OrdinalIgnoreCase);

            foreach (var folderPath in folderPaths ?? Enumerable.Empty<string>())
            {
                try
                {
                    foreach (var icon in _iconManagementService.GetIcons(folderPath, supportedExtensions))
                        desired[icon.Path] = icon;
                }
                catch (Exception ex)
                {
                    _loggingService.LogError($"Error retrieving icons from location: {folderPath}", ex);
                }
            }

            InvokeOnUI(() =>
            {
                for (var i = Icons.Count - 1; i >= 0; i--)
                {
                    if (!desired.ContainsKey(Icons[i].Path))
                        Icons.RemoveAt(i);
                }

                foreach (var icon in desired.Values)
                {
                    if (Icons.All(x => !string.Equals(x.Path, icon.Path, StringComparison.OrdinalIgnoreCase)))
                        Icons.Add(icon);
                }

                if (SelectedIcon != null && Icons.All(x =>
                        !string.Equals(x.Path, SelectedIcon.Path, StringComparison.OrdinalIgnoreCase)))
                    SelectedIcon = null;

                FilterIcons();
            });
        }

        private void SyncShortcutsFromLocations(IEnumerable<string>? folderPaths)
        {
            var desired = new Dictionary<string, Shortcut>(StringComparer.OrdinalIgnoreCase);

            foreach (var folderPath in folderPaths ?? Enumerable.Empty<string>())
            {
                try
                {
                    foreach (var shortcut in _shortcutService.GetShortcuts(folderPath))
                        desired[shortcut.Path] = shortcut;
                }
                catch (Exception ex)
                {
                    _loggingService.LogError($"Error retrieving shortcuts from location: {folderPath}", ex);
                }
            }

            InvokeOnUI(() =>
            {
                for (var i = Shortcuts.Count - 1; i >= 0; i--)
                {
                    if (!desired.ContainsKey(Shortcuts[i].Path))
                        Shortcuts.RemoveAt(i);
                }

                foreach (var shortcut in desired.Values)
                {
                    if (Shortcuts.All(x => !string.Equals(x.Path, shortcut.Path, StringComparison.OrdinalIgnoreCase)))
                        Shortcuts.Add(shortcut);
                }

                if (SelectedShortcut != null && Shortcuts.All(x =>
                        !string.Equals(x.Path, SelectedShortcut.Path, StringComparison.OrdinalIgnoreCase)))
                    SelectedShortcut = null;

                UpdateSwapButtonEnabledState();
            });
        }

        private void SyncFoldersFromLocations(IEnumerable<string>? folderPaths)
        {
            var desired = new Dictionary<string, FolderShortcut>(StringComparer.OrdinalIgnoreCase);

            foreach (var folderPath in folderPaths ?? Enumerable.Empty<string>())
            {
                try
                {
                    foreach (var folder in _folderManagementService.GetFolders(folderPath))
                        desired[folder.Path] = folder;
                }
                catch (Exception ex)
                {
                    _loggingService.LogError($"Error retrieving folders from location: {folderPath}", ex);
                }
            }

            InvokeOnUI(() =>
            {
                for (var i = Folders.Count - 1; i >= 0; i--)
                {
                    if (!desired.ContainsKey(Folders[i].Path))
                        Folders.RemoveAt(i);
                }

                foreach (var folder in desired.Values)
                {
                    if (Folders.All(x => !string.Equals(x.Path, folder.Path, StringComparison.OrdinalIgnoreCase)))
                        Folders.Add(folder);
                }

                if (SelectedFolder != null && Folders.All(x =>
                        !string.Equals(x.Path, SelectedFolder.Path, StringComparison.OrdinalIgnoreCase)))
                    SelectedFolder = null;

                UpdateSwapButtonEnabledState();
            });
        }

        private void OnIconsDirectoryChanged(object sender, FileSystemEventArgs e) =>
            SyncIconsFromLocations(IconsFolders);

        private void OnIconsDirectoryRenamed(object sender, RenamedEventArgs e) =>
            SyncIconsFromLocations(IconsFolders);

        private void OnShortcutsDirectoryChanged(object sender, FileSystemEventArgs e) =>
            SyncShortcutsFromLocations(ShortcutsFolders);

        private void OnShortcutsDirectoryRenamed(object sender, RenamedEventArgs e) =>
            SyncShortcutsFromLocations(ShortcutsFolders);

        private void OnFoldersDirectoryChanged(object sender, FileSystemEventArgs e) =>
            SyncFoldersFromLocations(FoldersFolders);

        private void OnFoldersDirectoryRenamed(object sender, RenamedEventArgs e) =>
            SyncFoldersFromLocations(FoldersFolders);

        private void LoadPreviousShortcuts()
        {
            _loggingService.LogInfo("Loading previous applications from saved location (async)");

            var appsList = _settingsService.Settings.Application.ShortcutLocations;

            if (appsList.Any())
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ShortcutsFolders = new ObservableCollection<string>(appsList);
                    PopulateShortcutsFromLocations(ShortcutsFolders);
                    SetupShortcutsDirectoryWatcher();
                });
            }
        }

        private void LoadPreviousIcons()
        {
            _loggingService.LogInfo("Loading previous icons from saved location (async)");

            var iconsList = _settingsService.Settings.Application.IconLocations;

            if (iconsList.Any())
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    IconsFolders = new ObservableCollection<string>(iconsList);
                    PopulateIconsFromLocations(IconsFolders);
                    SetupIconsDirectoryWatcher();
                });
            }
        }

        private void LoadPreviousFolders()
        {
            _loggingService.LogInfo("Loading previous folders from saved location (async)");

            var foldersList = _settingsService.Settings.Application.FolderShortcutLocations;

            if (foldersList.Any())
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    FoldersFolders = new ObservableCollection<string>(foldersList);
                    PopulateFoldersFromLocations(FoldersFolders);
                    SetupFoldersDirectoryWatcher();
                });
            }
        }
    }
}