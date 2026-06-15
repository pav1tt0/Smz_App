using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using Microsoft.Win32;
using SMZ.Conta.App.Data;
using SMZ.Conta.App.Infrastructure;
using SMZ.Conta.App.Models;
using SMZ.Conta.App.Printing;

namespace SMZ.Conta.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private bool HasModifichePersonaleNonSalvate() => CapturePersonaleEditorSnapshot() != _personaleEditorSnapshot;

    private bool HasModificheServizioNonSalvate() => CaptureServizioEditorSnapshot() != _servizioEditorSnapshot;

    private bool HasModificheTariffeNonSalvate() => CaptureTariffeEditorSnapshot() != _tariffeEditorSnapshot;

    private void RegistraSnapshotPersonale() => _personaleEditorSnapshot = CapturePersonaleEditorSnapshot();

    private void RegistraSnapshotServizio() => _servizioEditorSnapshot = CaptureServizioEditorSnapshot();

    private void RegistraSnapshotTariffeContabili() => _tariffeEditorSnapshot = CaptureTariffeEditorSnapshot();

    private string CapturePersonaleEditorSnapshot()
    {
        var builder = new StringBuilder();
        AppendSnapshot(builder, "PerIdInput", PerIdInput);
        AppendSnapshot(builder, "Cognome", Cognome);
        AppendSnapshot(builder, "Nome", Nome);
        AppendSnapshot(builder, "Qualifica", Qualifica);
        AppendSnapshot(builder, "DataDecorrenzaQualifica", DataDecorrenzaQualifica);
        AppendSnapshot(builder, "Profilo", ProfiloPersonale);
        AppendSnapshot(builder, "RuoloSanitario", RuoloSanitario);
        AppendSnapshot(builder, "CodiceFiscale", CodiceFiscale);
        AppendSnapshot(builder, "Matricola", MatricolaPersonale);
        AppendSnapshot(builder, "Brevetto", NumeroBrevettoSmz);
        AppendSnapshot(builder, "StatoServizio", StatoServizioPersonale);
        AppendSnapshot(builder, "DataFineServizio", DataFineServizio);
        AppendSnapshot(builder, "DataNascita", DataNascita);
        AppendSnapshot(builder, "LuogoNascita", LuogoNascita);
        AppendSnapshot(builder, "ViaResidenza", ViaResidenza);
        AppendSnapshot(builder, "CapResidenza", CapResidenza);
        AppendSnapshot(builder, "CittaResidenza", CittaResidenza);
        AppendSnapshot(builder, "Telefono1", Telefono1);
        AppendSnapshot(builder, "Telefono2", Telefono2);
        AppendSnapshot(builder, "Mail1Utente", Mail1Utente);
        AppendSnapshot(builder, "Mail2Utente", Mail2Utente);

        foreach (var row in Abilitazioni)
        {
            AppendSnapshot(
                builder,
                "Abilitazione",
                string.Join("|",
                    row.PersonaleAbilitazioneId?.ToString() ?? string.Empty,
                    row.TipoAbilitazioneId?.ToString() ?? string.Empty,
                    NormalizeSnapshotValue(row.TipoDescrizione),
                    NormalizeSnapshotValue(row.Livello),
                    NormalizeSnapshotValue(row.ProfonditaMetri),
                    NormalizeSnapshotValue(row.DataConseguimento),
                    NormalizeSnapshotValue(row.DataScadenza),
                    NormalizeSnapshotValue(row.Note)));
        }

        foreach (var row in VisiteMediche)
        {
            AppendSnapshot(
                builder,
                "Visita",
                string.Join("|",
                    row.VisitaMedicaId?.ToString() ?? string.Empty,
                    NormalizeSnapshotValue(row.TipoVisita),
                    NormalizeSnapshotValue(row.DataUltimaVisita),
                    NormalizeSnapshotValue(row.DataScadenza),
                    NormalizeSnapshotValue(row.Esito),
                    NormalizeSnapshotValue(row.Note)));
        }

        foreach (var row in Attagliamento)
        {
            AppendSnapshot(
                builder,
                "Attagliamento",
                string.Join("|",
                    row.PersonaleAttagliamentoId?.ToString() ?? string.Empty,
                    NormalizeSnapshotValue(row.Voce),
                    NormalizeSnapshotValue(row.TagliaMisura),
                    NormalizeSnapshotValue(row.Note)));
        }

        AppendSnapshot(builder, "AbilitazioneEditor", CaptureAbilitazioneEditorSnapshot());
        AppendSnapshot(builder, "VisitaEditor", CaptureVisitaEditorSnapshot());
        AppendSnapshot(builder, "AttagliamentoEditor", CaptureAttagliamentoEditorSnapshot());
        return builder.ToString();
    }

    private string CaptureServizioEditorSnapshot()
    {
        var builder = new StringBuilder();
        AppendSnapshot(builder, "ServizioId", _servizioGiornalieroId.ToString());
        AppendSnapshot(builder, "Data", ServizioData);
        AppendSnapshot(builder, "Ordine", ServizioNumeroOrdine);
        AppendSnapshot(builder, "Orario", BuildServizioOrarioValue());
        AppendSnapshot(builder, "OrarioFisso", ServizioOrarioFissoSelezionato);
        AppendSnapshot(builder, "OrarioDeroga", ServizioOrarioDerogaAttiva ? "1" : "0");
        AppendSnapshot(builder, "OrarioDerogaInizio", ServizioOrarioDerogaInizio);
        AppendSnapshot(builder, "OrarioDerogaFine", ServizioOrarioDerogaFine);
        AppendSnapshot(builder, "Tipo", ServizioTipoSelezionato);
        AppendSnapshot(builder, "Localita", ServizioLocalitaSelezionata?.LocalitaOperativaId.ToString() ?? string.Empty);
        AppendSnapshot(builder, "Scopo", ServizioScopoSelezionato?.ScopoImmersioneId.ToString() ?? string.Empty);
        AppendSnapshot(builder, "UnitaNavale", ServizioUnitaNavaleSelezionata?.UnitaNavaleId.ToString() ?? string.Empty);
        AppendSnapshot(builder, "Responsabile", GetPerIdOperatoreSelezionato(ServizioResponsabileSelezionato)?.ToString() ?? string.Empty);
        AppendSnapshot(builder, "FuoriSede", ServizioFuoriSede ? "1" : "0");
        AppendSnapshot(builder, "OrdinePubblico", ServizioIndennitaOrdinePubblico ? "1" : "0");
        AppendSnapshot(builder, "StraordinarioAttivo", ServizioStraordinarioAttivo ? "1" : "0");
        AppendSnapshot(builder, "StraordinarioInizio", ServizioStraordinarioInizio);
        AppendSnapshot(builder, "StraordinarioFine", ServizioStraordinarioFine);
        AppendSnapshot(builder, "Attivita", ServizioAttivitaSvolta);
        AppendSnapshot(builder, "Note", ServizioNote);

        foreach (var row in ServizioPartecipantiBozza)
        {
            AppendSnapshot(
                builder,
                "Partecipante",
                string.Join("|",
                    row.PerId.ToString(),
                    row.Presente ? "1" : "0",
                    row.GruppoOperativo?.GruppoOperativoId.ToString() ?? string.Empty,
                    row.RuoloOperativo?.RuoloOperativoId.ToString() ?? string.Empty,
                    NormalizeSnapshotValue(row.Note)));
        }

        foreach (var row in ServizioOperatoriSubEsterniBozza)
        {
            AppendSnapshot(
                builder,
                "OperatoreSubEsterno",
                string.Join("|",
                    NormalizeSnapshotValue(row.PerId),
                    NormalizeSnapshotValue(row.Qualifica),
                    NormalizeSnapshotValue(row.Nominativo),
                    NormalizeSnapshotValue(row.Reparto),
                    row.GruppoOperativo?.GruppoOperativoId.ToString() ?? string.Empty,
                    NormalizeSnapshotValue(row.Note)));
        }

        foreach (var immersione in ServizioImmersioniBozza)
        {
            AppendSnapshot(
                builder,
                "Immersione",
                string.Join("|",
                    immersione.NumeroImmersione.ToString(),
                    immersione.DirettoreImmersione?.PerId.ToString() ?? string.Empty,
                    immersione.OperatoreSoccorso?.PerId.ToString() ?? string.Empty,
                    immersione.AssistenteBlsd?.PerId.ToString() ?? string.Empty,
                    immersione.AssistenteSanitario?.PerId.ToString() ?? string.Empty,
                    NormalizeSnapshotValue(immersione.Note)));

            foreach (var partecipazione in immersione.Partecipazioni)
            {
                AppendSnapshot(
                    builder,
                    "PartecipazioneImmersione",
                    string.Join("|",
                        immersione.NumeroImmersione.ToString(),
                        partecipazione.PerId.ToString(),
                        partecipazione.IsEsterno ? "1" : "0",
                        partecipazione.InImmersione ? "1" : "0",
                        partecipazione.TipologiaImmersioneOperativa?.TipologiaImmersioneOperativaId.ToString() ?? string.Empty,
                        NormalizeSnapshotValue(partecipazione.ProfonditaMetri),
                        partecipazione.FasciaProfondita?.FasciaProfonditaId.ToString() ?? string.Empty,
                        NormalizeSnapshotValue(partecipazione.OreImmersione),
                        partecipazione.CategoriaContabileOre?.CategoriaContabileOreId.ToString() ?? string.Empty,
                        NormalizeSnapshotValue(partecipazione.Note)));
            }
        }

        foreach (var supporto in ServizioSupportiOccasionaliBozza)
        {
            AppendSnapshot(
                builder,
                "SupportoOccasionale",
                string.Join("|",
                    NormalizeSnapshotValue(supporto.Nominativo),
                    NormalizeSnapshotValue(supporto.Qualifica),
                    NormalizeSnapshotValue(supporto.Ruolo),
                    supporto.Presente ? "1" : "0",
                    NormalizeSnapshotValue(supporto.Contatti),
                    NormalizeSnapshotValue(supporto.Note)));
        }

        return builder.ToString();
    }

    private string CaptureTariffeEditorSnapshot()
    {
        var builder = new StringBuilder();

        foreach (var row in RegoleContabiliEditorItems)
        {
            AppendSnapshot(
                builder,
                "Tariffa",
                string.Join("|",
                    row.RegolaContabileImmersioneId.ToString(),
                    NormalizeSnapshotValue(row.Tariffa),
                    row.Attiva ? "1" : "0"));
        }

        return builder.ToString();
    }

    private string CaptureAbilitazioneEditorSnapshot()
    {
        if (SelectedAbilitazione is null)
        {
            return IsAbilitazioneEditorVuoto()
                ? string.Empty
                : string.Join("|",
                    "NEW",
                    AbilitazioneTipoSelezionato?.TipoAbilitazioneId.ToString() ?? string.Empty,
                    NormalizeSnapshotValue(AbilitazioneLivello),
                    NormalizeSnapshotValue(AbilitazioneProfondita),
                    NormalizeSnapshotValue(AbilitazioneDataConseguimento),
                    NormalizeSnapshotValue(AbilitazioneDataScadenza),
                    NormalizeSnapshotValue(AbilitazioneNote));
        }

        var tipoSelezionatoId = AbilitazioneTipoSelezionato?.TipoAbilitazioneId;
        var unchanged =
            tipoSelezionatoId == SelectedAbilitazione.TipoAbilitazioneId
            && string.Equals(NormalizeSnapshotValue(AbilitazioneLivello), NormalizeSnapshotValue(SelectedAbilitazione.Livello), StringComparison.Ordinal)
            && string.Equals(NormalizeSnapshotValue(AbilitazioneProfondita), NormalizeSnapshotValue(SelectedAbilitazione.ProfonditaMetri), StringComparison.Ordinal)
            && string.Equals(NormalizeSnapshotValue(AbilitazioneDataConseguimento), NormalizeSnapshotValue(SelectedAbilitazione.DataConseguimento), StringComparison.Ordinal)
            && string.Equals(NormalizeSnapshotValue(AbilitazioneDataScadenza), NormalizeSnapshotValue(SelectedAbilitazione.DataScadenza), StringComparison.Ordinal)
            && string.Equals(NormalizeSnapshotValue(AbilitazioneNote), NormalizeSnapshotValue(SelectedAbilitazione.Note), StringComparison.Ordinal);

        return unchanged
            ? string.Empty
            : string.Join("|",
                "EDIT",
                SelectedAbilitazione.PersonaleAbilitazioneId?.ToString() ?? string.Empty,
                tipoSelezionatoId?.ToString() ?? string.Empty,
                NormalizeSnapshotValue(AbilitazioneLivello),
                NormalizeSnapshotValue(AbilitazioneProfondita),
                NormalizeSnapshotValue(AbilitazioneDataConseguimento),
                NormalizeSnapshotValue(AbilitazioneDataScadenza),
                NormalizeSnapshotValue(AbilitazioneNote));
    }

    private string CaptureVisitaEditorSnapshot()
    {
        if (SelectedVisita is null)
        {
            return IsVisitaEditorVuoto()
                ? string.Empty
                : string.Join("|",
                    "NEW",
                    NormalizeSnapshotValue(VisitaTipoSelezionato?.Descrizione),
                    NormalizeSnapshotValue(VisitaDataUltimaVisita),
                    NormalizeSnapshotValue(VisitaEsito),
                    NormalizeSnapshotValue(VisitaNote));
        }

        var unchanged =
            string.Equals(NormalizeSnapshotValue(VisitaTipoSelezionato?.Descrizione), NormalizeSnapshotValue(SelectedVisita.TipoVisita), StringComparison.Ordinal)
            && string.Equals(NormalizeSnapshotValue(VisitaDataUltimaVisita), NormalizeSnapshotValue(SelectedVisita.DataUltimaVisita), StringComparison.Ordinal)
            && string.Equals(NormalizeSnapshotValue(VisitaEsito), NormalizeSnapshotValue(SelectedVisita.Esito), StringComparison.Ordinal)
            && string.Equals(NormalizeSnapshotValue(VisitaNote), NormalizeSnapshotValue(SelectedVisita.Note), StringComparison.Ordinal);

        return unchanged
            ? string.Empty
            : string.Join("|",
                "EDIT",
                SelectedVisita.VisitaMedicaId?.ToString() ?? string.Empty,
                NormalizeSnapshotValue(VisitaTipoSelezionato?.Descrizione),
                NormalizeSnapshotValue(VisitaDataUltimaVisita),
                NormalizeSnapshotValue(VisitaEsito),
                NormalizeSnapshotValue(VisitaNote));
    }

    private string CaptureAttagliamentoEditorSnapshot()
    {
        if (SelectedAttagliamento is null)
        {
            return IsAttagliamentoEditorVuoto()
                ? string.Empty
                : string.Join("|",
                    "NEW",
                    NormalizeSnapshotValue(AttagliamentoVoce),
                    NormalizeSnapshotValue(AttagliamentoTagliaMisura),
                    NormalizeSnapshotValue(AttagliamentoNote));
        }

        var unchanged =
            string.Equals(NormalizeSnapshotValue(AttagliamentoVoce), NormalizeSnapshotValue(SelectedAttagliamento.Voce), StringComparison.Ordinal)
            && string.Equals(NormalizeSnapshotValue(AttagliamentoTagliaMisura), NormalizeSnapshotValue(SelectedAttagliamento.TagliaMisura), StringComparison.Ordinal)
            && string.Equals(NormalizeSnapshotValue(AttagliamentoNote), NormalizeSnapshotValue(SelectedAttagliamento.Note), StringComparison.Ordinal);

        return unchanged
            ? string.Empty
            : string.Join("|",
                "EDIT",
                SelectedAttagliamento.PersonaleAttagliamentoId?.ToString() ?? string.Empty,
                NormalizeSnapshotValue(AttagliamentoVoce),
                NormalizeSnapshotValue(AttagliamentoTagliaMisura),
                NormalizeSnapshotValue(AttagliamentoNote));
    }

    private bool IsAbilitazioneEditorVuoto() =>
        AbilitazioneTipoSelezionato is null
        && string.IsNullOrWhiteSpace(AbilitazioneLivello)
        && string.IsNullOrWhiteSpace(AbilitazioneProfondita)
        && string.IsNullOrWhiteSpace(AbilitazioneDataConseguimento)
        && string.IsNullOrWhiteSpace(AbilitazioneDataScadenza)
        && string.IsNullOrWhiteSpace(AbilitazioneNote);

    private bool IsVisitaEditorVuoto() =>
        VisitaTipoSelezionato is null
        && string.IsNullOrWhiteSpace(VisitaDataUltimaVisita)
        && string.IsNullOrWhiteSpace(VisitaEsito)
        && string.IsNullOrWhiteSpace(VisitaNote);

    private bool IsAttagliamentoEditorVuoto() =>
        string.IsNullOrWhiteSpace(AttagliamentoVoce)
        && string.IsNullOrWhiteSpace(AttagliamentoTagliaMisura)
        && string.IsNullOrWhiteSpace(AttagliamentoNote);

    private static void AppendSnapshot(StringBuilder builder, string key, string? value)
    {
        builder.Append(key);
        builder.Append('=');
        builder.AppendLine(NormalizeSnapshotValue(value));
    }

    private static string NormalizeSnapshotValue(string? value) => value?.Trim() ?? string.Empty;

    private static void SostituisciCollection<T>(ObservableCollection<T> collection, IEnumerable<T> source)
    {
        collection.Clear();
        foreach (var item in source)
        {
            collection.Add(item);
        }
    }

    private static string FormatBackupInfo(BackupInfo? info, string fallback, string prefix)
    {
        if (info is null)
        {
            return fallback;
        }

        var sizeMb = info.SizeBytes / 1024d / 1024d;
        return $"{prefix}: {info.CreatedAtLocal:dd/MM/yyyy HH:mm} - {info.FileName} ({sizeMb:0.##} MB)";
    }

    private int ContaScadenzeScheda()
    {
        var totale = Abilitazioni.Count(item => TryParseDate(item.DataScadenza) is not null);
        totale += VisiteMediche.Count(item => TryParseDate(item.DataScadenza) is not null);
        return totale;
    }

    private int ContaScaduteScheda()
    {
        var oggi = DateOnly.FromDateTime(DateTime.Today);
        var totale = Abilitazioni.Count(item => TryParseDate(item.DataScadenza) is DateOnly data && data < oggi);
        totale += VisiteMediche.Count(item => TryParseDate(item.DataScadenza) is DateOnly data && data < oggi);
        return totale;
    }

    private string BuildScaduteHighlight()
    {
        var scadute = GetScadenzeScaduteScheda();
        if (scadute.Count == 0)
        {
            return "Nessuna voce da regolarizzare";
        }

        var prima = scadute[0];
        return $"{prima.origine}: {prima.titolo}";
    }

    private string BuildScaduteDettaglio()
    {
        var scadute = GetScadenzeScaduteScheda();
        if (scadute.Count == 0)
        {
            return "Situazione regolare";
        }

        var prima = scadute[0];
        var dettaglio = $"Scaduta il {prima.data:dd/MM/yyyy}";

        return scadute.Count == 1
            ? dettaglio
            : $"{dettaglio} | +{scadute.Count - 1} altre";
    }

    private string BuildAbilitazioniPrincipali()
    {
        var principali = Abilitazioni
            .OrderBy(item =>
            {
                var categoria = item.Categoria ?? string.Empty;
                return categoria switch
                {
                    "Subacquea" => 0,
                    "Sanitaria" => 1,
                    "Nautica" => 2,
                    _ => 3,
                };
            })
            .ThenBy(item => string.IsNullOrWhiteSpace(item.DataScadenza) ? 1 : 0)
            .ThenBy(item => item.TipoDescrizione)
            .Take(3)
            .Select(item =>
            {
                var dettagli = new List<string>();

                if (!string.IsNullOrWhiteSpace(item.Livello))
                {
                    dettagli.Add(item.Livello);
                }

                return dettagli.Count == 0
                    ? item.TipoDescrizione
                    : $"{item.TipoDescrizione} ({string.Join(", ", dettagli)})";
            })
            .ToList();

        return principali.Count == 0 ? "Nessuna abilitazione registrata" : string.Join("\n", principali);
    }

    private void AllineaVisitePredefinite()
    {
        var visiteEsistenti = VisiteMediche
            .GroupBy(item => item.TipoVisita.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var tipoSelezionato = SelectedVisita?.TipoVisita;
        var righe = new List<VisitaMedicaRowViewModel>();

        foreach (var tipo in TipiVisitaMedicaCatalogo)
        {
            visiteEsistenti.TryGetValue(tipo.Descrizione, out var esistente);
            var dataUltimaVisita = esistente?.DataUltimaVisita ?? string.Empty;
            var dataScadenza = CalcolaScadenzaVisita(tipo.Descrizione, TryParseDate(dataUltimaVisita));

            righe.Add(new VisitaMedicaRowViewModel
            {
                VisitaMedicaId = esistente?.VisitaMedicaId,
                TipoVisita = tipo.Descrizione,
                DataUltimaVisita = dataUltimaVisita,
                DataScadenza = FormatDate(dataScadenza),
                Esito = esistente?.Esito ?? string.Empty,
                Note = esistente?.Note ?? string.Empty,
            });
        }

        VisiteMediche.Clear();
        foreach (var riga in righe)
        {
            VisiteMediche.Add(riga);
        }

        SelectedVisita = VisiteMediche.FirstOrDefault(item => string.Equals(item.TipoVisita, tipoSelezionato, StringComparison.OrdinalIgnoreCase))
            ?? VisiteMediche.FirstOrDefault();
    }

    private (DateOnly data, string origine, string titolo)? CalcolaProssimaScadenzaScheda()
    {
        var oggi = DateOnly.FromDateTime(DateTime.Today);
        var voci = new List<(DateOnly data, string origine, string titolo)>();

        foreach (var abilitazione in Abilitazioni)
        {
            var data = TryParseDate(abilitazione.DataScadenza);
            if (data is not null && data.Value >= oggi)
            {
                voci.Add((data.Value, "Abilitazione", abilitazione.TipoDescrizione));
            }
        }

        foreach (var visita in VisiteMediche)
        {
            var data = TryParseDate(visita.DataScadenza);
            if (data is not null && data.Value >= oggi)
            {
                voci.Add((data.Value, "Visita", visita.TipoVisita));
            }
        }

        return voci.Count == 0 ? null : voci.OrderBy(voce => voce.data).First();
    }

    private List<(DateOnly data, string origine, string titolo)> GetScadenzeScaduteScheda()
    {
        var oggi = DateOnly.FromDateTime(DateTime.Today);
        var voci = new List<(DateOnly data, string origine, string titolo)>();

        foreach (var abilitazione in Abilitazioni)
        {
            var data = TryParseDate(abilitazione.DataScadenza);
            if (data is not null && data.Value < oggi)
            {
                voci.Add((data.Value, "Abilitazione", abilitazione.TipoDescrizione));
            }
        }

        foreach (var visita in VisiteMediche)
        {
            var data = TryParseDate(visita.DataScadenza);
            if (data is not null && data.Value < oggi)
            {
                voci.Add((data.Value, "Visita", visita.TipoVisita));
            }
        }

        return voci
            .OrderByDescending(voce => voce.data)
            .ToList();
    }

    private int ParseRequiredPerId()
    {
        if (string.IsNullOrWhiteSpace(PerIdInput))
        {
            throw new InvalidOperationException("Il PerID e obbligatorio.");
        }

        if (!int.TryParse(PerIdInput, out var perId) || perId <= 0)
        {
            throw new InvalidOperationException("Il PerID deve essere un numero intero positivo.");
        }

        if (IsExistingPerson && perId != PerId)
        {
            throw new InvalidOperationException("Il PerID di una scheda esistente non puo essere modificato.");
        }

        return perId;
    }

    private static DateOnly? TryParseDate(string value)
    {
        return DateOnly.TryParse(value, out var parsed) ? parsed : null;
    }

    private static (int Month, int Year)? TryParseMonthFilter(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalizedValue = value.Trim().Replace('-', '/').Replace('.', '/');
        var parts = normalizedValue.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 2
            && int.TryParse(parts[0], out var month)
            && int.TryParse(parts[1], out var year)
            && month is >= 1 and <= 12
            && year is >= 1900 and <= 9999)
        {
            return (month, year);
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length == 6
            && int.TryParse(digits[..2], out month)
            && int.TryParse(digits[2..], out year)
            && month is >= 1 and <= 12
            && year is >= 1900 and <= 9999)
        {
            return (month, year);
        }

        return null;
    }

    private static string Csv(object? value)
    {
        var text = value?.ToString() ?? string.Empty;
        if (text.Contains('"'))
        {
            text = text.Replace("\"", "\"\"");
        }

        return text.Contains(';') || text.Contains('"') || text.Contains('\n') || text.Contains('\r')
            ? $"\"{text}\""
            : text;
    }

    private static string GetApplicationVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        if (version is null)
        {
            return "1.0.0";
        }

        var build = version.Build >= 0 ? version.Build : 0;
        return $"{version.Major}.{version.Minor}.{build}";
    }
}
