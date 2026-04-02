using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using SMZ.Conta.App.Data;
using SMZ.Conta.App.Infrastructure;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private const int HomeSectionIndex = 0;
    private const int SearchSectionIndex = 1;
    private const int ServicesSectionIndex = 2;
    private const int PersonalSectionIndex = 3;
    private const int ArchiveSectionIndex = 4;
    private const int AccountingSectionIndex = 5;
    private const int ReportsSectionIndex = 6;
    private static readonly PersonaleListItemViewModel OperatoreVuoto = new();

    private readonly BackupService _backupService = new();
    private readonly PersonaleRepository _repository = new();
    private readonly RelayCommand _deleteCommand;
    private readonly RelayCommand _navigateSectionCommand;
    private readonly RelayCommand _newServizioCommand;
    private readonly RelayCommand _saveServizioCommand;
    private readonly RelayCommand _openServizioCommand;
    private readonly RelayCommand _deleteServizioCommand;
    private readonly RelayCommand _addSupportoOccasionaleCommand;
    private readonly RelayCommand _removeSupportoOccasionaleCommand;
    private readonly RelayCommand _openSelectedPersonaleCommand;
    private readonly RelayCommand _reloadServizioPersonaleCommand;
    private readonly RelayCommand _reloadContabilitaCommand;
    private readonly RelayCommand _reloadRegistroImmersioniCommand;
    private readonly RelayCommand _saveElaborazioneMensileCommand;
    private readonly RelayCommand _exportContabilitaCsvCommand;
    private readonly RelayCommand _saveTariffeContabiliCommand;
    private readonly RelayCommand _toggleTariffeContabiliCommand;
    private readonly RelayCommand _restoreArchivioCommand;
    private readonly RelayCommand _deleteArchivioDefinitivoCommand;
    private readonly RelayCommand _saveAttagliamentoCommand;
    private readonly RelayCommand _clearAttagliamentoEditorCommand;
    private readonly RelayCommand _removeAttagliamentoCommand;
    private readonly RelayCommand _enterAppCommand;
    private readonly RelayCommand _toggleWelcomeAudioCommand;
    private readonly RelayCommand _flaggaTuttiImmersioneCommand;
    private readonly RelayCommand _createLocalBackupCommand;
    private readonly RelayCommand _createExternalBackupCommand;
    private readonly RelayCommand _configureExternalBackupDirectoryCommand;
    private readonly RelayCommand _restoreBackupCommand;
    private readonly List<string> _allSearchSuggestions;
    private readonly BackupSettings _backupSettings;
    private PersonaleListItemViewModel? _selectedPersonale;
    private ScadenzaItemViewModel? _selectedScadenza;
    private PersonaleArchivioListItemViewModel? _selectedArchivio;
    private PersonaleAbilitazioneRowViewModel? _selectedAbilitazione;
    private VisitaMedicaRowViewModel? _selectedVisita;
    private PersonaleAttagliamentoRowViewModel? _selectedAttagliamento;
    private PersonaleArchivio? _archivioDettaglio;
    private string? _selectedSearchSuggestion;
    private string _filtroCognome = string.Empty;
    private string _filtroScadenzeSelezionato = "Tutte";
    private bool _isSearchSuggestionsOpen;
    private AbilitazioneFilterOptionViewModel? _filtroAbilitazione;
    private string _filtroVisiteEntro = string.Empty;
    private int _sezioneAttivaIndex;
    private int _schedaDettaglioTabIndex;
    private int _perId;
    private string _perIdInput = string.Empty;
    private string _cognome = string.Empty;
    private string _nome = string.Empty;
    private string _qualifica = string.Empty;
    private string _profiloPersonale = ProfiliPersonaleCatalogo.OperatoreSubacqueo;
    private string _ruoloSanitario = string.Empty;
    private string _codiceFiscale = string.Empty;
    private string _matricolaPersonale = string.Empty;
    private string _numeroBrevettoSmz = string.Empty;
    private string _dataNascita = string.Empty;
    private string _luogoNascita = string.Empty;
    private string _viaResidenza = string.Empty;
    private string _capResidenza = string.Empty;
    private string _cittaResidenza = string.Empty;
    private string _telefono1 = string.Empty;
    private string _telefono2 = string.Empty;
    private string _mail1Utente = string.Empty;
    private string _mail2Utente = string.Empty;
    private string _stato = "Pronto";
    private int _scadenzeTotali;
    private int _scadenzeUrgenti;
    private int _scadenzeScadute;
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
    private string _attagliamentoVoce = string.Empty;
    private string _attagliamentoTagliaMisura = string.Empty;
    private string _attagliamentoNote = string.Empty;
    private long _servizioGiornalieroId;
    private string _servizioData = DateTime.Today.ToString("dd/MM/yyyy");
    private string _servizioNumeroOrdine = string.Empty;
    private string _servizioOrario = string.Empty;
    private string _servizioTipoSelezionato = "InSede";
    private LocalitaOperativa? _servizioLocalitaSelezionata;
    private ScopoImmersioneItem? _servizioScopoSelezionato;
    private UnitaNavale? _servizioUnitaNavaleSelezionata;
    private bool _servizioFuoriSede;
    private string _servizioAttivitaSvolta = string.Empty;
    private string _servizioNote = string.Empty;
    private int _contabilitaAnnoSelezionato;
    private ContabilitaMeseItem? _contabilitaMeseSelezionato;
    private bool _contabilitaSelezionePronta;
    private bool _mostraTariffeContabili;
    private bool _isWelcomeVisible = true;
    private bool _isWelcomeAudioEnabled = true;
    private bool _isSyncingProfonditaPrimaRiga;
    private ElaborazioneMensileInfo? _elaborazioneMensileInfo;
    private ServizioGiornalieroSummary? _selectedServizioSalvato;
    private ServizioSupportoOccasionaleDraftViewModel? _selectedSupportoOccasionale;

    public MainWindowViewModel()
    {
        _backupSettings = _backupService.LoadSettings();
        var cataloghiServizio = _repository.GetCataloghiServizio();

        SearchCommand = new RelayCommand(CaricaElenco);
        OpenScadenzaCommand = new RelayCommand(ApriScadenzaDaParametro);
        ClearFiltersCommand = new RelayCommand(PulisciFiltri);
        _enterAppCommand = new RelayCommand(EntraNellApp);
        _toggleWelcomeAudioCommand = new RelayCommand(ToggleWelcomeAudio);
        _flaggaTuttiImmersioneCommand = new RelayCommand(FlaggaTuttiImmersione);
        _createLocalBackupCommand = new RelayCommand(CreaBackupLocaleManuale);
        _createExternalBackupCommand = new RelayCommand(CreaBackupEsternoManuale);
        _configureExternalBackupDirectoryCommand = new RelayCommand(ConfiguraCartellaBackupEsterno);
        _restoreBackupCommand = new RelayCommand(RipristinaDaBackup);
        _navigateSectionCommand = new RelayCommand(NavigaAllaSezione);
        _newServizioCommand = new RelayCommand(NuovoServizioGiornaliero);
        _saveServizioCommand = new RelayCommand(SalvaServizioGiornaliero);
        _openServizioCommand = new RelayCommand(ApriServizioSelezionato, () => SelectedServizioSalvato is not null);
        _deleteServizioCommand = new RelayCommand(EliminaServizioSelezionato, () => SelectedServizioSalvato is not null);
        _addSupportoOccasionaleCommand = new RelayCommand(AggiungiSupportoOccasionale);
        _removeSupportoOccasionaleCommand = new RelayCommand(RimuoviSupportoOccasionale, () => SelectedSupportoOccasionale is not null);
        NewCommand = new RelayCommand(() =>
        {
            NuovoPersonale();
            SezioneAttivaIndex = PersonalSectionIndex;
        });
        SaveCommand = new RelayCommand(SalvaPersonale);
        _deleteCommand = new RelayCommand(EliminaPersonale, () => PerId > 0);
        _openSelectedPersonaleCommand = new RelayCommand(ApriSchedaSelezionata, () => SelectedPersonale is not null);
        _restoreArchivioCommand = new RelayCommand(RipristinaArchivio, () => SelectedArchivio is not null);
        _deleteArchivioDefinitivoCommand = new RelayCommand(EliminaArchivioDefinitivamente, () => SelectedArchivio is not null);
        SaveAbilitazioneCommand = new RelayCommand(SalvaAbilitazioneInEditor);
        ClearAbilitazioneEditorCommand = new RelayCommand(PulisciEditorAbilitazione);
        RemoveAbilitazioneCommand = new RelayCommand(RimuoviAbilitazioneRiga);
        SaveVisitaCommand = new RelayCommand(SalvaVisitaInEditor);
        ClearVisitaEditorCommand = new RelayCommand(PulisciEditorVisita);
        AddVisitaCommand = new RelayCommand(PulisciEditorVisita);
        RemoveVisitaCommand = new RelayCommand(RimuoviVisitaRiga);
        _saveAttagliamentoCommand = new RelayCommand(SalvaAttagliamentoInEditor);
        _clearAttagliamentoEditorCommand = new RelayCommand(PulisciEditorAttagliamento);
        _removeAttagliamentoCommand = new RelayCommand(RimuoviAttagliamentoRiga);
        _reloadServizioPersonaleCommand = new RelayCommand(() => InizializzaBozzaServizio(preserveSelections: true));
        _reloadContabilitaCommand = new RelayCommand(CaricaContabilitaMensile);
        _reloadRegistroImmersioniCommand = new RelayCommand(CaricaRegistroImmersioniMensile);
        _saveElaborazioneMensileCommand = new RelayCommand(SalvaElaborazioneMensile);
        _exportContabilitaCsvCommand = new RelayCommand(EsportaContabilitaCsv);
        _saveTariffeContabiliCommand = new RelayCommand(SalvaTariffeContabili);
        _toggleTariffeContabiliCommand = new RelayCommand(ToggleTariffeContabili);

        Abilitazioni = new ObservableCollection<PersonaleAbilitazioneRowViewModel>();
        VisiteMediche = new ObservableCollection<VisitaMedicaRowViewModel>();
        Attagliamento = new ObservableCollection<PersonaleAttagliamentoRowViewModel>();
        Attagliamento.CollectionChanged += (_, _) => AggiornaStatoAttagliamento();
        OperatoriServizioDisponibili = new ObservableCollection<PersonaleListItemViewModel>();
        OperatoriServizioPresentiDisponibili = new ObservableCollection<PersonaleListItemViewModel>();
        ServizioPartecipantiBozza = new ObservableCollection<ServizioPartecipanteDraftViewModel>();
        ServizioImmersioniBozza = new ObservableCollection<ServizioImmersioneDraftViewModel>();
        ServizioSupportiOccasionaliBozza = new ObservableCollection<ServizioSupportoOccasionaleDraftViewModel>();
        ServiziSalvati = new ObservableCollection<ServizioGiornalieroSummary>();
        ContabilitaSmzItems = new ObservableCollection<ContabilitaSmzSummary>();
        ContabilitaSanitariItems = new ObservableCollection<ContabilitaSanitarioSummary>();
        ContabilitaSupportiItems = new ObservableCollection<ContabilitaSupportoSummary>();
        RegistroImmersioniItems = new ObservableCollection<RegistroImmersioneRiga>();
        RegistroImmersioniCategorieItems = new ObservableCollection<RegistroImmersioneCategoriaSummary>();
        RegoleContabiliEditorItems = new ObservableCollection<RegolaContabileEditorRowViewModel>();
        ContabilitaMesiDisponibili = new ObservableCollection<ContabilitaMeseItem>(
        [
            new ContabilitaMeseItem { NumeroMese = 1, Descrizione = "Gennaio" },
            new ContabilitaMeseItem { NumeroMese = 2, Descrizione = "Febbraio" },
            new ContabilitaMeseItem { NumeroMese = 3, Descrizione = "Marzo" },
            new ContabilitaMeseItem { NumeroMese = 4, Descrizione = "Aprile" },
            new ContabilitaMeseItem { NumeroMese = 5, Descrizione = "Maggio" },
            new ContabilitaMeseItem { NumeroMese = 6, Descrizione = "Giugno" },
            new ContabilitaMeseItem { NumeroMese = 7, Descrizione = "Luglio" },
            new ContabilitaMeseItem { NumeroMese = 8, Descrizione = "Agosto" },
            new ContabilitaMeseItem { NumeroMese = 9, Descrizione = "Settembre" },
            new ContabilitaMeseItem { NumeroMese = 10, Descrizione = "Ottobre" },
            new ContabilitaMeseItem { NumeroMese = 11, Descrizione = "Novembre" },
            new ContabilitaMeseItem { NumeroMese = 12, Descrizione = "Dicembre" },
        ]);
        ContabilitaAnniDisponibili = new ObservableCollection<int>();
        ArchivioItems = new ObservableCollection<PersonaleArchivioListItemViewModel>();
        ArchivioAbilitazioni = new ObservableCollection<PersonaleAbilitazioneRowViewModel>();
        ArchivioVisiteMediche = new ObservableCollection<VisitaMedicaRowViewModel>();
        ScadenzeProssime = new ObservableCollection<ScadenzaItemViewModel>();
        SearchSuggestions = new ObservableCollection<string>();
        FiltriScadenze = new ObservableCollection<string>(["Tutte", "Solo visite", "Solo abilitazioni"]);
        TipiAbilitazioneCatalogo = new ObservableCollection<TipoAbilitazione>(_repository.GetTipiAbilitazione());
        TipiServizioDisponibili = new ObservableCollection<string>(["InSede", "FuoriSede", "Misto"]);
        CategorieRegistroCatalogo = new ObservableCollection<CategoriaRegistroItem>(cataloghiServizio.CategorieRegistro);
        LocalitaOperativeCatalogo = new ObservableCollection<LocalitaOperativa>(cataloghiServizio.LocalitaOperative);
        ScopiImmersioneCatalogo = new ObservableCollection<ScopoImmersioneItem>(cataloghiServizio.ScopiImmersione);
        UnitaNavaliCatalogo = new ObservableCollection<UnitaNavale>(cataloghiServizio.UnitaNavali);
        TipologieImmersioneOperativeCatalogo = new ObservableCollection<TipologiaImmersioneOperativa>(cataloghiServizio.TipologieImmersione);
        FasceProfonditaCatalogo = new ObservableCollection<FasciaProfondita>(cataloghiServizio.FasceProfondita);
        CategorieContabiliOreCatalogo = new ObservableCollection<CategoriaContabileOre>(cataloghiServizio.CategorieContabiliOre);
        GruppiOperativiCatalogo = new ObservableCollection<GruppoOperativo>(cataloghiServizio.GruppiOperativi);
        RuoliOperativiCatalogo = new ObservableCollection<RuoloOperativo>(cataloghiServizio.RuoliOperativi);
        RegoleContabiliImmersioneCatalogo = new ObservableCollection<RegolaContabileImmersione>(cataloghiServizio.RegoleContabiliImmersione);
        AbilitazioneLivelliSuggeriti = new ObservableCollection<string>();
        AbilitazioneProfonditaSuggerite = new ObservableCollection<string>();
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
        _servizioLocalitaSelezionata = LocalitaOperativeCatalogo.FirstOrDefault();
        _servizioScopoSelezionato = ScopiImmersioneCatalogo.FirstOrDefault();
        _servizioUnitaNavaleSelezionata = UnitaNavaliCatalogo.FirstOrDefault();
        InizializzaEditorTariffeContabili();
        InizializzaContabilita();
        AggiornaSuggerimentiRicerca();
        InizializzaBozzaServizio(preserveSelections: false);

        CaricaElenco();
        CaricaArchivio();
        CaricaServiziSalvati();
        CaricaContabilitaMensile();
        CaricaRegistroImmersioniMensile();
        AggiornaScadenziario();
        NuovoPersonale();
        AggiornaStatoBackup();
        EseguiBackupLocaleAutomaticoAvvio();
        SezioneAttivaIndex = HomeSectionIndex;
    }

    public string Titolo => "SMZ La Spezia";

    public string Sottotitolo => "Gestione integrata di personale, servizi, immersioni e scadenze";

    public string WelcomeTitolo => "CENTRO NAUTICO E SMZ";

    public string WelcomeSottotitolo =>
        "Accesso al gestionale operativo per personale, servizi giornalieri, immersioni, scadenze e contabilita.";

    public string WelcomeCredits => "Sviluppato da Paolo Vittori";

    public string WelcomeVersione => $"Versione {GetApplicationVersion()}";

    public string HomeTitolo => "Centro Nautico e Sommozzatori";

    public string HomeSottotitolo =>
        "Una home iniziale piu moderna per accedere ai moduli del nucleo: ricerca, servizi giornalieri, anagrafica e archivio.";

    public int DashboardDipendentiTotali => PersonaleItems.Count;

    public int DashboardScadenzeAperte => ScadenzeTotali;

    public int DashboardCataloghiServizioTotali =>
        LocalitaOperativeCatalogo.Count
        + ScopiImmersioneCatalogo.Count
        + UnitaNavaliCatalogo.Count
        + TipologieImmersioneOperativeCatalogo.Count;

    public string DashboardScadenzeSintesi =>
        ScadenzeTotali == 0
            ? "Nessuna scadenza aperta nei prossimi 90 giorni."
            : $"{ScadenzeScadute} scadute, {ScadenzeUrgenti} urgenti, {ScadenzeTotali} totali.";

    public string DashboardServizioSintesi =>
        $"{ServizioPresentiTotali} presenti su {ServizioPartecipantiTotali} operatori, {ServizioImmersioniCompilate} immersioni compilate.";

    public string DashboardArchivioSintesi =>
        ArchivioItems.Count == 0
            ? "Nessuna scheda eliminata recuperabile."
            : $"{ArchivioItems.Count} schede eliminate ancora recuperabili.";

    public string DashboardStatoSintesi => ScadenzeScadute switch
    {
        > 0 => "Richiede attenzione: sono presenti visite mediche scadute.",
        _ when ScadenzeUrgenti > 0 => "Monitoraggio attivo: ci sono visite mediche in scadenza nei prossimi 7 giorni.",
        _ => "Situazione regolare: nessuna priorita immediata.",
    };

    public IReadOnlyList<ScadenzaItemViewModel> DashboardTopScadenze =>
        ScadenzeProssime.Take(6).ToList();

    public string DashboardTopScadenzeTitolo =>
        ScadenzeProssime.Count == 0
            ? "Nessuna scadenza prioritaria"
            : "Da controllare subito";

    public IReadOnlyList<ScadenzaItemViewModel> DashboardCriticitaItems =>
        ScadenzeProssime.Take(3).ToList();

    public int DashboardVisiteScadutePersonale => ScadenzeProssime
        .Where(item => string.Equals(item.Origine, "Visita medica", StringComparison.OrdinalIgnoreCase) && item.IsExpired)
        .Select(item => item.PerId)
        .Distinct()
        .Count();

    public int DashboardVisiteInScadenzaPersonale => ScadenzeProssime
        .Where(item => string.Equals(item.Origine, "Visita medica", StringComparison.OrdinalIgnoreCase) && !item.IsExpired)
        .Select(item => item.PerId)
        .Distinct()
        .Count();

    public RelayCommand NavigateSectionCommand => _navigateSectionCommand;

    public RelayCommand EnterAppCommand => _enterAppCommand;

    public RelayCommand ToggleWelcomeAudioCommand => _toggleWelcomeAudioCommand;

    public RelayCommand FlaggaTuttiImmersioneCommand => _flaggaTuttiImmersioneCommand;

    public RelayCommand CreateLocalBackupCommand => _createLocalBackupCommand;

    public RelayCommand CreateExternalBackupCommand => _createExternalBackupCommand;

    public RelayCommand ConfigureExternalBackupDirectoryCommand => _configureExternalBackupDirectoryCommand;

    public RelayCommand RestoreBackupCommand => _restoreBackupCommand;

    public bool IsWelcomeVisible
    {
        get => _isWelcomeVisible;
        set => SetProperty(ref _isWelcomeVisible, value);
    }

    public bool IsWelcomeAudioEnabled
    {
        get => _isWelcomeAudioEnabled;
        set
        {
            if (SetProperty(ref _isWelcomeAudioEnabled, value))
            {
                OnPropertyChanged(nameof(WelcomeAudioTooltip));
            }
        }
    }

    public string WelcomeAudioTooltip =>
        IsWelcomeAudioEnabled ? "Disattiva audio welcome" : "Attiva audio welcome";

    public int SezioneAttivaIndex
    {
        get => _sezioneAttivaIndex;
        set
        {
            if (SetProperty(ref _sezioneAttivaIndex, value))
            {
                OnPropertyChanged(nameof(IsHomeSection));

                if (value == AccountingSectionIndex)
                {
                    AggiornaAnniContabilitaDisponibili();
                    AggiornaDatiMensili();
                }
                else if (value == ReportsSectionIndex)
                {
                    AggiornaAnniContabilitaDisponibili();
                    AggiornaDatiMensili();
                }
            }
        }
    }

    public bool IsHomeSection => SezioneAttivaIndex == HomeSectionIndex;

    public int SchedaDettaglioTabIndex
    {
        get => _schedaDettaglioTabIndex;
        set => SetProperty(ref _schedaDettaglioTabIndex, value);
    }

    public ObservableCollection<PersonaleListItemViewModel> PersonaleItems { get; } = [];

    public ObservableCollection<string> SearchSuggestions { get; }

    public ObservableCollection<string> FiltriScadenze { get; }

    public ObservableCollection<TipoAbilitazione> TipiAbilitazioneCatalogo { get; }

    public ObservableCollection<string> TipiServizioDisponibili { get; }

    public ObservableCollection<CategoriaRegistroItem> CategorieRegistroCatalogo { get; }

    public ObservableCollection<LocalitaOperativa> LocalitaOperativeCatalogo { get; }

    public ObservableCollection<ScopoImmersioneItem> ScopiImmersioneCatalogo { get; }

    public ObservableCollection<UnitaNavale> UnitaNavaliCatalogo { get; }

    public ObservableCollection<TipologiaImmersioneOperativa> TipologieImmersioneOperativeCatalogo { get; }

    public ObservableCollection<FasciaProfondita> FasceProfonditaCatalogo { get; }

    public ObservableCollection<CategoriaContabileOre> CategorieContabiliOreCatalogo { get; }

    public ObservableCollection<GruppoOperativo> GruppiOperativiCatalogo { get; }

    public ObservableCollection<RuoloOperativo> RuoliOperativiCatalogo { get; }

    public ObservableCollection<RegolaContabileImmersione> RegoleContabiliImmersioneCatalogo { get; }

    public ObservableCollection<string> AbilitazioneLivelliSuggeriti { get; }

    public ObservableCollection<string> AbilitazioneProfonditaSuggerite { get; }

    public ObservableCollection<AbilitazioneFilterOptionViewModel> FiltroAbilitazioni { get; }

    public ObservableCollection<PersonaleAbilitazioneRowViewModel> Abilitazioni { get; }

    public ObservableCollection<VisitaMedicaRowViewModel> VisiteMediche { get; }

    public ObservableCollection<PersonaleAttagliamentoRowViewModel> Attagliamento { get; }

    public IReadOnlyList<PersonaleAttagliamentoRowViewModel> AttagliamentoSchedaItems =>
        Attagliamento
            .Where(item => item.IsPredefinita)
            .OrderBy(item => item.OrdineScheda)
            .ToList();

    public IReadOnlyList<PersonaleAttagliamentoRowViewModel> AttagliamentoAggiuntivoItems =>
        Attagliamento
            .Where(item => !item.IsPredefinita)
            .OrderBy(item => item.OrdineScheda)
            .ThenBy(item => item.Voce)
            .ToList();

    public bool HasAttagliamentoAggiuntivo => AttagliamentoAggiuntivoItems.Count > 0;

    public ObservableCollection<PersonaleListItemViewModel> OperatoriServizioDisponibili { get; }

    public ObservableCollection<PersonaleListItemViewModel> OperatoriServizioPresentiDisponibili { get; }

    public ObservableCollection<ServizioPartecipanteDraftViewModel> ServizioPartecipantiBozza { get; }

    public ObservableCollection<ServizioImmersioneDraftViewModel> ServizioImmersioniBozza { get; }

    public ObservableCollection<ServizioSupportoOccasionaleDraftViewModel> ServizioSupportiOccasionaliBozza { get; }

    public ObservableCollection<ServizioGiornalieroSummary> ServiziSalvati { get; }

    public ObservableCollection<ContabilitaSmzSummary> ContabilitaSmzItems { get; }

    public ObservableCollection<ContabilitaSanitarioSummary> ContabilitaSanitariItems { get; }

    public ObservableCollection<ContabilitaSupportoSummary> ContabilitaSupportiItems { get; }

    public ObservableCollection<RegistroImmersioneRiga> RegistroImmersioniItems { get; }

    public ObservableCollection<RegistroImmersioneCategoriaSummary> RegistroImmersioniCategorieItems { get; }

    public ObservableCollection<RegolaContabileEditorRowViewModel> RegoleContabiliEditorItems { get; }

    public ObservableCollection<ContabilitaMeseItem> ContabilitaMesiDisponibili { get; }

    public ObservableCollection<int> ContabilitaAnniDisponibili { get; }

    public ObservableCollection<ScadenzaItemViewModel> ScadenzeProssime { get; }

    public ScadenzaItemViewModel? SelectedScadenza
    {
        get => _selectedScadenza;
        set
        {
            if (!SetProperty(ref _selectedScadenza, value) || value is null)
            {
                return;
            }

            ApriScadenza(value);
            _selectedScadenza = null;
            OnPropertyChanged(nameof(SelectedScadenza));
        }
    }

    public ObservableCollection<PersonaleArchivioListItemViewModel> ArchivioItems { get; }

    public ObservableCollection<PersonaleAbilitazioneRowViewModel> ArchivioAbilitazioni { get; }

    public ObservableCollection<VisitaMedicaRowViewModel> ArchivioVisiteMediche { get; }

    public ServizioGiornalieroSummary? SelectedServizioSalvato
    {
        get => _selectedServizioSalvato;
        set
        {
            if (SetProperty(ref _selectedServizioSalvato, value))
            {
                _openServizioCommand.RaiseCanExecuteChanged();
                _deleteServizioCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public ServizioSupportoOccasionaleDraftViewModel? SelectedSupportoOccasionale
    {
        get => _selectedSupportoOccasionale;
        set
        {
            if (SetProperty(ref _selectedSupportoOccasionale, value))
            {
                _removeSupportoOccasionaleCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public ObservableCollection<TipoVisitaMedica> TipiVisitaMedicaCatalogo { get; } =
        new(CatalogoVisiteMediche.Tutte);

    public ContabilitaMeseItem? ContabilitaMeseSelezionato
    {
        get => _contabilitaMeseSelezionato;
        set
        {
            if (SetProperty(ref _contabilitaMeseSelezionato, value))
            {
                OnPropertyChanged(nameof(ContabilitaPeriodoTitolo));
                OnPropertyChanged(nameof(RegistroImmersioniPeriodoTitolo));

                if (_contabilitaSelezionePronta)
                {
                    AggiornaDatiMensili();
                }
            }
        }
    }

    public bool MostraTariffeContabili
    {
        get => _mostraTariffeContabili;
        set
        {
            if (SetProperty(ref _mostraTariffeContabili, value))
            {
                OnPropertyChanged(nameof(ToggleTariffeContabiliLabel));
            }
        }
    }

    public int ContabilitaAnnoSelezionato
    {
        get => _contabilitaAnnoSelezionato;
        set
        {
            if (SetProperty(ref _contabilitaAnnoSelezionato, value))
            {
                OnPropertyChanged(nameof(ContabilitaPeriodoTitolo));
                OnPropertyChanged(nameof(RegistroImmersioniPeriodoTitolo));

                if (_contabilitaSelezionePronta)
                {
                    AggiornaDatiMensili();
                }
            }
        }
    }

    public string ServizioData
    {
        get => _servizioData;
        set => SetProperty(ref _servizioData, value);
    }

    public string ServizioNumeroOrdine
    {
        get => _servizioNumeroOrdine;
        set => SetProperty(ref _servizioNumeroOrdine, value);
    }

    public string ServizioOrario
    {
        get => _servizioOrario;
        set => SetProperty(ref _servizioOrario, value);
    }

    public string ServizioTipoSelezionato
    {
        get => _servizioTipoSelezionato;
        set
        {
            if (SetProperty(ref _servizioTipoSelezionato, value))
            {
                OnPropertyChanged(nameof(ServizioTipoDescrizione));
            }
        }
    }

    public LocalitaOperativa? ServizioLocalitaSelezionata
    {
        get => _servizioLocalitaSelezionata;
        set => SetProperty(ref _servizioLocalitaSelezionata, value);
    }

    public ScopoImmersioneItem? ServizioScopoSelezionato
    {
        get => _servizioScopoSelezionato;
        set
        {
            if (SetProperty(ref _servizioScopoSelezionato, value))
            {
                OnPropertyChanged(nameof(ServizioCategoriaRegistroDescrizione));
            }
        }
    }

    public UnitaNavale? ServizioUnitaNavaleSelezionata
    {
        get => _servizioUnitaNavaleSelezionata;
        set => SetProperty(ref _servizioUnitaNavaleSelezionata, value);
    }

    public bool ServizioFuoriSede
    {
        get => _servizioFuoriSede;
        set
        {
            if (SetProperty(ref _servizioFuoriSede, value))
            {
                OnPropertyChanged(nameof(ServizioFuoriSedeDescrizione));
            }
        }
    }

    public string ServizioAttivitaSvolta
    {
        get => _servizioAttivitaSvolta;
        set => SetProperty(ref _servizioAttivitaSvolta, value);
    }

    public string ServizioNote
    {
        get => _servizioNote;
        set => SetProperty(ref _servizioNote, value);
    }

    public string ServizioTipoDescrizione => ServizioTipoSelezionato switch
    {
        "FuoriSede" => "Servizio operativo fuori sede",
        "Misto" => "Servizio con componenti in sede e fuori sede",
        _ => "Servizio operativo in sede",
    };

    public string ServizioFuoriSedeDescrizione => ServizioFuoriSede ? "Indennita fuori sede: SI" : "Indennita fuori sede: NO";

    public string ServizioCategoriaRegistroDescrizione
    {
        get
        {
            if (ServizioScopoSelezionato is null)
            {
                return "Categoria registro non selezionata";
            }

            var categoria = CategorieRegistroCatalogo.FirstOrDefault(item => item.CategoriaRegistroId == ServizioScopoSelezionato.CategoriaRegistroId);
            return categoria is null ? "Categoria registro non disponibile" : categoria.Descrizione;
        }
    }

    public int ServizioPartecipantiTotali =>
        ContaPartecipantiInterniBozza() + ContaSupportiOccasionaliBozza();

    public int ServizioPresentiTotali =>
        ContaPresentiInterniBozza() + ContaSupportiOccasionaliPresentiBozza();

    public int ServizioImmersioniCompilate => ServizioImmersioniBozza.Count(item =>
        !string.IsNullOrWhiteSpace(item.OrarioInizio)
        || !string.IsNullOrWhiteSpace(item.OrarioFine)
        || item.DirettoreImmersione is not null
        || item.OperatoreSoccorso is not null
        || item.AssistenteBlsd is not null
        || item.AssistenteSanitario is not null
        || item.Partecipazioni.Any(IsPartecipazioneImmersioneCompilata));

    public string ServizioBozzaStato =>
        IsExistingServizio
            ? $"Servizio #{_servizioGiornalieroId} caricato nel modulo. Le modifiche verranno salvate sul record esistente."
            : "Bozza non ancora salvata. Puoi registrarla nel database locale e riaprirla dall'elenco.";

    public string ServizioEditorTitolo =>
        IsExistingServizio ? $"Servizio giornaliero #{_servizioGiornalieroId}" : "Nuovo servizio giornaliero";

    public string ServizioEditorSottotitolo =>
        IsExistingServizio
            ? "Stai modificando un servizio gia registrato nel database locale."
            : "Compila la bozza, salva il servizio e riaprilo dall'elenco per le verifiche operative.";

    public string ServiziSalvatiStato =>
        ServiziSalvati.Count == 0
            ? "Nessun servizio ancora registrato."
            : $"{ServiziSalvati.Count} servizi recenti disponibili nel database locale.";

    public string ContabilitaPeriodoTitolo =>
        ContabilitaMeseSelezionato is null
            ? "Contabilita giornate di impiego"
            : $"{ContabilitaMeseSelezionato.Descrizione} {ContabilitaAnnoSelezionato}";

    public string ElaborazioneMensileStato =>
        _elaborazioneMensileInfo is null
            ? "Periodo non ancora chiuso: le tabelle mostrano il calcolo live corrente."
            : $"Chiusura mensile registrata il {_elaborazioneMensileInfo.AggiornataIlDescrizione}. Le tabelle mostrano lo snapshot congelato da consegnare ai pagamenti.";

    public string SalvaElaborazioneMensileLabel =>
        _elaborazioneMensileInfo is null ? "Chiudi mese" : "Rigenera chiusura";

    public string ContabilitaStato =>
        ContabilitaSmzItems.Count == 0 && ContabilitaSanitariItems.Count == 0 && ContabilitaSupportiItems.Count == 0
            ? "Nessuna giornata utile registrata nel periodo selezionato."
            : $"{ContabilitaSmzTotaleOre} ore SMZ e {ContabilitaSanitariTotaleGiornate + ContabilitaSupportoTotaleGiornate} giornate utili complessive nel periodo.";

    public int ContabilitaSmzTotaleRighe => ContabilitaSmzItems.Count;

    public decimal ContabilitaSmzTotaleOre => ContabilitaSmzItems.Sum(item => item.OreOrd + item.OreAdd + item.OreSper + item.OreCi);

    public decimal ContabilitaSmzTotaleImporti => ContabilitaSmzItems.Sum(item => item.Importo);

    public string ContabilitaSmzTotaleOreDisplay => ContabilitaSmzTotaleOre.ToString("0.##", CultureInfo.CurrentCulture);

    public string ContabilitaSmzTotaleImportiDisplay => ContabilitaSmzTotaleImporti.ToString("0.##", CultureInfo.CurrentCulture);

    public string ContabilitaSmzStato =>
        ContabilitaSmzItems.Count == 0
            ? "Nessuna riga contabile SMZ disponibile nel periodo selezionato."
            : $"{ContabilitaSmzItems.Count} righe contabili disponibili nel periodo selezionato.";

    public string TariffeContabiliStato =>
        RegoleContabiliEditorItems.Count == 0
            ? "Nessuna regola tariffaria disponibile."
            : $"{RegoleContabiliEditorItems.Count} righe tariffarie modificabili dal database.";

    public string ToggleTariffeContabiliLabel => MostraTariffeContabili ? "Nascondi tariffe" : "Mostra tariffe";

    public int ContabilitaSanitariTotalePersone => ContabilitaSanitariItems.Count;

    public int ContabilitaSanitariTotaleGiornate => ContabilitaSanitariItems.Sum(item => item.GiornateImpiego);

    public int ContabilitaSupportoTotalePersone => ContabilitaSupportiItems.Count;

    public int ContabilitaSupportoTotaleGiornate => ContabilitaSupportiItems.Sum(item => item.GiornateImpiego);

    public string ContabilitaSanitariStato =>
        ContabilitaSanitariItems.Count == 0
            ? "Nessun sanitario presente nel periodo selezionato."
            : $"{ContabilitaSanitariItems.Count} sanitari con {ContabilitaSanitariTotaleGiornate} giornate utili.";

    public string ContabilitaSupportoStato =>
        ContabilitaSupportiItems.Count == 0
            ? "Nessun supporto occasionale presente nel periodo selezionato."
            : $"{ContabilitaSupportiItems.Count} nominativi di supporto con {ContabilitaSupportoTotaleGiornate} giornate utili.";

    public string RegistroImmersioniPeriodoTitolo =>
        ContabilitaMeseSelezionato is null
            ? "Registro immersioni mensile"
            : $"Registro immersioni {ContabilitaMeseSelezionato.Descrizione} {ContabilitaAnnoSelezionato}";

    public int RegistroImmersioniTotaleRighe => RegistroImmersioniItems.Count;

    public int RegistroImmersioniTotaleImmersioni => RegistroImmersioniItems
        .Select(item => item.ServizioImmersioneId)
        .Distinct()
        .Count();

    public int RegistroImmersioniTotaleOperatori => RegistroImmersioniItems
        .Select(item => item.PerId)
        .Distinct()
        .Count();

    public decimal RegistroImmersioniTotaleOre => RegistroImmersioniItems.Sum(item => item.OreImmersione);

    public string RegistroImmersioniTotaleOreDisplay => RegistroImmersioniTotaleOre.ToString("0.##", CultureInfo.CurrentCulture);

    public string RegistroImmersioniStato =>
        RegistroImmersioniItems.Count == 0
            ? "Nessuna immersione registrata nel periodo selezionato."
            : $"{RegistroImmersioniTotaleImmersioni} immersioni, {RegistroImmersioniTotaleRighe} righe operatore e {RegistroImmersioniTotaleOreDisplay} ore complessive.";

    public string RegistroImmersioniCategorieStato =>
        RegistroImmersioniCategorieItems.Count == 0
            ? "Nessuna categoria alimentata nel periodo selezionato."
            : $"{RegistroImmersioniCategorieItems.Count} categorie di registro alimentate dai servizi del mese.";

    public string BackupLocaleStato => FormatBackupInfo(
        _backupService.GetLatestLocalBackup(),
        "Nessun backup locale ancora creato.",
        "Ultimo backup locale");

    public string BackupEsternoStato => FormatBackupInfo(
        _backupService.GetLatestExternalBackup(_backupSettings.ExternalBackupDirectory),
        string.IsNullOrWhiteSpace(_backupSettings.ExternalBackupDirectory)
            ? "Cartella backup esterno non configurata."
            : "Nessun backup esterno ancora creato.",
        "Ultimo backup esterno");

    public string BackupCartellaEsterna =>
        string.IsNullOrWhiteSpace(_backupSettings.ExternalBackupDirectory)
            ? "Non configurata"
            : _backupSettings.ExternalBackupDirectory;

    public string BackupDescrizione =>
        "Il backup locale protegge dalle modifiche accidentali. Il backup esterno serve per guasto o cambio PC.";

    public bool IsExistingServizio => _servizioGiornalieroId > 0;

    public ObservableCollection<string> QualificheDisponibili { get; } =
        new(
        [
            "Agente",
            "Agente Scelto",
            "Assistente",
            "Assistente Capo",
            "Assistente Capo Coordinatore",
            "Vice Sovrintendente",
            "Sovrintendente",
            "Sovrintendente Capo",
            "Sovrintendente Capo Coordinatore",
            "Vice Ispettore",
            "Ispettore",
            "Ispettore Capo",
            "Ispettore Superiore",
            "Sostituto Commissario",
            "Sostituto Commissario Coordinatore",
            "Vice Commissario",
            "Commissario",
            "Commissario Capo",
            "Vice Questore Aggiunto",
            "Vice Ispettore Tecnico",
            "Ispettore Tecnico",
            "Ispettore Capo Tecnico",
            "Ispettore Superiore Tecnico",
            "Sostituto Commissario Tecnico",
            "Sostituto Commissario Coordinatore Tecnico",
            "Medico",
            "Medico Principale",
            "Medico Capo",
        ]);

    public ObservableCollection<string> ProfiliPersonaleDisponibili { get; } =
        new(ProfiliPersonaleCatalogo.Tutti);

    public ObservableCollection<string> RuoliSanitariDisponibili { get; } =
        new(["Infermiere", "Medico"]);

    public RelayCommand SearchCommand { get; }

    public RelayCommand OpenScadenzaCommand { get; }

    public RelayCommand ClearFiltersCommand { get; }

    public RelayCommand NewServizioCommand => _newServizioCommand;

    public RelayCommand SaveServizioCommand => _saveServizioCommand;

    public RelayCommand OpenServizioCommand => _openServizioCommand;

    public RelayCommand DeleteServizioCommand => _deleteServizioCommand;

    public RelayCommand AddSupportoOccasionaleCommand => _addSupportoOccasionaleCommand;

    public RelayCommand RemoveSupportoOccasionaleCommand => _removeSupportoOccasionaleCommand;

    public RelayCommand NewCommand { get; }

    public RelayCommand SaveCommand { get; }

    public RelayCommand DeleteCommand => _deleteCommand;

    public RelayCommand OpenSelectedPersonaleCommand => _openSelectedPersonaleCommand;

    public RelayCommand RestoreArchivioCommand => _restoreArchivioCommand;

    public RelayCommand DeleteArchivioDefinitivoCommand => _deleteArchivioDefinitivoCommand;

    public RelayCommand SaveAbilitazioneCommand { get; }

    public RelayCommand ClearAbilitazioneEditorCommand { get; }

    public RelayCommand RemoveAbilitazioneCommand { get; }

    public RelayCommand SaveVisitaCommand { get; }

    public RelayCommand ClearVisitaEditorCommand { get; }

    public RelayCommand AddVisitaCommand { get; }

    public RelayCommand RemoveVisitaCommand { get; }

    public RelayCommand SaveAttagliamentoCommand => _saveAttagliamentoCommand;

    public RelayCommand ClearAttagliamentoEditorCommand => _clearAttagliamentoEditorCommand;

    public RelayCommand RemoveAttagliamentoCommand => _removeAttagliamentoCommand;

    public RelayCommand ReloadServizioPersonaleCommand => _reloadServizioPersonaleCommand;

    public RelayCommand ReloadContabilitaCommand => _reloadContabilitaCommand;

    public RelayCommand ReloadRegistroImmersioniCommand => _reloadRegistroImmersioniCommand;

    public RelayCommand SaveElaborazioneMensileCommand => _saveElaborazioneMensileCommand;

    public RelayCommand ExportContabilitaCsvCommand => _exportContabilitaCsvCommand;

    public RelayCommand SaveTariffeContabiliCommand => _saveTariffeContabiliCommand;

    public RelayCommand ToggleTariffeContabiliCommand => _toggleTariffeContabiliCommand;

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

    public PersonaleAttagliamentoRowViewModel? SelectedAttagliamento
    {
        get => _selectedAttagliamento;
        set
        {
            if (SetProperty(ref _selectedAttagliamento, value))
            {
                CaricaEditorAttagliamentoDaSelezione();
                OnPropertyChanged(nameof(AzioneAttagliamentoLabel));
            }
        }
    }

    public PersonaleArchivioListItemViewModel? SelectedArchivio
    {
        get => _selectedArchivio;
        set
        {
            if (SetProperty(ref _selectedArchivio, value))
            {
                _restoreArchivioCommand.RaiseCanExecuteChanged();
                _deleteArchivioDefinitivoCommand.RaiseCanExecuteChanged();

                if (value is null)
                {
                    PulisciDettaglioArchivio();
                }
                else
                {
                    CaricaDettaglioArchivio(value.PersonaleArchivioId);
                }
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

    public string FiltroScadenzeSelezionato
    {
        get => _filtroScadenzeSelezionato;
        set
        {
            if (SetProperty(ref _filtroScadenzeSelezionato, value))
            {
                AggiornaScadenziario();
            }
        }
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

    public string Qualifica
    {
        get => _qualifica;
        set => SetProperty(ref _qualifica, value);
    }

    public string ProfiloPersonale
    {
        get => _profiloPersonale;
        set
        {
            var valoreNormalizzato = ProfiliPersonaleCatalogo.Normalizza(value);
            if (SetProperty(ref _profiloPersonale, valoreNormalizzato))
            {
                if (!IsProfiloSanitario)
                {
                    RuoloSanitario = string.Empty;
                }

                OnPropertyChanged(nameof(IsProfiloSanitario));
                OnPropertyChanged(nameof(IsProfiloSmzOperativo));
                OnPropertyChanged(nameof(ProfiloPersonaleSintesi));
            }
        }
    }

    public string RuoloSanitario
    {
        get => _ruoloSanitario;
        set
        {
            if (SetProperty(ref _ruoloSanitario, value))
            {
                OnPropertyChanged(nameof(ProfiloPersonaleSintesi));
            }
        }
    }

    public bool IsProfiloSanitario => ProfiliPersonaleCatalogo.IsSanitario(ProfiloPersonale);

    public bool IsProfiloSmzOperativo => ProfiliPersonaleCatalogo.IsOperatoreSubacqueo(ProfiloPersonale);

    public string ProfiloPersonaleSintesi =>
        IsProfiloSanitario && !string.IsNullOrWhiteSpace(RuoloSanitario)
            ? $"Sanitario - {RuoloSanitario}"
            : ProfiloPersonale;

    public string CodiceFiscale
    {
        get => _codiceFiscale;
        set => SetProperty(ref _codiceFiscale, value);
    }

    public string MatricolaPersonale
    {
        get => _matricolaPersonale;
        set => SetProperty(ref _matricolaPersonale, value);
    }

    public string NumeroBrevettoSmz
    {
        get => _numeroBrevettoSmz;
        set => SetProperty(ref _numeroBrevettoSmz, value);
    }

    public string AttagliamentoVoce
    {
        get => _attagliamentoVoce;
        set => SetProperty(ref _attagliamentoVoce, value);
    }

    public string AttagliamentoTagliaMisura
    {
        get => _attagliamentoTagliaMisura;
        set => SetProperty(ref _attagliamentoTagliaMisura, value);
    }

    public string AttagliamentoNote
    {
        get => _attagliamentoNote;
        set => SetProperty(ref _attagliamentoNote, value);
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

    public string ViaResidenza
    {
        get => _viaResidenza;
        set => SetProperty(ref _viaResidenza, value);
    }

    public string CapResidenza
    {
        get => _capResidenza;
        set => SetProperty(ref _capResidenza, value);
    }

    public string CittaResidenza
    {
        get => _cittaResidenza;
        set => SetProperty(ref _cittaResidenza, value);
    }

    public string Telefono1
    {
        get => _telefono1;
        set => SetProperty(ref _telefono1, value);
    }

    public string Telefono2
    {
        get => _telefono2;
        set => SetProperty(ref _telefono2, value);
    }

    public string Mail1Utente
    {
        get => _mail1Utente;
        set => SetProperty(ref _mail1Utente, value);
    }

    public string Mail2Utente
    {
        get => _mail2Utente;
        set => SetProperty(ref _mail2Utente, value);
    }

    public TipoAbilitazione? AbilitazioneTipoSelezionato
    {
        get => _abilitazioneTipoSelezionato;
        set
        {
            if (SetProperty(ref _abilitazioneTipoSelezionato, value))
            {
                AggiornaLivelliSuggeriti();
                AggiornaProfonditaSuggerite();

                if (!(value?.RichiedeLivello ?? false))
                {
                    AbilitazioneLivello = string.Empty;
                }
                else if (string.IsNullOrWhiteSpace(AbilitazioneLivello) && AbilitazioneLivelliSuggeriti.Count > 0)
                {
                    AbilitazioneLivello = AbilitazioneLivelliSuggeriti[0];
                }

                if (!(value?.RichiedeProfondita ?? false))
                {
                    AbilitazioneProfondita = string.Empty;
                }
                else if (string.IsNullOrWhiteSpace(AbilitazioneProfondita) && AbilitazioneProfonditaSuggerite.Count > 0)
                {
                    AbilitazioneProfondita = AbilitazioneProfonditaSuggerite[0];
                }

                if (!(value?.RichiedeScadenza ?? false))
                {
                    AbilitazioneDataScadenza = string.Empty;
                }

                OnPropertyChanged(nameof(AbilitazioneRichiedeLivello));
                OnPropertyChanged(nameof(AbilitazioneLivelloEtichetta));
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
                if (value is not null)
                {
                    var visitaAssociata = VisiteMediche.FirstOrDefault(item =>
                        string.Equals(item.TipoVisita, value.Descrizione, StringComparison.OrdinalIgnoreCase));

                    if (visitaAssociata is not null && !ReferenceEquals(SelectedVisita, visitaAssociata))
                    {
                        SelectedVisita = visitaAssociata;
                    }
                }

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

    public string AbilitazioneLivelloEtichetta =>
        string.Equals(AbilitazioneTipoSelezionato?.Codice, "PATENTE_GUIDA", StringComparison.OrdinalIgnoreCase)
            ? "Certificato patente"
            : "Livello";

    public bool AbilitazioneRichiedeProfondita => AbilitazioneTipoSelezionato?.RichiedeProfondita ?? false;

    public bool AbilitazioneRichiedeScadenza => AbilitazioneTipoSelezionato?.RichiedeScadenza ?? false;

    public string AzioneAbilitazioneLabel => SelectedAbilitazione is null ? "Aggiungi abilitazione" : "Aggiorna abilitazione";

    public string AzioneVisitaLabel => "Aggiorna visita";

    public string AzioneAttagliamentoLabel => SelectedAttagliamento is null ? "Aggiungi riga" : "Aggiorna riga";

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
                richieste.Add(
                    AbilitazioneLivelliSuggeriti.Count == 0
                        ? "Livello richiesto"
                        : $"Certificato richiesto ({string.Join(", ", AbilitazioneLivelliSuggeriti)})");
            }

            if (AbilitazioneTipoSelezionato.RichiedeProfondita)
            {
                richieste.Add(
                    AbilitazioneProfonditaSuggerite.Count == 0
                        ? "Profondita richiesta"
                        : $"Profondita richiesta (suggerite: {string.Join(", ", AbilitazioneProfonditaSuggerite)} m)");
            }

            if (AbilitazioneTipoSelezionato.RichiedeScadenza)
            {
                richieste.Add("Scadenza richiesta");
            }

            return string.Join(" | ", richieste);
        }
    }

    public string ScadenzeTitolo => "Scadute e in scadenza entro 90 giorni";

    public string RegoleVisiteTitolo => "Regole visite mediche";

    public string RegoleVisiteDescrizione =>
        "Mantenimento brevetto M.M.: scadenza automatica a 24 mesi dalla data visita. D.Lgs. 81/08: scadenza automatica a 12 mesi. "
        + "Visita bimestrale: scadenza automatica a 2 mesi.";

    public string AttagliamentoIndicazioni =>
        "Compila le 7 misure principali della scheda taglie. La struttura resta estendibile se in futuro dovrai aggiungere altre misure.";

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
                return "Le tre visite sono obbligatorie e gia predisposte in scheda.";
            }

            return VisitaTipoSelezionato.RegolaScadenza;
        }
    }

    public string SchedaRiepilogoTitolo => string.IsNullOrWhiteSpace(Cognome) && string.IsNullOrWhiteSpace(Nome)
        ? "Nuova scheda"
        : $"{Cognome} {Nome}".Trim();

    public string SchedaRiepilogoPerId => string.IsNullOrWhiteSpace(PerIdInput) ? "PerID non impostato" : $"PerID {PerIdInput}";

    public int SchedaAbilitazioniTotali => Abilitazioni.Count;

    public string SchedaAbilitazioniPrincipali => BuildAbilitazioniPrincipali();

    public string SchedaAbilitazioniPrincipaliFooter => Math.Max(Abilitazioni.Count - 3, 0) switch
    {
        <= 0 => "Nessuna altra abilitazione",
        1 => "+1 altra abilitazione",
        var altre => $"+{altre} altre abilitazioni",
    };

    public int SchedaScadenzeTotali => ContaScadenzeScheda();

    public int SchedaVisiteTotali => VisiteMediche.Count(item => !string.IsNullOrWhiteSpace(item.DataUltimaVisita));

    public int SchedaScaduteTotali => ContaScaduteScheda();

    public bool SchedaHaScadute => SchedaScaduteTotali > 0;

    public string SchedaScaduteTitolo => SchedaScaduteTotali switch
    {
        0 => "Nessuna scaduta",
        1 => "Gia scaduta",
        _ => $"Gia scadute ({SchedaScaduteTotali})",
    };

    public string SchedaScaduteHighlight => BuildScaduteHighlight();

    public string SchedaScaduteDettaglio => SchedaScaduteTotali switch
    {
        0 => "Situazione regolare",
        _ => BuildScaduteDettaglio(),
    };

    public string MailDominioFisso => MailPoliziaHelper.DominioFisso;

    public string SchedaProssimaScadenza
    {
        get
        {
            var prossima = CalcolaProssimaScadenzaScheda();
            return prossima is null ? "Nessuna scadenza futura" : prossima.Value.data.ToString("dd/MM/yyyy");
        }
    }

    public string SchedaProssimaScadenzaDettaglio
    {
        get
        {
            var prossima = CalcolaProssimaScadenzaScheda();
            return prossima is null
                ? SchedaHaScadute
                    ? "Controlla la card delle scadute per gli adempimenti gia superati."
                    : "Aggiungi abilitazioni o visite con scadenza."
                : $"{prossima.Value.origine}: {prossima.Value.titolo}";
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
            OnPropertyChanged(nameof(DashboardDipendentiTotali));
            OnPropertyChanged(nameof(DashboardStatoSintesi));
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
        var scadenzeViewModel = items
            .Select(ScadenzaItemViewModel.FromModel)
            .Where(ApplicaFiltroScadenziario)
            .OrderBy(item => item.IsExpired ? 0 : item.IsUrgent ? 1 : 2)
            .ThenBy(item => item.IsExpired ? Math.Abs(item.GiorniResiduiNumero) : item.GiorniResiduiNumero)
            .ThenBy(item => item.Nominativo)
            .ToList();

        _scadenzeTotali = scadenzeViewModel.Count;
        _scadenzeScadute = scadenzeViewModel.Count(item => item.IsExpired);
        _scadenzeUrgenti = scadenzeViewModel.Count(item => item.IsUrgent);

        ScadenzeProssime.Clear();
        foreach (var item in scadenzeViewModel)
        {
            ScadenzeProssime.Add(item);
        }

        OnPropertyChanged(nameof(ScadenzeTotali));
        OnPropertyChanged(nameof(ScadenzeScadute));
        OnPropertyChanged(nameof(ScadenzeUrgenti));
        OnPropertyChanged(nameof(DashboardScadenzeAperte));
        OnPropertyChanged(nameof(DashboardScadenzeSintesi));
        OnPropertyChanged(nameof(DashboardStatoSintesi));
        OnPropertyChanged(nameof(DashboardTopScadenze));
        OnPropertyChanged(nameof(DashboardTopScadenzeTitolo));
        OnPropertyChanged(nameof(DashboardCriticitaItems));
        OnPropertyChanged(nameof(DashboardVisiteScadutePersonale));
        OnPropertyChanged(nameof(DashboardVisiteInScadenzaPersonale));
    }

    private void ApriScadenza(ScadenzaItemViewModel item)
    {
        var personaleListItem = PersonaleItems.FirstOrDefault(entry => entry.PerId == item.PerId);

        if (personaleListItem is null)
        {
            CaricaPersonale(item.PerId);
        }
        else
        {
            SelectedPersonale = personaleListItem;
        }

        if (string.Equals(item.Origine, "Visita medica", StringComparison.OrdinalIgnoreCase))
        {
            SchedaDettaglioTabIndex = 1;
            SelectedVisita = VisiteMediche.FirstOrDefault(row =>
                string.Equals(row.TipoVisita, item.Titolo, StringComparison.OrdinalIgnoreCase)
                && string.Equals(row.DataScadenza, item.DataScadenza, StringComparison.OrdinalIgnoreCase))
                ?? VisiteMediche.FirstOrDefault(row => string.Equals(row.TipoVisita, item.Titolo, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            SchedaDettaglioTabIndex = 0;
            SelectedAbilitazione = Abilitazioni.FirstOrDefault(row =>
                string.Equals(row.TipoDescrizione, item.Titolo, StringComparison.OrdinalIgnoreCase)
                && string.Equals(row.DataScadenza, item.DataScadenza, StringComparison.OrdinalIgnoreCase))
                ?? Abilitazioni.FirstOrDefault(row => string.Equals(row.TipoDescrizione, item.Titolo, StringComparison.OrdinalIgnoreCase));
        }

        SezioneAttivaIndex = PersonalSectionIndex;
    }

    private void ApriScadenzaDaParametro(object? parameter)
    {
        if (parameter is ScadenzaItemViewModel item)
        {
            ApriScadenza(item);
        }
    }

    private bool ApplicaFiltroScadenziario(ScadenzaItemViewModel item)
    {
        return FiltroScadenzeSelezionato switch
        {
            "Solo visite" => string.Equals(item.Origine, "Visita medica", StringComparison.OrdinalIgnoreCase),
            "Solo abilitazioni" => string.Equals(item.Origine, "Abilitazione", StringComparison.OrdinalIgnoreCase),
            _ => true,
        };
    }

    public int ScadenzeTotali => _scadenzeTotali;

    public int ScadenzeScadute => _scadenzeScadute;

    public int ScadenzeUrgenti => _scadenzeUrgenti;

    public string ArchivioTitolo => _archivioDettaglio?.NominativoCompleto ?? "Seleziona una scheda archiviata";

    public string ArchivioPerId => _archivioDettaglio is null
        ? "PerID originario non disponibile"
        : $"PerID originario {_archivioDettaglio.PerIdOriginale}";

    public string ArchivioCodiceFiscale => string.IsNullOrWhiteSpace(_archivioDettaglio?.CodiceFiscale)
        ? "Codice fiscale non disponibile"
        : _archivioDettaglio.CodiceFiscale;

    public string ArchivioDataArchiviazione => _archivioDettaglio is null
        ? "Data archiviazione non disponibile"
        : $"Archiviata il {_archivioDettaglio.DataArchiviazione:dd/MM/yyyy HH:mm}";

    public int ArchivioAbilitazioniTotali => ArchivioAbilitazioni.Count;

    public int ArchivioVisiteTotali => ArchivioVisiteMediche.Count;

    public string ArchivioContatti
    {
        get
        {
            if (_archivioDettaglio is null)
            {
                return "Contatti non disponibili";
            }

            return string.IsNullOrWhiteSpace(_archivioDettaglio.ContattiSintesi)
                ? "Contatti non disponibili"
                : _archivioDettaglio.ContattiSintesi;
        }
    }

    public string ArchivioAnagraficaSintesi
    {
        get
        {
            if (_archivioDettaglio is null)
            {
                return "Seleziona una scheda per vedere i dettagli archiviati.";
            }

            var parti = new List<string>();

            if (_archivioDettaglio.DataNascita is not null)
            {
                parti.Add($"Nata/o il {_archivioDettaglio.DataNascita.Value:dd/MM/yyyy}");
            }

            if (!string.IsNullOrWhiteSpace(_archivioDettaglio.LuogoNascita))
            {
                parti.Add(_archivioDettaglio.LuogoNascita);
            }

            if (!string.IsNullOrWhiteSpace(_archivioDettaglio.Qualifica))
            {
                parti.Add(_archivioDettaglio.Qualifica);
            }

            if (!string.IsNullOrWhiteSpace(_archivioDettaglio.ProfiloPersonale))
            {
                parti.Add(_archivioDettaglio.IsProfiloSanitario && !string.IsNullOrWhiteSpace(_archivioDettaglio.RuoloSanitario)
                    ? $"{_archivioDettaglio.ProfiloPersonale} - {_archivioDettaglio.RuoloSanitario}"
                    : _archivioDettaglio.ProfiloPersonale);
            }

            if (!string.IsNullOrWhiteSpace(_archivioDettaglio.NumeroBrevettoSmz))
            {
                parti.Add($"Brevetto subacqueo {_archivioDettaglio.NumeroBrevettoSmz}");
            }

            if (!string.IsNullOrWhiteSpace(_archivioDettaglio.IndirizzoResidenzaCompleto))
            {
                parti.Add(_archivioDettaglio.IndirizzoResidenzaCompleto);
            }

            return parti.Count == 0 ? "Nessun dato anagrafico aggiuntivo." : string.Join(" | ", parti);
        }
    }

    private void CaricaArchivio()
    {
        var items = _repository.GetArchivio();
        var selectedArchiveId = SelectedArchivio?.PersonaleArchivioId;

        ArchivioItems.Clear();
        foreach (var item in items)
        {
            ArchivioItems.Add(PersonaleArchivioListItemViewModel.FromModel(item));
        }

        if (ArchivioItems.Count == 0)
        {
            SelectedArchivio = null;
            OnPropertyChanged(nameof(DashboardArchivioSintesi));
            return;
        }

        SelectedArchivio = ArchivioItems.FirstOrDefault(item => item.PersonaleArchivioId == selectedArchiveId) ?? ArchivioItems[0];
        OnPropertyChanged(nameof(DashboardArchivioSintesi));
    }

    private void CaricaDettaglioArchivio(long archiveId)
    {
        var archivio = _repository.GetArchivioById(archiveId);
        if (archivio is null)
        {
            PulisciDettaglioArchivio();
            return;
        }

        _archivioDettaglio = archivio;

        ArchivioAbilitazioni.Clear();
        foreach (var abilitazione in archivio.Abilitazioni)
        {
            ArchivioAbilitazioni.Add(PersonaleAbilitazioneRowViewModel.FromModel(abilitazione));
        }

        ArchivioVisiteMediche.Clear();
        foreach (var visita in archivio.VisiteMediche)
        {
            ArchivioVisiteMediche.Add(VisitaMedicaRowViewModel.FromModel(visita));
        }

        AggiornaDettaglioArchivio();
    }

    private void PulisciDettaglioArchivio()
    {
        _archivioDettaglio = null;
        ArchivioAbilitazioni.Clear();
        ArchivioVisiteMediche.Clear();
        AggiornaDettaglioArchivio();
    }

    private void AggiornaDettaglioArchivio()
    {
        OnPropertyChanged(nameof(ArchivioTitolo));
        OnPropertyChanged(nameof(ArchivioPerId));
        OnPropertyChanged(nameof(ArchivioCodiceFiscale));
        OnPropertyChanged(nameof(ArchivioDataArchiviazione));
        OnPropertyChanged(nameof(ArchivioAbilitazioniTotali));
        OnPropertyChanged(nameof(ArchivioVisiteTotali));
        OnPropertyChanged(nameof(ArchivioContatti));
        OnPropertyChanged(nameof(ArchivioAnagraficaSintesi));
    }

    private void CaricaServiziSalvati(long? selectedServizioId = null)
    {
        var items = _repository.GetServiziGiornalieriRecenti();
        var selectedId = selectedServizioId ?? SelectedServizioSalvato?.ServizioGiornalieroId;

        ServiziSalvati.Clear();
        foreach (var item in items)
        {
            ServiziSalvati.Add(item);
        }

        SelectedServizioSalvato = selectedId is null
            ? null
            : ServiziSalvati.FirstOrDefault(item => item.ServizioGiornalieroId == selectedId.Value);

        OnPropertyChanged(nameof(ServiziSalvatiStato));
    }

    private void InizializzaContabilita()
    {
        AggiornaAnniContabilitaDisponibili();

        var oggi = DateTime.Today;
        _contabilitaAnnoSelezionato = ContabilitaAnniDisponibili.Contains(oggi.Year)
            ? oggi.Year
            : ContabilitaAnniDisponibili.FirstOrDefault();
        _contabilitaMeseSelezionato = ContabilitaMesiDisponibili.FirstOrDefault(item => item.NumeroMese == oggi.Month)
            ?? ContabilitaMesiDisponibili.FirstOrDefault();
        _contabilitaSelezionePronta = true;
    }

    private void InizializzaEditorTariffeContabili()
    {
        RegoleContabiliEditorItems.Clear();

        foreach (var regola in RegoleContabiliImmersioneCatalogo
                     .OrderBy(item => item.TipologiaImmersioneOperativaId)
                     .ThenBy(item => item.FasciaProfonditaId)
                     .ThenBy(item => item.CategoriaContabileOreId))
        {
            RegoleContabiliEditorItems.Add(new RegolaContabileEditorRowViewModel
            {
                RegolaContabileImmersioneId = regola.RegolaContabileImmersioneId,
                TipologiaDescrizione = TipologieImmersioneOperativeCatalogo.FirstOrDefault(item => item.TipologiaImmersioneOperativaId == regola.TipologiaImmersioneOperativaId)?.Descrizione ?? string.Empty,
                FasciaDescrizione = FasceProfonditaCatalogo.FirstOrDefault(item => item.FasciaProfonditaId == regola.FasciaProfonditaId)?.Descrizione ?? string.Empty,
                CategoriaDescrizione = CategorieContabiliOreCatalogo.FirstOrDefault(item => item.CategoriaContabileOreId == regola.CategoriaContabileOreId)?.Descrizione ?? string.Empty,
                Tariffa = FormatDecimal(regola.Tariffa),
                Attiva = regola.Attiva,
            });
        }

        OnPropertyChanged(nameof(TariffeContabiliStato));
    }

    private void AggiornaAnniContabilitaDisponibili()
    {
        var anni = _repository.GetAnniServiziDisponibili();
        var annoCorrente = DateTime.Today.Year;

        if (!anni.Contains(annoCorrente))
        {
            anni.Add(annoCorrente);
        }

        anni = anni
            .Distinct()
            .OrderByDescending(item => item)
            .ToList();

        ContabilitaAnniDisponibili.Clear();
        foreach (var anno in anni)
        {
            ContabilitaAnniDisponibili.Add(anno);
        }

        if (_contabilitaAnnoSelezionato > 0 && ContabilitaAnniDisponibili.Contains(_contabilitaAnnoSelezionato))
        {
            return;
        }

        _contabilitaAnnoSelezionato = ContabilitaAnniDisponibili.Contains(annoCorrente)
            ? annoCorrente
            : ContabilitaAnniDisponibili.FirstOrDefault();

        OnPropertyChanged(nameof(ContabilitaAnnoSelezionato));
        OnPropertyChanged(nameof(ContabilitaPeriodoTitolo));
        OnPropertyChanged(nameof(RegistroImmersioniPeriodoTitolo));
    }

    private void AggiornaDatiMensili()
    {
        CaricaContabilitaMensile();
        CaricaRegistroImmersioniMensile();
    }

    private void CaricaContabilitaMensile()
    {
        if (ContabilitaMeseSelezionato is null || ContabilitaAnnoSelezionato <= 0)
        {
            return;
        }

        _elaborazioneMensileInfo = _repository.GetElaborazioneMensileInfo(ContabilitaAnnoSelezionato, ContabilitaMeseSelezionato.NumeroMese);

        var snapshot = _elaborazioneMensileInfo is null
            ? _repository.GetContabilitaGiornateImpiego(ContabilitaAnnoSelezionato, ContabilitaMeseSelezionato.NumeroMese)
            : _repository.GetElaborazioneMensileSnapshot(ContabilitaAnnoSelezionato, ContabilitaMeseSelezionato.NumeroMese)
                ?? _repository.GetContabilitaGiornateImpiego(ContabilitaAnnoSelezionato, ContabilitaMeseSelezionato.NumeroMese);

        ContabilitaSmzItems.Clear();
        foreach (var item in snapshot.SmzImmersioni)
        {
            ContabilitaSmzItems.Add(item);
        }

        ContabilitaSanitariItems.Clear();
        foreach (var item in snapshot.Sanitari)
        {
            ContabilitaSanitariItems.Add(item);
        }

        ContabilitaSupportiItems.Clear();
        foreach (var item in snapshot.SupportiOccasionali)
        {
            ContabilitaSupportiItems.Add(item);
        }

        AggiornaRiepilogoContabilita();
    }

    private void CaricaRegistroImmersioniMensile()
    {
        if (ContabilitaMeseSelezionato is null || ContabilitaAnnoSelezionato <= 0)
        {
            return;
        }

        var items = _repository.GetRegistroImmersioniMensile(ContabilitaAnnoSelezionato, ContabilitaMeseSelezionato.NumeroMese);

        RegistroImmersioniItems.Clear();
        foreach (var item in items)
        {
            RegistroImmersioniItems.Add(item);
        }

        var categorie = items
            .GroupBy(item => item.CategoriaRegistro)
            .OrderBy(group =>
                CategorieRegistroCatalogo.FirstOrDefault(item =>
                    string.Equals(item.Descrizione, group.Key, StringComparison.OrdinalIgnoreCase))?.Ordine ?? int.MaxValue)
            .ThenBy(group => group.Key)
            .Select(group => new RegistroImmersioneCategoriaSummary
            {
                CategoriaRegistro = group.Key,
                ImmersioniTotali = group.Select(item => item.ServizioImmersioneId).Distinct().Count(),
                RigheOperatoreTotali = group.Count(),
                OreTotali = group.Sum(item => item.OreImmersione),
            })
            .ToList();

        RegistroImmersioniCategorieItems.Clear();
        foreach (var item in categorie)
        {
            RegistroImmersioniCategorieItems.Add(item);
        }

        AggiornaRiepilogoRegistroImmersioni();
    }

    private void AggiornaRiepilogoContabilita()
    {
        OnPropertyChanged(nameof(ContabilitaPeriodoTitolo));
        OnPropertyChanged(nameof(ContabilitaStato));
        OnPropertyChanged(nameof(ContabilitaSmzTotaleRighe));
        OnPropertyChanged(nameof(ContabilitaSmzTotaleOre));
        OnPropertyChanged(nameof(ContabilitaSmzTotaleImporti));
        OnPropertyChanged(nameof(ContabilitaSmzTotaleOreDisplay));
        OnPropertyChanged(nameof(ContabilitaSmzTotaleImportiDisplay));
        OnPropertyChanged(nameof(ContabilitaSmzStato));
        OnPropertyChanged(nameof(ContabilitaSanitariTotalePersone));
        OnPropertyChanged(nameof(ContabilitaSanitariTotaleGiornate));
        OnPropertyChanged(nameof(ContabilitaSupportoTotalePersone));
        OnPropertyChanged(nameof(ContabilitaSupportoTotaleGiornate));
        OnPropertyChanged(nameof(ContabilitaSanitariStato));
        OnPropertyChanged(nameof(ContabilitaSupportoStato));
        OnPropertyChanged(nameof(TariffeContabiliStato));
        OnPropertyChanged(nameof(ElaborazioneMensileStato));
        OnPropertyChanged(nameof(SalvaElaborazioneMensileLabel));
    }

    private void AggiornaRiepilogoRegistroImmersioni()
    {
        OnPropertyChanged(nameof(RegistroImmersioniPeriodoTitolo));
        OnPropertyChanged(nameof(RegistroImmersioniTotaleRighe));
        OnPropertyChanged(nameof(RegistroImmersioniTotaleImmersioni));
        OnPropertyChanged(nameof(RegistroImmersioniTotaleOperatori));
        OnPropertyChanged(nameof(RegistroImmersioniTotaleOre));
        OnPropertyChanged(nameof(RegistroImmersioniTotaleOreDisplay));
        OnPropertyChanged(nameof(RegistroImmersioniStato));
        OnPropertyChanged(nameof(RegistroImmersioniCategorieStato));
    }

    private void EntraNellApp()
    {
        IsWelcomeVisible = false;
        SezioneAttivaIndex = HomeSectionIndex;
        Stato = "Home iniziale caricata.";
    }

    private void ToggleWelcomeAudio()
    {
        IsWelcomeAudioEnabled = !IsWelcomeAudioEnabled;
        Stato = IsWelcomeAudioEnabled
            ? "Audio welcome attivato."
            : "Audio welcome disattivato.";
    }

    private void SalvaElaborazioneMensile()
    {
        if (ContabilitaMeseSelezionato is null || ContabilitaAnnoSelezionato <= 0)
        {
            return;
        }

        try
        {
            if (_elaborazioneMensileInfo is not null)
            {
                var result = MessageBox.Show(
                    $"Esiste gia una chiusura per {ContabilitaMeseSelezionato.Descrizione} {ContabilitaAnnoSelezionato}. Vuoi rigenerarla con i dati correnti?",
                    "Elaborazione mensile",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    Stato = "Rigenerazione elaborazione mensile annullata.";
                    return;
                }
            }

            var snapshot = _repository.GetContabilitaGiornateImpiego(ContabilitaAnnoSelezionato, ContabilitaMeseSelezionato.NumeroMese);
            _repository.SaveElaborazioneMensile(
                ContabilitaAnnoSelezionato,
                ContabilitaMeseSelezionato.NumeroMese,
                snapshot,
                $"Snapshot amministrativo {ContabilitaMeseSelezionato.Descrizione} {ContabilitaAnnoSelezionato}");

            CaricaContabilitaMensile();
            Stato = $"Elaborazione mensile registrata per {ContabilitaMeseSelezionato.Descrizione} {ContabilitaAnnoSelezionato}.";
            EseguiBackupLocaleSilenzioso("save-monthly-snapshot");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Elaborazione mensile", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Salvataggio elaborazione mensile non riuscito.";
        }
    }

    private void EsportaContabilitaCsv()
    {
        if (ContabilitaMeseSelezionato is null || ContabilitaAnnoSelezionato <= 0)
        {
            return;
        }

        if (_elaborazioneMensileInfo is null)
        {
            MessageBox.Show(
                "Chiudi prima il mese con \"Chiudi mese\". L'export CSV deve partire da uno snapshot congelato da inviare ai pagamenti.",
                "Export contabilita CSV",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Stato = "Export CSV non eseguito: manca la chiusura mensile.";
            return;
        }

        try
        {
            Directory.CreateDirectory(DatabasePaths.ExportDirectory);

            var fileName = $"contabilita-smz-{ContabilitaAnnoSelezionato:D4}-{ContabilitaMeseSelezionato.NumeroMese:D2}.csv";
            var filePath = Path.Combine(DatabasePaths.ExportDirectory, fileName);
            var builder = new StringBuilder();

            builder.AppendLine("Periodo;Data;Ordine;PerID;Qual;Cognome e Nome;Appar.;Prof.;Tariffa;ORE ORD;ORE ADD;ORE SPER;ORE C.I.;Importo;Med. Rag.;TOTALE");

            var periodo = $"{ContabilitaMeseSelezionato.Descrizione} {ContabilitaAnnoSelezionato}";
            foreach (var item in ContabilitaSmzItems
                         .OrderBy(x => x.Cognome)
                         .ThenBy(x => x.Nome)
                         .ThenBy(x => x.DataServizio)
                         .ThenBy(x => x.NumeroOrdineServizio)
                         .ThenBy(x => x.Apparato)
                         .ThenBy(x => x.FasciaProfondita))
            {
                builder.AppendLine(string.Join(";",
                    Csv(periodo),
                    Csv(item.DataServizioDescrizione),
                    Csv(item.NumeroOrdineServizio),
                    Csv(item.PerId),
                    Csv(item.Qualifica),
                    Csv(item.Nominativo),
                    Csv(item.Apparato),
                    Csv(item.FasciaProfondita),
                    Csv(item.TariffaDisplay),
                    Csv(item.OreOrdDisplay),
                    Csv(item.OreAddDisplay),
                    Csv(item.OreSperDisplay),
                    Csv(item.OreCiDisplay),
                    Csv(item.ImportoDisplay),
                    Csv(string.Empty),
                    Csv(item.ImportoDisplay)));
            }

            File.WriteAllText(filePath, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            Stato = $"Export CSV creato: {filePath}";
            MessageBox.Show(
                $"Export contabilita creato in:\n{filePath}",
                "Export contabilita CSV",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Export contabilita CSV", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Export contabilita CSV non riuscito.";
        }
    }

    private void SalvaTariffeContabili()
    {
        try
        {
            var regoleAggiornate = new List<RegolaContabileImmersione>();

            foreach (var row in RegoleContabiliEditorItems)
            {
                var tariffa = ParseNullableDecimal(row.Tariffa, $"Tariffa {row.TipologiaDescrizione} {row.FasciaDescrizione} {row.CategoriaDescrizione}")
                    ?? throw new InvalidOperationException($"Tariffa mancante per {row.TipologiaDescrizione} {row.FasciaDescrizione} {row.CategoriaDescrizione}.");

                var regola = RegoleContabiliImmersioneCatalogo.FirstOrDefault(item => item.RegolaContabileImmersioneId == row.RegolaContabileImmersioneId)
                    ?? throw new InvalidOperationException($"Regola tariffaria {row.RegolaContabileImmersioneId} non trovata.");

                regola.Tariffa = tariffa;
                regola.Attiva = row.Attiva;
                regoleAggiornate.Add(regola);
            }

            _repository.UpdateRegoleContabiliImmersione(regoleAggiornate);

            foreach (var immersione in ServizioImmersioniBozza)
            {
                foreach (var partecipazione in immersione.Partecipazioni)
                {
                    AggiornaCalcoliPartecipazioneImmersione(partecipazione);
                }
            }

            CaricaContabilitaMensile();
            Stato = "Tariffe contabili aggiornate nel database.";
            EseguiBackupLocaleSilenzioso("save-accounting-rules");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Tariffe contabili", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Aggiornamento tariffe non riuscito.";
        }
    }

    private void ToggleTariffeContabili()
    {
        MostraTariffeContabili = !MostraTariffeContabili;
    }

    private void CreaBackupLocaleManuale()
    {
        try
        {
            var result = _backupService.CreateLocalBackup("manual");
            AggiornaStatoBackup();
            Stato = $"Backup locale creato: {Path.GetFileName(result.BackupPath)}";
            MessageBox.Show(
                $"Backup locale creato in:\n{result.BackupPath}",
                "Backup locale",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Backup locale", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Backup locale non riuscito.";
        }
    }

    private void CreaBackupEsternoManuale()
    {
        if (string.IsNullOrWhiteSpace(_backupSettings.ExternalBackupDirectory))
        {
            ConfiguraCartellaBackupEsterno();
            if (string.IsNullOrWhiteSpace(_backupSettings.ExternalBackupDirectory))
            {
                return;
            }
        }

        try
        {
            var result = _backupService.CreateExternalBackup(_backupSettings.ExternalBackupDirectory, "manual");
            AggiornaStatoBackup();
            Stato = $"Backup esterno creato: {Path.GetFileName(result.BackupPath)}";
            MessageBox.Show(
                $"Backup esterno creato in:\n{result.BackupPath}",
                "Backup esterno",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Backup esterno", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Backup esterno non riuscito.";
        }
    }

    private void ConfiguraCartellaBackupEsterno()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Seleziona la cartella per il backup esterno",
            InitialDirectory = Directory.Exists(_backupSettings.ExternalBackupDirectory)
                ? _backupSettings.ExternalBackupDirectory
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        _backupSettings.ExternalBackupDirectory = dialog.FolderName;
        _backupService.SaveSettings(_backupSettings);
        AggiornaStatoBackup();
        Stato = $"Cartella backup esterno impostata: {_backupSettings.ExternalBackupDirectory}";
    }

    private void RipristinaDaBackup()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Seleziona un backup SMZ da ripristinare",
            Filter = "Backup SMZ (*.smzbak)|*.smzbak|Archivio ZIP (*.zip)|*.zip|Tutti i file|*.*",
            CheckFileExists = true,
            Multiselect = false,
            InitialDirectory = GetBackupRestoreInitialDirectory(),
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var conferma = MessageBox.Show(
            $"Ripristinare il backup selezionato?\n\n{dialog.FileName}\n\nPrima del ripristino verra creato un backup di sicurezza locale del database attuale.",
            "Ripristina backup",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (conferma != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var result = _backupService.RestoreBackup(dialog.FileName);
            RicaricaDatiApplicazioneDaDatabase();
            AggiornaStatoBackup();
            Stato = $"Backup ripristinato: {Path.GetFileName(result.RestoredBackupPath)}";
            MessageBox.Show(
                $"Ripristino completato.\n\nBackup applicato:\n{result.RestoredBackupPath}\n\nBackup di sicurezza creato prima del restore:\n{result.SafetyBackupPath}",
                "Ripristino backup",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ripristino backup", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Ripristino backup non riuscito.";
        }
    }

    private void EseguiBackupLocaleAutomaticoAvvio()
    {
        if (!_backupService.NeedsAutomaticLocalBackup())
        {
            return;
        }

        try
        {
            _backupService.CreateLocalBackup("startup-auto");
            AggiornaStatoBackup();
            Stato = "Backup locale automatico eseguito all'avvio.";
        }
        catch (Exception ex)
        {
            AggiornaStatoBackup();
            Stato = $"Avvio completato, ma il backup locale automatico non e riuscito: {ex.Message}";
        }
    }

    private void EseguiBackupLocaleSilenzioso(string reason)
    {
        try
        {
            _backupService.CreateLocalBackup(reason);
            AggiornaStatoBackup();
        }
        catch (Exception ex)
        {
            AggiornaStatoBackup();
            Stato = $"{Stato} Backup locale non riuscito: {ex.Message}";
        }
    }

    private string GetBackupRestoreInitialDirectory()
    {
        if (!string.IsNullOrWhiteSpace(_backupSettings.ExternalBackupDirectory)
            && Directory.Exists(_backupSettings.ExternalBackupDirectory))
        {
            return _backupSettings.ExternalBackupDirectory;
        }

        if (Directory.Exists(DatabasePaths.LocalBackupDirectory))
        {
            return DatabasePaths.LocalBackupDirectory;
        }

        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

    private void PulisciFiltri()
    {
        FiltroCognome = string.Empty;
        FiltroAbilitazione = FiltroAbilitazioni.FirstOrDefault();
        FiltroVisiteEntro = string.Empty;
        SelectedPersonale = null;
        IsSearchSuggestionsOpen = false;
        CaricaElenco();
    }

    private void NavigaAllaSezione(object? parameter)
    {
        if (parameter is int index)
        {
            SezioneAttivaIndex = index;
            return;
        }

        if (parameter is not null && int.TryParse(parameter.ToString(), out var parsed))
        {
            SezioneAttivaIndex = parsed;
        }
    }

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

    private void ApriServizioSelezionato()
    {
        if (SelectedServizioSalvato is null)
        {
            return;
        }

        CaricaServizioGiornaliero(SelectedServizioSalvato.ServizioGiornalieroId);
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
            MessageBox.Show("Servizio giornaliero non trovato.", "SMZ Conta", MessageBoxButton.OK, MessageBoxImage.Warning);
            CaricaServiziSalvati();
            return;
        }

        _servizioGiornalieroId = servizio.ServizioGiornalieroId;
        ServizioData = FormatDate(servizio.DataServizio);
        ServizioNumeroOrdine = servizio.NumeroOrdineServizio;
        ServizioOrario = servizio.OrarioServizio;
        ServizioTipoSelezionato = servizio.TipoServizio;
        ServizioLocalitaSelezionata = LocalitaOperativeCatalogo.FirstOrDefault(item => item.LocalitaOperativaId == servizio.LocalitaOperativaId);
        ServizioScopoSelezionato = ScopiImmersioneCatalogo.FirstOrDefault(item => item.ScopoImmersioneId == servizio.ScopoImmersioneId);
        ServizioUnitaNavaleSelezionata = UnitaNavaliCatalogo.FirstOrDefault(item => item.UnitaNavaleId == servizio.UnitaNavaleId);
        ServizioFuoriSede = servizio.FuoriSede;
        ServizioAttivitaSvolta = servizio.AttivitaSvolta;
        ServizioNote = servizio.Note;

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

        ServizioImmersioniBozza.Clear();
        foreach (var immersione in servizio.Immersioni.OrderBy(item => item.NumeroImmersione))
        {
            var item = new ServizioImmersioneDraftViewModel
            {
                NumeroImmersione = immersione.NumeroImmersione,
                OrarioInizio = FormatTime(immersione.OrarioInizio),
                OrarioFine = FormatTime(immersione.OrarioFine),
                DirettoreImmersione = TrovaOperatoreServizio(immersione.DirettoreImmersionePerId),
                OperatoreSoccorso = TrovaOperatoreServizio(immersione.OperatoreSoccorsoPerId),
                AssistenteBlsd = TrovaOperatoreServizio(immersione.AssistenteBlsdPerId),
                AssistenteSanitario = TrovaOperatoreServizio(immersione.AssistenteSanitarioPerId),
                Note = immersione.Note,
            };

            item.PropertyChanged += ServizioImmersioneBozza_PropertyChanged;
            ServizioImmersioniBozza.Add(item);
        }

        while (ServizioImmersioniBozza.Count < 2)
        {
            var item = new ServizioImmersioneDraftViewModel
            {
                NumeroImmersione = ServizioImmersioniBozza.Count + 1,
            };

            item.PropertyChanged += ServizioImmersioneBozza_PropertyChanged;
            ServizioImmersioniBozza.Add(item);
        }

        SincronizzaPartecipazioniImmersioneBozza();

        var perIdByServizioPartecipanteId = servizio.Partecipanti.ToDictionary(item => item.ServizioPartecipanteId, item => item.PerId);
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

        AggiornaContestoServizio();
        AggiornaRiepilogoBozzaServizio();
        Stato = $"Servizio giornaliero #{servizioGiornalieroId} caricato.";
    }

    private void NuovoServizioGiornaliero()
    {
        _servizioGiornalieroId = 0;
        SelectedServizioSalvato = null;
        ServizioData = DateTime.Today.ToString("dd/MM/yyyy");
        ServizioNumeroOrdine = string.Empty;
        ServizioOrario = string.Empty;
        ServizioTipoSelezionato = "InSede";
        ServizioLocalitaSelezionata = LocalitaOperativeCatalogo.FirstOrDefault();
        ServizioScopoSelezionato = ScopiImmersioneCatalogo.FirstOrDefault();
        ServizioUnitaNavaleSelezionata = UnitaNavaliCatalogo.FirstOrDefault();
        ServizioFuoriSede = false;
        ServizioAttivitaSvolta = string.Empty;
        ServizioNote = string.Empty;

        foreach (var partecipante in ServizioPartecipantiBozza)
        {
            partecipante.Presente = false;
            partecipante.GruppoOperativo = TrovaGruppoOperativo(partecipante.DefaultGruppoOperativoId);
            partecipante.RuoloOperativo = TrovaRuoloOperativo(partecipante.DefaultRuoloOperativoId);
            partecipante.Note = string.Empty;
        }

        foreach (var immersione in ServizioImmersioniBozza)
        {
            immersione.OrarioInizio = string.Empty;
            immersione.OrarioFine = string.Empty;
            immersione.DirettoreImmersione = null;
            immersione.OperatoreSoccorso = null;
            immersione.AssistenteBlsd = null;
            immersione.AssistenteSanitario = null;
            immersione.Note = string.Empty;

            foreach (var partecipazione in immersione.Partecipazioni)
            {
                partecipazione.InImmersione = false;
                partecipazione.TipologiaImmersioneOperativa = null;
                partecipazione.ProfonditaMetri = string.Empty;
                partecipazione.FasciaProfondita = null;
                partecipazione.OreImmersione = string.Empty;
                partecipazione.CategoriaContabileOre = null;
                partecipazione.TariffaProposta = null;
                partecipazione.ImportoStimato = null;
                partecipazione.Note = string.Empty;
            }
        }

        SincronizzaPartecipazioniImmersioneBozza();

        foreach (var supporto in ServizioSupportiOccasionaliBozza)
        {
            supporto.PropertyChanged -= ServizioSupportoOccasionale_PropertyChanged;
        }

        ServizioSupportiOccasionaliBozza.Clear();
        SelectedSupportoOccasionale = null;

        AggiornaContestoServizio();
        AggiornaRiepilogoBozzaServizio();
        Stato = "Nuova bozza servizio giornaliero.";
    }

    private void NuovoPersonale()
    {
        SelectedPersonale = null;
        PerId = 0;
        PerIdInput = string.Empty;
        Cognome = string.Empty;
        Nome = string.Empty;
        Qualifica = string.Empty;
        ProfiloPersonale = ProfiliPersonaleDisponibili[0];
        RuoloSanitario = string.Empty;
        CodiceFiscale = string.Empty;
        MatricolaPersonale = string.Empty;
        NumeroBrevettoSmz = string.Empty;
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
        Qualifica = personale.Qualifica;
        ProfiloPersonale = ProfiliPersonaleCatalogo.Normalizza(personale.ProfiloPersonale);
        RuoloSanitario = personale.RuoloSanitario;
        CodiceFiscale = personale.CodiceFiscale;
        MatricolaPersonale = personale.MatricolaPersonale;
        NumeroBrevettoSmz = personale.NumeroBrevettoSmz;
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
        Stato = $"Scheda caricata: {personale.NominativoCompleto}";
    }

    private void ApriSchedaSelezionata()
    {
        if (SelectedPersonale is null)
        {
            return;
        }

        CaricaPersonale(SelectedPersonale.PerId);
        SezioneAttivaIndex = PersonalSectionIndex;
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
            EseguiBackupLocaleSilenzioso("save-person");
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
            $"Archiviare la scheda di {Cognome} {Nome}?\n\nLa scheda verra rimossa dall'elenco operativo ma restera conservata nell'archivio interno.",
            "Conferma archiviazione",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        var archiveId = _repository.DeletePersonale(PerId);
        RicaricaSuggerimentiRicerca();
        CaricaElenco();
        InizializzaBozzaServizio(preserveSelections: true);
        CaricaArchivio();
        AggiornaScadenziario();
        NuovoPersonale();
        SelectedArchivio = ArchivioItems.FirstOrDefault(item => item.PersonaleArchivioId == archiveId);
        SezioneAttivaIndex = ArchiveSectionIndex;
        Stato = "Scheda archiviata. I dati restano conservati nell'archivio interno.";
        EseguiBackupLocaleSilenzioso("archive-person");
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

    private void SalvaAttagliamentoInEditor()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(AttagliamentoVoce))
            {
                throw new InvalidOperationException("La voce attagliamento e obbligatoria.");
            }

            if (string.IsNullOrWhiteSpace(AttagliamentoTagliaMisura) && string.IsNullOrWhiteSpace(AttagliamentoNote))
            {
                throw new InvalidOperationException("Indica almeno una taglia/misura oppure una nota.");
            }

            var nuovaRiga = PersonaleAttagliamentoRowViewModel.FromDraft(
                SelectedAttagliamento?.PersonaleAttagliamentoId,
                AttagliamentoVoce.Trim(),
                AttagliamentoTagliaMisura.Trim(),
                AttagliamentoNote.Trim());

            if (SelectedAttagliamento is null)
            {
                Attagliamento.Add(nuovaRiga);
            }
            else
            {
                var index = Attagliamento.IndexOf(SelectedAttagliamento);
                if (index >= 0)
                {
                    Attagliamento[index] = nuovaRiga;
                    SelectedAttagliamento = Attagliamento[index];
                }
            }

            PulisciEditorAttagliamento();
            Stato = "Attagliamento pronto in scheda. Salvare il personale per registrarlo nel database.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Attagliamento", MessageBoxButton.OK, MessageBoxImage.Warning);
            Stato = "Attagliamento non aggiunto";
        }
    }

    private void PulisciEditorAttagliamento()
    {
        _selectedAttagliamento = null;
        OnPropertyChanged(nameof(SelectedAttagliamento));
        OnPropertyChanged(nameof(AzioneAttagliamentoLabel));

        AttagliamentoVoce = string.Empty;
        AttagliamentoTagliaMisura = string.Empty;
        AttagliamentoNote = string.Empty;
    }

    private void CaricaEditorAttagliamentoDaSelezione()
    {
        if (SelectedAttagliamento is null)
        {
            AttagliamentoVoce = string.Empty;
            AttagliamentoTagliaMisura = string.Empty;
            AttagliamentoNote = string.Empty;
            return;
        }

        AttagliamentoVoce = SelectedAttagliamento.Voce;
        AttagliamentoTagliaMisura = SelectedAttagliamento.TagliaMisura;
        AttagliamentoNote = SelectedAttagliamento.Note;
    }

    private void RimuoviAttagliamentoRiga()
    {
        if (SelectedAttagliamento is null)
        {
            return;
        }

        Attagliamento.Remove(SelectedAttagliamento);
        PulisciEditorAttagliamento();
    }

    private ServizioGiornaliero BuildServizioGiornalieroModel()
    {
        var dataServizio = ParseDate(ServizioData, "Data servizio")
            ?? throw new InvalidOperationException("La data servizio e obbligatoria.");

        if (!TipiServizioDisponibili.Any(item => string.Equals(item, ServizioTipoSelezionato, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("Seleziona un tipo servizio valido.");
        }

        var partecipanti = BuildServizioPartecipanti();
        var supportiOccasionali = BuildSupportiOccasionali();

        if (partecipanti.Count == 0 && supportiOccasionali.Count == 0)
        {
            throw new InvalidOperationException("Inserisci almeno un partecipante o un supporto occasionale nel servizio.");
        }

        var immersioni = BuildServizioImmersioni(partecipanti);

        return new ServizioGiornaliero
        {
            ServizioGiornalieroId = _servizioGiornalieroId,
            DataServizio = dataServizio,
            NumeroOrdineServizio = ServizioNumeroOrdine.Trim(),
            OrarioServizio = ServizioOrario.Trim(),
            TipoServizio = ServizioTipoSelezionato,
            LocalitaOperativaId = ServizioLocalitaSelezionata?.LocalitaOperativaId,
            ScopoImmersioneId = ServizioScopoSelezionato?.ScopoImmersioneId,
            UnitaNavaleId = ServizioUnitaNavaleSelezionata?.UnitaNavaleId,
            FuoriSede = ServizioFuoriSede,
            AttivitaSvolta = ServizioAttivitaSvolta.Trim(),
            Note = ServizioNote.Trim(),
            Partecipanti = partecipanti,
            Immersioni = immersioni,
            SupportiOccasionali = supportiOccasionali,
        };
    }

    private List<ServizioPartecipante> BuildServizioPartecipanti()
    {
        var items = new List<ServizioPartecipante>();

        foreach (var row in ServizioPartecipantiBozza)
        {
            var includeRow = IsPartecipanteInternoCompilato(row);

            if (!includeRow)
            {
                continue;
            }

            if (row.GruppoOperativo is null)
            {
                throw new InvalidOperationException($"{row.Nominativo}: selezionare il gruppo operativo.");
            }

            items.Add(new ServizioPartecipante
            {
                PerId = row.PerId,
                GruppoOperativoId = row.GruppoOperativo.GruppoOperativoId,
                Presente = row.Presente,
                RuoloOperativoId = row.RuoloOperativo?.RuoloOperativoId,
                Note = row.Note.Trim(),
            });
        }

        return items;
    }

    private List<ServizioSupportoOccasionale> BuildSupportiOccasionali()
    {
        var items = new List<ServizioSupportoOccasionale>();

        foreach (var row in ServizioSupportiOccasionaliBozza)
        {
            var includeRow = IsSupportoOccasionaleCompilato(row);
            if (!includeRow)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(row.Nominativo))
            {
                throw new InvalidOperationException("Per ogni supporto occasionale il nominativo e obbligatorio.");
            }

            items.Add(new ServizioSupportoOccasionale
            {
                Nominativo = row.Nominativo.Trim(),
                Qualifica = row.Qualifica.Trim(),
                Ruolo = row.Ruolo.Trim(),
                Presente = row.Presente,
                Contatti = row.Contatti.Trim(),
                Note = row.Note.Trim(),
            });
        }

        return items;
    }

    private List<ServizioImmersione> BuildServizioImmersioni(IReadOnlyCollection<ServizioPartecipante> partecipanti)
    {
        var items = new List<ServizioImmersione>();
        var presentiPerId = partecipanti
            .Where(item => item.Presente)
            .Select(item => item.PerId)
            .ToHashSet();

        foreach (var row in ServizioImmersioniBozza)
        {
            var partecipazioniImmersione = BuildServizioPartecipazioniImmersione(row, presentiPerId);
            var includeRow = !string.IsNullOrWhiteSpace(row.OrarioInizio)
                || !string.IsNullOrWhiteSpace(row.OrarioFine)
                || row.DirettoreImmersione is not null
                || row.OperatoreSoccorso is not null
                || row.AssistenteBlsd is not null
                || row.AssistenteSanitario is not null
                || !string.IsNullOrWhiteSpace(row.Note)
                || partecipazioniImmersione.Count > 0;

            if (!includeRow)
            {
                continue;
            }

            var orarioInizio = ParseTime(row.OrarioInizio, $"Immersione {row.NumeroImmersione} - ora inizio");
            var orarioFine = ParseTime(row.OrarioFine, $"Immersione {row.NumeroImmersione} - ora fine");
            if (orarioInizio is not null && orarioFine is not null && orarioFine <= orarioInizio)
            {
                throw new InvalidOperationException($"Immersione {row.NumeroImmersione}: l'ora fine deve essere successiva all'ora inizio.");
            }

            ValidaOperatoreImmersione("direttore immersione", row.DirettoreImmersione, presentiPerId, row.NumeroImmersione);
            ValidaOperatoreImmersione("operatore soccorso", row.OperatoreSoccorso, presentiPerId, row.NumeroImmersione);
            ValidaOperatoreImmersione("assistenza BLSD", row.AssistenteBlsd, presentiPerId, row.NumeroImmersione);
            ValidaOperatoreImmersione("assistenza sanitaria", row.AssistenteSanitario, presentiPerId, row.NumeroImmersione);

            items.Add(new ServizioImmersione
            {
                NumeroImmersione = row.NumeroImmersione,
                OrarioInizio = orarioInizio,
                OrarioFine = orarioFine,
                DirettoreImmersionePerId = GetPerIdOperatoreSelezionato(row.DirettoreImmersione),
                OperatoreSoccorsoPerId = GetPerIdOperatoreSelezionato(row.OperatoreSoccorso),
                AssistenteBlsdPerId = GetPerIdOperatoreSelezionato(row.AssistenteBlsd),
                AssistenteSanitarioPerId = GetPerIdOperatoreSelezionato(row.AssistenteSanitario),
                LocalitaOperativaId = ServizioLocalitaSelezionata?.LocalitaOperativaId,
                ScopoImmersioneId = ServizioScopoSelezionato?.ScopoImmersioneId,
                Note = row.Note.Trim(),
                Partecipazioni = partecipazioniImmersione,
            });
        }

        return items;
    }

    private List<ServizioPartecipanteImmersione> BuildServizioPartecipazioniImmersione(
        ServizioImmersioneDraftViewModel immersione,
        IReadOnlySet<int> presentiPerId)
    {
        var items = new List<ServizioPartecipanteImmersione>();

        foreach (var row in immersione.Partecipazioni)
        {
            var includeRow = IsPartecipazioneImmersioneCompilata(row);
            if (!includeRow)
            {
                continue;
            }

            if (!presentiPerId.Contains(row.PerId))
            {
                throw new InvalidOperationException($"Immersione {immersione.NumeroImmersione}: {row.Nominativo} non risulta presente nel servizio.");
            }

            if (row.TipologiaImmersioneOperativa is null)
            {
                throw new InvalidOperationException($"Immersione {immersione.NumeroImmersione}: selezionare l'apparato per {row.Nominativo}.");
            }

            var profondita = ParseNullableInt(row.ProfonditaMetri, $"Immersione {immersione.NumeroImmersione} - profondita {row.Nominativo}");
            ValidaProfonditaPerTipologia(
                row.TipologiaImmersioneOperativa,
                profondita,
                $"Immersione {immersione.NumeroImmersione} - profondita {row.Nominativo}");
            var fascia = row.FasciaProfondita;
            if (fascia is null && profondita is not null)
            {
                fascia = FasceProfonditaCatalogo.FirstOrDefault(item => profondita.Value >= item.MetriDa && profondita.Value <= item.MetriA);
            }

            if (fascia is null)
            {
                throw new InvalidOperationException($"Immersione {immersione.NumeroImmersione}: selezionare la fascia profondita per {row.Nominativo}.");
            }

            var ore = ParseNullableDecimal(row.OreImmersione, $"Immersione {immersione.NumeroImmersione} - ore {row.Nominativo}");
            if (ore is null || ore <= 0)
            {
                throw new InvalidOperationException($"Immersione {immersione.NumeroImmersione}: indicare ore immersione valide per {row.Nominativo}.");
            }

            if (row.CategoriaContabileOre is null)
            {
                throw new InvalidOperationException($"Immersione {immersione.NumeroImmersione}: selezionare la categoria contabile per {row.Nominativo}.");
            }

            items.Add(new ServizioPartecipanteImmersione
            {
                ServizioPartecipanteId = row.PerId,
                TipologiaImmersioneOperativaId = row.TipologiaImmersioneOperativa.TipologiaImmersioneOperativaId,
                ProfonditaMetri = profondita,
                FasciaProfonditaId = fascia.FasciaProfonditaId,
                OreImmersione = ore,
                CategoriaContabileOreId = row.CategoriaContabileOre.CategoriaContabileOreId,
                Note = row.Note.Trim(),
            });
        }

        return items;
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

        if (!ProfiliPersonaleDisponibili.Contains(ProfiloPersonale, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("Seleziona un profilo personale valido.");
        }

        if (IsProfiloSanitario && string.IsNullOrWhiteSpace(RuoloSanitario))
        {
            throw new InvalidOperationException("Per il profilo sanitario seleziona il ruolo sanitario.");
        }

        return new Personale
        {
            PerId = ParseRequiredPerId(),
            Cognome = Cognome.Trim(),
            Nome = Nome.Trim(),
            Qualifica = Qualifica.Trim(),
            ProfiloPersonale = ProfiliPersonaleCatalogo.Normalizza(ProfiloPersonale),
            RuoloSanitario = IsProfiloSanitario ? RuoloSanitario.Trim() : string.Empty,
            CodiceFiscale = CodiceFiscale.Trim().ToUpperInvariant(),
            MatricolaPersonale = MatricolaPersonale.Trim(),
            NumeroBrevettoSmz = NumeroBrevettoSmz.Trim(),
            DataNascita = ParseDate(DataNascita, "Data di nascita"),
            LuogoNascita = LuogoNascita.Trim(),
            ViaResidenza = ViaResidenza.Trim(),
            CapResidenza = CapResidenza.Trim(),
            CittaResidenza = CittaResidenza.Trim(),
            Telefono1 = Telefono1.Trim(),
            Telefono2 = Telefono2.Trim(),
            Mail1Utente = NormalizeMailUtente(Mail1Utente, "Mail Polizia"),
            Mail2Utente = Mail2Utente.Trim(),
            Abilitazioni = BuildAbilitazioni(),
            VisiteMediche = BuildVisite(),
            Attagliamento = BuildAttagliamento(),
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
        if (IsProfiloSanitario)
        {
            return VisiteMediche
                .Where(row => !string.IsNullOrWhiteSpace(row.DataUltimaVisita))
                .Select(row =>
                {
                    var tipoVisita = row.TipoVisita.Trim();
                    var dataUltimaVisita = ParseDate(row.DataUltimaVisita, $"Data ultima visita {row.TipoVisita}");
                    var dataScadenzaManuale = ParseDate(row.DataScadenza, $"Data scadenza visita {row.TipoVisita}");
                    var dataScadenza = CalcolaScadenzaVisita(tipoVisita, dataUltimaVisita) ?? dataScadenzaManuale;

                    return new VisitaMedica
                    {
                        VisitaMedicaId = row.VisitaMedicaId ?? 0,
                        PerId = PerId,
                        TipoVisita = tipoVisita,
                        DataUltimaVisita = dataUltimaVisita,
                        DataScadenza = dataScadenza,
                        Esito = row.Esito.Trim(),
                        Note = row.Note.Trim(),
                    };
                })
                .ToList();
        }

        var items = new List<VisitaMedica>();
        var visitePerTipo = VisiteMediche.ToDictionary(
            row => row.TipoVisita.Trim(),
            row => row,
            StringComparer.OrdinalIgnoreCase);

        foreach (var tipo in TipiVisitaMedicaCatalogo)
        {
            if (!visitePerTipo.TryGetValue(tipo.Descrizione, out var row))
            {
                throw new InvalidOperationException($"{tipo.Descrizione}: visita obbligatoria non presente in scheda.");
            }

            if (string.IsNullOrWhiteSpace(row.DataUltimaVisita))
            {
                throw new InvalidOperationException($"{tipo.Descrizione}: la data ultima visita e obbligatoria.");
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

    private List<PersonaleAttagliamento> BuildAttagliamento()
    {
        return Attagliamento
            .Where(row => !string.IsNullOrWhiteSpace(row.Voce))
            .Select(row => new PersonaleAttagliamento
            {
                PersonaleAttagliamentoId = row.PersonaleAttagliamentoId ?? 0,
                PerId = PerId,
                Voce = row.Voce.Trim(),
                TagliaMisura = row.TagliaMisura.Trim(),
                Note = row.Note.Trim(),
            })
            .ToList();
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

    private static void ValidaOperatoreImmersione(
        string ruolo,
        PersonaleListItemViewModel? operatore,
        IReadOnlySet<int> presentiPerId,
        int numeroImmersione)
    {
        if (operatore is null || operatore.PerId <= 0)
        {
            return;
        }

        if (!presentiPerId.Contains(operatore.PerId))
        {
            throw new InvalidOperationException(
                $"Immersione {numeroImmersione}: {operatore.Nominativo} e indicato come {ruolo}, ma non risulta presente nel servizio.");
        }
    }

    private static TimeOnly? ParseTime(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (TimeOnly.TryParse(value, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"{fieldName}: usare un orario valido, ad esempio 08:30.");
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

    private static decimal? ParseNullableDecimal(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed)
            || decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"{fieldName}: valore numerico non valido.");
    }

    private static string NormalizeMailUtente(string value, string fieldName)
    {
        return MailPoliziaHelper.NormalizeUserPart(value, fieldName);
    }

    private static string FormatTime(TimeOnly? value) => value?.ToString("HH:mm") ?? string.Empty;

    private static string FormatDecimal(decimal? value) =>
        value?.ToString("0.##", CultureInfo.CurrentCulture) ?? string.Empty;

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

    private static TimeOnly? ParseTimeSilenzioso(string value)
    {
        return TimeOnly.TryParse(value, out var parsed) ? parsed : null;
    }

    private static decimal? ParseDecimalSilenzioso(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed)
            || decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
        {
            return parsed;
        }

        return null;
    }

    private static decimal CalcolaImportoContabile(decimal tariffa, string codiceCategoria, decimal ore)
    {
        return codiceCategoria switch
        {
            "ADD" => Math.Round(tariffa * (ore / 2m), 2, MidpointRounding.AwayFromZero),
            "SPER" => Math.Round((tariffa + tariffa * 0.25m) * ore, 2, MidpointRounding.AwayFromZero),
            "CI" => Math.Round(tariffa * ore * 0.8m, 2, MidpointRounding.AwayFromZero),
            _ => Math.Round(tariffa * ore, 2, MidpointRounding.AwayFromZero),
        };
    }

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
        if (SelectedVisita is not null)
        {
            CaricaEditorVisitaDaSelezione();
            return;
        }

        if (VisiteMediche.Count > 0)
        {
            SelectedVisita = VisiteMediche[0];
            return;
        }

        VisitaTipoSelezionato = null;
        VisitaDataUltimaVisita = string.Empty;
        VisitaEsito = string.Empty;
        VisitaNote = string.Empty;
    }

    private void CaricaEditorVisitaDaSelezione()
    {
        if (SelectedVisita is null)
        {
            if (VisiteMediche.Count > 0)
            {
                SelectedVisita = VisiteMediche[0];
                return;
            }

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
        OnPropertyChanged(nameof(SchedaAbilitazioniPrincipali));
        OnPropertyChanged(nameof(SchedaAbilitazioniPrincipaliFooter));
        OnPropertyChanged(nameof(SchedaScadenzeTotali));
        OnPropertyChanged(nameof(SchedaScaduteTotali));
        OnPropertyChanged(nameof(SchedaHaScadute));
        OnPropertyChanged(nameof(SchedaScaduteTitolo));
        OnPropertyChanged(nameof(SchedaScaduteHighlight));
        OnPropertyChanged(nameof(SchedaScaduteDettaglio));
        OnPropertyChanged(nameof(SchedaVisiteTotali));
        OnPropertyChanged(nameof(SchedaProssimaScadenza));
        OnPropertyChanged(nameof(SchedaProssimaScadenzaDettaglio));
    }

    private void InizializzaBozzaServizio(bool preserveSelections)
    {
        var personaleAttivo = _repository
            .SearchPersonale(string.Empty, null, null)
            .OrderBy(item => item.Cognome)
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
                Nominativo = personale.NominativoCompleto,
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
            ServizioImmersioniBozza.Add(new ServizioImmersioneDraftViewModel { NumeroImmersione = 1 });
            ServizioImmersioniBozza.Add(new ServizioImmersioneDraftViewModel { NumeroImmersione = 2 });

            foreach (var immersione in ServizioImmersioniBozza)
            {
                immersione.PropertyChanged += ServizioImmersioneBozza_PropertyChanged;
            }
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

    private void SincronizzaPartecipazioniImmersioneBozza()
    {
        var operatoriSmzPresenti = ServizioPartecipantiBozza
            .Where(item => item.Presente)
            .Select(item => TrovaOperatoreServizio(item.PerId))
            .Where(item => item is not null && !ProfiliPersonaleCatalogo.IsSanitario(item.ProfiloPersonale))
            .Cast<PersonaleListItemViewModel>()
            .OrderBy(item => item.Cognome)
            .ThenBy(item => item.Nome)
            .ToList();

        foreach (var immersione in ServizioImmersioniBozza)
        {
            var existing = immersione.Partecipazioni.ToDictionary(item => item.PerId);
            var orderedRows = new List<ServizioPartecipanteImmersioneDraftViewModel>();

            foreach (var operatore in operatoriSmzPresenti)
            {
                if (existing.TryGetValue(operatore.PerId, out var row))
                {
                    row.Qualifica = operatore.Qualifica;
                    row.Nominativo = operatore.Nominativo;
                    orderedRows.Add(row);
                    continue;
                }

                row = new ServizioPartecipanteImmersioneDraftViewModel
                {
                    PerId = operatore.PerId,
                    Qualifica = operatore.Qualifica,
                    Nominativo = operatore.Nominativo,
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

            AggiornaOreAutomaticheImmersione(immersione);
        }
    }

    private void AggiornaOreAutomaticheImmersione(ServizioImmersioneDraftViewModel immersione)
    {
        foreach (var partecipazione in immersione.Partecipazioni)
        {
            AggiornaOreAutomatichePerPartecipazione(partecipazione);
            AggiornaCalcoliPartecipazioneImmersione(partecipazione);
        }
    }

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

    private void PropagaProfonditaPrimaRiga(ServizioImmersioneDraftViewModel immersione)
    {
        if (_isSyncingProfonditaPrimaRiga || immersione.Partecipazioni.Count == 0)
        {
            return;
        }

        var profondita = immersione.Partecipazioni[0].ProfonditaMetri.Trim();
        if (string.IsNullOrWhiteSpace(profondita))
        {
            return;
        }

        _isSyncingProfonditaPrimaRiga = true;
        try
        {
            foreach (var partecipazione in immersione.Partecipazioni.Skip(1).Where(item => item.InImmersione))
            {
                partecipazione.ProfonditaMetri = profondita;
            }
        }
        finally
        {
            _isSyncingProfonditaPrimaRiga = false;
        }
    }

    private void AggiornaOreAutomatichePerPartecipazione(ServizioPartecipanteImmersioneDraftViewModel partecipazione)
    {
        if (!partecipazione.InImmersione || !string.IsNullOrWhiteSpace(partecipazione.OreImmersione))
        {
            return;
        }

        var immersione = ServizioImmersioniBozza.FirstOrDefault(item => item.Partecipazioni.Contains(partecipazione));
        if (immersione is null)
        {
            return;
        }

        var orarioInizio = ParseTimeSilenzioso(immersione.OrarioInizio);
        var orarioFine = ParseTimeSilenzioso(immersione.OrarioFine);
        if (orarioInizio is null || orarioFine is null || orarioFine <= orarioInizio)
        {
            return;
        }

        var ore = Math.Round((decimal)(orarioFine.Value - orarioInizio.Value).TotalMinutes / 60m, 2, MidpointRounding.AwayFromZero);
        partecipazione.OreImmersione = FormatDecimal(ore);
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
            : CalcolaImportoContabile(regola.Tariffa, categoria.Codice, ore.Value);
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
        if (e.PropertyName is nameof(ServizioImmersioneDraftViewModel.OrarioInizio)
            or nameof(ServizioImmersioneDraftViewModel.OrarioFine)
            or nameof(ServizioImmersioneDraftViewModel.DirettoreImmersione)
            or nameof(ServizioImmersioneDraftViewModel.OperatoreSoccorso)
            or nameof(ServizioImmersioneDraftViewModel.AssistenteBlsd)
            or nameof(ServizioImmersioneDraftViewModel.AssistenteSanitario))
        {
            if (sender is ServizioImmersioneDraftViewModel immersione
                && e.PropertyName is nameof(ServizioImmersioneDraftViewModel.OrarioInizio)
                    or nameof(ServizioImmersioneDraftViewModel.OrarioFine))
            {
                AggiornaOreAutomaticheImmersione(immersione);
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
                ApplicaProfonditaPrimaRigaAllaPartecipazione(row);
            }

            if (row.InImmersione && string.IsNullOrWhiteSpace(row.OreImmersione))
            {
                AggiornaOreAutomatichePerPartecipazione(row);
            }

            AggiornaCalcoliPartecipazioneImmersione(row);
            AggiornaRiepilogoBozzaServizio();
            return;
        }

        if (e.PropertyName is nameof(ServizioPartecipanteImmersioneDraftViewModel.ProfonditaMetri))
        {
            if (!_isSyncingProfonditaPrimaRiga
                && TrovaImmersioneDiPartecipazione(row) is { } immersione
                && immersione.Partecipazioni.Count > 0
                && ReferenceEquals(immersione.Partecipazioni[0], row))
            {
                PropagaProfonditaPrimaRiga(immersione);
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
            if (e.PropertyName is nameof(ServizioPartecipanteImmersioneDraftViewModel.TipologiaImmersioneOperativa))
            {
                AggiornaFasciaDaProfondita(row);
            }

            AggiornaCalcoliPartecipazioneImmersione(row);
            AggiornaRiepilogoBozzaServizio();
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

    private void ApplicaProfonditaPrimaRigaAllaPartecipazione(ServizioPartecipanteImmersioneDraftViewModel partecipazione)
    {
        if (_isSyncingProfonditaPrimaRiga
            || !partecipazione.InImmersione
            || !string.IsNullOrWhiteSpace(partecipazione.ProfonditaMetri))
        {
            return;
        }

        var immersione = TrovaImmersioneDiPartecipazione(partecipazione);
        if (immersione is null
            || immersione.Partecipazioni.Count == 0
            || ReferenceEquals(immersione.Partecipazioni[0], partecipazione))
        {
            return;
        }

        var profondita = immersione.Partecipazioni[0].ProfonditaMetri.Trim();
        if (string.IsNullOrWhiteSpace(profondita))
        {
            return;
        }

        _isSyncingProfonditaPrimaRiga = true;
        try
        {
            partecipazione.ProfonditaMetri = profondita;
        }
        finally
        {
            _isSyncingProfonditaPrimaRiga = false;
        }
    }

    private void AggiornaOperatoriServizioPresentiDisponibili()
    {
        var operatoriPresenti = ServizioPartecipantiBozza
            .Where(item => item.Presente)
            .Select(item => TrovaOperatoreServizio(item.PerId))
            .Where(item => item is not null)
            .Cast<PersonaleListItemViewModel>()
            .OrderBy(item => item.Cognome)
            .ThenBy(item => item.Nome)
            .ToList();

        OperatoriServizioPresentiDisponibili.Clear();
        OperatoriServizioPresentiDisponibili.Add(OperatoreVuoto);

        foreach (var operatore in operatoriPresenti)
        {
            OperatoriServizioPresentiDisponibili.Add(operatore);
        }

        SincronizzaRuoliImmersioneConPresenti();
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
        OnPropertyChanged(nameof(ServizioTipoDescrizione));
        OnPropertyChanged(nameof(ServizioFuoriSedeDescrizione));
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

        var cataloghiServizio = _repository.GetCataloghiServizio();
        SostituisciCollection(CategorieRegistroCatalogo, cataloghiServizio.CategorieRegistro);
        SostituisciCollection(LocalitaOperativeCatalogo, cataloghiServizio.LocalitaOperative);
        SostituisciCollection(ScopiImmersioneCatalogo, cataloghiServizio.ScopiImmersione);
        SostituisciCollection(UnitaNavaliCatalogo, cataloghiServizio.UnitaNavali);
        SostituisciCollection(TipologieImmersioneOperativeCatalogo, cataloghiServizio.TipologieImmersione);
        SostituisciCollection(FasceProfonditaCatalogo, cataloghiServizio.FasceProfondita);
        SostituisciCollection(CategorieContabiliOreCatalogo, cataloghiServizio.CategorieContabiliOre);
        SostituisciCollection(GruppiOperativiCatalogo, cataloghiServizio.GruppiOperativi);
        SostituisciCollection(RuoliOperativiCatalogo, cataloghiServizio.RuoliOperativi);
        SostituisciCollection(RegoleContabiliImmersioneCatalogo, cataloghiServizio.RegoleContabiliImmersione);

        _servizioLocalitaSelezionata = LocalitaOperativeCatalogo.FirstOrDefault();
        _servizioScopoSelezionato = ScopiImmersioneCatalogo.FirstOrDefault();
        _servizioUnitaNavaleSelezionata = UnitaNavaliCatalogo.FirstOrDefault();

        RicaricaSuggerimentiRicerca();
        AggiornaSuggerimentiRicerca();
        CaricaElenco();
        CaricaArchivio();
        CaricaServiziSalvati();
        AggiornaAnniContabilitaDisponibili();
        CaricaContabilitaMensile();
        CaricaRegistroImmersioniMensile();
        AggiornaScadenziario();
        InizializzaEditorTariffeContabili();
        InizializzaBozzaServizio(preserveSelections: false);
        NuovoServizioGiornaliero();
        NuovoPersonale();
        MostraTariffeContabili = false;
        SezioneAttivaIndex = HomeSectionIndex;
    }

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
