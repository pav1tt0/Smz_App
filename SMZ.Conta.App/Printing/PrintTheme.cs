using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SMZ.Conta.App.Printing;

internal static class PrintTheme
{
    public static readonly FontFamily DocumentFont = new("Calibri");
    public static readonly Brush BorderBrush = CreateBrush(83, 96, 112);
    public static readonly Brush HeaderBackground = CreateBrush(232, 238, 245);
    public static readonly Brush AlternateRowBackground = CreateBrush(248, 250, 252);
    public static readonly Brush SectionBackground = CreateBrush(241, 245, 249);
    public static readonly Brush TotalBackground = CreateBrush(224, 231, 240);
    public static readonly Brush TextBrush = Brushes.Black;

    public const double BorderThickness = 0.85;

    private static readonly Uri RepublicEmblemUri = new("pack://application:,,,/Assets/stemma-repubblica.png", UriKind.Absolute);

    public static Paragraph SectionTitle(string text, Thickness? margin = null) =>
        new(new Run(text))
        {
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Background = SectionBackground,
            Padding = new Thickness(5, 3, 5, 3),
            Margin = margin ?? new Thickness(0, 14, 0, 6),
        };

    public static BlockUIContainer RepublicEmblem(double height, Thickness? margin = null) =>
        new(new Image
        {
            Source = new BitmapImage(RepublicEmblemUri),
            Height = height,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
        })
        {
            Margin = margin ?? new Thickness(0, 0, 0, 6),
        };

    private static SolidColorBrush CreateBrush(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}
