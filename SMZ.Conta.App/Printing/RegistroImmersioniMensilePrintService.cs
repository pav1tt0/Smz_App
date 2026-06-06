using System.Globalization;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using SMZ.Conta.App.Data;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.Printing;

public sealed class RegistroImmersioniMensilePrintService
{
    private const double TableLineThickness = 0.8;
    private readonly PersonaleRepository _repository;

    public RegistroImmersioniMensilePrintService(PersonaleRepository repository)
    {
        _repository = repository;
    }

    public void Print(int anno, int mese, string meseDescrizione)
    {
        var righe = _repository.GetRegistroImmersioniMensile(anno, mese);
        var servizi = righe
            .Select(item => item.ServizioGiornalieroId)
            .Distinct()
            .Select(_repository.GetServizioGiornalieroById)
            .Where(item => item is not null)
            .Select(item => item!)
            .OrderBy(item => item.DataServizio)
            .ThenBy(item => item.NumeroOrdineServizio)
            .ToList();

        var dialog = new PrintDialog
        {
            PrintTicket = { PageOrientation = PageOrientation.Landscape },
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var document = BuildDocument(anno, meseDescrizione, servizi, righe);
        document.PageWidth = dialog.PrintableAreaWidth;
        document.PageHeight = dialog.PrintableAreaHeight;
        document.PagePadding = new Thickness(36);
        document.ColumnWidth = document.PageWidth - document.PagePadding.Left - document.PagePadding.Right;

        dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, $"Registro immersioni {meseDescrizione} {anno}");
    }

    private FixedDocument BuildFixedDocument(
        int anno,
        string meseDescrizione,
        IReadOnlyList<ServizioGiornaliero> servizi,
        IReadOnlyList<RegistroImmersioneRiga> righe,
        double pageWidth,
        double pageHeight)
    {
        var cataloghi = _repository.GetCataloghiServizio();
        var persone = GetPersone(servizi);
        var document = new FixedDocument();
        document.DocumentPaginator.PageSize = new Size(pageWidth, pageHeight);

        AddFixedPage(document, pageWidth, pageHeight, BuildCoverVisual(anno, meseDescrizione));

        foreach (var dayGroup in servizi.GroupBy(item => item.DataServizio).OrderBy(group => group.Key))
        {
            AddFixedPage(
                document,
                pageWidth,
                pageHeight,
                BuildDayVisual(dayGroup.Key, dayGroup.OrderBy(item => item.NumeroOrdineServizio), persone, cataloghi));
        }

        AddFixedPage(document, pageWidth, pageHeight, BuildSummaryVisual(anno, meseDescrizione, righe));
        return document;
    }

    private static void AddFixedPage(FixedDocument document, double pageWidth, double pageHeight, UIElement content)
    {
        const double margin = 36;
        var page = new FixedPage
        {
            Width = pageWidth,
            Height = pageHeight,
            Background = Brushes.White,
        };

        var viewbox = new Viewbox
        {
            Width = pageWidth - margin * 2,
            Height = pageHeight - margin * 2,
            Stretch = Stretch.Uniform,
            StretchDirection = StretchDirection.DownOnly,
            Child = content,
        };

        FixedPage.SetLeft(viewbox, margin);
        FixedPage.SetTop(viewbox, margin);
        page.Children.Add(viewbox);

        var pageContent = new PageContent
        {
            Child = page,
        };
        document.Pages.Add(pageContent);
    }

    private static Grid BuildCoverVisual(int anno, string meseDescrizione)
    {
        var grid = CreateFixedRoot();
        var stack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Top,
        };

