using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IconSwapperGui.Core.Interfaces;
using System;
using System.IO;

namespace IconSwapperGui.UI.ViewModels
{
    public partial class RenameIconViewModel : ObservableObject
    {
        private readonly IIconManagementService _iconManagementService;
        private readonly ILoggingService _loggingService;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string _newName;

        public event Action<bool>? RequestClose;

        public RenameIconViewModel(string currentName, IIconManagementService iconManagementService, ILoggingService loggingService)
        {
            ArgumentNullException.ThrowIfNull(currentName);
            ArgumentNullException.ThrowIfNull(iconManagementService);
            ArgumentNullException.ThrowIfNull(loggingService);

            _iconManagementService = iconManagementService;
            _loggingService = loggingService;

            var nameWithoutExtension = Path.GetFileNameWithoutExtension(currentName);
            _newName = nameWithoutExtension;
        }

        private bool CanSave() => !string.IsNullOrWhiteSpace(NewName);

        [RelayCommand(CanExecute = nameof(CanSave))]
        private void Save()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(NewName))
                {
                    _loggingService.LogWarning("Attempted to save with empty name");
                    return;
                }

                RequestClose?.Invoke(true);
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Error in RenameIconViewModel.Save", ex);
                RequestClose?.Invoke(false);
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            RequestClose?.Invoke(false);
        }
    }
}

