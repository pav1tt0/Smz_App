using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace SMZ.Conta.App.Infrastructure;

/// <summary>Keeps dialogs within the work area of their current monitor, including mixed DPI setups.</summary>
public static class WindowFitBehavior
{
    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
        "Enabled", typeof(bool), typeof(WindowFitBehavior), new PropertyMetadata(false, OnEnabledChanged));
    public static bool GetEnabled(DependencyObject element) => (bool)element.GetValue(EnabledProperty);
    public static void SetEnabled(DependencyObject element, bool value) => element.SetValue(EnabledProperty, value);

    private static void OnEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is not Window window) return;
        if ((bool)args.NewValue)
        {
            window.SourceInitialized += OnInitialized;
            window.Loaded += OnLoaded;
            window.DpiChanged += OnDpiChanged;
        }
        else
        {
            window.SourceInitialized -= OnInitialized;
            window.Loaded -= OnLoaded;
            window.DpiChanged -= OnDpiChanged;
        }
    }

    private static void OnInitialized(object? sender, EventArgs args) => Fit((Window)sender!);
    private static void OnLoaded(object sender, RoutedEventArgs args) => Fit((Window)sender);
    private static void OnDpiChanged(object sender, DpiChangedEventArgs args) =>
        ((Window)sender).Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => Fit((Window)sender)));

    private static void Fit(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (handle == IntPtr.Zero || !GetMonitorInfo(MonitorFromWindow(handle, 2), ref info)) return;
        var dpi = VisualTreeHelper.GetDpi(window);
        var width = (info.Work.Right - info.Work.Left) / dpi.DpiScaleX;
        var height = (info.Work.Bottom - info.Work.Top) / dpi.DpiScaleY;
        window.SetCurrentValue(FrameworkElement.MinWidthProperty, Math.Min(window.MinWidth, width));
        window.SetCurrentValue(FrameworkElement.MinHeightProperty, Math.Min(window.MinHeight, height));
        if (window.WindowState != WindowState.Normal) return;
        if (window.Width > width) window.SetCurrentValue(FrameworkElement.WidthProperty, width);
        if (window.Height > height) window.SetCurrentValue(FrameworkElement.HeightProperty, height);
        if (GetWindowRect(handle, out var bounds))
        {
            var left = Math.Clamp(bounds.Left, info.Work.Left, Math.Max(info.Work.Left, info.Work.Right - (bounds.Right - bounds.Left)));
            var top = Math.Clamp(bounds.Top, info.Work.Top, Math.Max(info.Work.Top, info.Work.Bottom - (bounds.Bottom - bounds.Top)));
            if (left != bounds.Left || top != bounds.Top) SetWindowPos(handle, IntPtr.Zero, left, top, 0, 0, 0x0015);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo { public int Size; public NativeRect Monitor, Work; public uint Flags; }
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Auto)] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetWindowRect(IntPtr window, out NativeRect rect);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
}