        stack.Children.Add(CreateFixedText("POLIZIA DI STATO", 18, FontWeights.Bold, TextAlignment.Center));
        stack.Children.Add(CreateFixedText("Centro Nautico e Sommozzatori", 15, FontWeights.Bold, TextAlignment.Center));
        stack.Children.Add(CreateFixedText("Nucleo Sommozzatori", 15, FontWeights.Bold, TextAlignment.Center));
        stack.Children.Add(CreateFixedText("La Spezia", 13, FontWeights.Normal, TextAlignment.Center));
        stack.Children.Add(CreateFixedText("REGISTRO IMMERSIONI", 28, FontWeights.Bold, TextAlignment.Center, new Thickness(0, 120, 0, 0)));
        stack.Children.Add(CreateFixedText($"Mese di {meseDescrizione.ToUpper(CultureInfo.CurrentCulture)} Anno {anno}", 18, FontWeights.Bold, TextAlignment.Center, new Thickness(0, 18, 0, 0)));
        stack.Children.Add(CreateFixedText("Il RESPONSABILE\nNUCLEO SOMMOZZATORI\n____________________________", 14, FontWeights.Bold, TextAlignment.Center, new Thickness(620, 150, 0, 0)));

        grid.Children.Add(stack);
        return grid;
    }

    private Grid BuildDayVisual(
        DateOnly dataServizio,
        IEnumerable<ServizioGiornaliero> servizi,
        IReadOnlyDictionary<int, Personale> persone,
        CataloghiServizioSnapshot cataloghi)
    {
        var grid = CreateFixedRoot();
        var stack = new StackPanel();
        stack.Children.Add(CreateFixedText(dataServizio.ToString("dddd dd/MM/yyyy", CultureInfo.CurrentCulture).ToUpper(CultureInfo.CurrentCulture), 15, FontWeights.Bold, TextAlignment.Left, new Thickness(0, 0, 0, 10)));

        foreach (var servizio in servizi)
        {
            stack.Children.Add(BuildFixedServiceHeader(servizio, persone, cataloghi));
            stack.Children.Add(BuildFixedIndennita(servizio, cataloghi));
            stack.Children.Add(BuildFixedPersonale(servizio, persone, cataloghi));
            stack.Children.Add(new Border { Height = 8 });
        }

        grid.Children.Add(stack);
        return grid;
    }

    private static Grid BuildSummaryVisual(int anno, string meseDescrizione, IReadOnlyList<RegistroImmersioneRiga> righe)
    {
        var grid = CreateFixedRoot();
        var stack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Top,
        };

        stack.Children.Add(CreateFixedText("RIEPILOGO IMMERSIONI", 20, FontWeights.Bold, TextAlignment.Center, new Thickness(0, 20, 0, 20)));
        stack.Children.Add(BuildFixedTable(
            new[] { 250d, 250d, 250d, 250d },
            new[]
            {
                new[] { $"RIEPILOGO NUMERO IMMERSIONI EFFETTUATE NEL MESE DI {meseDescrizione.ToUpper(CultureInfo.CurrentCulture)} ANNO {anno}", "", "", "" },
                new[] { "IMMERSIONI ADDESTRATIVE A MARE ED IN BACINO DELIMITATO", "IMMERSIONI ORDINARIE", "IMMERSIONI PER SPERIMENTAZIONE ATTREZZATURE E MATERIALI SUBACQUEI", "IMMERSIONI IN CAMERA IPERBARICA" },
                new[]
                {
                    CountCategoria(righe, "ADDESTR").ToString(CultureInfo.CurrentCulture),
                    CountCategoria(righe, "ORDIN").ToString(CultureInfo.CurrentCulture),
                    CountCategoria(righe, "SPER").ToString(CultureInfo.CurrentCulture),
                    CountCategoria(righe, "CAMERA").ToString(CultureInfo.CurrentCulture),
                },
            },
            headerRows: 2));

        grid.Children.Add(stack);
        return grid;
    }

    private Grid BuildFixedServiceHeader(
        ServizioGiornaliero servizio,
        IReadOnlyDictionary<int, Personale> persone,
        CataloghiServizioSnapshot cataloghi)
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = BuildFixedTable(
            new[] { 70d, 90d, 100d, 300d, 440d },
            new[]
            {
                new[] { "GIORNO", "DATA", "ORARIO", "LOCALITA'", "SCOPO IMMERSIONE" },
                new[]
                {
                    CultureInfo.CurrentCulture.TextInfo.ToTitleCase(servizio.DataServizio.ToString("dddd", CultureInfo.CurrentCulture)),
                    servizio.DataServizio.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture),
                    servizio.OrarioServizio,
                    ResolveLocalita(servizio.LocalitaOperativaId, cataloghi),
                    ResolveScopo(servizio.ScopoImmersioneId, cataloghi),
                },
            },
            headerRows: 1);
        grid.Children.Add(header);

        var roleRows = new List<string[]>
        {
            new[] { "IMMERSIONE", "DIRETTORE IMMERSIONE", "OPERATORE SOCCORSO", "ASSISTENZA BLSD", "ASSIST. SANITARIA" },
        };
        foreach (var immersione in servizio.Immersioni.OrderBy(item => item.NumeroImmersione))
        {
            roleRows.Add(new[]
            {
                $"IMMERSIONE {immersione.NumeroImmersione} {FormatOrario(immersione)}",
                FormatPersona(immersione.DirettoreImmersionePerId, persone),
                FormatPersona(immersione.OperatoreSoccorsoPerId, persone),
                FormatPersona(immersione.AssistenteBlsdPerId, persone),
                FormatPersona(immersione.AssistenteSanitarioPerId, persone),
            });
        }

        var roles = BuildFixedTable(new[] { 120d, 220d, 220d, 220d, 220d }, roleRows, headerRows: 1);
        Grid.SetRow(roles, 1);
        grid.Children.Add(roles);
        return grid;
    }

    private static Grid BuildFixedIndennita(ServizioGiornaliero servizio, CataloghiServizioSnapshot cataloghi) =>
        BuildFixedTable(
            new[] { 210d, 300d, 410d, 80d },
            new[]
            {
                new[]
                {
                    "Servizio svolto a bordo dell'unita navale",
                    ResolveUnitaNavale(servizio.UnitaNavaleId, cataloghi),
                    "Indennita supp. di fuori sede art. 12 D.P.R. 57/2022",
                    servizio.FuoriSede ? "SI" : string.Empty,
                },
            },
            headerRows: 0);

    private Grid BuildFixedPersonale(
        ServizioGiornaliero servizio,
        IReadOnlyDictionary<int, Personale> persone,
        CataloghiServizioSnapshot cataloghi)
    {
        var rows = new List<string[]>
        {
            new[] { "QUAL.", "COGNOME E NOME", "PRESENZA", "TIPOLOGIA", "PROF. Mt.", "ORE IMM." },
        };

        var righe = servizio.Partecipanti
            .Where(item => item.Presente)
            .SelectMany(partecipante => BuildPartecipanteRows(servizio, partecipante, persone, cataloghi))
            .ToList();

        if (righe.Count == 0)
        {
            rows.Add(new[] { "", "Nessun personale SMZ presente", "", "", "", "" });
        }
        else
        {
            rows.AddRange(righe.Select(row => new[]
            {
                row.Qualifica,
                row.Nominativo,
                row.Presenza,
                row.Tipologia,
                row.Profondita,
                row.Ore,
            }));
        }

        return BuildFixedTable(new[] { 70d, 360d, 80d, 260d, 110d, 120d }, rows, headerRows: 1);
    }

    private static Grid CreateFixedRoot() =>
        new()
        {
            Width = 1000,
            MinHeight = 650,
            Background = Brushes.White,
        };

    private static TextBlock CreateFixedText(
        string text,
        double fontSize,
        FontWeight fontWeight,
        TextAlignment textAlignment,
        Thickness? margin = null) =>
        new()
        {
            Text = text,
            FontFamily = new FontFamily("Calibri"),
            FontSize = fontSize,
            FontWeight = fontWeight,
            TextAlignment = textAlignment,
            TextWrapping = TextWrapping.Wrap,
            Margin = margin ?? new Thickness(0, 0, 0, 4),
        };

    private static Grid BuildFixedTable(IReadOnlyList<double> widths, IReadOnlyList<string[]> rows, int headerRows)
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 4) };
        foreach (var width in widths)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(width) });
        }

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var values = rows[rowIndex];
            for (var columnIndex = 0; columnIndex < widths.Count; columnIndex++)
            {
                var cell = CreateFixedCell(columnIndex < values.Length ? values[columnIndex] : string.Empty, rowIndex < headerRows);
                Grid.SetRow(cell, rowIndex);
                Grid.SetColumn(cell, columnIndex);
                grid.Children.Add(cell);
            }
        }

        return grid;
    }

    private static Border CreateFixedCell(string value, bool header) =>
        new()
        {
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(0.6),
            Background = header ? Brushes.WhiteSmoke : Brushes.White,
            Padding = new Thickness(3),
            Child = new TextBlock
            {
                Text = value,
                FontFamily = new FontFamily("Calibri"),
                FontSize = 10,
                FontWeight = header ? FontWeights.Bold : FontWeights.Normal,
                TextAlignment = header ? TextAlignment.Center : TextAlignment.Left,
                TextWrapping = TextWrapping.Wrap,
            },
        };

    private FlowDocument BuildDocument(
        int anno,
        string meseDescrizione,
        IReadOnlyList<ServizioGiornaliero> servizi,
        IReadOnlyList<RegistroImmersioneRiga> righe)
    {
        var cataloghi = _repository.GetCataloghiServizio();
        var persone = GetPersone(servizi);
        var document = new FlowDocument
        {
            FontFamily = new FontFamily("Calibri"),
            FontSize = 8.5,
            PagePadding = new Thickness(36),
        };

        AddCoverPage(document, anno, meseDescrizione);

        foreach (var serviceGroup in servizi
                     .GroupBy(item => new { item.DataServizio, item.NumeroOrdineServizio })
                     .OrderBy(group => group.Key.DataServizio)
                     .ThenBy(group => group.Key.NumeroOrdineServizio))
        {
            AddDayPage(
                document,
                serviceGroup.Key.DataServizio,
                serviceGroup.Key.NumeroOrdineServizio,
                serviceGroup.OrderBy(item => item.NumeroOrdineServizio),
                persone,
                cataloghi);
        }

        var riepilogoSection = new Section
        {
            BreakPageBefore = true,
        };
        riepilogoSection.Blocks.Add(BuildRiepilogoTable(anno, meseDescrizione, righe));
        riepilogoSection.Blocks.Add(BuildResponsabileSignature());
        document.Blocks.Add(riepilogoSection);

        return document;
    }

    private static void AddCoverPage(FlowDocument document, int anno, string meseDescrizione)
    {
        AddCentered(document, "POLIZIA DI STATO", 18, FontWeights.Bold);
        AddCentered(document, "Centro Nautico e Sommozzatori", 15, FontWeights.Bold);
        AddCentered(document, "Nucleo Sommozzatori", 15, FontWeights.Bold);
        AddCentered(document, "La Spezia", 13, FontWeights.Normal);
        AddCentered(document, "REGISTRO IMMERSIONI", 18, FontWeights.Bold, new Thickness(0, 72, 0, 0));
        AddCentered(document, $"Mese di {meseDescrizione.ToUpper(CultureInfo.CurrentCulture)} Anno {anno}", 13, FontWeights.Bold, new Thickness(0, 12, 0, 0));
    }

    private void AddDayPage(
        FlowDocument document,
        DateOnly dataServizio,
        string numeroOrdineServizio,
        IEnumerable<ServizioGiornaliero> servizi,
        IReadOnlyDictionary<int, Personale> persone,
        CataloghiServizioSnapshot cataloghi)
    {
        var section = new Section
        {
            BreakPageBefore = true,
        };

        var titleText = string.IsNullOrWhiteSpace(numeroOrdineServizio)
            ? dataServizio.ToString("dddd dd/MM/yyyy", CultureInfo.CurrentCulture).ToUpper(CultureInfo.CurrentCulture)
            : $"{dataServizio.ToString("dddd dd/MM/yyyy", CultureInfo.CurrentCulture).ToUpper(CultureInfo.CurrentCulture)} - Ordine servizio {numeroOrdineServizio}";
        var title = new Paragraph(new Run(titleText))
        {
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 8),
        };
        section.Blocks.Add(title);

        foreach (var servizio in servizi)
        {
            section.Blocks.Add(BuildServizioHeaderTable(servizio, persone, cataloghi));
            section.Blocks.Add(BuildIndennitaTable(servizio, cataloghi));
            section.Blocks.Add(BuildPersonaleTable(servizio, persone, cataloghi));
            section.Blocks.Add(BuildAttivitaSvoltaTable(servizio));
            section.Blocks.Add(new Paragraph { Margin = new Thickness(0, 2, 0, 6) });
        }

        document.Blocks.Add(section);
    }

    private Table BuildServizioHeaderTable(
        ServizioGiornaliero servizio,
        IReadOnlyDictionary<int, Personale> persone,
        CataloghiServizioSnapshot cataloghi)
    {
        var table = CreateTable(60, 75, 85, 80, 170, 200);
        AddHeaderRow(table, "GIORNO", "DATA", "N. SERVIZIO", "ORARIO", "LOCALITA'", "SCOPO IMMERSIONE");
        AddRow(
            table,
            CultureInfo.CurrentCulture.TextInfo.ToTitleCase(servizio.DataServizio.ToString("dddd", CultureInfo.CurrentCulture)),
            servizio.DataServizio.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture),
            servizio.NumeroOrdineServizio,
            servizio.OrarioServizio,
            ResolveLocalita(servizio.LocalitaOperativaId, cataloghi),
            ResolveScopo(servizio.ScopoImmersioneId, cataloghi));

        var roleTable = CreateTable(85, 145, 145, 145, 150);
        AddHeaderRow(roleTable, "IMMERSIONE", "DIRETTORE IMMERSIONE", "OPERATORE SOCCORSO", "ASSISTENZA BLSD", "ASSIST. SANITARIA");
        foreach (var immersione in servizio.Immersioni.OrderBy(item => item.NumeroImmersione))
        {
            AddRow(
                roleTable,
                $"IMMERSIONE {immersione.NumeroImmersione} {FormatOrario(immersione)}",
                FormatPersona(immersione.DirettoreImmersionePerId, persone),
                FormatPersona(immersione.OperatoreSoccorsoPerId, persone),
                FormatPersona(immersione.AssistenteBlsdPerId, persone),
                FormatPersona(immersione.AssistenteSanitarioPerId, persone));
        }

        var wrapper = CreateTable(670);
        var cell = new TableCell { Padding = new Thickness(0), BorderThickness = new Thickness(0) };
        cell.Blocks.Add(table);
        cell.Blocks.Add(roleTable);
        wrapper.RowGroups[0].Rows.Add(new TableRow());
        wrapper.RowGroups[0].Rows[0].Cells.Add(cell);
        return wrapper;
    }

    private Table BuildIndennitaTable(ServizioGiornaliero servizio, CataloghiServizioSnapshot cataloghi)
    {
        var table = CreateTable(210, 150, 250, 60);
        AddRow(
            table,
            "Servizio svolto a bordo dell'unita navale",
            ResolveUnitaNavale(servizio.UnitaNavaleId, cataloghi),
            "Indennita supp. di fuori sede art. 12 D.P.R. 57/2022",
            servizio.FuoriSede ? "SI" : string.Empty);
        return table;
    }

    private static Table BuildAttivitaSvoltaTable(ServizioGiornaliero servizio)
    {
        var table = CreateTable(130, 540);
        AddRow(table, "Attivita svolta", servizio.AttivitaSvolta);
        return table;
    }

    private Table BuildPersonaleTable(
        ServizioGiornaliero servizio,
        IReadOnlyDictionary<int, Personale> persone,
        CataloghiServizioSnapshot cataloghi)
    {
        var table = CreateTable(95, 215, 70, 140, 75, 75);
        AddHeaderRow(table, "QUAL.", "COGNOME E NOME", "PRESENZA", "TIPOLOGIA", "PROF. Mt.", "ORE IMM.");

        var righe = servizio.Partecipanti
            .Where(item => item.Presente)
            .SelectMany(partecipante => BuildPartecipanteRows(servizio, partecipante, persone, cataloghi))
            .ToList();

        if (righe.Count == 0)
        {
            AddRow(table, string.Empty, "Nessun personale SMZ presente", string.Empty, string.Empty, string.Empty, string.Empty);
            return table;
        }

        foreach (var row in righe)
        {
            AddRow(table, row.Qualifica, row.Nominativo, row.Presenza, row.Tipologia, row.Profondita, row.Ore);
        }

        return table;
    }

    private IEnumerable<RegistroPrintPersonaleRow> BuildPartecipanteRows(
        ServizioGiornaliero servizio,
        ServizioPartecipante partecipante,
        IReadOnlyDictionary<int, Personale> persone,
        CataloghiServizioSnapshot cataloghi)
    {
        if (!persone.TryGetValue(partecipante.PerId, out var persona))
        {
            yield break;
        }

        var partecipazioni = servizio.Immersioni
            .SelectMany(immersione => immersione.Partecipazioni
                .Where(item => item.ServizioPartecipanteId == partecipante.ServizioPartecipanteId)
                .Select(item => (immersione, partecipazione: item)))
            .ToList();

        if (partecipazioni.Count == 0)
        {
            yield return new RegistroPrintPersonaleRow(
                QualificaFormatter.AbbreviaPerVisualizzazione(persona.Qualifica),
                persona.NominativoCompleto,
                "X",
                string.Empty,
                string.Empty,
                string.Empty);
            yield break;
        }

        foreach (var item in partecipazioni)
        {
            var tipologia = cataloghi.TipologieImmersione
                .FirstOrDefault(tipo => tipo.TipologiaImmersioneOperativaId == item.partecipazione.TipologiaImmersioneOperativaId)
                ?.Descrizione ?? string.Empty;
            yield return new RegistroPrintPersonaleRow(
                QualificaFormatter.AbbreviaPerVisualizzazione(persona.Qualifica),
                persona.NominativoCompleto,
                "X",
                tipologia,
                item.partecipazione.ProfonditaMetri?.ToString(CultureInfo.CurrentCulture) ?? string.Empty,
                item.partecipazione.OreImmersione is { } ore ? ore.ToString("0.##", CultureInfo.CurrentCulture) : string.Empty);
        }
    }

    private static Table BuildRiepilogoTable(int anno, string meseDescrizione, IReadOnlyList<RegistroImmersioneRiga> righe)
    {
        var table = CreateTable(180, 180, 180, 180);
        var title = new TableRow();
        title.Cells.Add(new TableCell(new Paragraph(new Run($"RIEPILOGO NUMERO IMMERSIONI EFFETTUATE NEL MESE DI {meseDescrizione.ToUpper(CultureInfo.CurrentCulture)} ANNO {anno}"))
        {
            TextAlignment = TextAlignment.Center,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(2),
        })
        {
            ColumnSpan = 4,
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(TableLineThickness),
            Padding = new Thickness(3),
        });
        table.RowGroups[0].Rows.Add(title);

        AddHeaderRow(
            table,
            "IMMERSIONI ADDESTRATIVE A MARE ED IN BACINO DELIMITATO",
            "IMMERSIONI ORDINARIE",
            "IMMERSIONI PER SPERIMENTAZIONE ATTREZZATURE E MATERIALI SUBACQUEI",
            "IMMERSIONI IN CAMERA IPERBARICA");

        AddRow(
            table,
            CountCategoria(righe, "ADDESTR").ToString(CultureInfo.CurrentCulture),
            CountCategoria(righe, "ORDIN").ToString(CultureInfo.CurrentCulture),
            CountCategoria(righe, "SPER").ToString(CultureInfo.CurrentCulture),
            CountCategoria(righe, "CAMERA").ToString(CultureInfo.CurrentCulture));
        return table;
    }

    private static Paragraph BuildResponsabileSignature()
    {
        var firma = new Paragraph
        {
            TextAlignment = TextAlignment.Right,
            Margin = new Thickness(0, 34, 70, 0),
        };
        firma.Inlines.Add(new Run("Il RESPONSABILE"));
        firma.Inlines.Add(new LineBreak());
        firma.Inlines.Add(new Run("NUCLEO SOMMOZZATORI"));
        firma.Inlines.Add(new LineBreak());
        firma.Inlines.Add(new Run("____________________________"));
        return firma;
    }

    private Dictionary<int, Personale> GetPersone(IEnumerable<ServizioGiornaliero> servizi)
    {
        var perIds = servizi
            .SelectMany(servizio => servizio.Partecipanti.Select(item => item.PerId)
                .Concat(servizio.Immersioni.SelectMany(item => new[]
                {
                    item.DirettoreImmersionePerId,
                    item.OperatoreSoccorsoPerId,
                    item.AssistenteBlsdPerId,
                    item.AssistenteSanitarioPerId,
                }).Where(item => item is not null).Select(item => item!.Value)))
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

    private static int CountCategoria(IEnumerable<RegistroImmersioneRiga> righe, string token) =>
        righe
            .Where(item => item.CategoriaRegistro.Contains(token, StringComparison.CurrentCultureIgnoreCase))
            .Select(item => item.ServizioImmersioneId)
            .Distinct()
            .Count();

    private static string ResolveLocalita(int? id, CataloghiServizioSnapshot cataloghi) =>
        id is { } value
            ? cataloghi.LocalitaOperative.FirstOrDefault(item => item.LocalitaOperativaId == value)?.Descrizione ?? string.Empty
            : string.Empty;

    private static string ResolveScopo(int? id, CataloghiServizioSnapshot cataloghi) =>
        id is { } value
            ? cataloghi.ScopiImmersione.FirstOrDefault(item => item.ScopoImmersioneId == value)?.Descrizione ?? string.Empty
            : string.Empty;

    private static string ResolveUnitaNavale(int? id, CataloghiServizioSnapshot cataloghi) =>
        id is { } value
            ? cataloghi.UnitaNavali.FirstOrDefault(item => item.UnitaNavaleId == value)?.Descrizione ?? string.Empty
            : string.Empty;

    private static string FormatPersona(int? perId, IReadOnlyDictionary<int, Personale> persone)
    {
        if (perId is not { } value || !persone.TryGetValue(value, out var persona))
        {
            return string.Empty;
        }

        var qualifica = QualificaFormatter.AbbreviaPerVisualizzazione(persona.Qualifica);
        return string.IsNullOrWhiteSpace(qualifica)
            ? persona.NominativoCompleto
            : $"{qualifica} {persona.NominativoCompleto}";
    }

    private static string FormatOrario(ServizioImmersione immersione)
    {
        var inizio = immersione.OrarioInizio?.ToString("HH:mm") ?? string.Empty;
        var fine = immersione.OrarioFine?.ToString("HH:mm") ?? string.Empty;
        return (inizio, fine) switch
        {
            ("", "") => string.Empty,
            (_, "") => inizio,
            ("", _) => fine,
            _ => $"{inizio}-{fine}",
        };
    }

    private static void AddCentered(
        FlowDocument document,
        string text,
        double fontSize,
        FontWeight fontWeight,
        Thickness? margin = null)
    {
        document.Blocks.Add(new Paragraph(new Run(text))
        {
            TextAlignment = TextAlignment.Center,
            FontSize = fontSize,
            FontWeight = fontWeight,
            Margin = margin ?? new Thickness(0, 0, 0, 1),
        });
    }

    private static Table CreateTable(params double[] widths)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 4) };
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
            TextAlignment = header ? TextAlignment.Center : TextAlignment.Left,
        })
        {
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(
                columnIndex == 0 ? TableLineThickness : 0,
                rowIndex == 0 ? TableLineThickness : 0,
                TableLineThickness,
                TableLineThickness),
            Padding = new Thickness(2),
            Background = header ? Brushes.WhiteSmoke : Brushes.Transparent,
        };

    private sealed record RegistroPrintPersonaleRow(
        string Qualifica,
        string Nominativo,
        string Presenza,
        string Tipologia,
        string Profondita,
        string Ore);
}
