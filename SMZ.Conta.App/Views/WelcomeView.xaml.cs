using System.Windows;
using System.Windows.Controls;

namespace SMZ.Conta.App.Views;

public partial class WelcomeView : UserControl
{
    public WelcomeView()
    {
        InitializeComponent();
        SizeChanged += (_, _) => UpdateLayoutForScreen();
    }

    private void UpdateLayoutForScreen()
    {
        var compact = ActualWidth < 1200 || ActualHeight < 600;
        BrandTitle.FontSize = compact ? 36 : 48;
        BrandTitle.LineHeight = compact ? 44 : 56;
        BrandSubtitle.FontSize = UnitTitle.FontSize = compact ? 22 : 28;
        WelcomeDescription.FontSize = compact ? 16 : 20;
        WelcomeDescription.LineHeight = compact ? 24 : 32;
        WelcomeDescription.Margin = new Thickness(0, compact ? 16 : 30, 0, 0);
        EnterButton.Margin = new Thickness(0, compact ? 20 : 48, 0, 0);
        DecorativeLogo.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
    }

    private void CloseAppButton_Click(object sender, RoutedEventArgs e)
    {
        Window.GetWindow(this)?.Close();
    }
}
