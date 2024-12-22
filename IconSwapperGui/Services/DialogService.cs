﻿using System.Windows;
using IconSwapperGui.Services.Interfaces;

namespace IconSwapperGui.Services;

public class DialogService : IDialogService
{
    public void ShowError(string message, string caption)
    {
        MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Error);
    }


    public void ShowWarning(string message, string caption)
    {
        MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Warning);
    }


    public void ShowInformation(string message, string caption)
    {
        MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Information);
    }
}