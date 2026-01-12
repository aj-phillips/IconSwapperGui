using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace IconSwapperGui.UI.Views.Components;

public partial class ToastNotificationView : UserControl
{
    public ToastNotificationView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ViewModels.ToastNotificationViewModel oldVm)
        {
            oldVm.PropertyChanged -= OnViewModelPropertyChanged;
        }

        if (e.NewValue is ViewModels.ToastNotificationViewModel newVm)
        {
            newVm.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModels.ToastNotificationViewModel.IsVisible))
        {
            if (sender is ViewModels.ToastNotificationViewModel vm)
            {
                if (vm.IsVisible)
                {
                    Visibility = Visibility.Visible;
                    var slideIn = (Storyboard)FindResource("SlideInAnimation");
                    slideIn.Begin(this);
                }
                else
                {
                    var slideOut = (Storyboard)FindResource("SlideOutAnimation");
                    slideOut.Completed += (s, args) => Visibility = Visibility.Collapsed;
                    slideOut.Begin(this);
                }
            }
        }
    }
}
