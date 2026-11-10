using System.Windows;
using System.Windows.Controls.Primitives;

namespace SMZ.Conta.App.Infrastructure;

// Legge l'elenco dall'alto in basso nella prima colonna, poi nella seconda.
public sealed class ColumnFirstUniformGrid : UniformGrid
{
    protected override Size ArrangeOverride(Size arrangeSize)
    {
        var children = InternalChildren.Cast<UIElement>()
            .Where(child => child.Visibility != Visibility.Collapsed).ToList();
        var columns = Math.Max(1, Columns);
        var rows = Math.Max(1, (int)Math.Ceiling(children.Count / (double)columns));
        var width = arrangeSize.Width / columns;
        var height = arrangeSize.Height / rows;
        for (var i = 0; i < children.Count; i++)
        {
            children[i].Arrange(new Rect(i / rows * width, i % rows * height, width, height));
        }

        return arrangeSize;
    }
}
