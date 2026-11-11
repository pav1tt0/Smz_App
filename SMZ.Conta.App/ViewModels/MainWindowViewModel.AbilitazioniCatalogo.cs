using System.Collections.ObjectModel;
using System.Windows;
using SMZ.Conta.App.Infrastructure;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    private string _nuovaAbilitazione = string.Empty;

    public ObservableCollection<TipoAbilitazione> AbilitazioniGestioneCatalogo { get; } = [];

    public IEnumerable<TipoAbilitazione> TipiAbilitazioneSelezionabili => TipiAbilitazioneCatalogo
        .Where(tipo => tipo.Attiva || tipo.TipoAbilitazioneId == SelectedAbilitazione?.TipoAbilitazioneId);

    public RelayCommand AddTipoAbilitazioneCommand { get; private set; } = null!;

    public RelayCommand SaveTipiAbilitazioneCommand { get; private set; } = null!;

    public string NuovaAbilitazione
    {
        get => _nuovaAbilitazione;
        set
        {
            if (SetProperty(ref _nuovaAbilitazione, value))
                AddTipoAbilitazioneCommand.RaiseCanExecuteChanged();
        }
    }

    private void InizializzaGestioneAbilitazioni()
    {
        AddTipoAbilitazioneCommand = new RelayCommand(AggiungiTipoAbilitazione,
            () => IsAdministrator && !string.IsNullOrWhiteSpace(NuovaAbilitazione));
        SaveTipiAbilitazioneCommand = new RelayCommand(SalvaTipiAbilitazione, () => IsAdministrator);
        SostituisciCollection(AbilitazioniGestioneCatalogo, _repository.GetTipiAbilitazione(includeInactive: true));
    }

    private void AggiungiTipoAbilitazione()
    {
        if (!IsAdministrator) return;
        try
        {
            var tipo = _repository.AddTipoAbilitazione(NuovaAbilitazione);
            AbilitazioniGestioneCatalogo.Add(tipo);
            RicaricaTipiAbilitazione();
            NuovaAbilitazione = string.Empty;
            Stato = $"Abilitazione aggiunta: {tipo.Descrizione}";
            EseguiBackupLocaleSilenzioso("add-qualification-type");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Abilitazioni", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Inserimento abilitazione non riuscito.";
        }
    }

    private void SalvaTipiAbilitazione()
    {
        if (!IsAdministrator) return;
        try
        {
            _repository.UpdateTipiAbilitazione(AbilitazioniGestioneCatalogo);
            SostituisciCollection(AbilitazioniGestioneCatalogo, _repository.GetTipiAbilitazione(includeInactive: true));
            RicaricaTipiAbilitazione();
            AggiornaScadenziario();
            Stato = "Abilitazioni aggiornate.";
            EseguiBackupLocaleSilenzioso("save-qualification-types");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Abilitazioni", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Aggiornamento abilitazioni non riuscito.";
        }
    }

    private void RicaricaTipiAbilitazione()
    {
        var tipoSelezionatoId = AbilitazioneTipoSelezionato?.TipoAbilitazioneId;
        var filtroId = FiltroAbilitazione?.TipoAbilitazioneId;
        var livello = AbilitazioneLivello;
        var profondita = AbilitazioneProfondita;
        var scadenza = AbilitazioneDataScadenza;
        SostituisciCollection(TipiAbilitazioneCatalogo, _repository.GetTipiAbilitazione(includeInactive: true));
        SostituisciCollection(FiltroAbilitazioni,
            new[] { new AbilitazioneFilterOptionViewModel { Descrizione = "Tutte le abilitazioni" } }
                .Concat(TipiAbilitazioneCatalogo.Select(tipo => new AbilitazioneFilterOptionViewModel
                {
                    TipoAbilitazioneId = tipo.TipoAbilitazioneId, Descrizione = tipo.EtichettaCompleta,
                })));
        FiltroAbilitazione = FiltroAbilitazioni.FirstOrDefault(filtro => filtro.TipoAbilitazioneId == filtroId);
        OnPropertyChanged(nameof(TipiAbilitazioneSelezionabili));
        AbilitazioneTipoSelezionato = TipiAbilitazioneSelezionabili
            .FirstOrDefault(tipo => tipo.TipoAbilitazioneId == tipoSelezionatoId);
        AbilitazioneLivello = livello;
        AbilitazioneProfondita = profondita;
        AbilitazioneDataScadenza = scadenza;
        foreach (var row in Abilitazioni.Concat(ArchivioAbilitazioni))
        {
            var tipo = TipiAbilitazioneCatalogo.FirstOrDefault(item => item.TipoAbilitazioneId == row.TipoAbilitazioneId);
            if (tipo is null) continue;
            row.TipoDescrizione = tipo.Descrizione;
            row.Categoria = tipo.Categoria;
        }
        AggiornaRiepilogoScheda();
    }
}
