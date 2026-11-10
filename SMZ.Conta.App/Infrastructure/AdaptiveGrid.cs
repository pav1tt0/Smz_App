using System.Windows;
using System.Windows.Controls;

namespace SMZ.Conta.App.Infrastructure;

/// <summary>Reflows groups of fields without scaling their text or changing their bindings.</summary>
public sealed class AdaptiveGrid : Grid
{
    public double CollapseBelow { get; set; } = 760;
    public double MinItemWidth { get; set; } = 260;
    public int MaxColumns { get; set; } = 2;

    private ColumnDefinition[]? _columns;
    private RowDefinition[]? _rows;
    private Cell[]? _cells;
    private int _activeColumns = -1;

    protected override Size MeasureOverride(Size constraint)
    {
        var columns = double.IsFinite(constraint.Width) && constraint.Width < CollapseBelow
            ? Math.Clamp((int)((constraint.Width + 12) / (Math.Max(1, MinItemWidth) + 12)), 1, Math.Max(1, MaxColumns))
            : 0;
        if (columns != _activeColumns)
        {
            _columns ??= ColumnDefinitions.ToArray();
            _rows ??= RowDefinitions.ToArray();
            _cells ??= Children.Cast<UIElement>()
                .Select(child => new Cell(child, GetRow(child), GetColumn(child), GetRowSpan(child), GetColumnSpan(child), (child as FrameworkElement)?.Margin ?? default))
                .OrderBy(cell => cell.Row).ThenBy(cell => cell.Column).ToArray();
            ColumnDefinitions.Clear();
            RowDefinitions.Clear();
            if (columns == 0)
            {
                foreach (var column in _columns) ColumnDefinitions.Add(column);
                foreach (var row in _rows) RowDefinitions.Add(row);
                foreach (var cell in _cells) Place(cell, cell.Row, cell.Column, cell.RowSpan, cell.ColumnSpan, cell.Margin);
            }
            else
            {
                for (var column = 0; column < columns; column++) ColumnDefinitions.Add(new ColumnDefinition());
                var index = 0;
                foreach (var cell in _cells)
                {
                    var span = cell.ColumnSpan > 1 ? columns : 1;
                    if (span == columns && index % columns != 0) index += columns - index % columns;
                    var row = index / columns;
                    while (RowDefinitions.Count <= row) RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    var margin = cell.Margin;
                    margin.Bottom += 12;
                    if (index % columns < columns - 1 && span == 1) margin.Right += 12;
                    Place(cell, row, index % columns, 1, span, margin);
                    index += span;
                }
            }
            _activeColumns = columns;
        }
        return base.MeasureOverride(constraint);
    }

    private static void Place(Cell cell, int row, int column, int rowSpan, int columnSpan, Thickness margin)
    {
        cell.Element.SetCurrentValue(RowProperty, row);
        cell.Element.SetCurrentValue(ColumnProperty, column);
        cell.Element.SetCurrentValue(RowSpanProperty, rowSpan);
        cell.Element.SetCurrentValue(ColumnSpanProperty, columnSpan);
        if (cell.Element is FrameworkElement element) element.SetCurrentValue(MarginProperty, margin);
    }

    private sealed record Cell(UIElement Element, int Row, int Column, int RowSpan, int ColumnSpan, Thickness Margin);
}
