using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SMZ.Conta.App.Infrastructure;

public static class MouseWheelScrollViewerBehavior
{
    public static readonly DependencyProperty ForwardToParentProperty =
        DependencyProperty.RegisterAttached(
            "ForwardToParent",
            typeof(bool),
            typeof(MouseWheelScrollViewerBehavior),
            new PropertyMetadata(false, OnForwardToParentChanged));

    public static bool GetForwardToParent(DependencyObject obj) =>
        (bool)obj.GetValue(ForwardToParentProperty);

    public static void SetForwardToParent(DependencyObject obj, bool value) =>
        obj.SetValue(ForwardToParentProperty, value);

    private static void OnForwardToParentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            element.PreviewMouseWheel += ForwardMouseWheel;
            element.MouseWheel += ForwardMouseWheel;
            return;
        }

        element.PreviewMouseWheel -= ForwardMouseWheel;
        element.MouseWheel -= ForwardMouseWheel;
    }

    private static void ForwardMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not DependencyObject source)
        {
            return;
        }

        if (e.OriginalSource is DependencyObject originalSource && IsInsideOpenComboBox(originalSource))
        {
            return;
        }

        var scrollViewer = FindScrollableAncestor(source);
        if (scrollViewer is null || !scrollViewer.IsVisible)
        {
            return;
        }

        scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
        e.Handled = true;
    }

    private static ScrollViewer? FindScrollableAncestor(DependencyObject source)
    {
        var current = GetParent(source);
        ScrollViewer? fallbackScrollViewer = null;
        while (current is not null)
        {
            if (current is ScrollViewer scrollViewer)
            {
                fallbackScrollViewer ??= scrollViewer;

                if (scrollViewer.ScrollableHeight > 0)
                {
                    return scrollViewer;
                }
            }

            current = GetParent(current);
        }

        return fallbackScrollViewer;
    }

    private static bool IsInsideOpenComboBox(DependencyObject source)
    {
        var current = source;
        while (current is not null)
        {
            if (current is ComboBox { IsDropDownOpen: true })
            {
                return true;
            }

            current = GetParent(current);
        }

        return false;
    }

    private static DependencyObject? GetParent(DependencyObject source)
    {
        if (source is Visual or Visual3D)
        {
            return VisualTreeHelper.GetParent(source);
        }

        return source is FrameworkElement element ? element.Parent : null;
    }
}
