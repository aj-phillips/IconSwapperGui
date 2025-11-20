using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO;
using System.Windows;
using IconSwapperGui.Services;

namespace IconSwapperGui.Windows;

public partial class ExportDialogWindow : Window
{
    public string? FileName { get; private set; }
    public string? FolderPath { get; private set; }

    private readonly SettingsService _settingsService;

    public ExportDialogWindow(string defaultName = "Pixel_Art")
    {
        InitializeComponent();

        _settingsService = new SettingsService();

        NameTextBox.Text = defaultName;
        NameTextBox.SelectAll();
        NameTextBox.Focus();

        var saved = _settingsService.GetExportLocation();
        if (!string.IsNullOrWhiteSpace(saved) && Directory.Exists(saved))
        {
            FolderPath = saved;
            FolderText.Text = FolderPath;
        }
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new CommonOpenFileDialog { IsFolderPicker = true };
        if (dlg.ShowDialog() != CommonFileDialogResult.Ok) return;
        FolderPath = dlg.FileName;
        FolderText.Text = FolderPath;

        try
        {
            _settingsService.SaveExportLocation(FolderPath);
        }
        catch
        {
            // ignore save failures
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            MessageBox.Show("Please enter a file name", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(FolderPath) || !Directory.Exists(FolderPath))
        {
            MessageBox.Show("Please choose a valid export folder", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        FileName = NameTextBox.Text.Trim();

        try
        {
            _settingsService.SaveExportLocation(FolderPath);
        }
        catch
        {
            // ignore
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
