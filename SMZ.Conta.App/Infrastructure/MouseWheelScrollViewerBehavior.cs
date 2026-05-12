using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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

        var scrollViewer = FindServiceEditorScrollViewer(source);
        if (scrollViewer is null || !scrollViewer.IsVisible)
        {
            return;
        }

        scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
        e.Handled = true;
    }

    private static ScrollViewer? FindServiceEditorScrollViewer(DependencyObject source)
    {
        var current = VisualTreeHelper.GetParent(source);
        while (current is not null)
        {
            if (current is ScrollViewer { Name: "ServizioEditorScrollViewer" } scrollViewer)
            {
                return scrollViewer;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }
}
