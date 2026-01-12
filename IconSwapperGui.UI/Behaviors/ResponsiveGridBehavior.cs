using System.Windows;
using System.Windows.Controls;

namespace IconSwapperGui.UI.Behaviors;

public static class ResponsiveGridBehavior
{
    // Attached property to enable behavior
    public static readonly DependencyProperty AttachProperty = DependencyProperty.RegisterAttached(
        "Attach",
        typeof(bool),
        typeof(ResponsiveGridBehavior),
        new PropertyMetadata(false, OnAttachChanged));

    public static void SetAttach(DependencyObject element, bool value)
    {
        element.SetValue(AttachProperty, value);
    }

    public static bool GetAttach(DependencyObject element)
    {
        return (bool)element.GetValue(AttachProperty);
    }

    private static void OnAttachChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Grid grid)
            return;

        if (e.NewValue is bool attach && attach)
        {
            grid.SizeChanged += Grid_SizeChanged;
            // store original columns in Tag so we can restore later
            if (grid.Tag == null)
            {
                var cols = new ColumnDefinition[grid.ColumnDefinitions.Count];
                for (var i = 0; i < cols.Length; i++)
                {
                    var c = grid.ColumnDefinitions[i];
                    cols[i] = new ColumnDefinition { Width = c.Width, MinWidth = c.MinWidth, MaxWidth = c.MaxWidth };
                }

                grid.Tag = cols;
            }
        }
        else
        {
            grid.SizeChanged -= Grid_SizeChanged;
            if (grid.Tag is ColumnDefinition[] original)
            {
                grid.ColumnDefinitions.Clear();
                foreach (var c in original)
                    grid.ColumnDefinitions.Add(new ColumnDefinition
                        { Width = c.Width, MinWidth = c.MinWidth, MaxWidth = c.MaxWidth });
            }
        }
    }

    private static void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is not Grid grid)
            return;

        if (e.NewSize.Width < 600)
        {
            if (grid.ColumnDefinitions.Count != 1)
            {
                grid.ColumnDefinitions.Clear();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
        }
        else
        {
            if (grid.Tag is ColumnDefinition[] original && grid.ColumnDefinitions.Count != original.Length)
            {
                grid.ColumnDefinitions.Clear();
                foreach (var c in original)
                    grid.ColumnDefinitions.Add(new ColumnDefinition
                        { Width = c.Width, MinWidth = c.MinWidth, MaxWidth = c.MaxWidth });
            }
        }
    }
}