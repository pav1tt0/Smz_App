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
    private void SalvaServizioGiornaliero()
    {
        try
        {
            var isNuovoServizio = !IsExistingServizio;
            var servizio = BuildServizioGiornalieroModel();
            var servizioGiornalieroId = _repository.SaveServizioGiornaliero(servizio);

            _servizioGiornalieroId = servizioGiornalieroId;
            AggiornaContestoServizio();
            CaricaServiziSalvati(servizioGiornalieroId);
            AggiornaAnniContabilitaDisponibili();
            AggiornaDatiMensili();
            RegistraSnapshotServizio();
            IsSchedaServizioVisibile = false;
            IsServizioApertoDaReport = false;
            Stato = isNuovoServizio
                ? $"Servizio giornaliero salvato con ID {servizioGiornalieroId}."
                : $"Servizio giornaliero #{servizioGiornalieroId} aggiornato.";
            EseguiBackupLocaleSilenzioso("save-service");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Salvataggio servizio", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Salvataggio servizio non riuscito.";
        }
    }

    private void StampaServizioGiornaliero()
    {
        try
        {
            var servizio = BuildServizioGiornalieroModel();
            _servizioGiornalieroPrintService.Print(servizio);
            Stato = "Stampa servizio inviata.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Stampa servizio", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Stampa servizio non riuscita.";
        }
    }

    private void StampaServizioSelezionato()
    {
        if (SelectedServizioSalvato is null)
        {
            Stato = "Stampa servizio non eseguita: seleziona un servizio.";
            return;
        }

        try
        {
            var servizio = _repository.GetServizioGiornalieroById(SelectedServizioSalvato.ServizioGiornalieroId)
                ?? throw new InvalidOperationException("Servizio selezionato non trovato.");
            _servizioGiornalieroPrintService.Print(servizio);
            Stato = $"Stampa servizio del {servizio.DataServizio:dd/MM/yyyy} inviata.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Stampa servizio", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Stampa servizio non riuscita.";
        }
    }

    private void StampaRegistroImmersioniMensile(RegistroImmersioniMensilePrintLayout layout)
    {
        if (ContabilitaMeseSelezionato is null || ContabilitaAnnoSelezionato <= 0)
        {
            Stato = "Stampa registro non eseguita: seleziona mese e anno.";
            return;
        }

        try
        {
            _registroImmersioniMensilePrintService.Print(
                ContabilitaAnnoSelezionato,
                ContabilitaMeseSelezionato.NumeroMese,
                ContabilitaMeseSelezionato.Descrizione,
                layout);
            Stato = layout == RegistroImmersioniMensilePrintLayout.Compatto
                ? "Stampa registro immersioni compatta inviata."
                : "Stampa registro immersioni inviata.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Stampa registro immersioni", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Stampa registro immersioni non riuscita.";
        }
    }

    private void ApriServizioSelezionato()
    {
        if (SelectedServizioSalvato is null)
        {
            return;
        }

        CaricaServizioGiornaliero(SelectedServizioSalvato.ServizioGiornalieroId);
        SezioneAttivaIndex = ServicesSectionIndex;
        IsServizioApertoDaReport = true;
        IsSchedaServizioVisibile = true;
    }

    private void ApriServizioDaParametro(object? parameter)
    {
        if (parameter is not ServizioGiornalieroSummary servizio)
        {
            return;
        }

        SelectedServizioSalvato = servizio;
        CaricaServizioGiornaliero(servizio.ServizioGiornalieroId);
        IsServizioApertoDaReport = false;
        IsSchedaServizioVisibile = true;
    }

    private void ChiudiSchedaServizio()
    {
        IsSchedaServizioVisibile = false;
        IsServizioApertoDaReport = false;
    }

    private void DuplicaServizioSelezionato()
    {
        if (SelectedServizioSalvato is null)
        {
            return;
        }

        var servizioOrigine = SelectedServizioSalvato;
        CaricaServizioGiornaliero(servizioOrigine.ServizioGiornalieroId);

        _servizioGiornalieroId = 0;
        SelectedServizioSalvato = null;
        IsServizioApertoDaReport = false;
        ServizioData = DateTime.Today.ToString("dd/MM/yyyy");
        ServizioNumeroOrdine = string.Empty;
        ServizioOrarioDerogaAttiva = false;
        ServizioOrarioDerogaInizio = string.Empty;
        ServizioOrarioDerogaFine = string.Empty;

        AggiornaContestoServizio();
        AggiornaRiepilogoBozzaServizio();
        RegistraSnapshotServizio();
        IsSchedaServizioVisibile = true;
        Stato = $"Bozza creata duplicando il servizio del {servizioOrigine.DataServizio:dd/MM/yyyy}.";
    }

    private void EsportaPacchettoServizioSelezionato()
    {
        if (SelectedServizioSalvato is null)
        {
            return;
        }

        Directory.CreateDirectory(DatabasePaths.ExportDirectory);

        var dialog = new SaveFileDialog
        {
            Filter = "Pacchetto servizio SMZ (*.smzsvc)|*.smzsvc|JSON (*.json)|*.json|Tutti i file|*.*",
            DefaultExt = ".smzsvc",
            AddExtension = true,
            InitialDirectory = DatabasePaths.ExportDirectory,
            FileName = BuildServizioPackageFileName(SelectedServizioSalvato),
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            _servizioScambioService.ExportServizio(SelectedServizioSalvato.ServizioGiornalieroId, dialog.FileName);
            Stato = $"Pacchetto servizio esportato: {dialog.FileName}";
            MessageBox.Show(
                $"Pacchetto servizio creato in:\n{dialog.FileName}",
                "Esporta servizio",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Esporta servizio", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Esportazione servizio non riuscita.";
        }
    }

    private void ImportaPacchettoServizio()
    {
        Directory.CreateDirectory(DatabasePaths.ExportDirectory);

        var dialog = new OpenFileDialog
        {
            Filter = "Pacchetto servizio SMZ (*.smzsvc;*.json)|*.smzsvc;*.json|Tutti i file|*.*",
            InitialDirectory = DatabasePaths.ExportDirectory,
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var servizioGiornalieroId = _servizioScambioService.ImportServizio(dialog.FileName);
            CaricaServiziSalvati(servizioGiornalieroId);
            AggiornaAnniContabilitaDisponibili();
            AggiornaDatiMensili();
            CaricaServizioGiornaliero(servizioGiornalieroId);
            Stato = $"Pacchetto servizio importato con ID {servizioGiornalieroId}.";
            EseguiBackupLocaleSilenzioso("import-service-package");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Importa servizio", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Importazione servizio non riuscita.";
        }
    }

    private static string BuildServizioPackageFileName(ServizioGiornalieroSummary servizio)
    {
        var ordine = string.IsNullOrWhiteSpace(servizio.NumeroOrdineServizio)
            ? "senza-ordine"
            : SanitizeFileToken(servizio.NumeroOrdineServizio);
        return $"servizio_{servizio.DataServizio:yyyyMMdd}_{ordine}.smzsvc";
    }

    private static string SanitizeFileToken(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(value
            .Trim()
            .Select(ch => invalidChars.Contains(ch) || char.IsWhiteSpace(ch) ? '_' : ch)
            .ToArray());

        return string.IsNullOrWhiteSpace(sanitized) ? "file" : sanitized;
    }

    private void AggiungiSupportoOccasionale()
    {
        var item = new ServizioSupportoOccasionaleDraftViewModel
        {
            Presente = false,
        };

        item.PropertyChanged += ServizioSupportoOccasionale_PropertyChanged;
        ServizioSupportiOccasionaliBozza.Add(item);
        SelectedSupportoOccasionale = item;
        AggiornaRiepilogoBozzaServizio();
    }

    private void AggiungiOperatoreSubEsterno()
    {
        var item = new ServizioOperatoreSubEsternoDraftViewModel
        {
            GruppoOperativo = TrovaGruppoOperativo(1),
        };

        item.PropertyChanged += ServizioOperatoreSubEsterno_PropertyChanged;
        ServizioOperatoriSubEsterniBozza.Add(item);
        SelectedOperatoreSubEsterno = item;
        SincronizzaPartecipazioniImmersioneBozza();
        AggiornaRiepilogoBozzaServizio();
    }


    private void AggiungiLocalitaOperativa()
    {
        try
        {
            var item = _repository.AddLocalitaOperativa(NuovaLocalitaOperativa);
            if (!LocalitaOperativeCatalogo.Any(existing => existing.LocalitaOperativaId == item.LocalitaOperativaId))
            {
                LocalitaOperativeCatalogo.Add(item);
            }

            if (item.Attiva && !LocalitaOperativeServizioCatalogo.Any(existing => existing.LocalitaOperativaId == item.LocalitaOperativaId))
            {
                LocalitaOperativeServizioCatalogo.Add(item);
            }

            ServizioLocalitaSelezionata = LocalitaOperativeServizioCatalogo.First(existing => existing.LocalitaOperativaId == item.LocalitaOperativaId);
            NuovaLocalitaOperativa = string.Empty;
            Stato = $"Localita aggiunta: {item.Descrizione}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Localita operative", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Inserimento localita non riuscito.";
        }
    }

    private void AggiungiUnitaNavale()
    {
        try
        {
            var item = _repository.AddUnitaNavale(NuovaUnitaNavale);
            if (!UnitaNavaliCatalogo.Any(existing => existing.UnitaNavaleId == item.UnitaNavaleId))
            {
                UnitaNavaliCatalogo.Add(item);
            }

            if (!UnitaNavaliGestioneCatalogo.Any(existing => existing.UnitaNavaleId == item.UnitaNavaleId))
            {
                UnitaNavaliGestioneCatalogo.Add(item);
            }

            ServizioUnitaNavaleSelezionata = UnitaNavaliCatalogo.First(existing => existing.UnitaNavaleId == item.UnitaNavaleId);
            NuovaUnitaNavale = string.Empty;
            Stato = $"Mezzo nautico aggiunto: {item.Descrizione}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Mezzi nautici", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Inserimento mezzo nautico non riuscito.";
        }
    }

    private void RimuoviSupportoOccasionale()
    {
        if (SelectedSupportoOccasionale is null)
        {
            return;
        }

        SelectedSupportoOccasionale.PropertyChanged -= ServizioSupportoOccasionale_PropertyChanged;
        ServizioSupportiOccasionaliBozza.Remove(SelectedSupportoOccasionale);
        SelectedSupportoOccasionale = null;
        AggiornaRiepilogoBozzaServizio();
    }

    private void RimuoviOperatoreSubEsterno()
    {
        if (SelectedOperatoreSubEsterno is null)
        {
            return;
        }

        SelectedOperatoreSubEsterno.PropertyChanged -= ServizioOperatoreSubEsterno_PropertyChanged;
        ServizioOperatoriSubEsterniBozza.Remove(SelectedOperatoreSubEsterno);
        SelectedOperatoreSubEsterno = null;
        SincronizzaPartecipazioniImmersioneBozza();
        AggiornaRiepilogoBozzaServizio();
    }


    private void EliminaServizioSelezionato()
    {
        if (SelectedServizioSalvato is null)
        {
            return;
        }

        var result = MessageBox.Show(
            $"Eliminare il servizio del {SelectedServizioSalvato.DataServizio:dd/MM/yyyy}?\n\nL'operazione rimuove testata, partecipanti e immersioni registrate.",
            "Conferma eliminazione servizio",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var servizioGiornalieroId = SelectedServizioSalvato.ServizioGiornalieroId;
            _repository.DeleteServizioGiornaliero(servizioGiornalieroId);

            if (_servizioGiornalieroId == servizioGiornalieroId)
            {
                NuovoServizioGiornaliero();
                IsSchedaServizioVisibile = false;
            }

            CaricaServiziSalvati();
            AggiornaAnniContabilitaDisponibili();
            AggiornaDatiMensili();
            Stato = $"Servizio giornaliero #{servizioGiornalieroId} eliminato.";
            EseguiBackupLocaleSilenzioso("delete-service");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Eliminazione servizio", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Eliminazione servizio non riuscita.";
        }
    }

    private void CaricaServizioGiornaliero(long servizioGiornalieroId)
    {
        var servizio = _repository.GetServizioGiornalieroById(servizioGiornalieroId);
        if (servizio is null)
        {
            MessageBox.Show("Servizio giornaliero non trovato.", "SMZ", MessageBoxButton.OK, MessageBoxImage.Warning);
            CaricaServiziSalvati();
            return;
        }

        _servizioGiornalieroId = servizio.ServizioGiornalieroId;
        ServizioData = FormatDate(servizio.DataServizio);
        ServizioNumeroOrdine = servizio.NumeroOrdineServizio;
        ServizioOrario = servizio.OrarioServizio;
        ServizioStraordinarioAttivo = servizio.StraordinarioAttivo;
        ServizioStraordinarioInizio = servizio.StraordinarioInizio;
        ServizioStraordinarioFine = servizio.StraordinarioFine;
        ServizioTipoSelezionato = NormalizeTipoServizio(servizio.TipoServizio, servizio.FuoriSede);
        ServizioLocalitaSelezionata = LocalitaOperativeServizioCatalogo.FirstOrDefault(item => item.LocalitaOperativaId == servizio.LocalitaOperativaId);
        ServizioScopoSelezionato = ScopiImmersioneCatalogo.FirstOrDefault(item => item.ScopoImmersioneId == servizio.ScopoImmersioneId);
        ServizioUnitaNavaleSelezionata = UnitaNavaliCatalogo.FirstOrDefault(item => item.UnitaNavaleId == servizio.UnitaNavaleId)
            ?? UnitaNavaliCatalogo.FirstOrDefault();
        ServizioResponsabileSelezionato = TrovaOperatoreServizio(servizio.ResponsabileServizioPerId);
        ServizioFuoriSede = servizio.FuoriSede;
        ServizioIndennitaOrdinePubblico = servizio.IndennitaOrdinePubblico;
        ServizioAttivitaSvolta = servizio.AttivitaSvolta;
        ServizioNote = servizio.Note;
        NuovaLocalitaOperativa = string.Empty;
        NuovaUnitaNavale = string.Empty;

        InizializzaBozzaServizio(preserveSelections: false);

        var partecipantiByPerId = servizio.Partecipanti.ToDictionary(item => item.PerId);
        foreach (var partecipante in ServizioPartecipantiBozza)
        {
            if (!partecipantiByPerId.TryGetValue(partecipante.PerId, out var saved))
            {
                partecipante.Presente = false;
                partecipante.GruppoOperativo = TrovaGruppoOperativo(partecipante.DefaultGruppoOperativoId);
                partecipante.RuoloOperativo = TrovaRuoloOperativo(partecipante.DefaultRuoloOperativoId);
                partecipante.Note = string.Empty;
                continue;
            }

            partecipante.Presente = saved.Presente;
            partecipante.GruppoOperativo = TrovaGruppoOperativo(saved.GruppoOperativoId);
            partecipante.RuoloOperativo = TrovaRuoloOperativo(saved.RuoloOperativoId);
            partecipante.Note = saved.Note;
        }

        AggiornaOperatoriServizioPresentiDisponibili();
        ServizioResponsabileSelezionato = TrovaOperatoreServizio(servizio.ResponsabileServizioPerId);
        AggiornaResponsabileServizioAutomatico();

        ServizioImmersioniBozza.Clear();
        foreach (var immersione in servizio.Immersioni.OrderBy(item => item.NumeroImmersione))
        {
            var item = new ServizioImmersioneDraftViewModel
            {
                NumeroImmersione = immersione.NumeroImmersione,
                DirettoreImmersione = TrovaOperatoreServizio(immersione.DirettoreImmersionePerId),
                OperatoreSoccorso = TrovaOperatoreServizio(immersione.OperatoreSoccorsoPerId),
                AssistenteBlsd = TrovaOperatoreServizio(immersione.AssistenteBlsdPerId),
                AssistenteSanitario = TrovaOperatoreServizio(immersione.AssistenteSanitarioPerId),
                Note = immersione.Note,
            };

            item.PropertyChanged += ServizioImmersioneBozza_PropertyChanged;
            ServizioImmersioniBozza.Add(item);
        }

        if (ServizioImmersioniBozza.Count == 0)
        {
            CreaImmersioniBozzaDefault();
        }

        ServizioOperatoriSubEsterniBozza.Clear();
        foreach (var operatore in servizio.OperatoriSubEsterni)
        {
            var item = new ServizioOperatoreSubEsternoDraftViewModel
            {
                PerId = operatore.PerId.ToString(),
                Qualifica = operatore.Qualifica,
                Nominativo = operatore.Nominativo,
                Reparto = operatore.Reparto,
                GruppoOperativo = TrovaGruppoOperativo(operatore.GruppoOperativoId),
                Note = operatore.Note,
            };

            item.PropertyChanged += ServizioOperatoreSubEsterno_PropertyChanged;
            ServizioOperatoriSubEsterniBozza.Add(item);
        }

        SincronizzaPartecipazioniImmersioneBozza();

        var perIdByServizioPartecipanteId = servizio.Partecipanti.ToDictionary(item => item.ServizioPartecipanteId, item => item.PerId);
        var perIdByServizioOperatoreEsternoId = servizio.OperatoriSubEsterni.ToDictionary(item => item.ServizioOperatoreSubEsternoId, item => item.PerId);
        foreach (var immersione in servizio.Immersioni)
        {
            var immersioneBozza = ServizioImmersioniBozza.FirstOrDefault(item => item.NumeroImmersione == immersione.NumeroImmersione);
            if (immersioneBozza is null)
            {
                continue;
            }

            foreach (var partecipazione in immersione.Partecipazioni)
            {
                if (!perIdByServizioPartecipanteId.TryGetValue(partecipazione.ServizioPartecipanteId, out var perId))
                {
                    continue;
                }

                var partecipazioneBozza = immersioneBozza.Partecipazioni.FirstOrDefault(item => item.PerId == perId);
                if (partecipazioneBozza is null)
                {
                    continue;
                }

                partecipazioneBozza.InImmersione = true;
                partecipazioneBozza.TipologiaImmersioneOperativa = TipologieImmersioneOperativeCatalogo.FirstOrDefault(item => item.TipologiaImmersioneOperativaId == partecipazione.TipologiaImmersioneOperativaId);
                partecipazioneBozza.ProfonditaMetri = partecipazione.ProfonditaMetri?.ToString() ?? string.Empty;
                partecipazioneBozza.FasciaProfondita = FasceProfonditaCatalogo.FirstOrDefault(item => item.FasciaProfonditaId == partecipazione.FasciaProfonditaId);
                partecipazioneBozza.OreImmersione = FormatDecimal(partecipazione.OreImmersione);
                partecipazioneBozza.CategoriaContabileOre = CategorieContabiliOreCatalogo.FirstOrDefault(item => item.CategoriaContabileOreId == partecipazione.CategoriaContabileOreId);
                partecipazioneBozza.Note = partecipazione.Note;
                AggiornaCalcoliPartecipazioneImmersione(partecipazioneBozza);
            }

            foreach (var partecipazione in immersione.PartecipazioniEsterne)
            {
                if (!perIdByServizioOperatoreEsternoId.TryGetValue(partecipazione.ServizioOperatoreSubEsternoId, out var perId))
                {
                    continue;
                }

                var partecipazioneBozza = immersioneBozza.Partecipazioni.FirstOrDefault(item => item.PerId == perId && item.IsEsterno);
                if (partecipazioneBozza is null)
                {
                    continue;
                }

                partecipazioneBozza.InImmersione = true;
                partecipazioneBozza.TipologiaImmersioneOperativa = TipologieImmersioneOperativeCatalogo.FirstOrDefault(item => item.TipologiaImmersioneOperativaId == partecipazione.TipologiaImmersioneOperativaId);
                partecipazioneBozza.ProfonditaMetri = partecipazione.ProfonditaMetri?.ToString() ?? string.Empty;
                partecipazioneBozza.FasciaProfondita = FasceProfonditaCatalogo.FirstOrDefault(item => item.FasciaProfonditaId == partecipazione.FasciaProfonditaId);
                partecipazioneBozza.OreImmersione = FormatDecimal(partecipazione.OreImmersione);
                partecipazioneBozza.CategoriaContabileOre = CategorieContabiliOreCatalogo.FirstOrDefault(item => item.CategoriaContabileOreId == partecipazione.CategoriaContabileOreId);
                partecipazioneBozza.Note = partecipazione.Note;
                AggiornaCalcoliPartecipazioneImmersione(partecipazioneBozza);
            }

            if (immersione.Partecipazioni.Count > 0 || immersione.PartecipazioniEsterne.Count > 0)
            {
                immersioneBozza.ProfonditaCondivisaInizializzata = true;
                immersioneBozza.OreCondiviseInizializzate = true;
            }
        }

        ServizioSupportiOccasionaliBozza.Clear();
        foreach (var supporto in servizio.SupportiOccasionali)
        {
            var item = new ServizioSupportoOccasionaleDraftViewModel
            {
                Nominativo = supporto.Nominativo,
                Qualifica = supporto.Qualifica,
                Ruolo = supporto.Ruolo,
                Presente = supporto.Presente,
                Contatti = supporto.Contatti,
                Note = supporto.Note,
            };

            item.PropertyChanged += ServizioSupportoOccasionale_PropertyChanged;
            ServizioSupportiOccasionaliBozza.Add(item);
        }

        SelectedSupportoOccasionale = null;
        SelectedOperatoreSubEsterno = null;

        SincronizzaPartecipazioniImmersioneBozza();
        SincronizzaPartecipazioniContabiliUnicheBozza(aggiornaDaPartecipazioni: true);
        AggiornaContestoServizio();
        AggiornaRiepilogoBozzaServizio();
        RegistraSnapshotServizio();
        Stato = $"Servizio giornaliero #{servizioGiornalieroId} caricato.";
    }

    private void NuovoServizioGiornaliero()
    {
        _servizioGiornalieroId = 0;
        SelectedServizioSalvato = null;
        IsServizioApertoDaReport = false;
        ServizioData = DateTime.Today.ToString("dd/MM/yyyy");
        ServizioNumeroOrdine = string.Empty;
        ServizioOrario = string.Empty;
        ServizioStraordinarioAttivo = false;
        ServizioStraordinarioInizio = string.Empty;
        ServizioStraordinarioFine = string.Empty;
        ServizioTipoSelezionato = "InSede";
        ServizioLocalitaSelezionata = LocalitaOperativeServizioCatalogo.FirstOrDefault();
        ServizioScopoSelezionato = ScopiImmersioneCatalogo.FirstOrDefault();
        ServizioUnitaNavaleSelezionata = UnitaNavaliCatalogo.FirstOrDefault();
        ServizioResponsabileSelezionato = null;
        ServizioFuoriSede = false;
        ServizioIndennitaOrdinePubblico = false;
        ServizioAttivitaSvolta = string.Empty;
        ServizioNote = string.Empty;
        NuovaLocalitaOperativa = string.Empty;
        NuovaUnitaNavale = string.Empty;

        foreach (var partecipante in ServizioPartecipantiBozza)
        {
            partecipante.Presente = false;
            partecipante.GruppoOperativo = TrovaGruppoOperativo(partecipante.DefaultGruppoOperativoId);
            partecipante.RuoloOperativo = TrovaRuoloOperativo(partecipante.DefaultRuoloOperativoId);
            partecipante.Note = string.Empty;
        }

        foreach (var immersione in ServizioImmersioniBozza)
        {
            foreach (var partecipazione in immersione.Partecipazioni)
            {
                partecipazione.PropertyChanged -= ServizioPartecipazioneImmersione_PropertyChanged;
            }

            immersione.PropertyChanged -= ServizioImmersioneBozza_PropertyChanged;
        }

        ServizioImmersioniBozza.Clear();
        CreaImmersioniBozzaDefault();
        SincronizzaPartecipazioniImmersioneBozza();
        AggiornaResponsabileServizioAutomatico();

        foreach (var supporto in ServizioSupportiOccasionaliBozza)
        {
            supporto.PropertyChanged -= ServizioSupportoOccasionale_PropertyChanged;
        }

        ServizioSupportiOccasionaliBozza.Clear();
        SelectedSupportoOccasionale = null;

        foreach (var operatore in ServizioOperatoriSubEsterniBozza)
        {
            operatore.PropertyChanged -= ServizioOperatoreSubEsterno_PropertyChanged;
        }

        ServizioOperatoriSubEsterniBozza.Clear();
        SelectedOperatoreSubEsterno = null;

        AggiornaContestoServizio();
        AggiornaRiepilogoBozzaServizio();
        RegistraSnapshotServizio();
        IsSchedaServizioVisibile = true;
        Stato = "Nuova bozza servizio giornaliero.";
    }
}
