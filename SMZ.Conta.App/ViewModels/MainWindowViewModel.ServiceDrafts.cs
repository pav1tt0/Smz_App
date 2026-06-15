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
    private void InizializzaBozzaServizio(bool preserveSelections)
    {
        var dataServizioOperativa = TryParseDate(ServizioData) ?? DateOnly.FromDateTime(DateTime.Today);
        var personaleAttivo = _repository
            .SearchPersonale(string.Empty, null, null)
            .Where(item => item.IsUtilizzabileInData(dataServizioOperativa))
            .OrderBy(item => QualificaFormatter.GetGerarchiaOrdine(item.Qualifica, item.IsProfiloSanitario, item.RuoloSanitario))
            .ThenBy(item => GetOrdineDecorrenzaQualifica(item.DataDecorrenzaQualifica))
            .ThenBy(item => item.Cognome)
            .ThenBy(item => item.Nome)
            .ToList();

        var selezioniEsistenti = preserveSelections
            ? ServizioPartecipantiBozza.ToDictionary(item => item.PerId)
            : new Dictionary<int, ServizioPartecipanteDraftViewModel>();

        OperatoriServizioDisponibili.Clear();
        OperatoriServizioDisponibili.Add(OperatoreVuoto);
        foreach (var personale in personaleAttivo)
        {
            OperatoriServizioDisponibili.Add(PersonaleListItemViewModel.FromModel(personale));
        }

        ServizioPartecipantiBozza.Clear();
        foreach (var personale in personaleAttivo)
        {
            selezioniEsistenti.TryGetValue(personale.PerId, out var esistente);
            var defaultGruppoOperativoId = personale.IsProfiloSanitario ? 3 : 1;
            int? defaultRuoloOperativoId = personale.IsProfiloSanitario ? 3 : null;

            var item = new ServizioPartecipanteDraftViewModel
            {
                PerId = personale.PerId,
                Qualifica = personale.Qualifica,
                DataDecorrenzaQualifica = personale.DataDecorrenzaQualifica,
                Nominativo = personale.NominativoCompleto,
                ProfiloPersonale = personale.ProfiloPersonale,
                RuoloSanitario = personale.RuoloSanitario,
                Contatti = personale.ContattiSintesi,
                Presente = esistente?.Presente ?? false,
                DefaultGruppoOperativoId = defaultGruppoOperativoId,
                DefaultRuoloOperativoId = defaultRuoloOperativoId,
                GruppoOperativo = TrovaGruppoOperativo(esistente?.GruppoOperativo?.GruppoOperativoId)
                    ?? TrovaGruppoOperativo(defaultGruppoOperativoId),
                RuoloOperativo = TrovaRuoloOperativo(esistente?.RuoloOperativo?.RuoloOperativoId)
                    ?? TrovaRuoloOperativo(defaultRuoloOperativoId),
                Note = esistente?.Note ?? string.Empty,
            };

            item.PropertyChanged += ServizioPartecipanteBozza_PropertyChanged;
            ServizioPartecipantiBozza.Add(item);
        }

        AggiornaOperatoriServizioPresentiDisponibili();

        if (ServizioImmersioniBozza.Count == 0)
        {
            CreaImmersioniBozzaDefault();
        }
        else if (preserveSelections)
        {
            foreach (var immersione in ServizioImmersioniBozza)
            {
                immersione.DirettoreImmersione = TrovaOperatoreServizio(immersione.DirettoreImmersione?.PerId);
                immersione.OperatoreSoccorso = TrovaOperatoreServizio(immersione.OperatoreSoccorso?.PerId);
                immersione.AssistenteBlsd = TrovaOperatoreServizio(immersione.AssistenteBlsd?.PerId);
                immersione.AssistenteSanitario = TrovaOperatoreServizio(immersione.AssistenteSanitario?.PerId);
            }
        }

        SincronizzaPartecipazioniImmersioneBozza();
        AggiornaRiepilogoBozzaServizio();
    }

    private ServizioImmersioneDraftViewModel CreaImmersioneBozza(int numeroImmersione)
    {
        var item = new ServizioImmersioneDraftViewModel { NumeroImmersione = numeroImmersione };
        item.PropertyChanged += ServizioImmersioneBozza_PropertyChanged;
        return item;
    }

    private void CreaImmersioniBozzaDefault()
    {
        ServizioImmersioniBozza.Add(CreaImmersioneBozza(1));
        ServizioImmersioniBozza.Add(CreaImmersioneBozza(2));
    }

    private void AggiungiImmersione()
    {
        var numeroImmersione = ServizioImmersioniBozza.Count == 0
            ? 1
            : ServizioImmersioniBozza.Max(item => item.NumeroImmersione) + 1;

        ServizioImmersioniBozza.Add(CreaImmersioneBozza(numeroImmersione));
        SincronizzaPartecipazioniImmersioneBozza();
        AggiornaRiepilogoBozzaServizio();
        Stato = $"Immersione {numeroImmersione} aggiunta.";
    }

    private static void PulisciDettaglioContabileImmersione(ServizioPartecipanteImmersioneDraftViewModel partecipazione)
    {
        partecipazione.TipologiaImmersioneOperativa = null;
        partecipazione.ProfonditaMetri = string.Empty;
        partecipazione.FasciaProfondita = null;
        partecipazione.OreImmersione = string.Empty;
        partecipazione.CategoriaContabileOre = null;
        partecipazione.TariffaProposta = null;
        partecipazione.ImportoStimato = null;
        partecipazione.Note = string.Empty;
    }

    private void RimuoviImmersione(object? parameter)
    {
        if (parameter is not ServizioImmersioneDraftViewModel immersione || ServizioImmersioniBozza.Count <= 1)
        {
            Stato = "Mantieni almeno una immersione nella bozza del servizio.";
            return;
        }

        immersione.PropertyChanged -= ServizioImmersioneBozza_PropertyChanged;
        foreach (var partecipazione in immersione.Partecipazioni)
        {
            partecipazione.PropertyChanged -= ServizioPartecipazioneImmersione_PropertyChanged;
        }

        ServizioImmersioniBozza.Remove(immersione);
        RinumeraImmersioniBozza();
        SincronizzaPartecipazioniImmersioneBozza();
        AggiornaRiepilogoBozzaServizio();
        Stato = "Immersione rimossa.";
    }

    private void RinumeraImmersioniBozza()
    {
        var numero = 1;
        foreach (var immersione in ServizioImmersioniBozza.OrderBy(item => item.NumeroImmersione))
        {
            immersione.NumeroImmersione = numero++;
        }
    }

    private void SincronizzaPartecipazioniImmersioneBozza()
    {
        var operatoriSubPresenti = GetOperatoriSubPresentiOrdinati();

        foreach (var immersione in ServizioImmersioniBozza)
        {
            var existing = immersione.Partecipazioni.ToDictionary(item => item.PerId);
            var orderedRows = new List<ServizioPartecipanteImmersioneDraftViewModel>();
            var direttorePerId = GetPerIdOperatoreSelezionato(immersione.DirettoreImmersione);

            foreach (var operatore in operatoriSubPresenti)
            {
                if (!operatore.IsEsterno && direttorePerId == operatore.PerId)
                {
                    continue;
                }

                if (existing.TryGetValue(operatore.PerId, out var row))
                {
                    row.NumeroImmersione = immersione.NumeroImmersione;
                    row.Qualifica = operatore.Qualifica;
                    row.DataDecorrenzaQualifica = operatore.DataDecorrenzaQualifica;
                    row.Nominativo = operatore.Nominativo;
                    row.IsEsterno = operatore.IsEsterno;
                    row.Reparto = operatore.Reparto;
                    orderedRows.Add(row);
                    continue;
                }

                row = new ServizioPartecipanteImmersioneDraftViewModel
                {
                    NumeroImmersione = immersione.NumeroImmersione,
                    PerId = operatore.PerId,
                    Qualifica = operatore.Qualifica,
                    DataDecorrenzaQualifica = operatore.DataDecorrenzaQualifica,
                    Nominativo = operatore.Nominativo,
                    IsEsterno = operatore.IsEsterno,
                    Reparto = operatore.Reparto,
                };
                row.PropertyChanged += ServizioPartecipazioneImmersione_PropertyChanged;
                orderedRows.Add(row);
            }

            foreach (var row in immersione.Partecipazioni.ToList())
            {
                if (orderedRows.Any(item => item.PerId == row.PerId))
                {
                    continue;
                }

                row.PropertyChanged -= ServizioPartecipazioneImmersione_PropertyChanged;
            }

            immersione.Partecipazioni.Clear();
            foreach (var row in orderedRows)
            {
                immersione.Partecipazioni.Add(row);
                AggiornaCalcoliPartecipazioneImmersione(row);
            }

            AggiornaStatoCondivisioneValoriImmersione(immersione);
        }

        OnPropertyChanged(nameof(ServizioPartecipazioniContabiliBozza));
        SincronizzaPartecipazioniContabiliUnicheBozza(aggiornaDaPartecipazioni: false);
    }

    private List<PersonaleListItemViewModel> GetOperatoriSmzPresentiOrdinati() =>
        ServizioPartecipantiBozza
            .Where(item => item.Presente)
            .Select(item => TrovaOperatoreServizio(item.PerId))
            .Where(item => item is not null && !ProfiliPersonaleCatalogo.IsSanitario(item.ProfiloPersonale))
            .Cast<PersonaleListItemViewModel>()
            .OrderBy(item => QualificaFormatter.GetGerarchiaOrdine(item.Qualifica, item.IsProfiloSanitario, item.RuoloSanitario))
            .ThenBy(item => GetOrdineDecorrenzaQualifica(item.DataDecorrenzaQualifica))
            .ThenBy(item => item.Cognome)
            .ThenBy(item => item.Nome)
            .ToList();

    private List<OperatoreSubServizioDraft> GetOperatoriSubPresentiOrdinati()
    {
        var interni = GetOperatoriSmzPresentiOrdinati()
            .Select(item => new OperatoreSubServizioDraft(
                item.PerId,
                item.Qualifica,
                item.DataDecorrenzaQualifica,
                item.Nominativo,
                IsEsterno: false,
                Reparto: string.Empty,
                Ordine: QualificaFormatter.GetGerarchiaOrdine(item.Qualifica, item.IsProfiloSanitario, item.RuoloSanitario),
                CognomeNome: $"{item.Cognome} {item.Nome}".Trim()));

        var esterni = ServizioOperatoriSubEsterniBozza
            .Select(item => (item, perId: ParseIntSilenzioso(item.PerId)))
            .Where(item => item.perId is > 0 && IsOperatoreSubEsternoCompilato(item.item))
            .Select(item => new OperatoreSubServizioDraft(
                item.perId!.Value,
                item.item.Qualifica,
                null,
                item.item.Nominativo.Trim(),
                IsEsterno: true,
                Reparto: item.item.Reparto.Trim(),
                Ordine: QualificaFormatter.GetGerarchiaOrdine(item.item.Qualifica, isSanitario: false, ruoloSanitario: string.Empty) + 10_000,
                CognomeNome: item.item.Nominativo.Trim()));

        return interni
            .Concat(esterni)
            .OrderBy(item => item.Ordine)
            .ThenBy(item => item.IsEsterno)
            .ThenBy(item => item.CognomeNome)
            .ToList();
    }

    private void SincronizzaPartecipazioniContabiliUnicheBozza(bool aggiornaDaPartecipazioni)
    {
        if (_isSyncingPartecipazioniUniche)
        {
            return;
        }

        _isSyncingPartecipazioniUniche = true;
        try
        {
            var existing = ServizioPartecipazioniContabiliUnicheBozza.ToDictionary(item => item.PerId);
            var operatoriSmzPresenti = GetOperatoriSubPresentiOrdinati();
            var orderedRows = new List<ServizioPartecipanteImmersioneUnicoDraftViewModel>();

            foreach (var operatore in operatoriSmzPresenti)
            {
                if (!existing.TryGetValue(operatore.PerId, out var row))
                {
                    row = new ServizioPartecipanteImmersioneUnicoDraftViewModel { PerId = operatore.PerId };
                    row.PropertyChanged += ServizioPartecipazioneUnicaImmersione_PropertyChanged;
                }

                row.Qualifica = operatore.Qualifica;
                row.DataDecorrenzaQualifica = operatore.DataDecorrenzaQualifica;
                row.Nominativo = operatore.Nominativo;
                row.IsEsterno = operatore.IsEsterno;
                row.Reparto = operatore.Reparto;
                row.RuoliImmersione = operatore.IsEsterno ? "Altro reparto" : BuildRuoliImmersioneDisplay(operatore.PerId);
                if (aggiornaDaPartecipazioni)
                {
                    PopolaRigaUnicaDaPartecipazioni(row);
                }

                orderedRows.Add(row);
            }

            foreach (var row in ServizioPartecipazioniContabiliUnicheBozza.ToList())
            {
                if (orderedRows.Any(item => item.PerId == row.PerId))
                {
                    continue;
                }

                row.PropertyChanged -= ServizioPartecipazioneUnicaImmersione_PropertyChanged;
            }

            ServizioPartecipazioniContabiliUnicheBozza.Clear();
            foreach (var row in orderedRows)
            {
                ServizioPartecipazioniContabiliUnicheBozza.Add(row);
            }
        }
        finally
        {
            _isSyncingPartecipazioniUniche = false;
        }

        if (aggiornaDaPartecipazioni)
        {
            AggiornaImportiRigheUniche();
        }
        else
        {
            ApplicaPartecipazioniDaRigheUniche();
        }
    }

    private void PopolaRigaUnicaDaPartecipazioni(ServizioPartecipanteImmersioneUnicoDraftViewModel row)
    {
        var partecipazione = ServizioImmersioniBozza
            .OrderBy(item => item.NumeroImmersione)
            .SelectMany(item => item.Partecipazioni)
            .FirstOrDefault(item => item.PerId == row.PerId && item.InImmersione);
        if (partecipazione is null)
        {
            return;
        }

        row.TipologiaImmersioneOperativa = partecipazione.TipologiaImmersioneOperativa;
        row.ProfonditaMetri = partecipazione.ProfonditaMetri;
        row.FasciaProfondita = partecipazione.FasciaProfondita;
        row.OreImmersione = partecipazione.OreImmersione;
        row.CategoriaContabileOre = partecipazione.CategoriaContabileOre;
        row.Note = partecipazione.Note;
    }

    private string BuildRuoliImmersioneDisplay(int perId)
    {
        var ruoli = new List<string>();
        foreach (var immersione in ServizioImmersioniBozza.OrderBy(item => item.NumeroImmersione))
        {
            AggiungiRuoloImmersioneSelezionato(ruoli, immersione, perId, immersione.DirettoreImmersione, "Dir.");
            AggiungiRuoloImmersioneSelezionato(ruoli, immersione, perId, immersione.OperatoreSoccorso, "Soccorso");
            AggiungiRuoloImmersioneSelezionato(ruoli, immersione, perId, immersione.AssistenteBlsd, "BLSD");
            AggiungiRuoloImmersioneSelezionato(ruoli, immersione, perId, immersione.AssistenteSanitario, "San.");
        }

        return string.Join(", ", ruoli);
    }

    private static void AggiungiRuoloImmersioneSelezionato(
        ICollection<string> ruoli,
        ServizioImmersioneDraftViewModel immersione,
        int perId,
        PersonaleListItemViewModel? operatore,
        string ruolo)
    {
        if (GetPerIdOperatoreSelezionato(operatore) == perId)
        {
            ruoli.Add($"Imm. {immersione.NumeroImmersione}: {ruolo}");
        }
    }

    private void ApplicaPartecipazioniDaRigheUniche()
    {
        if (_isSyncingPartecipazioniUniche)
        {
            return;
        }

        _isSyncingPartecipazioniUniche = true;
        _isSyncingValoriCondivisiImmersione = true;
        try
        {
            var righeUniche = ServizioPartecipazioniContabiliUnicheBozza.ToDictionary(item => item.PerId);
            var immersioneUtileByPerId = righeUniche.Keys.ToDictionary(
                perId => perId,
                TrovaPrimaImmersioneUtilePerOperatore);
            foreach (var immersione in ServizioImmersioniBozza)
            {
                foreach (var partecipazione in immersione.Partecipazioni)
                {
                    if (!righeUniche.TryGetValue(partecipazione.PerId, out var rigaUnica)
                        || !ReferenceEquals(immersioneUtileByPerId[partecipazione.PerId], immersione)
                        || !IsRigaUnicaContabileCompilata(rigaUnica))
                    {
                        partecipazione.InImmersione = false;
                        PulisciDettaglioContabileImmersione(partecipazione);
                        AggiornaCalcoliPartecipazioneImmersione(partecipazione);
                        continue;
                    }

                    partecipazione.InImmersione = true;
                    partecipazione.TipologiaImmersioneOperativa = rigaUnica.TipologiaImmersioneOperativa;
                    partecipazione.ProfonditaMetri = rigaUnica.ProfonditaMetri;
                    partecipazione.FasciaProfondita = rigaUnica.FasciaProfondita;
                    partecipazione.OreImmersione = rigaUnica.OreImmersione;
                    partecipazione.CategoriaContabileOre = rigaUnica.CategoriaContabileOre;
                    partecipazione.Note = rigaUnica.Note;
                    AggiornaFasciaDaProfondita(partecipazione);
                    AggiornaCalcoliPartecipazioneImmersione(partecipazione);
                }

                AggiornaStatoCondivisioneValoriImmersione(immersione);
            }

            AggiornaImportiRigheUniche();
        }
        finally
        {
            _isSyncingValoriCondivisiImmersione = false;
            _isSyncingPartecipazioniUniche = false;
        }

        OnPropertyChanged(nameof(ServizioPartecipazioniContabiliBozza));
        AggiornaRiepilogoBozzaServizio();
    }

    private ServizioImmersioneDraftViewModel? TrovaPrimaImmersioneUtilePerOperatore(int perId) =>
        ServizioImmersioniBozza
            .OrderBy(item => item.NumeroImmersione)
            .FirstOrDefault(item => !IsOperatoreInRuoloImmersione(item, perId));

    private void AggiornaImportiRigheUniche()
    {
        foreach (var row in ServizioPartecipazioniContabiliUnicheBozza)
        {
            var partecipazioni = ServizioImmersioniBozza
                .SelectMany(item => item.Partecipazioni)
                .Where(item => item.PerId == row.PerId && item.InImmersione)
                .ToList();

            row.TariffaProposta = partecipazioni
                .Select(item => item.TariffaProposta)
                .FirstOrDefault(item => item is not null);
            row.ImportoStimato = partecipazioni.Sum(item => item.ImportoStimato ?? 0m);
            if (row.ImportoStimato == 0m && partecipazioni.All(item => item.ImportoStimato is null))
            {
                row.ImportoStimato = null;
            }
        }
    }

    private static bool IsRigaUnicaContabileCompilata(ServizioPartecipanteImmersioneUnicoDraftViewModel row) =>
        row.TipologiaImmersioneOperativa is not null
        || !string.IsNullOrWhiteSpace(row.ProfonditaMetri)
        || row.FasciaProfondita is not null
        || !string.IsNullOrWhiteSpace(row.OreImmersione)
        || row.CategoriaContabileOre is not null
        || !string.IsNullOrWhiteSpace(row.Note);

    private static bool IsOperatoreInRuoloImmersione(ServizioImmersioneDraftViewModel immersione, int perId) =>
        GetPerIdOperatoreSelezionato(immersione.DirettoreImmersione) == perId
        || GetPerIdOperatoreSelezionato(immersione.OperatoreSoccorso) == perId
        || GetPerIdOperatoreSelezionato(immersione.AssistenteBlsd) == perId
        || GetPerIdOperatoreSelezionato(immersione.AssistenteSanitario) == perId;

    private void FlaggaTuttiImmersione(object? parameter)
    {
        if (parameter is not ServizioImmersioneDraftViewModel immersione)
        {
            return;
        }

        var impostaInImmersione = immersione.Partecipazioni.Any(item => !item.InImmersione);
        foreach (var partecipazione in immersione.Partecipazioni)
        {
            partecipazione.InImmersione = impostaInImmersione;
        }
    }

    private void SincronizzaValoriCondivisiImmersione(
        ServizioImmersioneDraftViewModel immersione,
        ServizioPartecipanteImmersioneDraftViewModel? origine = null,
        bool sincronizzaProfondita = true,
        bool sincronizzaOre = true)
    {
        if (_isSyncingValoriCondivisiImmersione)
        {
            return;
        }

        var righeInImmersione = immersione.Partecipazioni
            .Where(item => item.InImmersione)
            .ToList();

        if (righeInImmersione.Count <= 1)
        {
            return;
        }

        var profonditaCondivisa = sincronizzaProfondita
            && !immersione.ProfonditaCondivisaInizializzata
            ? TrovaValoreCondivisoImmersione(
                origine?.ProfonditaMetri,
                righeInImmersione.Select(item => item.ProfonditaMetri))
            : string.Empty;
        var oreCondivise = sincronizzaOre
            && !immersione.OreCondiviseInizializzate
            ? TrovaValoreCondivisoImmersione(
                origine?.OreImmersione,
                righeInImmersione.Select(item => item.OreImmersione))
            : string.Empty;

        if (string.IsNullOrWhiteSpace(profonditaCondivisa) && string.IsNullOrWhiteSpace(oreCondivise))
        {
            return;
        }

        _isSyncingValoriCondivisiImmersione = true;
        try
        {
            foreach (var partecipazione in righeInImmersione)
            {
                if (sincronizzaProfondita
                    && !string.IsNullOrWhiteSpace(profonditaCondivisa)
                    && !string.Equals(partecipazione.ProfonditaMetri, profonditaCondivisa, StringComparison.Ordinal))
                {
                    partecipazione.ProfonditaMetri = profonditaCondivisa;
                }

                if (sincronizzaOre
                    && !string.IsNullOrWhiteSpace(oreCondivise)
                    && !string.Equals(partecipazione.OreImmersione, oreCondivise, StringComparison.Ordinal))
                {
                    partecipazione.OreImmersione = oreCondivise;
                }
            }
        }
        finally
        {
            _isSyncingValoriCondivisiImmersione = false;
        }

        if (sincronizzaProfondita && !string.IsNullOrWhiteSpace(profonditaCondivisa))
        {
            immersione.ProfonditaCondivisaInizializzata = true;
        }

        if (sincronizzaOre && !string.IsNullOrWhiteSpace(oreCondivise))
        {
            immersione.OreCondiviseInizializzate = true;
        }
    }

    private static string TrovaValoreCondivisoImmersione(string? valoreOrigine, IEnumerable<string> valori)
    {
        if (!string.IsNullOrWhiteSpace(valoreOrigine))
        {
            return valoreOrigine.Trim();
        }

        return valori.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item))?.Trim() ?? string.Empty;
    }

    private static void AggiornaStatoCondivisioneValoriImmersione(ServizioImmersioneDraftViewModel immersione)
    {
        if (!immersione.Partecipazioni.Any(item => item.InImmersione && !string.IsNullOrWhiteSpace(item.ProfonditaMetri)))
        {
            immersione.ProfonditaCondivisaInizializzata = false;
        }

        if (!immersione.Partecipazioni.Any(item => item.InImmersione && !string.IsNullOrWhiteSpace(item.OreImmersione)))
        {
            immersione.OreCondiviseInizializzate = false;
        }
    }

    private void AggiornaFasciaDaProfondita(ServizioPartecipanteImmersioneDraftViewModel row)
    {
        if (!int.TryParse(row.ProfonditaMetri, out var profondita))
        {
            row.FasciaProfondita = null;
            return;
        }

        if (!ProfonditaRientraNellIntervallo(row.TipologiaImmersioneOperativa, profondita))
        {
            row.FasciaProfondita = null;
            return;
        }

        row.FasciaProfondita = FasceProfonditaCatalogo
            .FirstOrDefault(item => profondita >= item.MetriDa && profondita <= item.MetriA);
    }

    private void AggiornaFasciaDaProfondita(ServizioPartecipanteImmersioneUnicoDraftViewModel row)
    {
        if (!int.TryParse(row.ProfonditaMetri, out var profondita))
        {
            row.FasciaProfondita = null;
            return;
        }

        if (!ProfonditaRientraNellIntervallo(row.TipologiaImmersioneOperativa, profondita))
        {
            row.FasciaProfondita = null;
            return;
        }

        row.FasciaProfondita = FasceProfonditaCatalogo
            .FirstOrDefault(item => profondita >= item.MetriDa && profondita <= item.MetriA);
    }

    private static bool ProfonditaRientraNellIntervallo(TipologiaImmersioneOperativa? tipologia, int profondita)
    {
        if (tipologia is null)
        {
            return true;
        }

        if (tipologia.ProfonditaMinimaMetri is { } min && profondita < min)
        {
            return false;
        }

        if (tipologia.ProfonditaMassimaMetri is { } max && profondita > max)
        {
            return false;
        }

        return true;
    }

    private static void ValidaProfonditaPerTipologia(TipologiaImmersioneOperativa? tipologia, int? profondita, string fieldName)
    {
        if (tipologia is null || profondita is null || ProfonditaRientraNellIntervallo(tipologia, profondita.Value))
        {
            return;
        }

        var intervallo = tipologia.ProfonditaMinimaMetri is { } min && tipologia.ProfonditaMassimaMetri is { } max
            ? $"{min:0}-{max:0} m"
            : "intervallo consentito";

        throw new InvalidOperationException($"{fieldName}: per {tipologia.Descrizione} usare una profondita compresa tra {intervallo}.");
    }

    private static bool MostraAvvisoProfonditaNonValida(TipologiaImmersioneOperativa? tipologia, string profonditaText)
    {
        if (tipologia is null
            || !int.TryParse(profonditaText, out var profondita)
            || ProfonditaRientraNellIntervallo(tipologia, profondita))
        {
            return false;
        }

        var intervallo = tipologia.ProfonditaMinimaMetri is { } min && tipologia.ProfonditaMassimaMetri is { } max
            ? $"{min:0}-{max:0} m"
            : "l'intervallo previsto";
        MessageBox.Show(
            $"La profondita {profondita} m non e prevista per l'apparato {tipologia.Descrizione}.\n\nUsa una profondita compresa in {intervallo}, oppure cambia apparato.",
            "Profondita non valida",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return true;
    }

    private void AggiornaCalcoliPartecipazioneImmersione(ServizioPartecipanteImmersioneDraftViewModel row)
    {
        if (!row.InImmersione)
        {
            row.TariffaProposta = null;
            row.ImportoStimato = null;
            return;
        }

        var categoria = row.CategoriaContabileOre;
        var tipologia = row.TipologiaImmersioneOperativa;
        var fascia = row.FasciaProfondita;
        if (categoria is null || tipologia is null || fascia is null)
        {
            row.TariffaProposta = null;
            row.ImportoStimato = null;
            return;
        }

        var regola = RegoleContabiliImmersioneCatalogo.FirstOrDefault(item =>
            item.Attiva
            && item.TipologiaImmersioneOperativaId == tipologia.TipologiaImmersioneOperativaId
            && item.FasciaProfonditaId == fascia.FasciaProfonditaId
            && item.CategoriaContabileOreId == categoria.CategoriaContabileOreId);

        row.TariffaProposta = regola?.Tariffa;
        var ore = ParseDecimalSilenzioso(row.OreImmersione);
        row.ImportoStimato = regola is null || ore is null
            ? null
            : CalcolaImportoRiepilogoImmersione(regola.Tariffa, ore.Value);
    }

    private void ServizioSupportoOccasionale_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ServizioSupportoOccasionaleDraftViewModel.Nominativo)
            or nameof(ServizioSupportoOccasionaleDraftViewModel.Qualifica)
            or nameof(ServizioSupportoOccasionaleDraftViewModel.Ruolo)
            or nameof(ServizioSupportoOccasionaleDraftViewModel.Presente)
            or nameof(ServizioSupportoOccasionaleDraftViewModel.Contatti)
            or nameof(ServizioSupportoOccasionaleDraftViewModel.Note))
        {
            AggiornaRiepilogoBozzaServizio();
        }
    }

    private void ServizioOperatoreSubEsterno_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ServizioOperatoreSubEsternoDraftViewModel.PerId)
            or nameof(ServizioOperatoreSubEsternoDraftViewModel.Nominativo)
            or nameof(ServizioOperatoreSubEsternoDraftViewModel.Qualifica)
            or nameof(ServizioOperatoreSubEsternoDraftViewModel.Reparto)
            or nameof(ServizioOperatoreSubEsternoDraftViewModel.GruppoOperativo)
            or nameof(ServizioOperatoreSubEsternoDraftViewModel.Note))
        {
            SincronizzaPartecipazioniImmersioneBozza();
            AggiornaRiepilogoBozzaServizio();
        }
    }

    private void ServizioPartecipanteBozza_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ServizioPartecipanteDraftViewModel.Presente))
        {
            AggiornaOperatoriServizioPresentiDisponibili();
            SincronizzaPartecipazioniImmersioneBozza();
            AggiornaRiepilogoBozzaServizio();
            return;
        }

        if (e.PropertyName is nameof(ServizioPartecipanteDraftViewModel.GruppoOperativo)
            or nameof(ServizioPartecipanteDraftViewModel.RuoloOperativo))
        {
            AggiornaRiepilogoBozzaServizio();
        }
    }

    private void ServizioImmersioneBozza_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ServizioImmersioneDraftViewModel.DirettoreImmersione)
            or nameof(ServizioImmersioneDraftViewModel.OperatoreSoccorso)
            or nameof(ServizioImmersioneDraftViewModel.AssistenteBlsd)
            or nameof(ServizioImmersioneDraftViewModel.AssistenteSanitario)
            or nameof(ServizioImmersioneDraftViewModel.Note))
        {
            if (e.PropertyName is nameof(ServizioImmersioneDraftViewModel.DirettoreImmersione)
                or nameof(ServizioImmersioneDraftViewModel.OperatoreSoccorso)
                or nameof(ServizioImmersioneDraftViewModel.AssistenteBlsd)
                or nameof(ServizioImmersioneDraftViewModel.AssistenteSanitario))
            {
                SincronizzaPartecipazioniImmersioneBozza();
            }

            AggiornaRiepilogoBozzaServizio();
        }
    }

    private void ServizioPartecipazioneImmersione_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not ServizioPartecipanteImmersioneDraftViewModel row)
        {
            return;
        }

        if (e.PropertyName is nameof(ServizioPartecipanteImmersioneDraftViewModel.InImmersione))
        {
            if (row.InImmersione)
            {
                ApplicaValoriCondivisiAllaPartecipazione(row);
            }
            else if (TrovaImmersioneDiPartecipazione(row) is { } immersione)
            {
                AggiornaStatoCondivisioneValoriImmersione(immersione);
            }

            AggiornaCalcoliPartecipazioneImmersione(row);
            AggiornaRiepilogoBozzaServizio();
            return;
        }

        if (e.PropertyName is nameof(ServizioPartecipanteImmersioneDraftViewModel.ProfonditaMetri))
        {
            if (!_isSyncingValoriCondivisiImmersione
                && TrovaImmersioneDiPartecipazione(row) is { } immersione
                && !immersione.ProfonditaCondivisaInizializzata)
            {
                SincronizzaValoriCondivisiImmersione(immersione, row, sincronizzaProfondita: true, sincronizzaOre: false);
            }

            if (TrovaImmersioneDiPartecipazione(row) is { } immersioneProfondita)
            {
                AggiornaStatoCondivisioneValoriImmersione(immersioneProfondita);
            }

            AggiornaFasciaDaProfondita(row);
            AggiornaCalcoliPartecipazioneImmersione(row);
            AggiornaRiepilogoBozzaServizio();
            return;
        }

        if (e.PropertyName is nameof(ServizioPartecipanteImmersioneDraftViewModel.TipologiaImmersioneOperativa)
            or nameof(ServizioPartecipanteImmersioneDraftViewModel.FasciaProfondita)
            or nameof(ServizioPartecipanteImmersioneDraftViewModel.OreImmersione)
            or nameof(ServizioPartecipanteImmersioneDraftViewModel.CategoriaContabileOre)
            or nameof(ServizioPartecipanteImmersioneDraftViewModel.Note))
        {
            if (e.PropertyName is nameof(ServizioPartecipanteImmersioneDraftViewModel.TipologiaImmersioneOperativa)
                && !_isSyncingValoriCondivisiImmersione)
            {
                PropagaSelezioniAiSuccessivi(row, sincronizzaTipologia: true, sincronizzaCategoria: false);
            }

            if (e.PropertyName is nameof(ServizioPartecipanteImmersioneDraftViewModel.OreImmersione)
                && !_isSyncingValoriCondivisiImmersione
                && TrovaImmersioneDiPartecipazione(row) is { } immersione
                && !immersione.OreCondiviseInizializzate)
            {
                SincronizzaValoriCondivisiImmersione(immersione, row, sincronizzaProfondita: false, sincronizzaOre: true);
            }

            if (e.PropertyName is nameof(ServizioPartecipanteImmersioneDraftViewModel.CategoriaContabileOre)
                && !_isSyncingValoriCondivisiImmersione)
            {
                PropagaSelezioniAiSuccessivi(row, sincronizzaTipologia: false, sincronizzaCategoria: true);
            }

            if (e.PropertyName is nameof(ServizioPartecipanteImmersioneDraftViewModel.OreImmersione)
                && TrovaImmersioneDiPartecipazione(row) is { } immersioneOre)
            {
                AggiornaStatoCondivisioneValoriImmersione(immersioneOre);
            }

            if (e.PropertyName is nameof(ServizioPartecipanteImmersioneDraftViewModel.TipologiaImmersioneOperativa))
            {
                AggiornaFasciaDaProfondita(row);
            }

            AggiornaCalcoliPartecipazioneImmersione(row);
            AggiornaRiepilogoBozzaServizio();
        }
    }

    private void ServizioPartecipazioneUnicaImmersione_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isSyncingPartecipazioniUniche || sender is not ServizioPartecipanteImmersioneUnicoDraftViewModel row)
        {
            return;
        }

        if (e.PropertyName is nameof(ServizioPartecipanteImmersioneUnicoDraftViewModel.ProfonditaMetri))
        {
            AggiornaFasciaDaProfondita(row);
            MostraAvvisoProfonditaNonValida(row.TipologiaImmersioneOperativa, row.ProfonditaMetri);
        }
        else if (e.PropertyName is nameof(ServizioPartecipanteImmersioneUnicoDraftViewModel.TipologiaImmersioneOperativa))
        {
            AggiornaFasciaDaProfondita(row);
            MostraAvvisoProfonditaNonValida(row.TipologiaImmersioneOperativa, row.ProfonditaMetri);
        }

        if (e.PropertyName is nameof(ServizioPartecipanteImmersioneUnicoDraftViewModel.TipologiaImmersioneOperativa)
            or nameof(ServizioPartecipanteImmersioneUnicoDraftViewModel.ProfonditaMetri)
            or nameof(ServizioPartecipanteImmersioneUnicoDraftViewModel.FasciaProfondita)
            or nameof(ServizioPartecipanteImmersioneUnicoDraftViewModel.OreImmersione)
            or nameof(ServizioPartecipanteImmersioneUnicoDraftViewModel.CategoriaContabileOre)
            or nameof(ServizioPartecipanteImmersioneUnicoDraftViewModel.Note))
        {
            PropagaValoriUniciAiSuccessivi(row, e.PropertyName);
            ApplicaPartecipazioniDaRigheUniche();
        }
    }

    private void PropagaValoriUniciAiSuccessivi(ServizioPartecipanteImmersioneUnicoDraftViewModel origine, string? propertyName)
    {
        var indiceOrigine = ServizioPartecipazioniContabiliUnicheBozza.IndexOf(origine);
        if (indiceOrigine < 0)
        {
            return;
        }

        var righeSuccessive = ServizioPartecipazioniContabiliUnicheBozza
            .Skip(indiceOrigine + 1)
            .ToList();
        if (righeSuccessive.Count == 0)
        {
            return;
        }

        _isSyncingPartecipazioniUniche = true;
        try
        {
            foreach (var row in righeSuccessive)
            {
                if (propertyName == nameof(ServizioPartecipanteImmersioneUnicoDraftViewModel.TipologiaImmersioneOperativa)
                    && row.TipologiaImmersioneOperativa is null)
                {
                    row.TipologiaImmersioneOperativa = origine.TipologiaImmersioneOperativa;
                }
                else if (propertyName == nameof(ServizioPartecipanteImmersioneUnicoDraftViewModel.ProfonditaMetri)
                    && string.IsNullOrWhiteSpace(row.ProfonditaMetri))
                {
                    row.ProfonditaMetri = origine.ProfonditaMetri;
                    AggiornaFasciaDaProfondita(row);
                }
                else if (propertyName == nameof(ServizioPartecipanteImmersioneUnicoDraftViewModel.FasciaProfondita)
                    && row.FasciaProfondita is null)
                {
                    row.FasciaProfondita = origine.FasciaProfondita;
                }
                else if (propertyName == nameof(ServizioPartecipanteImmersioneUnicoDraftViewModel.OreImmersione)
                    && string.IsNullOrWhiteSpace(row.OreImmersione))
                {
                    row.OreImmersione = origine.OreImmersione;
                }
                else if (propertyName == nameof(ServizioPartecipanteImmersioneUnicoDraftViewModel.CategoriaContabileOre)
                    && row.CategoriaContabileOre is null)
                {
                    row.CategoriaContabileOre = origine.CategoriaContabileOre;
                }
            }
        }
        finally
        {
            _isSyncingPartecipazioniUniche = false;
        }
    }

    private GruppoOperativo? TrovaGruppoOperativo(int? gruppoOperativoId) =>
        gruppoOperativoId is null ? null : GruppiOperativiCatalogo.FirstOrDefault(item => item.GruppoOperativoId == gruppoOperativoId.Value);

    private RuoloOperativo? TrovaRuoloOperativo(int? ruoloOperativoId) =>
        ruoloOperativoId is null ? null : RuoliOperativiCatalogo.FirstOrDefault(item => item.RuoloOperativoId == ruoloOperativoId.Value);

    private PersonaleListItemViewModel? TrovaOperatoreServizio(int? perId) =>
        perId is not > 0 ? null : OperatoriServizioDisponibili.FirstOrDefault(item => item.PerId == perId.Value);

    private ServizioImmersioneDraftViewModel? TrovaImmersioneDiPartecipazione(ServizioPartecipanteImmersioneDraftViewModel partecipazione) =>
        ServizioImmersioniBozza.FirstOrDefault(item => item.Partecipazioni.Contains(partecipazione));

    private void PropagaSelezioniAiSuccessivi(
        ServizioPartecipanteImmersioneDraftViewModel origine,
        bool sincronizzaTipologia,
        bool sincronizzaCategoria)
    {
        if (_isSyncingValoriCondivisiImmersione || !origine.InImmersione)
        {
            return;
        }

        var immersione = TrovaImmersioneDiPartecipazione(origine);
        if (immersione is null)
        {
            return;
        }

        var indiceOrigine = immersione.Partecipazioni.IndexOf(origine);
        if (indiceOrigine < 0)
        {
            return;
        }

        var righeSuccessive = immersione.Partecipazioni
            .Skip(indiceOrigine + 1)
            .Where(item => item.InImmersione)
            .ToList();
        if (righeSuccessive.Count == 0)
        {
            return;
        }

        var tipologiaCondivisa = sincronizzaTipologia ? origine.TipologiaImmersioneOperativa : null;
        var categoriaCondivisa = sincronizzaCategoria ? origine.CategoriaContabileOre : null;
        if (tipologiaCondivisa is null && categoriaCondivisa is null)
        {
            return;
        }

        _isSyncingValoriCondivisiImmersione = true;
        try
        {
            foreach (var partecipazione in righeSuccessive)
            {
                if (tipologiaCondivisa is not null
                    && !ReferenceEquals(partecipazione.TipologiaImmersioneOperativa, tipologiaCondivisa))
                {
                    partecipazione.TipologiaImmersioneOperativa = tipologiaCondivisa;
                }

                if (categoriaCondivisa is not null
                    && !ReferenceEquals(partecipazione.CategoriaContabileOre, categoriaCondivisa))
                {
                    partecipazione.CategoriaContabileOre = categoriaCondivisa;
                }
            }
        }
        finally
        {
            _isSyncingValoriCondivisiImmersione = false;
        }
    }

    private void ApplicaValoriCondivisiAllaPartecipazione(ServizioPartecipanteImmersioneDraftViewModel partecipazione)
    {
        if (_isSyncingValoriCondivisiImmersione || !partecipazione.InImmersione)
        {
            return;
        }

        var immersione = TrovaImmersioneDiPartecipazione(partecipazione);
        if (immersione is null)
        {
            return;
        }

        var indicePartecipazione = immersione.Partecipazioni.IndexOf(partecipazione);
        var righePrecedentiInImmersione = indicePartecipazione <= 0
            ? new List<ServizioPartecipanteImmersioneDraftViewModel>()
            : immersione.Partecipazioni
                .Take(indicePartecipazione)
                .Where(item => item.InImmersione)
                .ToList();
        var altreRigheInImmersione = immersione.Partecipazioni
            .Where(item => item.InImmersione && !ReferenceEquals(item, partecipazione))
            .ToList();
        var tipologiaCondivisa = righePrecedentiInImmersione
            .Select(item => item.TipologiaImmersioneOperativa)
            .LastOrDefault(item => item is not null);
        var profonditaCondivisa = immersione.ProfonditaCondivisaInizializzata
            ? string.Empty
            : TrovaValoreCondivisoImmersione(null, altreRigheInImmersione.Select(item => item.ProfonditaMetri));
        var categoriaCondivisa = righePrecedentiInImmersione
            .Select(item => item.CategoriaContabileOre)
            .LastOrDefault(item => item is not null);
        var oreCondivise = immersione.OreCondiviseInizializzate
            ? string.Empty
            : TrovaValoreCondivisoImmersione(null, altreRigheInImmersione.Select(item => item.OreImmersione));

        _isSyncingValoriCondivisiImmersione = true;
        try
        {
            if (tipologiaCondivisa is not null)
            {
                partecipazione.TipologiaImmersioneOperativa = tipologiaCondivisa;
            }

            if (!string.IsNullOrWhiteSpace(profonditaCondivisa))
            {
                partecipazione.ProfonditaMetri = profonditaCondivisa;
            }

            if (categoriaCondivisa is not null)
            {
                partecipazione.CategoriaContabileOre = categoriaCondivisa;
            }

            if (!string.IsNullOrWhiteSpace(oreCondivise))
            {
                partecipazione.OreImmersione = oreCondivise;
            }
        }
        finally
        {
            _isSyncingValoriCondivisiImmersione = false;
        }
    }

    private void AggiornaOperatoriServizioPresentiDisponibili()
    {
        var operatoriPresenti = ServizioPartecipantiBozza
            .Where(item => item.Presente)
            .Select(item => TrovaOperatoreServizio(item.PerId))
            .Where(item => item is not null)
            .Cast<PersonaleListItemViewModel>()
            .OrderBy(item => QualificaFormatter.GetGerarchiaOrdine(item.Qualifica, item.IsProfiloSanitario, item.RuoloSanitario))
            .ThenBy(item => GetOrdineDecorrenzaQualifica(item.DataDecorrenzaQualifica))
            .ThenBy(item => item.Cognome)
            .ThenBy(item => item.Nome)
            .ToList();

        OperatoriServizioPresentiDisponibili.Clear();
        OperatoriServizioPresentiDisponibili.Add(OperatoreVuoto);

        foreach (var operatore in operatoriPresenti)
        {
            OperatoriServizioPresentiDisponibili.Add(operatore);
        }

        AggiornaResponsabileServizioAutomatico();
        SincronizzaRuoliImmersioneConPresenti();
    }

    private void AggiornaResponsabileServizioAutomatico()
    {
        var responsabileCorrente = GetPerIdOperatoreSelezionato(ServizioResponsabileSelezionato);
        var responsabileAncoraPresente = responsabileCorrente is > 0
            && OperatoriServizioPresentiDisponibili.Any(item => item.PerId == responsabileCorrente.Value);
        if (responsabileAncoraPresente)
        {
            return;
        }

        ServizioResponsabileSelezionato = OperatoriServizioPresentiDisponibili
            .Where(item => item.PerId > 0)
            .OrderBy(item => QualificaFormatter.GetGerarchiaOrdine(item.Qualifica, item.IsProfiloSanitario, item.RuoloSanitario))
            .ThenBy(item => GetOrdineDecorrenzaQualifica(item.DataDecorrenzaQualifica))
            .ThenBy(item => item.Cognome)
            .ThenBy(item => item.Nome)
            .FirstOrDefault();
    }

    private void SincronizzaRuoliImmersioneConPresenti()
    {
        var operatoriValidi = OperatoriServizioPresentiDisponibili
            .Where(item => item.PerId > 0)
            .Select(item => item.PerId)
            .ToHashSet();

        foreach (var immersione in ServizioImmersioniBozza)
        {
            if (immersione.DirettoreImmersione is { PerId: > 0 } direttore && !operatoriValidi.Contains(direttore.PerId))
            {
                immersione.DirettoreImmersione = null;
            }

            if (immersione.OperatoreSoccorso is { PerId: > 0 } soccorso && !operatoriValidi.Contains(soccorso.PerId))
            {
                immersione.OperatoreSoccorso = null;
            }

            if (immersione.AssistenteBlsd is { PerId: > 0 } blsd && !operatoriValidi.Contains(blsd.PerId))
            {
                immersione.AssistenteBlsd = null;
            }

            if (immersione.AssistenteSanitario is { PerId: > 0 } sanitario && !operatoriValidi.Contains(sanitario.PerId))
            {
                immersione.AssistenteSanitario = null;
            }
        }
    }

    private static int? GetPerIdOperatoreSelezionato(PersonaleListItemViewModel? operatore) =>
        operatore is { PerId: > 0 } ? operatore.PerId : null;

    private static bool IsDirettoreImmersione(ServizioImmersioneDraftViewModel immersione, int perId) =>
        GetPerIdOperatoreSelezionato(immersione.DirettoreImmersione) == perId;

    private int ContaPartecipantiInterniBozza() =>
        ServizioPartecipantiBozza.Count(IsPartecipanteInternoCompilato);

    private int ContaPresentiInterniBozza() =>
        ServizioPartecipantiBozza.Count(item => item.Presente && IsPartecipanteInternoCompilato(item));

    private int ContaSupportiOccasionaliBozza() =>
        ServizioSupportiOccasionaliBozza.Count(IsSupportoOccasionaleCompilato);

    private int ContaSupportiOccasionaliPresentiBozza() =>
        ServizioSupportiOccasionaliBozza.Count(item => item.Presente && IsSupportoOccasionaleCompilato(item));

    private static bool IsPartecipanteInternoCompilato(ServizioPartecipanteDraftViewModel row) =>
        row.Presente
        || row.GruppoOperativo?.GruppoOperativoId != row.DefaultGruppoOperativoId
        || row.RuoloOperativo?.RuoloOperativoId != row.DefaultRuoloOperativoId
        || !string.IsNullOrWhiteSpace(row.Note);

    private static bool IsSupportoOccasionaleCompilato(ServizioSupportoOccasionaleDraftViewModel row) =>
        !string.IsNullOrWhiteSpace(row.Nominativo)
        || !string.IsNullOrWhiteSpace(row.Qualifica)
        || !string.IsNullOrWhiteSpace(row.Ruolo)
        || row.Presente
        || !string.IsNullOrWhiteSpace(row.Contatti)
        || !string.IsNullOrWhiteSpace(row.Note);

    private int ContaOperatoriSubEsterniBozza() =>
        ServizioOperatoriSubEsterniBozza.Count(IsOperatoreSubEsternoCompilato);

    private static bool IsOperatoreSubEsternoCompilato(ServizioOperatoreSubEsternoDraftViewModel row) =>
        !string.IsNullOrWhiteSpace(row.PerId)
        || !string.IsNullOrWhiteSpace(row.Nominativo)
        || !string.IsNullOrWhiteSpace(row.Qualifica)
        || !string.IsNullOrWhiteSpace(row.Reparto)
        || !string.IsNullOrWhiteSpace(row.Note);

    private static int? ParseIntSilenzioso(string value) =>
        int.TryParse(value, out var parsed) ? parsed : null;

    private sealed record OperatoreSubServizioDraft(
        int PerId,
        string Qualifica,
        DateOnly? DataDecorrenzaQualifica,
        string Nominativo,
        bool IsEsterno,
        string Reparto,
        long Ordine,
        string CognomeNome);

    private static bool IsPartecipazioneImmersioneCompilata(ServizioPartecipanteImmersioneDraftViewModel row) =>
        row.InImmersione
        || row.TipologiaImmersioneOperativa is not null
        || !string.IsNullOrWhiteSpace(row.ProfonditaMetri)
        || row.FasciaProfondita is not null
        || !string.IsNullOrWhiteSpace(row.OreImmersione)
        || row.CategoriaContabileOre is not null
        || !string.IsNullOrWhiteSpace(row.Note);

    private void AggiornaContestoServizio()
    {
        OnPropertyChanged(nameof(IsExistingServizio));
        OnPropertyChanged(nameof(ServizioBozzaStato));
        OnPropertyChanged(nameof(ServizioEditorTitolo));
        OnPropertyChanged(nameof(ServizioEditorSottotitolo));
    }

    private void AggiornaRiepilogoBozzaServizio()
    {
        OnPropertyChanged(nameof(ServizioBozzaStato));
        OnPropertyChanged(nameof(ServizioOrario));
        OnPropertyChanged(nameof(ServizioOrarioRiepilogo));
        OnPropertyChanged(nameof(ServizioTipoDescrizione));
        OnPropertyChanged(nameof(ServizioFuoriSedeDescrizione));
        OnPropertyChanged(nameof(ServizioOrdinePubblicoDescrizione));
        OnPropertyChanged(nameof(ServizioStraordinarioOreDisplay));
        OnPropertyChanged(nameof(ServizioCategoriaRegistroDescrizione));
        OnPropertyChanged(nameof(ServizioPartecipantiTotali));
        OnPropertyChanged(nameof(ServizioPresentiTotali));
        OnPropertyChanged(nameof(ServizioImmersioniCompilate));
        OnPropertyChanged(nameof(ServizioBozzaStato));
        OnPropertyChanged(nameof(DashboardServizioSintesi));
    }

    private void AggiornaStatoBackup()
    {
        OnPropertyChanged(nameof(BackupLocaleStato));
        OnPropertyChanged(nameof(BackupEsternoStato));
        OnPropertyChanged(nameof(BackupCartellaEsterna));
        OnPropertyChanged(nameof(BackupDescrizione));
    }

    private void RicaricaDatiApplicazioneDaDatabase()
    {
        DatabaseInitializer.EnsureDatabase();

        RicaricaCataloghiServizio(preserveSelections: false);
        RicaricaSuggerimentiRicerca();
        AggiornaSuggerimentiRicerca();
        CaricaElenco();
        CaricaArchivio();
        CaricaServiziSalvati();
        AggiornaAnniContabilitaDisponibili();
        CaricaContabilitaMensile();
        CaricaRegistroImmersioniMensile();
        AggiornaScadenziario();
        InizializzaBozzaServizio(preserveSelections: false);
        NuovoServizioGiornaliero();
        IsSchedaServizioVisibile = false;
        NuovoPersonale();
        SezioneAttivaIndex = HomeSectionIndex;
    }

    private void RicaricaCataloghiServizio(bool preserveSelections)
    {
        var localitaId = preserveSelections ? ServizioLocalitaSelezionata?.LocalitaOperativaId : null;
        var scopoId = preserveSelections ? ServizioScopoSelezionato?.ScopoImmersioneId : null;
        var unitaId = preserveSelections ? ServizioUnitaNavaleSelezionata?.UnitaNavaleId : null;

        var cataloghiServizio = _repository.GetCataloghiServizio();
        SostituisciCollection(CategorieRegistroCatalogo, cataloghiServizio.CategorieRegistro);
        SostituisciCollection(LocalitaOperativeCatalogo, cataloghiServizio.LocalitaOperative);
        SostituisciCollection(LocalitaOperativeServizioCatalogo, BuildLocalitaOperativeServizioCatalogo(cataloghiServizio.LocalitaOperative));
        SostituisciCollection(ScopiImmersioneCatalogo, cataloghiServizio.ScopiImmersione);
        SostituisciCollection(UnitaNavaliCatalogo, BuildUnitaNavaliCatalogo(cataloghiServizio.UnitaNavali));
        SostituisciCollection(UnitaNavaliGestioneCatalogo, cataloghiServizio.UnitaNavali);
        SostituisciCollection(TipologieImmersioneOperativeCatalogo, cataloghiServizio.TipologieImmersione);
        SostituisciCollection(FasceProfonditaCatalogo, cataloghiServizio.FasceProfondita);
        SostituisciCollection(CategorieContabiliOreCatalogo, cataloghiServizio.CategorieContabiliOre);
        SostituisciCollection(GruppiOperativiCatalogo, cataloghiServizio.GruppiOperativi);
        SostituisciCollection(RuoliOperativiCatalogo, cataloghiServizio.RuoliOperativi);
        SostituisciCollection(RegoleContabiliImmersioneCatalogo, cataloghiServizio.RegoleContabiliImmersione);

        _servizioLocalitaSelezionata = localitaId is null
            ? LocalitaOperativeServizioCatalogo.FirstOrDefault()
            : LocalitaOperativeServizioCatalogo.FirstOrDefault(item => item.LocalitaOperativaId == localitaId.Value) ?? LocalitaOperativeServizioCatalogo.FirstOrDefault();
        _servizioScopoSelezionato = scopoId is null
            ? ScopiImmersioneCatalogo.FirstOrDefault()
            : ScopiImmersioneCatalogo.FirstOrDefault(item => item.ScopoImmersioneId == scopoId.Value) ?? ScopiImmersioneCatalogo.FirstOrDefault();
        _servizioUnitaNavaleSelezionata = unitaId is null
            ? UnitaNavaliCatalogo.FirstOrDefault()
            : UnitaNavaliCatalogo.FirstOrDefault(item => item.UnitaNavaleId == unitaId.Value) ?? UnitaNavaliCatalogo.FirstOrDefault();

        OnPropertyChanged(nameof(ServizioLocalitaSelezionata));
        OnPropertyChanged(nameof(ServizioScopoSelezionato));
        OnPropertyChanged(nameof(ServizioUnitaNavaleSelezionata));
        InizializzaEditorTariffeContabili();
    }
}
