using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using SMZ.Conta.App.Data;
using SMZ.Conta.App.Models;
using SMZ.Conta.App.ViewModels;

namespace SMZ.Conta.App.Printing;

public sealed class ServizioGiornalieroPrintService
{
    private readonly PersonaleRepository _repository;

    public ServizioGiornalieroPrintService(PersonaleRepository repository)
    {
        _repository = repository;
    }

    public void Print(ServizioGiornaliero servizio)
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var document = BuildDocument(servizio);
        document.PageWidth = dialog.PrintableAreaWidth;
        document.PageHeight = dialog.PrintableAreaHeight;
        document.PagePadding = new Thickness(30);
        document.ColumnWidth = document.PageWidth - document.PagePadding.Left - document.PagePadding.Right;
        dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, "Foglio servizio SMZ");
    }

    private FlowDocument BuildDocument(ServizioGiornaliero servizio)
    {
        var cataloghi = _repository.GetCataloghiServizio();
        var persone = GetPersone(servizio);
        var localita = cataloghi.LocalitaOperative.FirstOrDefault(item => item.LocalitaOperativaId == servizio.LocalitaOperativaId)?.Descrizione ?? string.Empty;
        var scopo = cataloghi.ScopiImmersione.FirstOrDefault(item => item.ScopoImmersioneId == servizio.ScopoImmersioneId)?.Descrizione ?? string.Empty;
        var unita = cataloghi.UnitaNavali.FirstOrDefault(item => item.UnitaNavaleId == servizio.UnitaNavaleId)?.Descrizione ?? string.Empty;
        var responsabile = servizio.ResponsabileServizioPerId is { } responsabileId && persone.TryGetValue(responsabileId, out var responsabilePersonale)
            ? FormatPersona(responsabilePersonale)
            : string.Empty;

        var document = new FlowDocument
        {
            FontFamily = PrintTheme.DocumentFont,
            FontSize = 9.5,
            PagePadding = new Thickness(30),
            Foreground = PrintTheme.TextBrush,
        };

        document.Blocks.Add(PrintTheme.RepublicEmblem(44));
        AddCentered(document, "POLIZIA DI STATO", 15, FontWeights.Bold);
        AddCentered(document, "CENTRO NAUTICO E SOMMOZZATORI", 12, FontWeights.Bold);
        AddCentered(document, "Nucleo Sommozzatori - La Spezia", 10.5, FontWeights.Normal);
        document.Blocks.Add(CreateDivider());
        document.Blocks.Add(CreateRecipientBlock());

        AddLabelParagraph(document, "OGGETTO", $"Relazione sul servizio n. {servizio.NumeroOrdineServizio} del {servizio.DataServizio:dd/MM/yyyy}");
        document.Blocks.Add(BuildDatiServizioTable(servizio, localita, scopo, unita, responsabile));

        document.Blocks.Add(PrintTheme.SectionTitle("Attività svolta ed eventuali variazioni di servizio"));
        document.Blocks.Add(new Paragraph(new Run(EmptyDash(servizio.AttivitaSvolta)))
        {
            Margin = new Thickness(3, 0, 3, 8),
            TextAlignment = TextAlignment.Justify,
        });

        if (servizio.Immersioni.Count > 0)
        {
            document.Blocks.Add(PrintTheme.SectionTitle("Riepilogo immersioni"));
            document.Blocks.Add(BuildImmersioniTable(servizio, cataloghi));
        }

        document.Blocks.Add(PrintTheme.SectionTitle("Personale impiegato"));
        document.Blocks.Add(BuildPersonaleTable(servizio, persone, cataloghi));

        document.Blocks.Add(PrintTheme.SectionTitle("Trattamento economico accessorio", new Thickness(0, 12, 0, 6)));
        document.Blocks.Add(BuildRiepilogoTable(servizio));

        if (!string.IsNullOrWhiteSpace(servizio.Note))
        {
            AddLabelParagraph(document, "Note", servizio.Note);
        }

        var firma = new Paragraph
        {
            TextAlignment = TextAlignment.Right,
            Margin = new Thickness(0, 34, 0, 0),
        };
        firma.Inlines.Add(new Run("Il Responsabile del Team"));
        firma.Inlines.Add(new LineBreak());
        firma.Inlines.Add(new Run(responsabile));
        firma.Inlines.Add(new LineBreak());
        firma.Inlines.Add(new Run("____________________________"));
        document.Blocks.Add(firma);

        return document;
    }

    private static Table BuildDatiServizioTable(
        ServizioGiornaliero servizio,
        string localita,
        string scopo,
        string unita,
        string responsabile)
    {
        var table = CreateTable(95, 220, 95, 220);
        AddInfoRow(table, "Data", servizio.DataServizio.ToString("dd/MM/yyyy"), "Orario", EmptyDash(servizio.OrarioServizio));
        AddInfoRow(table, "Località", EmptyDash(localita), "Tipo servizio", servizio.FuoriSede ? "Fuori sede" : "In sede");
        AddInfoRow(table, "Scopo", EmptyDash(scopo), "Unità navale", EmptyDash(unita));
        AddInfoRow(table, "Responsabile", EmptyDash(responsabile), "Ordine pubblico", servizio.IndennitaOrdinePubblico ? "SÌ" : "NO");
        return table;
    }

    private static Table BuildImmersioniTable(ServizioGiornaliero servizio, CataloghiServizioSnapshot cataloghi)
    {
        var table = CreateTable(38, 75, 62, 135, 135, 170);
        AddHeaderRow(table, "N.", "Orario", "Prof. max", "Località", "Scopo", "Note");
        foreach (var immersione in servizio.Immersioni.OrderBy(item => item.NumeroImmersione))
        {
            var localita = cataloghi.LocalitaOperative.FirstOrDefault(item => item.LocalitaOperativaId == immersione.LocalitaOperativaId)?.Descrizione ?? string.Empty;
            var scopo = cataloghi.ScopiImmersione.FirstOrDefault(item => item.ScopoImmersioneId == immersione.ScopoImmersioneId)?.Descrizione ?? string.Empty;
            var profondita = immersione.Partecipazioni.Select(item => item.ProfonditaMetri)
                .Concat(immersione.PartecipazioniEsterne.Select(item => item.ProfonditaMetri))
                .Where(item => item.HasValue)
                .Select(item => item!.Value)
                .DefaultIfEmpty()
                .Max();
            var orario = $"{immersione.OrarioInizio?.ToString("HH:mm") ?? "--:--"} - {immersione.OrarioFine?.ToString("HH:mm") ?? "--:--"}";
            AddRow(table,
                immersione.NumeroImmersione.ToString(CultureInfo.CurrentCulture),
                orario,
                profondita > 0 ? $"{profondita} m" : string.Empty,
                EmptyDash(localita),
                EmptyDash(scopo),
                immersione.Note);
        }

        return table;
    }

    private Table BuildPersonaleTable(
        ServizioGiornaliero servizio,
        IReadOnlyDictionary<int, Personale> persone,
        CataloghiServizioSnapshot cataloghi)
    {
        var table = CreateTable(120, 210, 170, 140, 70);
        AddHeaderRow(table, "Qualifica", "Cognome e nome", "Mansione", "Apparecchiatura", "Ore imm.");

        foreach (var partecipante in servizio.Partecipanti.Where(item => item.Presente).OrderBy(item => GetPersonOrder(item.PerId, persone)))
        {
            if (!persone.TryGetValue(partecipante.PerId, out var persona))
            {
                continue;
            }

            var immersioni = servizio.Immersioni
                .SelectMany(immersione => immersione.Partecipazioni.Select(partecipazione => (immersione, partecipazione)))
                .Where(item => ResolvePerId(item.partecipazione, servizio) == partecipante.PerId)
                .ToList();
            var apparato = immersioni
                .Select(item => cataloghi.TipologieImmersione.FirstOrDefault(tipo => tipo.TipologiaImmersioneOperativaId == item.partecipazione.TipologiaImmersioneOperativaId)?.Descrizione)
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
            var ore = immersioni.Sum(item => item.partecipazione.OreImmersione ?? 0m);

            AddRow(
                table,
                QualificaFormatter.AbbreviaPerVisualizzazione(persona.Qualifica),
                persona.NominativoCompleto,
                BuildMansione(servizio, partecipante.PerId),
                apparato,
                ore > 0 ? ore.ToString("0.##", CultureInfo.CurrentCulture) : string.Empty);
        }

        foreach (var operatore in servizio.OperatoriSubEsterni.OrderBy(item => item.Nominativo))
        {
            var immersioni = servizio.Immersioni
                .SelectMany(immersione => immersione.PartecipazioniEsterne.Select(partecipazione => (immersione, partecipazione)))
                .Where(item => item.partecipazione.ServizioOperatoreSubEsternoId == operatore.ServizioOperatoreSubEsternoId)
                .ToList();
            var apparato = immersioni
                .Select(item => cataloghi.TipologieImmersione.FirstOrDefault(tipo => tipo.TipologiaImmersioneOperativaId == item.partecipazione.TipologiaImmersioneOperativaId)?.Descrizione)
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
            var ore = immersioni.Sum(item => item.partecipazione.OreImmersione ?? 0m);

            AddRow(
                table,
                QualificaFormatter.AbbreviaPerVisualizzazione(operatore.Qualifica),
                $"{operatore.Nominativo} ({operatore.Reparto})",
                BuildMansioneEsterna(servizio, operatore.ServizioOperatoreSubEsternoId),
                apparato,
                ore > 0 ? ore.ToString("0.##", CultureInfo.CurrentCulture) : string.Empty);
        }

        return table;
    }

    private static Table BuildRiepilogoTable(ServizioGiornaliero servizio)
    {
        var table = CreateTable(120, 100, 100, 140, 100, 100);
        AddHeaderRow(table, "Straordinario", "Presenze", "Ore imm.", "Fuori sede", "C.I.", "Ord. Pub.");
        var oreImmersione = servizio.Immersioni
            .Sum(item =>
                item.Partecipazioni.Sum(partecipazione => partecipazione.OreImmersione ?? 0m)
                + item.PartecipazioniEsterne.Sum(partecipazione => partecipazione.OreImmersione ?? 0m));
        AddRow(
            table,
            servizio.StraordinarioAttivo ? $"{servizio.StraordinarioInizio}-{servizio.StraordinarioFine}" : string.Empty,
            (servizio.Partecipanti.Count(item => item.Presente) + servizio.OperatoriSubEsterni.Count).ToString(CultureInfo.CurrentCulture),
            oreImmersione > 0 ? oreImmersione.ToString("0.##", CultureInfo.CurrentCulture) : string.Empty,
            servizio.FuoriSede ? "SI" : string.Empty,
            string.Empty,
            servizio.IndennitaOrdinePubblico ? "SI" : string.Empty);
        return table;
    }

    private Dictionary<int, Personale> GetPersone(ServizioGiornaliero servizio)
    {
        var perIds = servizio.Partecipanti.Select(item => item.PerId)
            .Concat(servizio.Immersioni.SelectMany(item => new[]
            {
                item.DirettoreImmersionePerId,
                item.OperatoreSoccorsoPerId,
                item.AssistenteBlsdPerId,
                item.AssistenteSanitarioPerId,
            }).Where(item => item is not null).Select(item => item!.Value))
            .Append(servizio.ResponsabileServizioPerId ?? 0)
            .Where(item => item > 0)
            .Distinct();

        var result = new Dictionary<int, Personale>();
        foreach (var perId in perIds)
        {
            if (_repository.GetPersonaleById(perId) is { } persona)
            {
                result[perId] = persona;
            }
        }

        return result;
    }

    private static string BuildMansione(ServizioGiornaliero servizio, int perId)
    {
        var ruoli = new List<string>();
        foreach (var immersione in servizio.Immersioni.OrderBy(item => item.NumeroImmersione))
        {
            AddRole(ruoli, immersione.NumeroImmersione, "Direttore", immersione.DirettoreImmersionePerId, perId);
            AddRole(ruoli, immersione.NumeroImmersione, "Soccorso", immersione.OperatoreSoccorsoPerId, perId);
            AddRole(ruoli, immersione.NumeroImmersione, "BLSD", immersione.AssistenteBlsdPerId, perId);
            AddRole(ruoli, immersione.NumeroImmersione, "Sanitario", immersione.AssistenteSanitarioPerId, perId);
        }

        var immersioniEffettuate = servizio.Immersioni
            .Where(immersione => immersione.Partecipazioni.Any(partecipazione => ResolvePerId(partecipazione, servizio) == perId))
            .Select(immersione => $"Imm. {immersione.NumeroImmersione}");
        ruoli.AddRange(immersioniEffettuate);
        return ruoli.Count == 0 ? "Presente" : string.Join(", ", ruoli);
    }

    private static string BuildMansioneEsterna(ServizioGiornaliero servizio, long servizioOperatoreSubEsternoId)
    {
        var immersioniEffettuate = servizio.Immersioni
            .Where(immersione => immersione.PartecipazioniEsterne.Any(partecipazione => partecipazione.ServizioOperatoreSubEsternoId == servizioOperatoreSubEsternoId))
            .Select(immersione => $"Imm. {immersione.NumeroImmersione}");
        var result = string.Join(", ", immersioniEffettuate);
        return string.IsNullOrWhiteSpace(result) ? "Presente" : result;
    }

    private static int ResolvePerId(ServizioPartecipanteImmersione partecipazione, ServizioGiornaliero servizio)
    {
        var partecipante = servizio.Partecipanti.FirstOrDefault(item => item.ServizioPartecipanteId == partecipazione.ServizioPartecipanteId);
        return partecipante?.PerId ?? (int)partecipazione.ServizioPartecipanteId;
    }

    private static void AddRole(ICollection<string> ruoli, int numeroImmersione, string ruolo, int? rolePerId, int perId)
    {
        if (rolePerId == perId)
        {
            ruoli.Add($"Imm. {numeroImmersione}: {ruolo}");
        }
    }

    private static long GetPersonOrder(int perId, IReadOnlyDictionary<int, Personale> persone)
    {
        if (!persone.TryGetValue(perId, out var persona))
        {
            return long.MaxValue;
        }

        return QualificaFormatter.GetGerarchiaOrdine(persona.Qualifica, persona.IsProfiloSanitario, persona.RuoloSanitario) * 1_000_000L
            + GetDateOrder(persona.DataDecorrenzaQualifica);
    }

    private static int GetDateOrder(DateOnly? date) => date?.DayNumber ?? int.MaxValue;

    private static string FormatPersona(Personale persona)
    {
        var qualifica = QualificaFormatter.AbbreviaPerVisualizzazione(persona.Qualifica);
        return string.IsNullOrWhiteSpace(qualifica)
            ? persona.NominativoCompleto
            : $"{qualifica} {persona.NominativoCompleto}";
    }

    private static void AddCentered(FlowDocument document, string text, double fontSize, FontWeight fontWeight)
    {
        document.Blocks.Add(new Paragraph(new Run(text))
        {
            TextAlignment = TextAlignment.Center,
            FontSize = fontSize,
            FontWeight = fontWeight,
            Margin = new Thickness(0, 0, 0, 2),
        });
    }

    private static BlockUIContainer CreateDivider() =>
        new(new Border
        {
            Height = 1,
            Background = PrintTheme.BorderBrush,
            Margin = new Thickness(0, 8, 0, 8),
        });

    private static BlockUIContainer CreateRecipientBlock()
    {
        var panel = new StackPanel
        {
            Width = 330,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 18, 45, 14),
        };
        panel.Children.Add(new TextBlock
        {
            Text = "AL SIGNOR DIRETTORE CENTRO NAUTICO E SOMMOZZATORI",
            TextWrapping = TextWrapping.Wrap,
        });
        panel.Children.Add(new TextBlock
        {
            Text = "SEDE",
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 3, 0, 0),
        });

        return new BlockUIContainer(panel);
    }

    private static void AddLabelParagraph(FlowDocument document, string label, string value)
    {
        var paragraph = new Paragraph { Margin = new Thickness(0, 4, 0, 4) };
        paragraph.Inlines.Add(new Run($"{label}: ") { FontWeight = FontWeights.Bold });
        paragraph.Inlines.Add(new Run(value));
        document.Blocks.Add(paragraph);
    }

    private static Table CreateTable(params double[] widths)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 8) };
        foreach (var width in widths)
        {
            table.Columns.Add(new TableColumn { Width = new GridLength(width) });
        }

        table.RowGroups.Add(new TableRowGroup());
        return table;
    }

    private static void AddHeaderRow(Table table, params string[] values)
    {
        var row = new TableRow { FontWeight = FontWeights.Bold };
        var rowIndex = table.RowGroups[0].Rows.Count;
        for (var columnIndex = 0; columnIndex < values.Length; columnIndex++)
        {
            row.Cells.Add(CreateCell(values[columnIndex], true, rowIndex, columnIndex));
        }

        table.RowGroups[0].Rows.Add(row);
    }

    private static void AddInfoRow(Table table, string label1, string value1, string label2, string value2)
    {
        var row = new TableRow();
        var rowIndex = table.RowGroups[0].Rows.Count;
        row.Cells.Add(CreateCell(label1, true, rowIndex, 0));
        row.Cells.Add(CreateCell(value1, false, rowIndex, 1));
        row.Cells.Add(CreateCell(label2, true, rowIndex, 2));
        row.Cells.Add(CreateCell(value2, false, rowIndex, 3));
        table.RowGroups[0].Rows.Add(row);
    }

    private static void AddRow(Table table, params string[] values)
    {
        var row = new TableRow();
        var rowIndex = table.RowGroups[0].Rows.Count;
        for (var columnIndex = 0; columnIndex < values.Length; columnIndex++)
        {
            row.Cells.Add(CreateCell(values[columnIndex], false, rowIndex, columnIndex));
        }

        table.RowGroups[0].Rows.Add(row);
    }

    private static TableCell CreateCell(string value, bool header, int rowIndex, int columnIndex) =>
        new(new Paragraph(new Run(value))
        {
            Margin = new Thickness(2),
            TextAlignment = header ? TextAlignment.Center : GetCellAlignment(columnIndex),
        })
        {
            BorderBrush = PrintTheme.BorderBrush,
            BorderThickness = new Thickness(PrintTheme.BorderThickness),
            Padding = new Thickness(4, 3, 4, 3),
            Background = header
                ? PrintTheme.HeaderBackground
                : rowIndex % 2 == 0 ? PrintTheme.AlternateRowBackground : Brushes.Transparent,
        };

    private static TextAlignment GetCellAlignment(int columnIndex) =>
        columnIndex >= 4 ? TextAlignment.Right : TextAlignment.Left;

    private static string EmptyDash(string value) => string.IsNullOrWhiteSpace(value) ? "________________" : value.Trim();
}
