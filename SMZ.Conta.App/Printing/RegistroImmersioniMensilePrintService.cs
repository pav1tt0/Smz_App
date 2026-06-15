using System.Globalization;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using SMZ.Conta.App.Data;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.Printing;

public enum RegistroImmersioniMensilePrintLayout
{
    Normale,
    Compatto,
}

public sealed class RegistroImmersioniMensilePrintService
{
    private const double TableLineThickness = 0.8;
    private readonly PersonaleRepository _repository;

    public RegistroImmersioniMensilePrintService(PersonaleRepository repository)
    {
        _repository = repository;
    }

    public void Print(
        int anno,
        int mese,
        string meseDescrizione,
        RegistroImmersioniMensilePrintLayout layout = RegistroImmersioniMensilePrintLayout.Normale)
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

        var document = BuildDocument(anno, meseDescrizione, servizi, righe, layout);
        var pagePadding = GetPagePadding(layout);
        document.PageWidth = dialog.PrintableAreaWidth;
        document.PageHeight = dialog.PrintableAreaHeight;
        document.PagePadding = pagePadding;
        document.ColumnWidth = document.PageWidth - document.PagePadding.Left - document.PagePadding.Right;

        dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, $"Registro immersioni {meseDescrizione} {anno}");
    }

    private FlowDocument BuildDocument(
        int anno,
        string meseDescrizione,
        IReadOnlyList<ServizioGiornaliero> servizi,
        IReadOnlyList<RegistroImmersioneRiga> righe,
        RegistroImmersioniMensilePrintLayout layout)
    {
        var cataloghi = _repository.GetCataloghiServizio();
        var persone = GetPersone(servizi);
        var document = new FlowDocument
        {
            FontFamily = new FontFamily("Calibri"),
            FontSize = GetFontSize(layout),
            PagePadding = GetPagePadding(layout),
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

    private static double GetFontSize(RegistroImmersioniMensilePrintLayout layout) =>
        layout == RegistroImmersioniMensilePrintLayout.Compatto ? 7.4 : 8.5;

    private static Thickness GetPagePadding(RegistroImmersioniMensilePrintLayout layout) =>
        layout == RegistroImmersioniMensilePrintLayout.Compatto
            ? new Thickness(22, 20, 22, 20)
            : new Thickness(36);

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
        var table = CreateTable(95, 255, 165, 75, 75);
        AddHeaderRow(table, "QUAL.", "COGNOME E NOME", "TIPOLOGIA", "PROF. Mt.", "ORE IMM.");

        var righe = servizio.Partecipanti
            .Where(item => item.Presente)
            .SelectMany(partecipante => BuildPartecipanteRows(servizio, partecipante, persone, cataloghi))
            .Concat(servizio.OperatoriSubEsterni.SelectMany(operatore => BuildOperatoreEsternoRows(servizio, operatore, cataloghi)))
            .ToList();

        if (righe.Count == 0)
        {
            AddRow(table, string.Empty, "Nessun personale SMZ presente", string.Empty, string.Empty, string.Empty);
            return table;
        }

        foreach (var row in righe)
        {
            AddRow(table, row.Qualifica, row.Nominativo, row.Tipologia, row.Profondita, row.Ore);
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
                tipologia,
                item.partecipazione.ProfonditaMetri?.ToString(CultureInfo.CurrentCulture) ?? string.Empty,
                item.partecipazione.OreImmersione is { } ore ? ore.ToString("0.##", CultureInfo.CurrentCulture) : string.Empty);
        }
    }

    private static IEnumerable<RegistroPrintPersonaleRow> BuildOperatoreEsternoRows(
        ServizioGiornaliero servizio,
        ServizioOperatoreSubEsterno operatore,
        CataloghiServizioSnapshot cataloghi)
    {
        var partecipazioni = servizio.Immersioni
            .SelectMany(immersione => immersione.PartecipazioniEsterne
                .Where(item => item.ServizioOperatoreSubEsternoId == operatore.ServizioOperatoreSubEsternoId)
                .Select(item => (immersione, partecipazione: item)))
            .ToList();

        if (partecipazioni.Count == 0)
        {
            yield return new RegistroPrintPersonaleRow(
                QualificaFormatter.AbbreviaPerVisualizzazione(operatore.Qualifica),
                $"{operatore.Nominativo} ({operatore.Reparto})",
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
                QualificaFormatter.AbbreviaPerVisualizzazione(operatore.Qualifica),
                $"{operatore.Nominativo} ({operatore.Reparto})",
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
        string Tipologia,
        string Profondita,
        string Ore);
}
