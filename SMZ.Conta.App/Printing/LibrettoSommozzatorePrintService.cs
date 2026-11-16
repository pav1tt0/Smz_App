using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.Printing;

public sealed class LibrettoSommozzatorePrintService
{
    public void Print(Personale persona, IReadOnlyList<RegistroImmersioneRiga> immersioni)
    {
        var dialog = new PrintDialog { PrintTicket = { PageOrientation = PageOrientation.Portrait,
            PageMediaSize = new PageMediaSize(PageMediaSizeName.ISOA4) } };
        if (dialog.ShowDialog() == true)
            dialog.PrintDocument(BuildPaginator(persona, immersioni), $"Libretto sommozzatore - {persona.NominativoCompleto}");
    }

    public DocumentPaginator BuildPaginator(Personale persona, IReadOnlyList<RegistroImmersioneRiga> immersioni)
    {
        FlowDocument Sheet() => new() { FontFamily = PrintTheme.DocumentFont, FontSize = 11,
            PagePadding = new Thickness(40), Foreground = PrintTheme.TextBrush };
        var sheets = new List<FlowDocument>();
        var cover = Sheet();
        cover.Blocks.Add(PrintTheme.RepublicEmblem(56));
        cover.Blocks.Add(Text("LIBRETTO DEL SOMMOZZATORE", 20));
        cover.Blocks.Add(Text("Bozza - impaginazione da definire sul modello cartaceo", 11));
        cover.Blocks.Add(Text($"{persona.Qualifica}\n{persona.NominativoCompleto}", 17));
        cover.Blocks.Add(Text($"PerID: {persona.PerId}\nMatricola: {persona.MatricolaPersonale}\nBrevetto SMZ: {persona.NumeroBrevettoSmz}\nData di nascita: {persona.DataNascita:dd/MM/yyyy}\nLuogo di nascita: {persona.LuogoNascita}", 12));
        cover.Blocks.Add(Text($"Immersioni registrate: {immersioni.Select(i => i.ServizioImmersioneId).Distinct().Count()}\nOre complessive: {immersioni.Sum(i => i.OreImmersione):0.##}", 13));
        sheets.Add(cover);
        var ordered = immersioni.OrderBy(i => i.DataServizio).ThenBy(i => i.ServizioGiornalieroId).ThenBy(i => i.NumeroImmersione).ToList();
        var progressivo = 0;
        foreach (var chunk in ordered.GroupBy(i => i.ServizioImmersioneId).Chunk(12))
        {
            var sheet = Sheet();
            sheet.Blocks.Add(Text($"IMMERSIONI - {persona.NominativoCompleto}", 15));
            var table = new Table { CellSpacing = 0 };
            double[] widths = [.6, 1.1, .8, 2, 2.4, 1.5, .7, .7];
            foreach (var w in widths)
                table.Columns.Add(new TableColumn { Width = new GridLength(w / widths.Sum() * (SingleSheetDocumentPaginator.A4.Width - 80)) });
            table.RowGroups.Add(new TableRowGroup());
            AddRow(table, true, "N.", "Data", "Serv./Imm.", "Località", "Scopo", "Apparato", "Prof.", "Ore");
            foreach (var dive in chunk)
            {
                progressivo++;
                foreach (var r in dive)
                    AddRow(table, false, progressivo.ToString(), r.DataServizioDescrizione,
                        $"{r.NumeroOrdineServizio}/{r.NumeroImmersione}", r.Localita, r.ScopoImmersione,
                        r.Apparato, r.ProfonditaDisplay, r.OreImmersioneDisplay);
            }
            sheet.Blocks.Add(table);
            sheet.Blocks.Add(Text($"Pagina {sheets.Count} · Bozza", 10));
            sheets.Add(sheet);
        }
        return new SingleSheetDocumentPaginator(sheets, SingleSheetDocumentPaginator.A4);
    }

    private static Paragraph Text(string text, double size) => new(new Run(text))
    { FontSize = size, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 12, 0, 14) };

    private static void AddRow(Table table, bool header, params string[] cells)
    {
        var row = new TableRow();
        foreach (var text in cells)
            row.Cells.Add(new TableCell(new Paragraph(new Run(text)) { Margin = new Thickness(0) })
            { BorderBrush = PrintTheme.BorderBrush, BorderThickness = new Thickness(.5),
                Padding = new Thickness(4), FontWeight = header ? FontWeights.Bold : FontWeights.Normal,
                Background = header ? PrintTheme.HeaderBackground : null });
        table.RowGroups[0].Rows.Add(row);
    }
}
