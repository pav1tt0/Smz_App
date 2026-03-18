using System.Collections.ObjectModel;
using System.Windows;
using SMZ.Conta.App.Data;
using SMZ.Conta.App.Infrastructure;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private readonly PersonaleRepository _repository = new();
    private readonly RelayCommand _deleteCommand;
    private readonly RelayCommand _openSelectedPersonaleCommand;
    private readonly List<string> _allSearchSuggestions;
    private PersonaleListItemViewModel? _selectedPersonale;
    private PersonaleAbilitazioneRowViewModel? _selectedAbilitazione;
    private VisitaMedicaRowViewModel? _selectedVisita;
    private string? _selectedSearchSuggestion;
    private string _filtroCognome = string.Empty;
    private bool _isSearchSuggestionsOpen;
    private AbilitazioneFilterOptionViewModel? _filtroAbilitazione;
    private string _filtroVisiteEntro = string.Empty;
    private int _perId;
    private string _perIdInput = string.Empty;
    private string _cognome = string.Empty;
    private string _nome = string.Empty;
    private string _codiceFiscale = string.Empty;
    private string _dataNascita = string.Empty;
    private string _luogoNascita = string.Empty;
    private string _indirizzoResidenza = string.Empty;
    private string _telefono = string.Empty;
    private string _mail = string.Empty;
    private string _stato = "Pronto";
    private TipoAbilitazione? _abilitazioneTipoSelezionato;
    private string _abilitazioneLivello = string.Empty;
    private string _abilitazioneProfondita = string.Empty;
    private string _abilitazioneDataConseguimento = string.Empty;
    private string _abilitazioneDataScadenza = string.Empty;
    private string _abilitazioneNote = string.Empty;
    private TipoVisitaMedica? _visitaTipoSelezionato;
    private string _visitaDataUltimaVisita = string.Empty;
    private string _visitaEsito = string.Empty;
    private string _visitaNote = string.Empty;

    public MainWindowViewModel()
    {
        SearchCommand = new RelayCommand(CaricaElenco);
        ClearFiltersCommand = new RelayCommand(PulisciFiltri);
        NewCommand = new RelayCommand(NuovoPersonale);
        SaveCommand = new RelayCommand(SalvaPersonale);
        _deleteCommand = new RelayCommand(EliminaPersonale, () => PerId > 0);
        _openSelectedPersonaleCommand = new RelayCommand(ApriSchedaSelezionata, () => SelectedPersonale is not null);
        SaveAbilitazioneCommand = new RelayCommand(SalvaAbilitazioneInEditor);
        ClearAbilitazioneEditorCommand = new RelayCommand(PulisciEditorAbilitazione);
        RemoveAbilitazioneCommand = new RelayCommand(RimuoviAbilitazioneRiga);
        SaveVisitaCommand = new RelayCommand(SalvaVisitaInEditor);
        ClearVisitaEditorCommand = new RelayCommand(PulisciEditorVisita);
        AddVisitaCommand = new RelayCommand(PulisciEditorVisita);
        RemoveVisitaCommand = new RelayCommand(RimuoviVisitaRiga);

        Abilitazioni = new ObservableCollection<PersonaleAbilitazioneRowViewModel>();
        VisiteMediche = new ObservableCollection<VisitaMedicaRowViewModel>();
        ScadenzeProssime = new ObservableCollection<ScadenzaItemViewModel>();
        SearchSuggestions = new ObservableCollection<string>();
        TipiAbilitazioneCatalogo = new ObservableCollection<TipoAbilitazione>(_repository.GetTipiAbilitazione());
        _allSearchSuggestions = _repository.GetSearchSuggestions();
        FiltroAbilitazioni = new ObservableCollection<AbilitazioneFilterOptionViewModel>(
        [
            new AbilitazioneFilterOptionViewModel { TipoAbilitazioneId = null, Descrizione = "Tutte le abilitazioni" },
            ..TipiAbilitazioneCatalogo.Select(tipo => new AbilitazioneFilterOptionViewModel
            {
                TipoAbilitazioneId = tipo.TipoAbilitazioneId,
                Descrizione = tipo.EtichettaCompleta,
            })
        ]);

        _filtroAbilitazione = FiltroAbilitazioni.FirstOrDefault();
        AggiornaSuggerimentiRicerca();

        CaricaElenco();
        AggiornaScadenziario();
        NuovoPersonale();
    }

    public string Titolo => "SMZ Conta";

    public string Sottotitolo => "Gestione personale, abilitazioni professionali e visite mediche";

    public ObservableCollection<PersonaleListItemViewModel> PersonaleItems { get; } = [];

    public ObservableCollection<string> SearchSuggestions { get; }

    public ObservableCollection<TipoAbilitazione> TipiAbilitazioneCatalogo { get; }

    public ObservableCollection<AbilitazioneFilterOptionViewModel> FiltroAbilitazioni { get; }

    public ObservableCollection<PersonaleAbilitazioneRowViewModel> Abilitazioni { get; }

    public ObservableCollection<VisitaMedicaRowViewModel> VisiteMediche { get; }

    public ObservableCollection<ScadenzaItemViewModel> ScadenzeProssime { get; }

    public ObservableCollection<TipoVisitaMedica> TipiVisitaMedicaCatalogo { get; } =
        new(CatalogoVisiteMediche.Tutte);

    public RelayCommand SearchCommand { get; }

    public RelayCommand ClearFiltersCommand { get; }

    public RelayCommand NewCommand { get; }

    public RelayCommand SaveCommand { get; }

    public RelayCommand DeleteCommand => _deleteCommand;

    public RelayCommand OpenSelectedPersonaleCommand => _openSelectedPersonaleCommand;

    public RelayCommand SaveAbilitazioneCommand { get; }

    public RelayCommand ClearAbilitazioneEditorCommand { get; }

    public RelayCommand RemoveAbilitazioneCommand { get; }

    public RelayCommand SaveVisitaCommand { get; }

    public RelayCommand ClearVisitaEditorCommand { get; }

    public RelayCommand AddVisitaCommand { get; }

    public RelayCommand RemoveVisitaCommand { get; }

    public PersonaleListItemViewModel? SelectedPersonale
    {
        get => _selectedPersonale;
        set
        {
            if (SetProperty(ref _selectedPersonale, value) && value is not null)
            {
                _openSelectedPersonaleCommand.RaiseCanExecuteChanged();
                CaricaPersonale(value.PerId);
            }
            else
            {
                _openSelectedPersonaleCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public PersonaleAbilitazioneRowViewModel? SelectedAbilitazione
    {
        get => _selectedAbilitazione;
        set
        {
            if (SetProperty(ref _selectedAbilitazione, value))
            {
                CaricaEditorAbilitazioneDaSelezione();
                OnPropertyChanged(nameof(AzioneAbilitazioneLabel));
            }
        }
    }

    public VisitaMedicaRowViewModel? SelectedVisita
    {
        get => _selectedVisita;
        set
        {
            if (SetProperty(ref _selectedVisita, value))
            {
                CaricaEditorVisitaDaSelezione();
                OnPropertyChanged(nameof(AzioneVisitaLabel));
            }
        }
    }

    public string FiltroCognome
    {
        get => _filtroCognome;
        set
        {
            if (SetProperty(ref _filtroCognome, value))
            {
                AggiornaSuggerimentiRicerca();
            }
        }
    }

    public string? SelectedSearchSuggestion
    {
        get => _selectedSearchSuggestion;
        set
        {
            if (SetProperty(ref _selectedSearchSuggestion, value) && !string.IsNullOrWhiteSpace(value))
            {
                FiltroCognome = value;
                IsSearchSuggestionsOpen = false;
            }
        }
    }

    public bool IsSearchSuggestionsOpen
    {
        get => _isSearchSuggestionsOpen;
        set => SetProperty(ref _isSearchSuggestionsOpen, value);
    }

    public AbilitazioneFilterOptionViewModel? FiltroAbilitazione
    {
        get => _filtroAbilitazione;
        set => SetProperty(ref _filtroAbilitazione, value);
    }

    public string FiltroVisiteEntro
    {
        get => _filtroVisiteEntro;
        set => SetProperty(ref _filtroVisiteEntro, value);
    }

    public int PerId
    {
        get => _perId;
        set
        {
            if (SetProperty(ref _perId, value))
            {
                OnPropertyChanged(nameof(IsExistingPerson));
                _deleteCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string PerIdInput
    {
        get => _perIdInput;
        set
        {
            if (SetProperty(ref _perIdInput, value))
            {
                OnPropertyChanged(nameof(SchedaRiepilogoPerId));
            }
        }
    }

    public bool IsExistingPerson => PerId > 0;

    public string Cognome
    {
        get => _cognome;
        set
        {
            if (SetProperty(ref _cognome, value))
            {
                OnPropertyChanged(nameof(SchedaRiepilogoTitolo));
            }
        }
    }

    public string Nome
    {
        get => _nome;
        set
        {
            if (SetProperty(ref _nome, value))
            {
                OnPropertyChanged(nameof(SchedaRiepilogoTitolo));
            }
        }
    }

    public string CodiceFiscale
    {
        get => _codiceFiscale;
        set => SetProperty(ref _codiceFiscale, value);
    }

    public string DataNascita
    {
        get => _dataNascita;
        set => SetProperty(ref _dataNascita, value);
    }

    public string LuogoNascita
    {
        get => _luogoNascita;
        set => SetProperty(ref _luogoNascita, value);
    }

    public string IndirizzoResidenza
    {
        get => _indirizzoResidenza;
        set => SetProperty(ref _indirizzoResidenza, value);
    }

    public string Telefono
    {
        get => _telefono;
        set => SetProperty(ref _telefono, value);
    }

    public string Mail
    {
        get => _mail;
        set => SetProperty(ref _mail, value);
    }

    public TipoAbilitazione? AbilitazioneTipoSelezionato
    {
        get => _abilitazioneTipoSelezionato;
        set
        {
            if (SetProperty(ref _abilitazioneTipoSelezionato, value))
            {
                if (!(value?.RichiedeLivello ?? false))
                {
                    AbilitazioneLivello = string.Empty;
                }

                if (!(value?.RichiedeProfondita ?? false))
                {
                    AbilitazioneProfondita = string.Empty;
                }

                if (!(value?.RichiedeScadenza ?? false))
                {
                    AbilitazioneDataScadenza = string.Empty;
                }

                OnPropertyChanged(nameof(AbilitazioneRichiedeLivello));
                OnPropertyChanged(nameof(AbilitazioneRichiedeProfondita));
                OnPropertyChanged(nameof(AbilitazioneRichiedeScadenza));
                OnPropertyChanged(nameof(AbilitazioneIndicazioni));
            }
        }
    }

    public string AbilitazioneLivello
    {
        get => _abilitazioneLivello;
        set => SetProperty(ref _abilitazioneLivello, value);
    }

    public string AbilitazioneProfondita
    {
        get => _abilitazioneProfondita;
        set => SetProperty(ref _abilitazioneProfondita, value);
    }

    public string AbilitazioneDataConseguimento
    {
        get => _abilitazioneDataConseguimento;
        set => SetProperty(ref _abilitazioneDataConseguimento, value);
    }

    public string AbilitazioneDataScadenza
    {
        get => _abilitazioneDataScadenza;
        set => SetProperty(ref _abilitazioneDataScadenza, value);
    }

    public string AbilitazioneNote
    {
        get => _abilitazioneNote;
        set => SetProperty(ref _abilitazioneNote, value);
    }

    public TipoVisitaMedica? VisitaTipoSelezionato
    {
        get => _visitaTipoSelezionato;
        set
        {
            if (SetProperty(ref _visitaTipoSelezionato, value))
            {
                OnPropertyChanged(nameof(VisitaScadenzaCalcolata));
                OnPropertyChanged(nameof(VisitaIndicazioni));
            }
        }
    }

    public string VisitaDataUltimaVisita
    {
        get => _visitaDataUltimaVisita;
        set
        {
            if (SetProperty(ref _visitaDataUltimaVisita, value))
            {
                OnPropertyChanged(nameof(VisitaScadenzaCalcolata));
            }
        }
    }

    public string VisitaEsito
    {
        get => _visitaEsito;
        set => SetProperty(ref _visitaEsito, value);
    }

    public string VisitaNote
    {
        get => _visitaNote;
        set => SetProperty(ref _visitaNote, value);
    }

    public bool AbilitazioneRichiedeLivello => AbilitazioneTipoSelezionato?.RichiedeLivello ?? false;

    public bool AbilitazioneRichiedeProfondita => AbilitazioneTipoSelezionato?.RichiedeProfondita ?? false;

    public bool AbilitazioneRichiedeScadenza => AbilitazioneTipoSelezionato?.RichiedeScadenza ?? false;

    public string AzioneAbilitazioneLabel => SelectedAbilitazione is null ? "Aggiungi abilitazione" : "Aggiorna abilitazione";

    public string AzioneVisitaLabel => SelectedVisita is null ? "Aggiungi visita" : "Aggiorna visita";

    public string AbilitazioneIndicazioni
    {
        get
        {
            if (AbilitazioneTipoSelezionato is null)
            {
                return "Seleziona un tipo per vedere quali campi sono richiesti.";
            }

            var richieste = new List<string> { "Data conseguimento facoltativa" };

            if (AbilitazioneTipoSelezionato.RichiedeLivello)
            {
                richieste.Add("Livello richiesto");
            }

            if (AbilitazioneTipoSelezionato.RichiedeProfondita)
            {
                richieste.Add("Profondita richiesta");
            }

            if (AbilitazioneTipoSelezionato.RichiedeScadenza)
            {
                richieste.Add("Scadenza richiesta");
            }

            return string.Join(" | ", richieste);
        }
    }

    public string ScadenzeTitolo => "Scadenziario automatico: prossimi 90 giorni";

    public string RegoleVisiteTitolo => "Regole visite mediche";

    public string RegoleVisiteDescrizione =>
        "Mantenimento brevetto M.M. e D.Lgs. 81/08: scadenza automatica a 24 mesi dalla data visita. "
        + "Visita bimestrale: scadenza automatica a 2 mesi.";

    public string VisitaScadenzaCalcolata
    {
        get
        {
            var dataUltimaVisita = TryParseDate(VisitaDataUltimaVisita);
            if (VisitaTipoSelezionato is null || dataUltimaVisita is null || VisitaTipoSelezionato.MesiValidita is null)
            {
                return "Scadenza automatica non disponibile";
            }

            return dataUltimaVisita.Value.AddMonths(VisitaTipoSelezionato.MesiValidita.Value).ToString("dd/MM/yyyy");
        }
    }

    public string VisitaIndicazioni
    {
        get
        {
            if (VisitaTipoSelezionato is null)
            {
                return "Seleziona un tipo visita per vedere la regola di scadenza.";
            }

            return VisitaTipoSelezionato.RegolaScadenza;
        }
    }

    public string SchedaRiepilogoTitolo => string.IsNullOrWhiteSpace(Cognome) && string.IsNullOrWhiteSpace(Nome)
        ? "Nuova scheda"
        : $"{Cognome} {Nome}".Trim();

    public string SchedaRiepilogoPerId => string.IsNullOrWhiteSpace(PerIdInput) ? "PerID non impostato" : $"PerID {PerIdInput}";

    public int SchedaAbilitazioniTotali => Abilitazioni.Count;

    public int SchedaVisiteTotali => VisiteMediche.Count;

    public string SchedaProssimaScadenza
    {
        get
        {
            var prossima = CalcolaProssimaScadenzaScheda();
            return prossima is null ? "Nessuna scadenza registrata" : prossima.Value.data.ToString("dd/MM/yyyy");
        }
    }

    public string SchedaProssimaScadenzaDettaglio
    {
        get
        {
            var prossima = CalcolaProssimaScadenzaScheda();
            return prossima is null ? "Aggiungi abilitazioni o visite con scadenza." : $"{prossima.Value.origine}: {prossima.Value.titolo}";
        }
    }

    public string Stato
    {
        get => _stato;
        set => SetProperty(ref _stato, value);
    }

    private void CaricaElenco()
    {
        try
        {
            var ricercaPerNomeAttiva = !string.IsNullOrWhiteSpace(FiltroCognome);
            var filtroAbilitazioneEffettivo = ricercaPerNomeAttiva ? null : FiltroAbilitazione?.TipoAbilitazioneId;

            var items = _repository.SearchPersonale(
                FiltroCognome,
                filtroAbilitazioneEffettivo,
                ParseDate(FiltroVisiteEntro, "Filtro visite entro"));

            PersonaleItems.Clear();
            foreach (var personale in items)
            {
                PersonaleItems.Add(PersonaleListItemViewModel.FromModel(personale));
            }

            if (PersonaleItems.Count == 1)
            {
                SelectedPersonale = PersonaleItems[0];
            }

            IsSearchSuggestionsOpen = false;
            Stato = ricercaPerNomeAttiva && FiltroAbilitazione?.TipoAbilitazioneId is not null
                ? $"{PersonaleItems.Count} dipendenti trovati. Ricerca per cognome/nome attiva: filtro abilitazione sospeso."
                : $"{PersonaleItems.Count} dipendenti trovati";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ricerca personale", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Errore nella ricerca";
        }
    }

    private void AggiornaScadenziario()
    {
        var oggi = DateOnly.FromDateTime(DateTime.Today);
        var finoA = oggi.AddDays(90);
        var items = _repository.GetScadenzeProssime(oggi, finoA);

        ScadenzeProssime.Clear();
        foreach (var item in items.Take(12))
        {
            ScadenzeProssime.Add(ScadenzaItemViewModel.FromModel(item));
        }

        OnPropertyChanged(nameof(ScadenzeTotali));
        OnPropertyChanged(nameof(ScadenzeUrgenti));
    }

    public int ScadenzeTotali => ScadenzeProssime.Count;

    public int ScadenzeUrgenti => ScadenzeProssime.Count(item => item.IsUrgent);

    private void PulisciFiltri()
    {
        FiltroCognome = string.Empty;
        FiltroAbilitazione = FiltroAbilitazioni.FirstOrDefault();
        FiltroVisiteEntro = string.Empty;
        SelectedPersonale = null;
        IsSearchSuggestionsOpen = false;
        CaricaElenco();
    }

    private void NuovoPersonale()
    {
        SelectedPersonale = null;
        PerId = 0;
        PerIdInput = string.Empty;
        Cognome = string.Empty;
        Nome = string.Empty;
        CodiceFiscale = string.Empty;
        DataNascita = string.Empty;
        LuogoNascita = string.Empty;
        IndirizzoResidenza = string.Empty;
        Telefono = string.Empty;
        Mail = string.Empty;
        Abilitazioni.Clear();
        VisiteMediche.Clear();
        PulisciEditorAbilitazione();
        PulisciEditorVisita();
        AggiornaRiepilogoScheda();
        Stato = "Nuova scheda personale";
    }

    private void CaricaPersonale(int perId)
    {
        var personale = _repository.GetPersonaleById(perId);
        if (personale is null)
        {
            MessageBox.Show("Scheda personale non trovata.", "SMZ Conta", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        PerId = personale.PerId;
        PerIdInput = personale.PerId.ToString();
        Cognome = personale.Cognome;
        Nome = personale.Nome;
        CodiceFiscale = personale.CodiceFiscale;
        DataNascita = FormatDate(personale.DataNascita);
        LuogoNascita = personale.LuogoNascita;
        IndirizzoResidenza = personale.IndirizzoResidenza;
        Telefono = personale.Telefono;
        Mail = personale.Mail;

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

        PulisciEditorAbilitazione();
        PulisciEditorVisita();
        AggiornaRiepilogoScheda();
        Stato = $"Scheda caricata: {personale.NominativoCompleto}";
    }

    private void ApriSchedaSelezionata()
    {
        if (SelectedPersonale is null)
        {
            return;
        }

        CaricaPersonale(SelectedPersonale.PerId);
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
            AggiornaScadenziario();
            SelectedPersonale = PersonaleItems.FirstOrDefault(item => item.PerId == perId);
            if (SelectedPersonale is null)
            {
                CaricaPersonale(perId);
            }

            Stato = $"Scheda salvata con PerID {perId}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Salvataggio personale", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Salvataggio non riuscito";
        }
    }

    private void EliminaPersonale()
    {
        if (PerId == 0)
        {
            return;
        }

        var result = MessageBox.Show(
            $"Eliminare la scheda di {Cognome} {Nome}?",
            "Conferma eliminazione",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        _repository.DeletePersonale(PerId);
        RicaricaSuggerimentiRicerca();
        CaricaElenco();
        AggiornaScadenziario();
        NuovoPersonale();
        Stato = "Scheda eliminata";
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

            if (SelectedVisita is null)
            {
                VisiteMediche.Add(nuovaRiga);
            }
            else
            {
                var index = VisiteMediche.IndexOf(SelectedVisita);
                if (index >= 0)
                {
                    VisiteMediche[index] = nuovaRiga;
                }
            }

            PulisciEditorVisita();
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

        VisiteMediche.Remove(SelectedVisita);
        PulisciEditorVisita();
        AggiornaRiepilogoScheda();
    }

    private Personale BuildModelFromEditor()
    {
        if (string.IsNullOrWhiteSpace(Cognome))
        {
            throw new InvalidOperationException("Il cognome e obbligatorio.");
        }

        if (string.IsNullOrWhiteSpace(Nome))
        {
            throw new InvalidOperationException("Il nome e obbligatorio.");
        }

        if (string.IsNullOrWhiteSpace(CodiceFiscale))
        {
            throw new InvalidOperationException("Il codice fiscale e obbligatorio.");
        }

        return new Personale
        {
            PerId = ParseRequiredPerId(),
            Cognome = Cognome.Trim(),
            Nome = Nome.Trim(),
            CodiceFiscale = CodiceFiscale.Trim().ToUpperInvariant(),
            DataNascita = ParseDate(DataNascita, "Data di nascita"),
            LuogoNascita = LuogoNascita.Trim(),
            IndirizzoResidenza = IndirizzoResidenza.Trim(),
            Telefono = Telefono.Trim(),
            Mail = Mail.Trim(),
            Abilitazioni = BuildAbilitazioni(),
            VisiteMediche = BuildVisite(),
        };
    }

    private List<PersonaleAbilitazione> BuildAbilitazioni()
    {
        var items = new List<PersonaleAbilitazione>();

        foreach (var row in Abilitazioni)
        {
            if (row.TipoAbilitazioneId is null)
            {
                throw new InvalidOperationException("Ogni riga abilitazione deve avere un tipo selezionato.");
            }

            var tipo = TipiAbilitazioneCatalogo.Single(item => item.TipoAbilitazioneId == row.TipoAbilitazioneId.Value);

            items.Add(new PersonaleAbilitazione
            {
                PersonaleAbilitazioneId = row.PersonaleAbilitazioneId ?? 0,
                PerId = PerId,
                TipoAbilitazioneId = tipo.TipoAbilitazioneId,
                Tipo = tipo,
                Livello = row.Livello.Trim(),
                ProfonditaMetri = ParseNullableInt(row.ProfonditaMetri, $"Profondita abilitazione {tipo.Descrizione}"),
                DataConseguimento = ParseDate(row.DataConseguimento, $"Data conseguimento {tipo.Descrizione}"),
                DataScadenza = ParseDate(row.DataScadenza, $"Data scadenza {tipo.Descrizione}"),
                Note = row.Note.Trim(),
            });
        }

        return items;
    }

    private List<VisitaMedica> BuildVisite()
    {
        var items = new List<VisitaMedica>();

        foreach (var row in VisiteMediche)
        {
            if (string.IsNullOrWhiteSpace(row.TipoVisita) &&
                string.IsNullOrWhiteSpace(row.DataUltimaVisita) &&
                string.IsNullOrWhiteSpace(row.DataScadenza) &&
                string.IsNullOrWhiteSpace(row.Esito) &&
                string.IsNullOrWhiteSpace(row.Note))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(row.TipoVisita))
            {
                throw new InvalidOperationException("Ogni visita medica deve avere un tipo visita.");
            }

            var tipoVisita = row.TipoVisita.Trim();
            var dataUltimaVisita = ParseDate(row.DataUltimaVisita, $"Data ultima visita {row.TipoVisita}");
            var dataScadenzaManuale = ParseDate(row.DataScadenza, $"Data scadenza visita {row.TipoVisita}");
            var dataScadenza = CalcolaScadenzaVisita(tipoVisita, dataUltimaVisita) ?? dataScadenzaManuale;

            if (CatalogoVisiteMediche.TrovaPerDescrizione(tipoVisita) is not null && dataUltimaVisita is null)
            {
                throw new InvalidOperationException($"{tipoVisita}: la data ultima visita e obbligatoria per calcolare la scadenza.");
            }

            items.Add(new VisitaMedica
            {
                VisitaMedicaId = row.VisitaMedicaId ?? 0,
                PerId = PerId,
                TipoVisita = tipoVisita,
                DataUltimaVisita = dataUltimaVisita,
                DataScadenza = dataScadenza,
                Esito = row.Esito.Trim(),
                Note = row.Note.Trim(),
            });
        }

        return items;
    }

    private static DateOnly? CalcolaScadenzaVisita(string tipoVisita, DateOnly? dataUltimaVisita)
    {
        if (dataUltimaVisita is null)
        {
            return null;
        }

        var tipo = CatalogoVisiteMediche.TrovaPerDescrizione(tipoVisita);
        if (tipo?.MesiValidita is null)
        {
            return null;
        }

        return dataUltimaVisita.Value.AddMonths(tipo.MesiValidita.Value);
    }

    private static int? ParseNullableInt(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!int.TryParse(value, out var parsed))
        {
            throw new InvalidOperationException($"{fieldName}: valore numerico non valido.");
        }

        return parsed;
    }

    private static DateOnly? ParseDate(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateOnly.TryParse(value, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"{fieldName}: usare una data valida, ad esempio 18/03/2026.");
    }

    private static string FormatDate(DateOnly? value) => value?.ToString("dd/MM/yyyy") ?? string.Empty;

    private void AggiornaSuggerimentiRicerca()
    {
        SearchSuggestions.Clear();
        SelectedSearchSuggestion = null;

        if (string.IsNullOrWhiteSpace(FiltroCognome))
        {
            IsSearchSuggestionsOpen = false;
            return;
        }

        var testo = FiltroCognome.Trim();
        var suggerimenti = _allSearchSuggestions
            .Where(item => item.Contains(testo, StringComparison.OrdinalIgnoreCase))
            .Take(8)
            .ToList();

        foreach (var suggerimento in suggerimenti)
        {
            SearchSuggestions.Add(suggerimento);
        }

        IsSearchSuggestionsOpen = SearchSuggestions.Count > 0;
    }

    private void RicaricaSuggerimentiRicerca()
    {
        _allSearchSuggestions.Clear();
        _allSearchSuggestions.AddRange(_repository.GetSearchSuggestions());
        AggiornaSuggerimentiRicerca();
    }

    private void PulisciEditorVisita()
    {
        _selectedVisita = null;
        OnPropertyChanged(nameof(SelectedVisita));
        OnPropertyChanged(nameof(AzioneVisitaLabel));

        VisitaTipoSelezionato = null;
        VisitaDataUltimaVisita = string.Empty;
        VisitaEsito = string.Empty;
        VisitaNote = string.Empty;
    }

    private void CaricaEditorVisitaDaSelezione()
    {
        if (SelectedVisita is null)
        {
            VisitaTipoSelezionato = null;
            VisitaDataUltimaVisita = string.Empty;
            VisitaEsito = string.Empty;
            VisitaNote = string.Empty;
            return;
        }

        VisitaTipoSelezionato = TipiVisitaMedicaCatalogo.FirstOrDefault(tipo => tipo.Descrizione == SelectedVisita.TipoVisita);
        VisitaDataUltimaVisita = SelectedVisita.DataUltimaVisita;
        VisitaEsito = SelectedVisita.Esito;
        VisitaNote = SelectedVisita.Note;
    }

    private void AggiornaRiepilogoScheda()
    {
        OnPropertyChanged(nameof(SchedaRiepilogoTitolo));
        OnPropertyChanged(nameof(SchedaRiepilogoPerId));
        OnPropertyChanged(nameof(SchedaAbilitazioniTotali));
        OnPropertyChanged(nameof(SchedaVisiteTotali));
        OnPropertyChanged(nameof(SchedaProssimaScadenza));
        OnPropertyChanged(nameof(SchedaProssimaScadenzaDettaglio));
    }

    private (DateOnly data, string origine, string titolo)? CalcolaProssimaScadenzaScheda()
    {
        var voci = new List<(DateOnly data, string origine, string titolo)>();

        foreach (var abilitazione in Abilitazioni)
        {
            var data = TryParseDate(abilitazione.DataScadenza);
            if (data is not null)
            {
                voci.Add((data.Value, "Abilitazione", abilitazione.TipoDescrizione));
            }
        }

        foreach (var visita in VisiteMediche)
        {
            var data = TryParseDate(visita.DataScadenza);
            if (data is not null)
            {
                voci.Add((data.Value, "Visita", visita.TipoVisita));
            }
        }

        return voci.Count == 0 ? null : voci.OrderBy(voce => voce.data).First();
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
}
