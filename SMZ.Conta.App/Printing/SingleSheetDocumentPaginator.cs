using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace SMZ.Conta.App.Printing;

// Each supplied document is one complete sheet. Oversized sheets are fitted
// proportionally, preserving every row and note instead of spilling or clipping.
public sealed class SingleSheetDocumentPaginator : DocumentPaginator
{
    public static readonly Size A4 = new(210 / 25.4 * 96, 297 / 25.4 * 96);
    private readonly List<(DocumentPaginator Paginator, Size Size, Thickness Padding)> _sheets = [];
    private Size _pageSize;

    public SingleSheetDocumentPaginator(IEnumerable<FlowDocument> documents, Size pageSize)
    {
        _pageSize = pageSize;
        foreach (var document in documents)
        {
            document.PageWidth = pageSize.Width;
            document.ColumnWidth = pageSize.Width - document.PagePadding.Left - document.PagePadding.Right;
            document.PageHeight = pageSize.Height;
            var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
            paginator.ComputePageCount();
            if (paginator.PageCount > 1)
            {
                var low = pageSize.Height;
                var high = low;
                do
                {
                    high *= 2;
                    if (high > 1_000_000) throw new InvalidOperationException("Scheda troppo estesa per la stampa.");
                    document.PageHeight = high;
                    paginator.ComputePageCount();
                } while (paginator.PageCount > 1);
                for (var i = 0; i < 14; i++)
                {
                    var middle = (low + high) / 2;
                    document.PageHeight = middle;
                    paginator.ComputePageCount();
                    if (paginator.PageCount > 1) low = middle; else high = middle;
                }
                document.PageHeight = high + 2;
                paginator.ComputePageCount();
            }
            _sheets.Add((paginator, new Size(document.PageWidth, document.PageHeight), document.PagePadding));
        }
    }

    public override bool IsPageCountValid => true;
    public override int PageCount => _sheets.Count;
    public override Size PageSize
    {
        get => _pageSize;
        set
        {
            if (value != _pageSize) throw new InvalidOperationException("Ricrea il documento per cambiare formato pagina.");
        }
    }
    public override IDocumentPaginatorSource Source => null!;

    public override DocumentPage GetPage(int pageNumber)
    {
        if (pageNumber < 0 || pageNumber >= _sheets.Count) return DocumentPage.Missing;
        var sheet = _sheets[pageNumber];
        var source = sheet.Paginator.GetPage(0);
        var verticalPadding = sheet.Padding.Top + sheet.Padding.Bottom;
        var scale = Math.Min(1, (_pageSize.Height - verticalPadding) / (sheet.Size.Height - verticalPadding));
        var size = new Size(sheet.Size.Width * scale, sheet.Size.Height * scale);
        var visual = new DrawingVisual();
        using (var drawing = visual.RenderOpen())
        {
            drawing.DrawRectangle(Brushes.White, null, new Rect(_pageSize));
            drawing.DrawRectangle(new VisualBrush(source.Visual) { Stretch = Stretch.Fill,
                ViewboxUnits = BrushMappingMode.Absolute, Viewbox = new Rect(sheet.Size) },
                null, new Rect(new Point((_pageSize.Width - size.Width) / 2, sheet.Padding.Top * (1 - scale)), size));
        }
        var box = new Rect(_pageSize);
        return new DocumentPage(visual, _pageSize, box, box);
    }
}
