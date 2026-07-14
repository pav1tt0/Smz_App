using System.Globalization;
using System.Text.RegularExpressions;
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
            FontFamily = new FontFamily("Times New Roman"),
            FontSize = 9,
            PagePadding = new Thickness(30),
            Foreground = PrintTheme.TextBrush,
        };

        document.Blocks.Add(PrintTheme.RepublicEmblem(44));
        AddCentered(document, "POLIZIA DI STATO", 15, FontWeights.Bold);
        AddCentered(document, "CENTRO NAUTICO E SOMMOZZATORI", 12, FontWeights.Bold);
        AddCentered(document, "Nucleo Sommozzatori - La Spezia", 10.5, FontWeights.Normal);
        document.Blocks.Add(CreateDivider());
        document.Blocks.Add(CreateRecipientBlock());

        AddOggetto(document, servizio, localita, scopo, responsabile);

        var personaleRows = BuildPersonaleRows(servizio, persone, cataloghi);
        AddPersonaleSection(document, personaleRows, "SMZ", "Personale SOMMOZZATORE", alwaysShow: true);
        AddPersonaleSection(document, personaleRows, "OSSALC", "Personale O.S.S.A.L.C.");
        AddPersonaleSection(document, personaleRows, "SUPPORTO", "Personale ASS. IMMERSIONE");
        AddPersonaleSection(document, personaleRows, "SANITARIA", "Personale ASS. SANITARIA");

        AddActivityDeclaration(document, servizio);
        AddEconomicDeclaration(document);
        document.Blocks.Add(BuildRiepilogoTable(servizio, cataloghi, unita, localita));

        var firma = new Paragraph
        {
            TextAlignment = TextAlignment.Right,
            Margin = new Thickness(0, 26, 0, 0),
        };
        firma.Inlines.Add(new Run("Il Responsabile del Team"));
        firma.Inlines.Add(new LineBreak());
        firma.Inlines.Add(new Run(responsabile));
        firma.Inlines.Add(new LineBreak());
        firma.Inlines.Add(new Run("____________________________"));
        document.Blocks.Add(firma);

        return document;
    }

    private static void AddOggetto(
        FlowDocument document,
        ServizioGiornaliero servizio,
        string localita,
        string scopo,
        string responsabile)
    {
        var oggetto = new Paragraph { Margin = new Thickness(0, 4, 0, 7) };
        oggetto.Inlines.Add(new Run("OGGETTO: ") { FontWeight = FontWeights.Bold });
        oggetto.Inlines.Add(new Run($"Riferimento Ordine di Servizio nr. {EmptyField(servizio.NumeroOrdineServizio)} del {servizio.DataServizio:dd/MM/yyyy}."));
        document.Blocks.Add(oggetto);

        document.Blocks.Add(new Paragraph(new Run(
            $"Relazione sulle attività specialistiche svolte a {EmptyField(localita)} per il servizio di {EmptyField(scopo)}"))
        {
            Margin = new Thickness(0, 0, 0, 4),
            TextAlignment = TextAlignment.Justify,
        });
        document.Blocks.Add(new Paragraph(new Run($"con orario {EmptyField(FormatOrarioServizio(servizio.OrarioServizio))}"))
        {
            Margin = new Thickness(0, 0, 0, 7),
        });
        document.Blocks.Add(new Paragraph(new Run(
            $"Il sottoscritto {EmptyField(responsabile)}, responsabile del Team SMZ composto dal seguente personale:"))
        {
            Margin = new Thickness(0, 0, 0, 7),
            TextAlignment = TextAlignment.Justify,
        });
    }

    private static List<PersonaleStampaRow> BuildPersonaleRows(
        ServizioGiornaliero servizio,
        IReadOnlyDictionary<int, Personale> persone,
        CataloghiServizioSnapshot cataloghi)
    {
        var gruppoById = cataloghi.GruppiOperativi.ToDictionary(item => item.GruppoOperativoId, item => item.Codice);
        var ruoloById = cataloghi.RuoliOperativi.ToDictionary(item => item.RuoloOperativoId, item => item.Codice);
        var rows = new List<PersonaleStampaRow>();

        foreach (var partecipante in servizio.Partecipanti.Where(item => item.Presente))
        {
            if (!persone.TryGetValue(partecipante.PerId, out var persona))
            {
                continue;
            }

            rows.Add(new PersonaleStampaRow(
                gruppoById.GetValueOrDefault(partecipante.GruppoOperativoId, "SMZ"),
                FormatPersona(persona),
                BuildMansione(servizio, partecipante.PerId, partecipante.RuoloOperativoId, ruoloById),
                BuildApparati(servizio, partecipante.PerId, cataloghi),
                GetPersonOrder(partecipante.PerId, persone)));
        }

        foreach (var operatore in servizio.OperatoriSubEsterni)
        {
            var qualifica = QualificaFormatter.AbbreviaPerVisualizzazione(operatore.Qualifica);
            var nominativo = string.IsNullOrWhiteSpace(qualifica)
                ? operatore.Nominativo
                : $"{qualifica} {operatore.Nominativo}";
            if (!string.IsNullOrWhiteSpace(operatore.Reparto))
            {
                nominativo += $" ({operatore.Reparto})";
            }

            rows.Add(new PersonaleStampaRow(
                gruppoById.GetValueOrDefault(operatore.GruppoOperativoId, "SMZ"),
                nominativo,
                BuildMansioneEsterna(servizio, operatore.ServizioOperatoreSubEsternoId),
                BuildApparatiEsterni(servizio, operatore.ServizioOperatoreSubEsternoId, cataloghi),
                long.MaxValue));
        }

        foreach (var supporto in servizio.SupportiOccasionali.Where(item => item.Presente))
        {
            var qualifica = QualificaFormatter.AbbreviaPerVisualizzazione(supporto.Qualifica);
            var nominativo = string.IsNullOrWhiteSpace(qualifica)
                ? supporto.Nominativo
                : $"{qualifica} {supporto.Nominativo}";
            var gruppo = supporto.Ruolo.Contains("sanitar", StringComparison.OrdinalIgnoreCase)
                ? "SANITARIA"
                : "SUPPORTO";
            rows.Add(new PersonaleStampaRow(
                gruppo,
                nominativo,
                NormalizeMansione(supporto.Ruolo),
                string.Empty,
                long.MaxValue));
        }

        return rows
            .OrderBy(item => item.Ordine)
            .ThenBy(item => item.Nominativo, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static void AddPersonaleSection(
        FlowDocument document,
        IReadOnlyCollection<PersonaleStampaRow> source,
        string gruppo,
        string title,
        bool alwaysShow = false)
    {
        var rows = source.Where(item => string.Equals(item.Gruppo, gruppo, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!alwaysShow && rows.Count == 0)
        {
            return;
        }

        var table = CreateRelativeTable(4.5, 2.5, 2.4);
        var titleRow = new TableRow { FontWeight = FontWeights.Bold };
        var titleCell = CreateCell(title, true, 0, 0);
        titleCell.ColumnSpan = 3;
        titleRow.Cells.Add(titleCell);
        table.RowGroups[0].Rows.Add(titleRow);
        AddHeaderRow(table, "COGNOME E NOME", "Mansione", "Apparecchiatura");

        if (rows.Count == 0)
        {
            AddRow(table, string.Empty, string.Empty, string.Empty);
        }
        else
        {
            foreach (var row in rows)
            {
                AddRow(table, row.Nominativo, row.Mansione, row.Apparato);
            }
        }

        document.Blocks.Add(table);
    }

    private static void AddActivityDeclaration(FlowDocument document, ServizioGiornaliero servizio)
    {
        document.Blocks.Add(new Paragraph(new Run(
            "Riferisce sulle attività specialistiche svolte in data odierna ed eventuali variazioni di servizio:"))
        {
            Margin = new Thickness(0, 7, 0, 4),
        });

        var dettaglio = string.Join(
            Environment.NewLine,
            new[] { servizio.AttivitaSvolta, servizio.Note }.Where(value => !string.IsNullOrWhiteSpace(value)));
        document.Blocks.Add(new Paragraph(new Run(EmptyField(dettaglio)))
        {
            Margin = new Thickness(0, 0, 0, 9),
            TextAlignment = TextAlignment.Justify,
        });
    }

    private static void AddEconomicDeclaration(FlowDocument document)
    {
        var paragraph = new Paragraph { Margin = new Thickness(0, 4, 0, 7), TextAlignment = TextAlignment.Justify };
        paragraph.Inlines.Add(new Run("Premesso quanto sopra lo scrivente "));
        paragraph.Inlines.Add(new Run("DICHIARA") { FontWeight = FontWeights.Bold });
        paragraph.Inlines.Add(new Run(" che il personale in elenco indicato ha maturato il diritto al seguente trattamento economico accessorio:"));
        document.Blocks.Add(paragraph);
    }

    private static Table BuildRiepilogoTable(
        ServizioGiornaliero servizio,
        CataloghiServizioSnapshot cataloghi,
        string unita,
        string localita)
    {
        var table = CreateRelativeTable(0.54, 0.57, 0.57, 0.85, 0.57, 0.57, 0.57, 0.57, 0.99, 0.99, 0.99, 0.99, 2.41);
        table.FontSize = 7;

        var row1 = new TableRow { FontWeight = FontWeights.Bold };
        row1.Cells.Add(CreateSummaryCell("Ore Straordinario", true, 0, 0, columnSpan: 4));
        row1.Cells.Add(CreateSummaryCell("Presenze", true, 0, 4, columnSpan: 3));
        row1.Cells.Add(CreateSummaryCell("Ore Immersione", true, 0, 7, columnSpan: 5));
        row1.Cells.Add(CreateSummaryCell("Indennità supplementare di fuori sede di cui all’art. 12 D.P.R. 57/2022", true, 0, 12));
        table.RowGroups[0].Rows.Add(row1);

        var row2 = new TableRow { FontWeight = FontWeights.Bold };
        foreach (var value in new[] { "Fer.", "Fest.", "Nott.", "Fest. Nott.", "Est.", "Nott.", "Fest.", string.Empty, "ARA", "ARO", "ARM", "C.I.", string.Empty })
        {
            row2.Cells.Add(CreateSummaryCell(value, true, 1, row2.Cells.Count));
        }
        table.RowGroups[0].Rows.Add(row2);

        var straordinario = CalcolaStraordinario(servizio);
        var presenze = CalcolaPresenze(servizio);
        var immersioni = CalcolaRiepilogoImmersioni(servizio, cataloghi);
        var fuoriSede = servizio.FuoriSede
            ? $"UNITÀ NAVALE: {EmptyField(unita)}"
            : string.Empty;

        var row3 = new TableRow();
        foreach (var value in new[]
                 {
                     FormatOre(straordinario.Feriali),
                     FormatOre(straordinario.Festive),
                     FormatOre(straordinario.Notturne),
                     FormatOre(straordinario.FestiveNotturne),
                     presenze.Esterna ? "X" : string.Empty,
                     presenze.Notturna ? "X" : string.Empty,
                     presenze.Festiva ? "X" : string.Empty,
                     "h",
                     FormatOre(immersioni.Ara.Ore),
                     FormatOre(immersioni.Aro.Ore),
                     FormatOre(immersioni.Arm.Ore),
                     FormatOre(immersioni.Ci.Ore),
                     fuoriSede,
                 })
        {
            row3.Cells.Add(CreateSummaryCell(value, false, 2, row3.Cells.Count));
        }
        table.RowGroups[0].Rows.Add(row3);

        var row4 = new TableRow();
        foreach (var value in new[]
                 {
                     string.Empty, string.Empty, string.Empty, string.Empty,
                     string.Empty, string.Empty, string.Empty,
                     "mt",
                     FormatMetri(immersioni.Ara.Metri),
                     FormatMetri(immersioni.Aro.Metri),
                     FormatMetri(immersioni.Arm.Metri),
                     FormatMetri(immersioni.Ci.Metri),
                     servizio.FuoriSede ? $"LOCALITÀ: {EmptyField(localita)}" : string.Empty,
                 })
        {
            row4.Cells.Add(CreateSummaryCell(value, false, 3, row4.Cells.Count));
        }
        table.RowGroups[0].Rows.Add(row4);
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

    private static string BuildMansione(
        ServizioGiornaliero servizio,
        int perId,
        int? ruoloOperativoId,
        IReadOnlyDictionary<int, string> ruoloById)
    {
        var ruoli = new List<string>();
        foreach (var immersione in servizio.Immersioni)
        {
            AddRole(ruoli, "Direttore immersione", immersione.DirettoreImmersionePerId, perId);
            AddRole(ruoli, "Operatore soccorso", immersione.OperatoreSoccorsoPerId, perId);
            AddRole(ruoli, "Assistenza BLSD", immersione.AssistenteBlsdPerId, perId);
            AddRole(ruoli, "Assistenza sanitaria", immersione.AssistenteSanitarioPerId, perId);
        }

        if (ruoloOperativoId is { } ruoloId && ruoloById.TryGetValue(ruoloId, out var codiceRuolo))
        {
            var ruolo = NormalizeMansione(codiceRuolo);
            if (!string.IsNullOrWhiteSpace(ruolo) && !ruoli.Contains(ruolo, StringComparer.OrdinalIgnoreCase))
            {
                ruoli.Add(ruolo);
            }
        }

        return string.Join(", ", ruoli);
    }

    private static string BuildMansioneEsterna(ServizioGiornaliero servizio, long servizioOperatoreSubEsternoId)
    {
        var operatore = servizio.OperatoriSubEsterni.FirstOrDefault(item =>
            item.ServizioOperatoreSubEsternoId == servizioOperatoreSubEsternoId
            || item.PerId == servizioOperatoreSubEsternoId);
        if (operatore is null || operatore.PerId <= 0)
        {
            return string.Empty;
        }

        return BuildMansione(servizio, operatore.PerId, null, new Dictionary<int, string>());
    }

    private static string BuildApparati(
        ServizioGiornaliero servizio,
        int perId,
        CataloghiServizioSnapshot cataloghi)
    {
        var tipologie = servizio.Immersioni
            .SelectMany(immersione => immersione.Partecipazioni)
            .Where(partecipazione => ResolvePerId(partecipazione, servizio) == perId)
            .Select(partecipazione => partecipazione.TipologiaImmersioneOperativaId);
        return FormatApparati(tipologie, cataloghi);
    }

    private static string BuildApparatiEsterni(
        ServizioGiornaliero servizio,
        long servizioOperatoreSubEsternoId,
        CataloghiServizioSnapshot cataloghi)
    {
        var operatore = servizio.OperatoriSubEsterni.FirstOrDefault(item =>
            item.ServizioOperatoreSubEsternoId == servizioOperatoreSubEsternoId
            || item.PerId == servizioOperatoreSubEsternoId);
        var ids = new HashSet<long> { servizioOperatoreSubEsternoId };
        if (operatore is not null)
        {
            ids.Add(operatore.ServizioOperatoreSubEsternoId);
            ids.Add(operatore.PerId);
        }

        var tipologie = servizio.Immersioni
            .SelectMany(immersione => immersione.PartecipazioniEsterne)
            .Where(partecipazione => ids.Contains(partecipazione.ServizioOperatoreSubEsternoId))
            .Select(partecipazione => partecipazione.TipologiaImmersioneOperativaId);
        return FormatApparati(tipologie, cataloghi);
    }

    private static string FormatApparati(IEnumerable<int?> tipologieIds, CataloghiServizioSnapshot cataloghi)
    {
        var descrizioneById = cataloghi.TipologieImmersione.ToDictionary(
            item => item.TipologiaImmersioneOperativaId,
            item => item.Descrizione);
        return string.Join(", ", tipologieIds
            .Where(item => item.HasValue && descrizioneById.ContainsKey(item.Value))
            .Select(item => descrizioneById[item!.Value])
            .Distinct(StringComparer.CurrentCultureIgnoreCase));
    }

    private static string NormalizeMansione(string value)
    {
        if (value.Contains("DIRETT", StringComparison.OrdinalIgnoreCase))
        {
            return "Direttore immersione";
        }

        if (value.Contains("SOCCORS", StringComparison.OrdinalIgnoreCase))
        {
            return "Operatore soccorso";
        }

        if (value.Contains("BLSD", StringComparison.OrdinalIgnoreCase)
            || value.Contains("BLS-D", StringComparison.OrdinalIgnoreCase))
        {
            return "Assistenza BLSD";
        }

        return value.Contains("SANIT", StringComparison.OrdinalIgnoreCase)
            ? "Assistenza sanitaria"
            : string.Empty;
    }

    private static int ResolvePerId(ServizioPartecipanteImmersione partecipazione, ServizioGiornaliero servizio)
    {
        var partecipante = servizio.Partecipanti.FirstOrDefault(item => item.ServizioPartecipanteId == partecipazione.ServizioPartecipanteId);
        return partecipante?.PerId ?? (int)partecipazione.ServizioPartecipanteId;
    }

    private static void AddRole(ICollection<string> ruoli, string ruolo, int? rolePerId, int perId)
    {
        if (rolePerId == perId && !ruoli.Contains(ruolo, StringComparer.OrdinalIgnoreCase))
        {
            ruoli.Add(ruolo);
        }
    }

    public static StraordinarioStampaSummary CalcolaStraordinario(ServizioGiornaliero servizio)
    {
        if (!servizio.StraordinarioAttivo
            || !TryBuildInterval(
                servizio.DataServizio,
                servizio.StraordinarioInizio,
                servizio.StraordinarioFine,
                out var inizio,
                out var fine))
        {
            return default;
        }

        decimal feriali = 0;
        decimal festive = 0;
        decimal notturne = 0;
        decimal festiveNotturne = 0;
        for (var cursor = inizio; cursor < fine; cursor = cursor.AddMinutes(1))
        {
            var prossimo = cursor.AddMinutes(1) < fine ? cursor.AddMinutes(1) : fine;
            var durata = (prossimo.Ticks - cursor.Ticks) / (decimal)TimeSpan.TicksPerHour;
            var festivo = IsFestivo(DateOnly.FromDateTime(cursor));
            var notturno = IsNotturno(cursor.TimeOfDay);
            if (festivo && notturno)
            {
                festiveNotturne += durata;
            }
            else if (festivo)
            {
                festive += durata;
            }
            else if (notturno)
            {
                notturne += durata;
            }
            else
            {
                feriali += durata;
            }
        }

        return new StraordinarioStampaSummary(
            decimal.Round(feriali, 6),
            decimal.Round(festive, 6),
            decimal.Round(notturne, 6),
            decimal.Round(festiveNotturne, 6));
    }

    private static PresenzeStampaSummary CalcolaPresenze(ServizioGiornaliero servizio)
    {
        var presenti = servizio.Partecipanti.Count(item => item.Presente)
                       + servizio.OperatoriSubEsterni.Count
                       + servizio.SupportiOccasionali.Count(item => item.Presente);
        if (presenti == 0)
        {
            return default;
        }

        var notturna = false;
        var festiva = IsFestivo(servizio.DataServizio);
        if (TryBuildInterval(servizio.DataServizio, servizio.OrarioServizio, out var inizio, out var fine))
        {
            for (var cursor = inizio; cursor < fine; cursor = cursor.AddMinutes(1))
            {
                notturna |= IsNotturno(cursor.TimeOfDay);
                festiva |= IsFestivo(DateOnly.FromDateTime(cursor));
                if (notturna && festiva)
                {
                    break;
                }
            }
        }

        return new PresenzeStampaSummary(true, notturna, festiva);
    }

    private static ImmersioniStampaSummary CalcolaRiepilogoImmersioni(
        ServizioGiornaliero servizio,
        CataloghiServizioSnapshot cataloghi)
    {
        var codiceById = cataloghi.TipologieImmersione.ToDictionary(
            item => item.TipologiaImmersioneOperativaId,
            item => item.Codice);
        var values = new Dictionary<string, ApparatoAccumulator>(StringComparer.OrdinalIgnoreCase)
        {
            ["ARA"] = new(),
            ["ARO"] = new(),
            ["ARM"] = new(),
            ["CI"] = new(),
        };

        foreach (var partecipazione in servizio.Immersioni.SelectMany(item => item.Partecipazioni))
        {
            AddImmersione(values, codiceById, partecipazione.TipologiaImmersioneOperativaId, partecipazione.OreImmersione, partecipazione.ProfonditaMetri);
        }

        foreach (var partecipazione in servizio.Immersioni.SelectMany(item => item.PartecipazioniEsterne))
        {
            AddImmersione(values, codiceById, partecipazione.TipologiaImmersioneOperativaId, partecipazione.OreImmersione, partecipazione.ProfonditaMetri);
        }

        return new ImmersioniStampaSummary(
            values["ARA"].ToSummary(),
            values["ARO"].ToSummary(),
            values["ARM"].ToSummary(),
            values["CI"].ToSummary());
    }

    private static void AddImmersione(
        IDictionary<string, ApparatoAccumulator> values,
        IReadOnlyDictionary<int, string> codiceById,
        int? tipologiaId,
        decimal? ore,
        int? metri)
    {
        if (tipologiaId is null || !codiceById.TryGetValue(tipologiaId.Value, out var codice))
        {
            return;
        }

        var key = codice.StartsWith("ARA", StringComparison.OrdinalIgnoreCase) ? "ARA" : codice;
        if (values.TryGetValue(key, out var value))
        {
            value.Ore += ore ?? 0m;
            value.Metri = Math.Max(value.Metri, metri ?? 0);
        }
    }

    private static bool TryBuildInterval(
        DateOnly data,
        string inizioValue,
        string fineValue,
        out DateTime inizio,
        out DateTime fine)
    {
        inizio = default;
        fine = default;
        if (!TryParseMinutes(inizioValue, out var minutiInizio)
            || !TryParseMinutes(fineValue, out var minutiFine))
        {
            return false;
        }

        var giorno = data.ToDateTime(TimeOnly.MinValue);
        inizio = giorno.AddMinutes(minutiInizio);
        fine = giorno.AddMinutes(minutiFine);
        if (fine < inizio)
        {
            fine = fine.AddDays(1);
        }

        return true;
    }

    private static bool TryBuildInterval(DateOnly data, string value, out DateTime inizio, out DateTime fine)
    {
        var matches = Regex.Matches(value ?? string.Empty, @"\d{1,2}[\.:]\d{2}");
        if (matches.Count < 2)
        {
            inizio = default;
            fine = default;
            return false;
        }

        return TryBuildInterval(data, matches[0].Value, matches[1].Value, out inizio, out fine);
    }

    private static bool TryParseMinutes(string value, out int minutes)
    {
        minutes = 0;
        var normalized = value.Trim().Replace('.', ':');
        var parts = normalized.Split(':');
        if (parts.Length != 2
            || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var hours)
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var mins)
            || hours is < 0 or > 24
            || mins is < 0 or > 59
            || hours == 24 && mins != 0)
        {
            return false;
        }

        minutes = hours * 60 + mins;
        return true;
    }

    private static bool IsNotturno(TimeSpan value) =>
        value >= TimeSpan.FromHours(22) || value < TimeSpan.FromHours(6);

    private static bool IsFestivo(DateOnly data)
    {
        if (data.DayOfWeek == DayOfWeek.Sunday)
        {
            return true;
        }

        if ((data.Month, data.Day) is
            (1, 1) or (1, 6) or (4, 25) or (5, 1) or (6, 2) or
            (8, 15) or (11, 1) or (12, 8) or (12, 25) or (12, 26))
        {
            return true;
        }

        return data == GetPasqua(data.Year).AddDays(1);
    }

    private static DateOnly GetPasqua(int year)
    {
        var a = year % 19;
        var b = year / 100;
        var c = year % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = (19 * a + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + 2 * e + 2 * i - h - k) % 7;
        var m = (a + 11 * h + 22 * l) / 451;
        var month = (h + l - 7 * m + 114) / 31;
        var day = (h + l - 7 * m + 114) % 31 + 1;
        return new DateOnly(year, month, day);
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

    private static Table CreateRelativeTable(params double[] weights)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 8) };
        foreach (var weight in weights)
        {
            table.Columns.Add(new TableColumn { Width = new GridLength(weight, GridUnitType.Star) });
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

    private static TableCell CreateSummaryCell(
        string value,
        bool header,
        int rowIndex,
        int columnIndex,
        int columnSpan = 1)
    {
        var cell = new TableCell(new Paragraph(new Run(value))
        {
            Margin = new Thickness(0),
            TextAlignment = TextAlignment.Center,
        })
        {
            BorderBrush = PrintTheme.BorderBrush,
            BorderThickness = new Thickness(PrintTheme.BorderThickness),
            Padding = new Thickness(2, 3, 2, 3),
            Background = header
                ? PrintTheme.HeaderBackground
                : rowIndex % 2 == 0 ? PrintTheme.AlternateRowBackground : Brushes.Transparent,
            ColumnSpan = columnSpan,
        };
        return cell;
    }

    private static TextAlignment GetCellAlignment(int columnIndex) =>
        columnIndex >= 4 ? TextAlignment.Right : TextAlignment.Left;

    private static string FormatOrarioServizio(string value)
    {
        var matches = Regex.Matches(value ?? string.Empty, @"\d{1,2}[\.:]\d{2}");
        return matches.Count >= 2
            ? $"{matches[0].Value.Replace(':', '.')} - {matches[1].Value.Replace(':', '.')}"
            : value?.Trim() ?? string.Empty;
    }

    private static string FormatOre(decimal value) =>
        value > 0 ? value.ToString("0.##", CultureInfo.CurrentCulture) : string.Empty;

    private static string FormatMetri(int value) =>
        value > 0 ? value.ToString(CultureInfo.CurrentCulture) : string.Empty;

    private static string EmptyField(string value) =>
        string.IsNullOrWhiteSpace(value) ? "________________" : value.Trim();

    public readonly record struct StraordinarioStampaSummary(
        decimal Feriali,
        decimal Festive,
        decimal Notturne,
        decimal FestiveNotturne);

    private readonly record struct PresenzeStampaSummary(bool Esterna, bool Notturna, bool Festiva);

    private readonly record struct ApparatoStampaSummary(decimal Ore, int Metri);

    private readonly record struct ImmersioniStampaSummary(
        ApparatoStampaSummary Ara,
        ApparatoStampaSummary Aro,
        ApparatoStampaSummary Arm,
        ApparatoStampaSummary Ci);

    private sealed record PersonaleStampaRow(
        string Gruppo,
        string Nominativo,
        string Mansione,
        string Apparato,
        long Ordine);

    private sealed class ApparatoAccumulator
    {
        public decimal Ore { get; set; }

        public int Metri { get; set; }

        public ApparatoStampaSummary ToSummary() => new(Ore, Metri);
    }
}
