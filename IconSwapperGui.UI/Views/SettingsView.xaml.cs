using System.Windows;
using System.Windows.Controls;
using IconSwapperGui.UI.ViewModels;

namespace IconSwapperGui.UI.Views;

/// <summary>
///     Interaction logic for SettingsView.xaml
/// </summary>
public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }
    
    private void AccentColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.IsAccentColorPickerOpen = true;
        }
    }
    
    private void AccentColorPicker_ColorPicked(object? sender, EventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.IsAccentColorPickerOpen = false;
        }
    }
    
    private void BackgroundColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.IsBackgroundColorPickerOpen = true;
        }
    }
    
    private void BackgroundColorPicker_ColorPicked(object? sender, EventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.IsBackgroundColorPickerOpen = false;
        }
    }
    
    private void SurfaceColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.IsSurfaceColorPickerOpen = true;
        }
    }
    
    private void SurfaceColorPicker_ColorPicked(object? sender, EventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.IsSurfaceColorPickerOpen = false;
        }
    }
    
    private void PrimaryTextColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.IsPrimaryTextColorPickerOpen = true;
        }
    }
    
    private void PrimaryTextColorPicker_ColorPicked(object? sender, EventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.IsPrimaryTextColorPickerOpen = false;
        }
    }
    
    private void SecondaryTextColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.IsSecondaryTextColorPickerOpen = true;
        }
    }
    
    private void SecondaryTextColorPicker_ColorPicked(object? sender, EventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.IsSecondaryTextColorPickerOpen = false;
        }
    }
}