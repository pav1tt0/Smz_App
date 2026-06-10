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
    private void NuovoPersonale()
    {
        SelectedPersonale = null;
        PerId = 0;
        PerIdInput = string.Empty;
        Cognome = string.Empty;
        Nome = string.Empty;
        Qualifica = string.Empty;
        DataDecorrenzaQualifica = string.Empty;
        ProfiloPersonale = ProfiliPersonaleDisponibili[0];
        RuoloSanitario = string.Empty;
        CodiceFiscale = string.Empty;
        MatricolaPersonale = string.Empty;
        NumeroBrevettoSmz = string.Empty;
        StatoServizioPersonale = StatoServizioPersonaleCatalogo.Attivo;
        DataFineServizio = string.Empty;
        DataNascita = string.Empty;
        LuogoNascita = string.Empty;
        ViaResidenza = string.Empty;
        CapResidenza = string.Empty;
        CittaResidenza = string.Empty;
        Telefono1 = string.Empty;
        Telefono2 = string.Empty;
        Mail1Utente = string.Empty;
        Mail2Utente = string.Empty;
        Abilitazioni.Clear();
        VisiteMediche.Clear();
        AllineaVisitePredefinite();
        AllineaAttagliamentoPredefinito([]);
        PulisciEditorAbilitazione();
        PulisciEditorVisita();
        AggiornaRiepilogoScheda();
        RegistraSnapshotPersonale();
        Stato = "Nuova scheda personale";
    }

    private void CaricaPersonale(int perId)
    {
        var personale = _repository.GetPersonaleById(perId);
        if (personale is null)
        {
            MessageBox.Show("Scheda personale non trovata.", "SMZ", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        PerId = personale.PerId;
        PerIdInput = personale.PerId.ToString();
        Cognome = personale.Cognome;
        Nome = personale.Nome;
        Qualifica = personale.Qualifica;
        DataDecorrenzaQualifica = FormatDate(personale.DataDecorrenzaQualifica);
        ProfiloPersonale = ProfiliPersonaleCatalogo.Normalizza(personale.ProfiloPersonale);
        RuoloSanitario = personale.RuoloSanitario;
        CodiceFiscale = personale.CodiceFiscale;
        MatricolaPersonale = personale.MatricolaPersonale;
        NumeroBrevettoSmz = personale.NumeroBrevettoSmz;
        StatoServizioPersonale = personale.StatoServizio;
        DataFineServizio = FormatDate(personale.DataFineServizio);
        DataNascita = FormatDate(personale.DataNascita);
        LuogoNascita = personale.LuogoNascita;
        ViaResidenza = personale.ViaResidenza;
        CapResidenza = personale.CapResidenza;
        CittaResidenza = personale.CittaResidenza;
        Telefono1 = personale.Telefono1;
        Telefono2 = personale.Telefono2;
        Mail1Utente = personale.Mail1Utente;
        Mail2Utente = personale.Mail2Utente;

        Abilitazioni.Clear();
        foreach (var abilitazione in personale.Abilitazioni)
        {
            Abilitazioni.Add(PersonaleAbilitazioneRowViewModel.FromModel(abilitazione));
        }

        VisiteMediche.Clear();
        foreach (var visita in personale.VisiteMediche)
        {
            VisiteMediche.Add(VisitaMedicaRowViewModel.FromModel(visita));
        }

        AllineaVisitePredefinite();
        AllineaAttagliamentoPredefinito(personale.Attagliamento);
        PulisciEditorAbilitazione();
        PulisciEditorVisita();
        AggiornaRiepilogoScheda();
        RegistraSnapshotPersonale();
        Stato = $"Scheda caricata: {personale.NominativoCompleto}";
    }

    private void ApriSchedaSelezionata(object? parameter)
    {
        if (parameter is PersonaleListItemViewModel personale)
        {
            SelectedPersonale = personale;
        }

        if (SelectedPersonale is null)
        {
            return;
        }

        CaricaPersonale(SelectedPersonale.PerId);
        SezioneAttivaIndex = PersonalSectionIndex;
        IsSchedaPersonaleVisibile = true;
    }

    private void SalvaPersonale()
    {
        try
        {
            var personale = BuildModelFromEditor();
            if (!IsExistingPerson && _repository.ExistsPersonale(personale.PerId))
            {
                throw new InvalidOperationException($"Esiste gia una scheda con PerID {personale.PerId}.");
            }

            var perId = _repository.SavePersonale(personale, isNewRecord: !IsExistingPerson);
            RicaricaSuggerimentiRicerca();
            CaricaElenco();
            InizializzaBozzaServizio(preserveSelections: true);
            AggiornaScadenziario();
            SelectedPersonale = PersonaleItems.FirstOrDefault(item => item.PerId == perId);
            if (SelectedPersonale is null)
            {
                CaricaPersonale(perId);
            }

            Stato = $"Scheda salvata con PerID {perId}";
            RegistraSnapshotPersonale();
            EseguiBackupLocaleSilenzioso("save-person");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Salvataggio personale", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Salvataggio non riuscito";
        }
    }

    private void DisattivaPersonaleDaOggi()
    {
        if (PerId == 0)
        {
            return;
        }

        var result = MessageBox.Show(
            $"Impostare {Cognome} {Nome} come cessato dal servizio da oggi?\n\nLa scheda resta nello storico e non verra proposta nei nuovi servizi successivi a oggi.",
            "Conferma cessazione",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            StatoServizioPersonale = StatoServizioPersonaleCatalogo.Cessato;
            DataFineServizio = DateTime.Today.ToString("dd/MM/yyyy");
            var personale = BuildModelFromEditor();
            var perId = _repository.SavePersonale(personale, isNewRecord: false);
            RicaricaSuggerimentiRicerca();
            CaricaElenco();
            InizializzaBozzaServizio(preserveSelections: true);
            AggiornaScadenziario();
            CaricaPersonale(perId);
            Stato = "Scheda cessata. Resta disponibile nello storico e nei servizi fino alla data di fine servizio.";
            EseguiBackupLocaleSilenzioso("disable-person");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Cessazione personale", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Cessazione non riuscita";
        }
    }

    private void EliminaPersonaleDefinitivamente()
    {
        if (PerId == 0)
        {
            return;
        }

        var result = MessageBox.Show(
            $"Eliminare definitivamente la scheda di {Cognome} {Nome}?\n\nUsa questa funzione solo per schede create per errore. Se la scheda e collegata a servizi salvati, l'eliminazione sara bloccata.",
            "Conferma eliminazione definitiva",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var nominativo = $"{Cognome} {Nome}".Trim();
            _repository.DeletePersonaleDefinitivo(PerId);
            RicaricaSuggerimentiRicerca();
            CaricaElenco();
            InizializzaBozzaServizio(preserveSelections: true);
            AggiornaScadenziario();
            NuovoPersonale();
            Stato = $"Scheda eliminata definitivamente: {nominativo}.";
            EseguiBackupLocaleSilenzioso("delete-person");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Eliminazione definitiva personale", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Eliminazione definitiva non riuscita";
        }
    }

    private void RipristinaArchivioDaParametro(object? parameter)
    {
        if (parameter is PersonaleArchivioListItemViewModel archivio)
        {
            SelectedArchivio = archivio;
        }

        RipristinaArchivio();
    }

    private void RipristinaArchivio()
    {
        if (SelectedArchivio is null)
        {
            return;
        }

        var result = MessageBox.Show(
            $"Ripristinare la scheda archiviata di {SelectedArchivio.Nominativo}?\n\nLa scheda tornera nell'elenco operativo.",
            "Conferma ripristino",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var perIdOriginale = SelectedArchivio.PerIdOriginale;
            var perIdRipristinato = _repository.RestorePersonaleArchivio(SelectedArchivio.PersonaleArchivioId);

            RicaricaSuggerimentiRicerca();
            CaricaElenco();
            InizializzaBozzaServizio(preserveSelections: true);
            CaricaArchivio();
            AggiornaScadenziario();

            SelectedPersonale = PersonaleItems.FirstOrDefault(item => item.PerId == perIdRipristinato);
            if (SelectedPersonale is null)
            {
                CaricaPersonale(perIdRipristinato);
            }

            SezioneAttivaIndex = PersonalSectionIndex;
            Stato = perIdRipristinato == perIdOriginale
                ? $"Scheda ripristinata con PerID {perIdRipristinato}."
                : $"Scheda ripristinata con PerID {perIdRipristinato}. Il PerID originario {perIdOriginale} era gia occupato.";
            EseguiBackupLocaleSilenzioso("restore-archive-person");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ripristino archivio", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Ripristino non riuscito";
        }
    }

    private void EliminaArchivioDefinitivamenteDaParametro(object? parameter)
    {
        if (parameter is PersonaleArchivioListItemViewModel archivio)
        {
            SelectedArchivio = archivio;
        }

        EliminaArchivioDefinitivamente();
    }

    private void EliminaArchivioDefinitivamente()
    {
        if (SelectedArchivio is null)
        {
            return;
        }

        var result = MessageBox.Show(
            $"Eliminare definitivamente la scheda archiviata di {SelectedArchivio.Nominativo}?\n\nQuesta operazione non e recuperabile.",
            "Conferma eliminazione definitiva",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var nominativo = SelectedArchivio.Nominativo;
            _repository.DeletePersonaleArchivio(SelectedArchivio.PersonaleArchivioId);
            CaricaArchivio();
            Stato = $"Scheda archiviata eliminata definitivamente: {nominativo}.";
            EseguiBackupLocaleSilenzioso("delete-archive-person");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Eliminazione archivio", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Eliminazione definitiva non riuscita";
        }
    }

    private void SalvaAbilitazioneInEditor()
    {
        try
        {
            if (AbilitazioneTipoSelezionato is null)
            {
                throw new InvalidOperationException("Seleziona prima il tipo di abilitazione.");
            }

            if (AbilitazioneRichiedeLivello && string.IsNullOrWhiteSpace(AbilitazioneLivello))
            {
                throw new InvalidOperationException("Per questa abilitazione il livello e obbligatorio.");
            }

            if (AbilitazioneRichiedeLivello
                && AbilitazioneLivelliSuggeriti.Count > 0
                && !AbilitazioneLivelliSuggeriti.Contains(AbilitazioneLivello.Trim(), StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Per questa abilitazione seleziona un certificato valido: {string.Join(", ", AbilitazioneLivelliSuggeriti)}.");
            }

            if (AbilitazioneRichiedeProfondita && string.IsNullOrWhiteSpace(AbilitazioneProfondita))
            {
                throw new InvalidOperationException("Per questa abilitazione la profondita e obbligatoria.");
            }

            if (AbilitazioneRichiedeScadenza && string.IsNullOrWhiteSpace(AbilitazioneDataScadenza))
            {
                throw new InvalidOperationException("Per questa abilitazione la scadenza e obbligatoria.");
            }

            ParseNullableInt(AbilitazioneProfondita, $"Profondita abilitazione {AbilitazioneTipoSelezionato.Descrizione}");
            ParseDate(AbilitazioneDataConseguimento, $"Data conseguimento {AbilitazioneTipoSelezionato.Descrizione}");
            ParseDate(AbilitazioneDataScadenza, $"Data scadenza {AbilitazioneTipoSelezionato.Descrizione}");

            var nuovaRiga = PersonaleAbilitazioneRowViewModel.FromDraft(
                AbilitazioneTipoSelezionato,
                SelectedAbilitazione?.PersonaleAbilitazioneId,
                AbilitazioneLivello.Trim(),
                AbilitazioneProfondita.Trim(),
                AbilitazioneDataConseguimento.Trim(),
                AbilitazioneDataScadenza.Trim(),
                AbilitazioneNote.Trim());

            if (SelectedAbilitazione is null)
            {
                Abilitazioni.Add(nuovaRiga);
            }
            else
            {
                var index = Abilitazioni.IndexOf(SelectedAbilitazione);
                if (index >= 0)
                {
                    Abilitazioni[index] = nuovaRiga;
                }
            }

            PulisciEditorAbilitazione();
            AggiornaRiepilogoScheda();
            Stato = "Abilitazione pronta in scheda. Salvare il personale per registrarla nel database.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Abilitazione", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Abilitazione non aggiunta";
        }
    }

    private void PulisciEditorAbilitazione()
    {
        _selectedAbilitazione = null;
        OnPropertyChanged(nameof(SelectedAbilitazione));
        OnPropertyChanged(nameof(AzioneAbilitazioneLabel));

        AbilitazioneTipoSelezionato = null;
        AbilitazioneLivello = string.Empty;
        AbilitazioneProfondita = string.Empty;
        AbilitazioneDataConseguimento = string.Empty;
        AbilitazioneDataScadenza = string.Empty;
        AbilitazioneNote = string.Empty;
    }

    private void CaricaEditorAbilitazioneDaSelezione()
    {
        if (SelectedAbilitazione is null)
        {
            AbilitazioneTipoSelezionato = null;
            AbilitazioneLivello = string.Empty;
            AbilitazioneProfondita = string.Empty;
            AbilitazioneDataConseguimento = string.Empty;
            AbilitazioneDataScadenza = string.Empty;
            AbilitazioneNote = string.Empty;
            return;
        }

        AbilitazioneTipoSelezionato = TipiAbilitazioneCatalogo.FirstOrDefault(tipo => tipo.TipoAbilitazioneId == SelectedAbilitazione.TipoAbilitazioneId);
        AbilitazioneLivello = SelectedAbilitazione.Livello;
        AbilitazioneProfondita = SelectedAbilitazione.ProfonditaMetri;
        AbilitazioneDataConseguimento = SelectedAbilitazione.DataConseguimento;
        AbilitazioneDataScadenza = SelectedAbilitazione.DataScadenza;
        AbilitazioneNote = SelectedAbilitazione.Note;
    }

    private void AggiornaProfonditaSuggerite()
    {
        AbilitazioneProfonditaSuggerite.Clear();

        if (AbilitazioneTipoSelezionato?.ProfonditaSuggerite is null)
        {
            return;
        }

        foreach (var profondita in AbilitazioneTipoSelezionato.ProfonditaSuggerite)
        {
            AbilitazioneProfonditaSuggerite.Add(profondita);
        }
    }

    private void AggiornaLivelliSuggeriti()
    {
        AbilitazioneLivelliSuggeriti.Clear();

        if (AbilitazioneTipoSelezionato?.LivelliSuggeriti is null)
        {
            return;
        }

        foreach (var livello in AbilitazioneTipoSelezionato.LivelliSuggeriti)
        {
            AbilitazioneLivelliSuggeriti.Add(livello);
        }
    }

    private void RimuoviAbilitazioneRiga()
    {
        if (SelectedAbilitazione is null)
        {
            return;
        }

        Abilitazioni.Remove(SelectedAbilitazione);
        PulisciEditorAbilitazione();
        AggiornaRiepilogoScheda();
    }

    private void SalvaVisitaInEditor()
    {
        try
        {
            if (VisitaTipoSelezionato is null)
            {
                throw new InvalidOperationException("Seleziona prima il tipo visita.");
            }

            var dataUltimaVisita = ParseDate(VisitaDataUltimaVisita, $"Data ultima visita {VisitaTipoSelezionato.Descrizione}");
            if (dataUltimaVisita is null)
            {
                throw new InvalidOperationException("La data ultima visita e obbligatoria.");
            }

            var dataScadenza = CalcolaScadenzaVisita(VisitaTipoSelezionato.Descrizione, dataUltimaVisita);
            var nuovaRiga = new VisitaMedicaRowViewModel
            {
                VisitaMedicaId = SelectedVisita?.VisitaMedicaId,
                TipoVisita = VisitaTipoSelezionato.Descrizione,
                DataUltimaVisita = FormatDate(dataUltimaVisita),
                DataScadenza = FormatDate(dataScadenza),
                Esito = VisitaEsito.Trim(),
                Note = VisitaNote.Trim(),
            };

            var visitaSelezionata = SelectedVisita
                ?? VisiteMediche.FirstOrDefault(item =>
                    string.Equals(item.TipoVisita, VisitaTipoSelezionato.Descrizione, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Seleziona prima il tipo visita.");
            var index = VisiteMediche.IndexOf(visitaSelezionata);
            if (index >= 0)
            {
                VisiteMediche[index] = nuovaRiga;
                SelectedVisita = VisiteMediche[index];
            }

            AggiornaRiepilogoScheda();
            Stato = "Visita pronta in scheda. Salvare il personale per registrarla nel database.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Visita medica", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Visita non aggiunta";
        }
    }

    private void RimuoviVisitaRiga()
    {
        if (SelectedVisita is null)
        {
            return;
        }

        var tipoVisita = SelectedVisita.TipoVisita;
        var index = VisiteMediche.IndexOf(SelectedVisita);
        if (index >= 0)
        {
            VisiteMediche[index] = new VisitaMedicaRowViewModel
            {
                TipoVisita = tipoVisita,
            };
            SelectedVisita = VisiteMediche[index];
        }

        AggiornaRiepilogoScheda();
    }

    private void AllineaAttagliamentoPredefinito(IEnumerable<PersonaleAttagliamento> attagliamentoEsistente)
    {
        var righeEsistenti = attagliamentoEsistente
            .Select(PersonaleAttagliamentoRowViewModel.FromModel)
            .ToList();

        var perVoce = righeEsistenti.ToDictionary(
            item => item.Voce.Trim(),
            item => item,
            StringComparer.OrdinalIgnoreCase);

        Attagliamento.Clear();

        foreach (var definizione in CatalogoAttagliamento.MisurePredefinite)
        {
            perVoce.TryGetValue(definizione.Voce, out var esistente);
            Attagliamento.Add(PersonaleAttagliamentoRowViewModel.FromDefinition(definizione, esistente));
        }

        var extras = righeEsistenti
            .Where(item => !CatalogoAttagliamento.IsPredefinita(item.Voce))
            .OrderBy(item => item.Voce)
            .ToList();

        for (var index = 0; index < extras.Count; index++)
        {
            var extra = extras[index];
            extra.OrdineScheda = 100 + index;
            extra.NumeroScheda = string.Empty;
            extra.EtichettaScheda = extra.Voce;
            extra.UnitaScheda = string.Empty;
            extra.IsPredefinita = false;
            Attagliamento.Add(extra);
        }

        AggiornaStatoAttagliamento();
    }

    private void AggiornaStatoAttagliamento()
    {
        OnPropertyChanged(nameof(AttagliamentoSchedaItems));
        OnPropertyChanged(nameof(AttagliamentoAggiuntivoItems));
        OnPropertyChanged(nameof(HasAttagliamentoAggiuntivo));
    }
}
