using System.Windows;
using System.Windows.Controls.Primitives;

namespace SMZ.Conta.App.Infrastructure;

public sealed class AdaptiveUniformGrid : UniformGrid
{
    public double MinItemWidth { get; set; } = 240;
    public int MaxColumns { get; set; } = 3;

    protected override Size MeasureOverride(Size constraint)
    {
        var columns = double.IsFinite(constraint.Width)
            ? Math.Clamp((int)(constraint.Width / Math.Max(1, MinItemWidth)), 1, Math.Max(1, MaxColumns))
            : Math.Max(1, MaxColumns);
        if (Columns != columns) SetCurrentValue(ColumnsProperty, columns);
        return base.MeasureOverride(constraint);
    }
}
