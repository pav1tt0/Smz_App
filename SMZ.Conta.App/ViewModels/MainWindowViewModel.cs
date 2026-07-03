using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using SMZ.Conta.App.Data;
using SMZ.Conta.App.Infrastructure;
using SMZ.Conta.App.Models;
using SMZ.Conta.App.Printing;

namespace SMZ.Conta.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private const int HomeSectionIndex = 0;
    private const int ServicesSectionIndex = 1;
    private const int PersonalSectionIndex = 2;
    private const int ArchiveSectionIndex = 3;
    private const int AccountingSectionIndex = 4;
    private const int ReportsSectionIndex = 5;
    private const int SettingsSectionIndex = 6;
    private static readonly PersonaleListItemViewModel OperatoreVuoto = new();
    private static readonly UnitaNavale UnitaNavaleVuota = new() { UnitaNavaleId = 0, Descrizione = string.Empty, Ordine = 0 };
    private static readonly string[] OrariServizioFissi =
    [
        "07.00/13.00",
        "08.00/14.00",
        "13.00/19.00",
        "14.00/20.00",
        "19.00/24.00",
        "00.00/07.00",
    ];

    private readonly BackupService _backupService = new();
    private readonly PersonaleRepository _repository = new();
    private readonly ServizioScambioService _servizioScambioService;
    private readonly ServizioGiornalieroPrintService _servizioGiornalieroPrintService;
    private readonly RegistroImmersioniMensilePrintService _registroImmersioniMensilePrintService;
    private readonly RelayCommand _deleteCommand;
    private readonly RelayCommand _deleteDefinitivoCommand;
    private readonly RelayCommand _navigateSectionCommand;
    private readonly RelayCommand _newServizioCommand;
    private readonly RelayCommand _saveServizioCommand;
    private readonly RelayCommand _printServizioCommand;
    private readonly RelayCommand _printServizioSelezionatoCommand;
    private readonly RelayCommand _openServizioCommand;
    private readonly RelayCommand _openServizioFromListCommand;
    private readonly RelayCommand _closeSchedaServizioCommand;
    private readonly RelayCommand _duplicateServizioCommand;
    private readonly RelayCommand _clearServiziSearchCommand;
    private readonly RelayCommand _exportServizioPackageCommand;
    private readonly RelayCommand _importServizioPackageCommand;
    private readonly RelayCommand _deleteServizioCommand;
    private readonly RelayCommand _addLocalitaOperativaCommand;
    private readonly RelayCommand _addUnitaNavaleCommand;
    private readonly RelayCommand _addSupportoOccasionaleCommand;
    private readonly RelayCommand _removeSupportoOccasionaleCommand;
    private readonly RelayCommand _addOperatoreSubEsternoCommand;
    private readonly RelayCommand _removeOperatoreSubEsternoCommand;
    private readonly RelayCommand _addImmersioneCommand;
    private readonly RelayCommand _removeImmersioneCommand;
    private readonly RelayCommand _openSelectedPersonaleCommand;
    private readonly RelayCommand _closeSchedaPersonaleCommand;
    private readonly RelayCommand _reloadServizioPersonaleCommand;
    private readonly RelayCommand _reloadContabilitaCommand;
    private readonly RelayCommand _reloadRegistroImmersioniCommand;
    private readonly RelayCommand _reloadReportPersonaleCommand;
    private readonly RelayCommand _printRegistroImmersioniMensileCommand;
    private readonly RelayCommand _printRegistroImmersioniMensileCompattoCommand;
    private readonly RelayCommand _printContabilitaMensileCommand;
    private readonly RelayCommand _saveElaborazioneMensileCommand;
    private readonly RelayCommand _exportContabilitaCsvCommand;
    private readonly RelayCommand _exportContabilitaExcelCommand;
    private readonly RelayCommand _clearContabilitaSmzFiltersCommand;
    private readonly RelayCommand _clearReportPersonaleFiltersCommand;
    private readonly RelayCommand _saveLocalitaOperativeCommand;
    private readonly RelayCommand _saveUnitaNavaliCommand;
    private readonly RelayCommand _saveTariffeContabiliCommand;
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
    private readonly DispatcherTimer _clockTimer;
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
    private bool _isSchedaPersonaleVisibile;
    private bool _isSchedaServizioVisibile;
    private bool _isServizioApertoDaReport;
    private AbilitazioneFilterOptionViewModel? _filtroAbilitazione;
    private string _filtroVisiteEntro = string.Empty;
    private int _sezioneAttivaIndex;
    private int _schedaDettaglioTabIndex;
    private int _perId;
    private string _perIdInput = string.Empty;
    private string _cognome = string.Empty;
    private string _nome = string.Empty;
    private string _qualifica = string.Empty;
    private string _dataDecorrenzaQualifica = string.Empty;
    private string _profiloPersonale = ProfiliPersonaleCatalogo.OperatoreSubacqueo;
    private string _ruoloSanitario = string.Empty;
    private string _codiceFiscale = string.Empty;
    private string _matricolaPersonale = string.Empty;
    private string _numeroBrevettoSmz = string.Empty;
    private string _statoServizioPersonale = StatoServizioPersonaleCatalogo.Attivo;
    private string _dataFineServizio = string.Empty;
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
    private DateTime _headerDateTime = DateTime.Now;
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
    private string _serviziSearchText = string.Empty;
    private string _serviziNumeroSearchText = string.Empty;
    private string _serviziDataSearchText = string.Empty;
    private long _servizioGiornalieroId;
    private string _servizioData = DateTime.Today.ToString("dd/MM/yyyy");
    private string _servizioNumeroOrdine = string.Empty;
    private string _servizioOrario = string.Empty;
    private string _servizioOrarioFissoSelezionato = string.Empty;
    private bool _servizioOrarioDerogaAttiva;
    private string _servizioOrarioDerogaInizio = string.Empty;
    private string _servizioOrarioDerogaFine = string.Empty;
    private string _servizioTipoSelezionato = "InSede";
    private LocalitaOperativa? _servizioLocalitaSelezionata;
    private ScopoImmersioneItem? _servizioScopoSelezionato;
    private UnitaNavale? _servizioUnitaNavaleSelezionata;
    private PersonaleListItemViewModel? _servizioResponsabileSelezionato;
    private bool _servizioFuoriSede;
    private bool _servizioIndennitaOrdinePubblico;
    private bool _servizioStraordinarioAttivo;
    private string _servizioStraordinarioInizio = string.Empty;
    private string _servizioStraordinarioFine = string.Empty;
    private string _nuovaLocalitaOperativa = string.Empty;
    private string _nuovaUnitaNavale = string.Empty;
    private string _servizioAttivitaSvolta = string.Empty;
    private string _servizioNote = string.Empty;
    private int _contabilitaAnnoSelezionato;
    private ContabilitaMeseItem? _contabilitaMeseSelezionato;
    private ContabilitaMeseItem? _reportPersonaleMeseSelezionato;
    private bool _contabilitaSelezionePronta;
    private string _contabilitaSmzFiltroData = string.Empty;
    private string _contabilitaSmzFiltroNumeroServizio = string.Empty;
    private string _contabilitaSmzFiltroNominativo = string.Empty;
    private string _contabilitaSmzFiltroApparato = string.Empty;
    private string _reportPersonaleFiltroNominativo = string.Empty;
    private bool _isWelcomeVisible = true;
    private bool _isWelcomeAudioEnabled = true;
    private bool _isSyncingValoriCondivisiImmersione;
    private bool _isSyncingPartecipazioniUniche;
    private ElaborazioneMensileInfo? _elaborazioneMensileInfo;
    private ServizioGiornalieroSummary? _selectedServizioSalvato;
    private ServizioSupportoOccasionaleDraftViewModel? _selectedSupportoOccasionale;
    private ServizioOperatoreSubEsternoDraftViewModel? _selectedOperatoreSubEsterno;
    private string _personaleEditorSnapshot = string.Empty;
    private string _servizioEditorSnapshot = string.Empty;
    private string _tariffeEditorSnapshot = string.Empty;
    private readonly List<ContabilitaSmzSummary> _contabilitaSmzSource = [];
    private readonly List<ReportPersonaleMensileRiga> _reportPersonaleSource = [];

    public MainWindowViewModel()
    {
        _backupSettings = _backupService.LoadSettings();
        _servizioScambioService = new ServizioScambioService(_repository);
        _servizioGiornalieroPrintService = new ServizioGiornalieroPrintService(_repository);
        _registroImmersioniMensilePrintService = new RegistroImmersioniMensilePrintService(_repository);
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
        _printServizioCommand = new RelayCommand(StampaServizioGiornaliero);
        _printServizioSelezionatoCommand = new RelayCommand(StampaServizioSelezionato, () => SelectedServizioSalvato is not null);
        _openServizioCommand = new RelayCommand(ApriServizioSelezionato, () => SelectedServizioSalvato is not null);
        _openServizioFromListCommand = new RelayCommand(ApriServizioDaParametro);
        _closeSchedaServizioCommand = new RelayCommand(ChiudiSchedaServizio);
        _duplicateServizioCommand = new RelayCommand(DuplicaServizioSelezionato, () => SelectedServizioSalvato is not null);
        _clearServiziSearchCommand = new RelayCommand(PulisciRicercaServizi);
        _exportServizioPackageCommand = new RelayCommand(EsportaPacchettoServizioSelezionato, () => SelectedServizioSalvato is not null);
        _importServizioPackageCommand = new RelayCommand(ImportaPacchettoServizio);
        _deleteServizioCommand = new RelayCommand(EliminaServizioSelezionato, () => SelectedServizioSalvato is not null);
        _addLocalitaOperativaCommand = new RelayCommand(AggiungiLocalitaOperativa);
        _addUnitaNavaleCommand = new RelayCommand(AggiungiUnitaNavale);
        _addSupportoOccasionaleCommand = new RelayCommand(AggiungiSupportoOccasionale);
        _removeSupportoOccasionaleCommand = new RelayCommand(RimuoviSupportoOccasionale, () => SelectedSupportoOccasionale is not null);
        _addOperatoreSubEsternoCommand = new RelayCommand(AggiungiOperatoreSubEsterno);
        _removeOperatoreSubEsternoCommand = new RelayCommand(RimuoviOperatoreSubEsterno, () => SelectedOperatoreSubEsterno is not null);
        _addImmersioneCommand = new RelayCommand(AggiungiImmersione);
        _removeImmersioneCommand = new RelayCommand(RimuoviImmersione);
        NewCommand = new RelayCommand(() =>
        {
            NuovoPersonale();
            SezioneAttivaIndex = PersonalSectionIndex;
            IsSchedaPersonaleVisibile = true;
        });
        SaveCommand = new RelayCommand(SalvaPersonale);
        _deleteCommand = new RelayCommand(DisattivaPersonaleDaOggi, () => PerId > 0);
        _deleteDefinitivoCommand = new RelayCommand(EliminaPersonaleDefinitivamente, () => PerId > 0);
        _openSelectedPersonaleCommand = new RelayCommand(ApriSchedaSelezionata);
        _closeSchedaPersonaleCommand = new RelayCommand(() => IsSchedaPersonaleVisibile = false);
        _restoreArchivioCommand = new RelayCommand(RipristinaArchivioDaParametro, () => SelectedArchivio is not null);
        _deleteArchivioDefinitivoCommand = new RelayCommand(EliminaArchivioDefinitivamenteDaParametro, () => SelectedArchivio is not null);
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
        _reloadReportPersonaleCommand = new RelayCommand(CaricaReportPersonaleMensile);
        _printRegistroImmersioniMensileCommand = new RelayCommand(() => StampaRegistroImmersioniMensile(RegistroImmersioniMensilePrintLayout.Normale));
        _printRegistroImmersioniMensileCompattoCommand = new RelayCommand(() => StampaRegistroImmersioniMensile(RegistroImmersioniMensilePrintLayout.Compatto));
        _printContabilitaMensileCommand = new RelayCommand(StampaContabilitaMensile);
        _saveElaborazioneMensileCommand = new RelayCommand(SalvaElaborazioneMensile);
        _exportContabilitaCsvCommand = new RelayCommand(EsportaContabilitaCsv);
        _exportContabilitaExcelCommand = new RelayCommand(EsportaContabilitaExcel);
        _clearContabilitaSmzFiltersCommand = new RelayCommand(PulisciFiltriContabilitaSmz);
        _clearReportPersonaleFiltersCommand = new RelayCommand(PulisciFiltriReportPersonale);
        _saveLocalitaOperativeCommand = new RelayCommand(SalvaLocalitaOperative);
        _saveUnitaNavaliCommand = new RelayCommand(SalvaUnitaNavali);
        _saveTariffeContabiliCommand = new RelayCommand(SalvaTariffeContabili);
        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30),
        };
        _clockTimer.Tick += (_, _) => AggiornaDataOraHeader();
        _clockTimer.Start();

        Abilitazioni = new ObservableCollection<PersonaleAbilitazioneRowViewModel>();
        VisiteMediche = new ObservableCollection<VisitaMedicaRowViewModel>();
        Attagliamento = new ObservableCollection<PersonaleAttagliamentoRowViewModel>();
        Attagliamento.CollectionChanged += (_, _) => AggiornaStatoAttagliamento();
        OperatoriServizioDisponibili = new ObservableCollection<PersonaleListItemViewModel>();
        OperatoriServizioPresentiDisponibili = new ObservableCollection<PersonaleListItemViewModel>();
        ServizioPartecipantiBozza = new ObservableCollection<ServizioPartecipanteDraftViewModel>();
        ServizioImmersioniBozza = new ObservableCollection<ServizioImmersioneDraftViewModel>();
        ServizioPartecipazioniContabiliUnicheBozza = new ObservableCollection<ServizioPartecipanteImmersioneUnicoDraftViewModel>();
        ServizioOperatoriSubEsterniBozza = new ObservableCollection<ServizioOperatoreSubEsternoDraftViewModel>();
        ServizioSupportiOccasionaliBozza = new ObservableCollection<ServizioSupportoOccasionaleDraftViewModel>();
        ServiziSalvati = new ObservableCollection<ServizioGiornalieroSummary>();
        ContabilitaSmzItems = new ObservableCollection<ContabilitaSmzSummary>();
        ContabilitaSmzDateDisponibili = new ObservableCollection<string>();
        ContabilitaSmzNumeriServizioDisponibili = new ObservableCollection<string>();
        ContabilitaSmzApparatiDisponibili = new ObservableCollection<string>();
        ContabilitaSanitariItems = new ObservableCollection<ContabilitaSanitarioSummary>();
        ContabilitaSupportiItems = new ObservableCollection<ContabilitaSupportoSummary>();
        RegistroImmersioniItems = new ObservableCollection<RegistroImmersioneRiga>();
        RegistroImmersioniCategorieItems = new ObservableCollection<RegistroImmersioneCategoriaSummary>();
        ReportPersonaleItems = new ObservableCollection<ReportPersonaleMensileRiga>();
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
        ReportPersonaleMesiDisponibili = new ObservableCollection<ContabilitaMeseItem>(
        [
            new ContabilitaMeseItem { NumeroMese = 0, Descrizione = "Tutto l'anno" },
            ..ContabilitaMesiDisponibili,
        ]);
        ContabilitaAnniDisponibili = new ObservableCollection<int>();
        ArchivioItems = new ObservableCollection<PersonaleArchivioListItemViewModel>();
        ArchivioAbilitazioni = new ObservableCollection<PersonaleAbilitazioneRowViewModel>();
        ArchivioVisiteMediche = new ObservableCollection<VisitaMedicaRowViewModel>();
        ScadenzeProssime = new ObservableCollection<ScadenzaItemViewModel>();
        SearchSuggestions = new ObservableCollection<string>();
        FiltriScadenze = new ObservableCollection<string>(["Tutte", "Solo visite", "Solo abilitazioni"]);
        TipiAbilitazioneCatalogo = new ObservableCollection<TipoAbilitazione>(_repository.GetTipiAbilitazione());
        TipiServizioDisponibili = new ObservableCollection<string>(["InSede", "FuoriSede"]);
        OrariServizioFissiDisponibili = new ObservableCollection<string>(["", ..OrariServizioFissi]);
        CategorieRegistroCatalogo = new ObservableCollection<CategoriaRegistroItem>(cataloghiServizio.CategorieRegistro);
        LocalitaOperativeCatalogo = new ObservableCollection<LocalitaOperativa>(cataloghiServizio.LocalitaOperative);
        LocalitaOperativeServizioCatalogo = new ObservableCollection<LocalitaOperativa>(BuildLocalitaOperativeServizioCatalogo(cataloghiServizio.LocalitaOperative));
        ScopiImmersioneCatalogo = new ObservableCollection<ScopoImmersioneItem>(cataloghiServizio.ScopiImmersione);
        UnitaNavaliCatalogo = new ObservableCollection<UnitaNavale>(BuildUnitaNavaliCatalogo(cataloghiServizio.UnitaNavali));
        UnitaNavaliGestioneCatalogo = new ObservableCollection<UnitaNavale>(cataloghiServizio.UnitaNavali);
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
        _servizioLocalitaSelezionata = LocalitaOperativeServizioCatalogo.FirstOrDefault();
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

    private void AggiornaDataOraHeader()
    {
        _headerDateTime = DateTime.Now;
        OnPropertyChanged(nameof(HeaderDateText));
        OnPropertyChanged(nameof(HeaderTimeText));
    }

    public string Titolo => "SMZ La Spezia";

    public string Sottotitolo => "Gestione integrata di personale, servizi, immersioni e scadenze";

    public string WelcomeTitolo => "CENTRO NAUTICO E SMZ";

    public string WelcomeSottotitolo =>
        "Accesso al gestionale operativo per personale, servizi giornalieri, immersioni, scadenze e contabilita.";

    public string WelcomeCredits => "© Nucleo SMZ La Spezia 2026 - Sviluppato da Paolo Vittori e Codex";

    public string WelcomeVersione => $"Versione {GetApplicationVersion()}";

    public string HomeTitolo => "Centro Nautico e Sommozzatori";

    public string HomeSottotitolo =>
        "Una home iniziale piu moderna per accedere ai moduli del nucleo: personale, servizi giornalieri, archivio e contabilita.";

    public string HeaderDateText => _headerDateTime.ToString("d MMMM yyyy", CultureInfo.GetCultureInfo("it-IT"));

    public string HeaderTimeText => _headerDateTime.ToString("HH:mm", CultureInfo.GetCultureInfo("it-IT"));

    public int DashboardDipendentiTotali => PersonaleItems.Count;

    public int DashboardScadenzeAperte => ScadenzeTotali;

    public int DashboardCataloghiServizioTotali =>
        LocalitaOperativeCatalogo.Count
        + ScopiImmersioneCatalogo.Count
        + UnitaNavaliCatalogo.Count(item => item.UnitaNavaleId > 0)
        + TipologieImmersioneOperativeCatalogo.Count;

    public string DashboardScadenzeSintesi =>
        ScadenzeTotali == 0
            ? "Nessuna scadenza aperta nei prossimi 90 giorni."
            : $"{ScadenzeScadute} scadute, {ScadenzeUrgenti} urgenti, {ScadenzeTotali} totali.";

    public string DashboardServizioSintesi =>
        $"{ServizioPresentiTotali} presenti su {ServizioPartecipantiTotali} operatori, {ServizioImmersioniCompilate} immersioni compilate.";

    public string DashboardArchivioSintesi =>
        ArchivioItems.Count == 0
            ? "Backup disponibili e nessuna scheda archiviata da recuperare."
            : $"Backup disponibili e {ArchivioItems.Count} schede archiviate recuperabili.";

    public string DashboardStatoSintesi => ScadenzeScadute switch
    {
        > 0 => "Richiede attenzione: sono presenti visite mediche scadute.",
        _ when ScadenzeUrgenti > 0 => "Monitoraggio attivo: ci sono visite mediche in scadenza nei prossimi 7 giorni.",
        _ => "Situazione regolare: nessuna priorita immediata.",
    };

    public IReadOnlyList<ScadenzaItemViewModel> DashboardTopScadenze =>
        ScadenzeProssime
            .Where(IsVisitaMedicaScadenza)
            .Take(6)
            .ToList();

    public string DashboardTopScadenzeTitolo =>
        ScadenzeProssime.Count == 0
            ? "Nessuna scadenza prioritaria"
            : "Da controllare subito";

    public IReadOnlyList<ScadenzaItemViewModel> DashboardCriticitaItems =>
        ScadenzeProssime
            .Where(item => !IsVisitaMedicaScadenza(item))
            .Take(4)
            .ToList();

    public bool HasDashboardCriticitaOperative => DashboardCriticitaItems.Count > 0;

    public string DashboardCriticitaOperativeEmptyText =>
        "Nessuna criticità operativa non sanitaria rilevata.";

    public int DashboardVisiteScadutePersonale => ScadenzeProssime
        .Where(item => IsVisitaMedicaScadenza(item) && item.IsExpired)
        .Select(item => item.PerId)
        .Distinct()
        .Count();

    public int DashboardVisiteInScadenzaPersonale => ScadenzeProssime
        .Where(item => IsVisitaMedicaScadenza(item) && !item.IsExpired)
        .Select(item => item.PerId)
        .Distinct()
        .Count();

    private static bool IsVisitaMedicaScadenza(ScadenzaItemViewModel item) =>
        string.Equals(item.Origine, "Visita medica", StringComparison.OrdinalIgnoreCase);

    public RelayCommand NavigateSectionCommand => _navigateSectionCommand;

    public RelayCommand EnterAppCommand => _enterAppCommand;

    public RelayCommand ToggleWelcomeAudioCommand => _toggleWelcomeAudioCommand;

    public RelayCommand FlaggaTuttiImmersioneCommand => _flaggaTuttiImmersioneCommand;

    public RelayCommand AddImmersioneCommand => _addImmersioneCommand;

    public RelayCommand RemoveImmersioneCommand => _removeImmersioneCommand;

    public RelayCommand AddLocalitaOperativaCommand => _addLocalitaOperativaCommand;

    public RelayCommand AddUnitaNavaleCommand => _addUnitaNavaleCommand;

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

                if (value == ServicesSectionIndex)
                {
                    IsSchedaServizioVisibile = false;
                }
                else if (value == AccountingSectionIndex)
                {
                    AggiornaAnniContabilitaDisponibili();
                    AggiornaDatiMensili();
                }
                else if (value == ReportsSectionIndex)
                {
                    AggiornaAnniContabilitaDisponibili();
                    AggiornaDatiMensili();
                }
                else if (value == SettingsSectionIndex)
                {
                    InizializzaEditorTariffeContabili();
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

    public ObservableCollection<string> OrariServizioFissiDisponibili { get; }

    public ObservableCollection<CategoriaRegistroItem> CategorieRegistroCatalogo { get; }

    public ObservableCollection<LocalitaOperativa> LocalitaOperativeCatalogo { get; }

    public ObservableCollection<LocalitaOperativa> LocalitaOperativeServizioCatalogo { get; }

    public ObservableCollection<ScopoImmersioneItem> ScopiImmersioneCatalogo { get; }

    public ObservableCollection<UnitaNavale> UnitaNavaliCatalogo { get; }

    public ObservableCollection<UnitaNavale> UnitaNavaliGestioneCatalogo { get; }

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

    public ObservableCollection<ServizioPartecipanteImmersioneUnicoDraftViewModel> ServizioPartecipazioniContabiliUnicheBozza { get; }

    public IReadOnlyList<ServizioPartecipanteImmersioneDraftViewModel> ServizioPartecipazioniContabiliBozza =>
        ServizioImmersioniBozza
            .OrderBy(item => item.NumeroImmersione)
            .SelectMany(item => item.Partecipazioni)
            .ToList();

    public ObservableCollection<ServizioSupportoOccasionaleDraftViewModel> ServizioSupportiOccasionaliBozza { get; }

    public ObservableCollection<ServizioOperatoreSubEsternoDraftViewModel> ServizioOperatoriSubEsterniBozza { get; }

    public ObservableCollection<ServizioGiornalieroSummary> ServiziSalvati { get; }

    public string ServiziSearchText
    {
        get => _serviziSearchText;
        set
        {
            if (SetProperty(ref _serviziSearchText, value))
            {
                CaricaServiziSalvati();
            }
        }
    }

    public string ServiziNumeroSearchText
    {
        get => _serviziNumeroSearchText;
        set
        {
            if (SetProperty(ref _serviziNumeroSearchText, value))
            {
                CaricaServiziSalvati();
            }
        }
    }

    public string ServiziDataSearchText
    {
        get => _serviziDataSearchText;
        set
        {
            if (SetProperty(ref _serviziDataSearchText, value))
            {
                CaricaServiziSalvati();
            }
        }
    }

    public string ServiziDataSearchToolTip =>
        "Cerca per data esatta oppure per mese. Esempi: 15/03/2026 o 03/2026.";

    public ObservableCollection<ContabilitaSmzSummary> ContabilitaSmzItems { get; }

    public ObservableCollection<string> ContabilitaSmzDateDisponibili { get; }

    public ObservableCollection<string> ContabilitaSmzNumeriServizioDisponibili { get; }

    public ObservableCollection<string> ContabilitaSmzApparatiDisponibili { get; }

    public ObservableCollection<ContabilitaSanitarioSummary> ContabilitaSanitariItems { get; }

    public ObservableCollection<ContabilitaSupportoSummary> ContabilitaSupportiItems { get; }

    public ObservableCollection<RegistroImmersioneRiga> RegistroImmersioniItems { get; }

    public ObservableCollection<RegistroImmersioneCategoriaSummary> RegistroImmersioniCategorieItems { get; }

    public ObservableCollection<ReportPersonaleMensileRiga> ReportPersonaleItems { get; }

    public ObservableCollection<RegolaContabileEditorRowViewModel> RegoleContabiliEditorItems { get; }

    public ObservableCollection<ContabilitaMeseItem> ContabilitaMesiDisponibili { get; }

    public ObservableCollection<ContabilitaMeseItem> ReportPersonaleMesiDisponibili { get; }

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
                _printServizioSelezionatoCommand.RaiseCanExecuteChanged();
                _duplicateServizioCommand.RaiseCanExecuteChanged();
                _exportServizioPackageCommand.RaiseCanExecuteChanged();
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

    public ServizioOperatoreSubEsternoDraftViewModel? SelectedOperatoreSubEsterno
    {
        get => _selectedOperatoreSubEsterno;
        set
        {
            if (SetProperty(ref _selectedOperatoreSubEsterno, value))
            {
                _removeOperatoreSubEsternoCommand.RaiseCanExecuteChanged();
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

    public ContabilitaMeseItem? ReportPersonaleMeseSelezionato
    {
        get => _reportPersonaleMeseSelezionato;
        set
        {
            if (SetProperty(ref _reportPersonaleMeseSelezionato, value))
            {
                OnPropertyChanged(nameof(ReportPersonalePeriodoTitolo));

                if (_contabilitaSelezionePronta)
                {
                    CaricaReportPersonaleMensile();
                }
            }
        }
    }

    public string ContabilitaSmzFiltroData
    {
        get => _contabilitaSmzFiltroData;
        set
        {
            if (SetProperty(ref _contabilitaSmzFiltroData, value ?? string.Empty))
            {
                ApplicaFiltriContabilitaSmz();
            }
        }
    }

    public string ContabilitaSmzFiltroNumeroServizio
    {
        get => _contabilitaSmzFiltroNumeroServizio;
        set
        {
            if (SetProperty(ref _contabilitaSmzFiltroNumeroServizio, value ?? string.Empty))
            {
                ApplicaFiltriContabilitaSmz();
            }
        }
    }

    public string ContabilitaSmzFiltroNominativo
    {
        get => _contabilitaSmzFiltroNominativo;
        set
        {
            if (SetProperty(ref _contabilitaSmzFiltroNominativo, value ?? string.Empty))
            {
                ApplicaFiltriContabilitaSmz();
            }
        }
    }

    public string ContabilitaSmzFiltroApparato
    {
        get => _contabilitaSmzFiltroApparato;
        set
        {
            if (SetProperty(ref _contabilitaSmzFiltroApparato, value ?? string.Empty))
            {
                ApplicaFiltriContabilitaSmz();
            }
        }
    }

    public string ReportPersonaleFiltroNominativo
    {
        get => _reportPersonaleFiltroNominativo;
        set
        {
            if (SetProperty(ref _reportPersonaleFiltroNominativo, value ?? string.Empty))
            {
                ApplicaFiltriReportPersonale();
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
                OnPropertyChanged(nameof(ContabilitaAnnoEffettivo));
                OnPropertyChanged(nameof(ContabilitaPeriodoTitolo));
                OnPropertyChanged(nameof(RegistroImmersioniPeriodoTitolo));
                OnPropertyChanged(nameof(ReportPersonalePeriodoTitolo));

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
        set
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (string.Equals(_servizioOrario, normalized, StringComparison.Ordinal))
            {
                return;
            }

            _servizioOrario = normalized;
            ApplicaOrarioServizioSalvato(normalized);
            OnPropertyChanged(nameof(ServizioOrario));
            OnPropertyChanged(nameof(ServizioOrarioRiepilogo));
        }
    }

    public string ServizioOrarioFissoSelezionato
    {
        get => _servizioOrarioFissoSelezionato;
        set
        {
            if (SetProperty(ref _servizioOrarioFissoSelezionato, value))
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    ServizioOrarioDerogaAttiva = false;
                }

                AggiornaValoreOrarioServizio();
            }
        }
    }

    public bool ServizioOrarioDerogaAttiva
    {
        get => _servizioOrarioDerogaAttiva;
        set
        {
            if (SetProperty(ref _servizioOrarioDerogaAttiva, value))
            {
                if (value)
                {
                    _servizioOrarioFissoSelezionato = string.Empty;
                    OnPropertyChanged(nameof(ServizioOrarioFissoSelezionato));
                }

                AggiornaValoreOrarioServizio();
                OnPropertyChanged(nameof(ServizioOrarioRiepilogo));
            }
        }
    }

    public string ServizioOrarioDerogaInizio
    {
        get => _servizioOrarioDerogaInizio;
        set
        {
            if (SetProperty(ref _servizioOrarioDerogaInizio, value))
            {
                AggiornaValoreOrarioServizio();
            }
        }
    }

    public string ServizioOrarioDerogaFine
    {
        get => _servizioOrarioDerogaFine;
        set
        {
            if (SetProperty(ref _servizioOrarioDerogaFine, value))
            {
                AggiornaValoreOrarioServizio();
            }
        }
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

    public PersonaleListItemViewModel? ServizioResponsabileSelezionato
    {
        get => _servizioResponsabileSelezionato;
        set
        {
            if (SetProperty(ref _servizioResponsabileSelezionato, value))
            {
                AggiornaRiepilogoBozzaServizio();
            }
        }
    }

    public bool ServizioFuoriSede
    {
        get => _servizioFuoriSede;
        set
        {
            if (SetProperty(ref _servizioFuoriSede, value))
            {
                if (value && ServizioIndennitaOrdinePubblico)
                {
                    ServizioIndennitaOrdinePubblico = false;
                }

                OnPropertyChanged(nameof(ServizioFuoriSedeDescrizione));
                AggiornaRiepilogoBozzaServizio();
            }
        }
    }

    public bool ServizioIndennitaOrdinePubblico
    {
        get => _servizioIndennitaOrdinePubblico;
        set
        {
            if (SetProperty(ref _servizioIndennitaOrdinePubblico, value))
            {
                if (value && ServizioFuoriSede)
                {
                    ServizioFuoriSede = false;
                }

                OnPropertyChanged(nameof(ServizioOrdinePubblicoDescrizione));
                AggiornaRiepilogoBozzaServizio();
            }
        }
    }

    public bool ServizioStraordinarioAttivo
    {
        get => _servizioStraordinarioAttivo;
        set
        {
            if (SetProperty(ref _servizioStraordinarioAttivo, value))
            {
                OnPropertyChanged(nameof(ServizioStraordinarioOreDisplay));
                AggiornaRiepilogoBozzaServizio();
            }
        }
    }

    public string ServizioStraordinarioInizio
    {
        get => _servizioStraordinarioInizio;
        set
        {
            if (SetProperty(ref _servizioStraordinarioInizio, value))
            {
                OnPropertyChanged(nameof(ServizioStraordinarioOreDisplay));
            }
        }
    }

    public string ServizioStraordinarioFine
    {
        get => _servizioStraordinarioFine;
        set
        {
            if (SetProperty(ref _servizioStraordinarioFine, value))
            {
                OnPropertyChanged(nameof(ServizioStraordinarioOreDisplay));
            }
        }
    }

    public string NuovaLocalitaOperativa
    {
        get => _nuovaLocalitaOperativa;
        set => SetProperty(ref _nuovaLocalitaOperativa, value);
    }

    public string NuovaUnitaNavale
    {
        get => _nuovaUnitaNavale;
        set => SetProperty(ref _nuovaUnitaNavale, value);
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
        _ => "Servizio operativo in sede",
    };

    public string ServizioFuoriSedeDescrizione => ServizioFuoriSede ? "Indennita fuori sede: SI" : "Indennita fuori sede: NO";

    public string ServizioOrdinePubblicoDescrizione => ServizioIndennitaOrdinePubblico ? "Indennita ordine pubblico: SI" : "Indennita ordine pubblico: NO";

    public string ServizioOrarioRiepilogo =>
        string.IsNullOrWhiteSpace(ServizioOrario)
            ? "Orario servizio non impostato"
            : $"Orario servizio: {ServizioOrario}";

    public string ServizioStraordinarioOreDisplay
    {
        get
        {
            if (!ServizioStraordinarioAttivo)
            {
                return "Lavoro straordinario non registrato";
            }

            var durata = CalcolaDurataOre(ServizioStraordinarioInizio, ServizioStraordinarioFine);
            return durata is null
                ? "Inserisci inizio e fine straordinario"
                : $"Totale straordinario: {durata.Value:0.##} ore";
        }
    }

    private static string NormalizeTipoServizio(string? tipoServizio, bool fuoriSede)
    {
        if (string.Equals(tipoServizio, "FuoriSede", StringComparison.Ordinal))
        {
            return "FuoriSede";
        }

        if (string.Equals(tipoServizio, "InSede", StringComparison.Ordinal))
        {
            return "InSede";
        }

        return fuoriSede ? "FuoriSede" : "InSede";
    }

    private static IEnumerable<UnitaNavale> BuildUnitaNavaliCatalogo(IEnumerable<UnitaNavale> source)
    {
        yield return UnitaNavaleVuota;

        foreach (var item in source.Where(item => item.Attiva))
        {
            yield return item;
        }
    }

    private static IEnumerable<LocalitaOperativa> BuildLocalitaOperativeServizioCatalogo(IEnumerable<LocalitaOperativa> source) =>
        source.Where(item => item.Attiva);

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
        ContaPartecipantiInterniBozza() + ContaOperatoriSubEsterniBozza() + ContaSupportiOccasionaliBozza();

    public int ServizioPresentiTotali =>
        ContaPresentiInterniBozza() + ContaOperatoriSubEsterniBozza() + ContaSupportiOccasionaliPresentiBozza();

    public int ServizioImmersioniCompilate => ServizioImmersioniBozza.Count(item =>
        item.DirettoreImmersione is not null
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
            ? !HasFiltroServiziAttivo
                ? "Nessun servizio ancora registrato."
                : "Nessun servizio trovato con la ricerca impostata."
            : !HasFiltroServiziAttivo
                ? $"{ServiziSalvati.Count} servizi recenti visualizzati."
                : $"{ServiziSalvati.Count} servizi trovati per la ricerca impostata.";

    private bool HasFiltroServiziAttivo =>
        !string.IsNullOrWhiteSpace(ServiziSearchText)
        || !string.IsNullOrWhiteSpace(ServiziNumeroSearchText)
        || !string.IsNullOrWhiteSpace(ServiziDataSearchText);

    public string ContabilitaPeriodoTitolo =>
        ContabilitaMeseSelezionato is null
            ? $"Contabilita giornate di impiego {ContabilitaAnnoEffettivo}"
            : $"{ContabilitaMeseSelezionato.Descrizione} {ContabilitaAnnoEffettivo}";

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
        _contabilitaSmzSource.Count == 0
            ? "Nessuna riga contabile SMZ disponibile nel periodo selezionato."
            : HasFiltriContabilitaSmzAttivi
            ? $"{ContabilitaSmzItems.Count} righe visualizzate su {_contabilitaSmzSource.Count} nel periodo selezionato."
            : $"{ContabilitaSmzItems.Count} righe contabili disponibili nel periodo selezionato.";

    public bool HasFiltriContabilitaSmzAttivi =>
        !string.IsNullOrWhiteSpace(ContabilitaSmzFiltroData)
        || !string.IsNullOrWhiteSpace(ContabilitaSmzFiltroNumeroServizio)
        || !string.IsNullOrWhiteSpace(ContabilitaSmzFiltroNominativo)
        || !string.IsNullOrWhiteSpace(ContabilitaSmzFiltroApparato);

    public string TariffeContabiliStato =>
        RegoleContabiliEditorItems.Count == 0
            ? "Nessuna regola tariffaria disponibile."
            : $"{RegoleContabiliEditorItems.Count} righe tariffarie modificabili dal database.";

    public int ContabilitaAnnoEffettivo => ContabilitaAnnoSelezionato > 0 ? ContabilitaAnnoSelezionato : DateTime.Today.Year;

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
            ? "Nessuna Assistenza SMZ presente nel periodo selezionato."
            : $"{ContabilitaSupportiItems.Count} nominativi di Assistenza SMZ con {ContabilitaSupportoTotaleGiornate} giornate utili.";

    public string RegistroImmersioniPeriodoTitolo =>
        ContabilitaMeseSelezionato is null
            ? $"Registro immersioni {ContabilitaAnnoEffettivo}"
            : $"Registro immersioni {ContabilitaMeseSelezionato.Descrizione} {ContabilitaAnnoEffettivo}";

    public string ReportPersonalePeriodoTitolo =>
        ReportPersonaleMeseSelezionato is null || ReportPersonaleMeseSelezionato.NumeroMese == 0
            ? $"Report personale {ContabilitaAnnoEffettivo}"
            : $"Report personale {ReportPersonaleMeseSelezionato.Descrizione} {ContabilitaAnnoEffettivo}";

    public string ReportPersonaleStato =>
        _reportPersonaleSource.Count == 0
            ? "Nessun servizio o immersione registrato per il personale nel periodo selezionato."
            : HasFiltriReportPersonaleAttivi
                ? $"{ReportPersonaleItems.Count} righe visualizzate su {_reportPersonaleSource.Count} nel periodo selezionato."
                : $"{ReportPersonaleItems.Count} righe personale disponibili nel periodo selezionato.";

    public int ReportPersonaleTotaleRighe => ReportPersonaleItems.Count;

    public int ReportPersonaleTotalePersone => ReportPersonaleItems
        .Select(item => item.PerId?.ToString() ?? item.Nominativo)
        .Where(item => !string.IsNullOrWhiteSpace(item))
        .Distinct(StringComparer.CurrentCultureIgnoreCase)
        .Count();

    public int ReportPersonaleTotaleImmersioni => ReportPersonaleItems
        .Count(item => string.Equals(item.TipoRiga, "Immersione", StringComparison.OrdinalIgnoreCase));

    public decimal ReportPersonaleTotaleOre => ReportPersonaleItems.Sum(item => item.OreImmersione);

    public string ReportPersonaleTotaleOreDisplay => ReportPersonaleTotaleOre.ToString("0.##", CultureInfo.CurrentCulture);

    public bool HasFiltriReportPersonaleAttivi => !string.IsNullOrWhiteSpace(ReportPersonaleFiltroNominativo);

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

    public string BackupLocaleSintesi => FormatBackupFooterInfo(
        _backupService.GetLatestLocalBackup(),
        "Backup locale: non creato",
        "Backup locale");

    public string BackupEsternoSintesi => FormatBackupFooterInfo(
        _backupService.GetLatestExternalBackup(_backupSettings.ExternalBackupDirectory),
        string.IsNullOrWhiteSpace(_backupSettings.ExternalBackupDirectory)
            ? "Backup esterno: non configurato"
            : "Backup esterno: non creato",
        "Backup esterno");

    public string BackupCartellaEsterna =>
        string.IsNullOrWhiteSpace(_backupSettings.ExternalBackupDirectory)
            ? "Non configurata"
            : _backupSettings.ExternalBackupDirectory;

    public string BackupDescrizione =>
        "Il backup locale protegge dalle modifiche accidentali. Configura una cartella esterna su USB, disco esterno o cartella sincronizzata: dopo ogni salvataggio importante il programma crea automaticamente anche un backup esterno, utile per guasto o cambio PC.";

    public bool IsExistingServizio => _servizioGiornalieroId > 0;

    public ObservableCollection<string> QualificheDisponibili { get; } =
        new(
        [
            "Vice Questore Aggiunto",
            "Commissario Capo",
            "Commissario",
            "Vice Commissario",
            "Sostituto Commissario C. Tecnico",
            "Sostituto Commissario Tecnico",
            "Sostituto Commissario C.",
            "Sostituto Commissario",
            "Ispettore Superiore Tecnico",
            "Ispettore Capo Tecnico",
            "Ispettore Tecnico",
            "Vice Ispettore Tecnico",
            "Ispettore Superiore",
            "Ispettore Capo",
            "Ispettore",
            "Vice Ispettore",
            "Sovrintendente Capo C.",
            "Sovrintendente Capo",
            "Sovrintendente",
            "Vice Sovrintendente",
            "Assistente Capo C.",
            "Assistente Capo",
            "Assistente",
            "Agente Scelto",
            "Agente",
            "Medico Capo",
            "Medico Principale",
            "Medico",
        ]);

    public ObservableCollection<string> ProfiliPersonaleDisponibili { get; } =
        new(ProfiliPersonaleCatalogo.Tutti);

    public ObservableCollection<string> RuoliSanitariDisponibili { get; } =
        new(["Infermiere", "Medico"]);

    public ObservableCollection<string> StatiServizioPersonaleDisponibili { get; } =
        new(StatoServizioPersonaleCatalogo.Tutti);

    public RelayCommand SearchCommand { get; }

    public RelayCommand OpenScadenzaCommand { get; }

    public RelayCommand ClearFiltersCommand { get; }

    public RelayCommand NewServizioCommand => _newServizioCommand;

    public RelayCommand SaveServizioCommand => _saveServizioCommand;

    public RelayCommand PrintServizioCommand => _printServizioCommand;

    public RelayCommand PrintServizioSelezionatoCommand => _printServizioSelezionatoCommand;

    public RelayCommand OpenServizioCommand => _openServizioCommand;

    public RelayCommand OpenServizioFromListCommand => _openServizioFromListCommand;

    public RelayCommand CloseSchedaServizioCommand => _closeSchedaServizioCommand;

    public RelayCommand DuplicateServizioCommand => _duplicateServizioCommand;

    public RelayCommand ClearServiziSearchCommand => _clearServiziSearchCommand;

    public RelayCommand ExportServizioPackageCommand => _exportServizioPackageCommand;

    public RelayCommand ImportServizioPackageCommand => _importServizioPackageCommand;

    public RelayCommand DeleteServizioCommand => _deleteServizioCommand;

    public RelayCommand AddSupportoOccasionaleCommand => _addSupportoOccasionaleCommand;

    public RelayCommand RemoveSupportoOccasionaleCommand => _removeSupportoOccasionaleCommand;

    public RelayCommand AddOperatoreSubEsternoCommand => _addOperatoreSubEsternoCommand;

    public RelayCommand RemoveOperatoreSubEsternoCommand => _removeOperatoreSubEsternoCommand;

    public RelayCommand NewCommand { get; }

    public RelayCommand SaveCommand { get; }

    public RelayCommand DeleteCommand => _deleteCommand;

    public RelayCommand DeleteDefinitivoCommand => _deleteDefinitivoCommand;

    public RelayCommand OpenSelectedPersonaleCommand => _openSelectedPersonaleCommand;

    public RelayCommand CloseSchedaPersonaleCommand => _closeSchedaPersonaleCommand;

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

    public RelayCommand ReloadReportPersonaleCommand => _reloadReportPersonaleCommand;

    public RelayCommand PrintRegistroImmersioniMensileCommand => _printRegistroImmersioniMensileCommand;

    public RelayCommand PrintRegistroImmersioniMensileCompattoCommand => _printRegistroImmersioniMensileCompattoCommand;

    public RelayCommand PrintContabilitaMensileCommand => _printContabilitaMensileCommand;

    public RelayCommand SaveElaborazioneMensileCommand => _saveElaborazioneMensileCommand;

    public RelayCommand ExportContabilitaCsvCommand => _exportContabilitaCsvCommand;

    public RelayCommand ExportContabilitaExcelCommand => _exportContabilitaExcelCommand;

    public RelayCommand ClearContabilitaSmzFiltersCommand => _clearContabilitaSmzFiltersCommand;

    public RelayCommand ClearReportPersonaleFiltersCommand => _clearReportPersonaleFiltersCommand;

    public RelayCommand SaveLocalitaOperativeCommand => _saveLocalitaOperativeCommand;

    public RelayCommand SaveUnitaNavaliCommand => _saveUnitaNavaliCommand;

    public RelayCommand SaveTariffeContabiliCommand => _saveTariffeContabiliCommand;

    public IReadOnlyList<string> GetAreeConModificheNonSalvate()
    {
        var aree = new List<string>();

        if (HasModifichePersonaleNonSalvate())
        {
            aree.Add("scheda personale");
        }

        if (HasModificheServizioNonSalvate())
        {
            aree.Add("servizio giornaliero");
        }

        if (HasModificheTariffeNonSalvate())
        {
            aree.Add("tariffe contabili");
        }

        return aree;
    }

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

    public bool IsSchedaPersonaleVisibile
    {
        get => _isSchedaPersonaleVisibile;
        set
        {
            if (SetProperty(ref _isSchedaPersonaleVisibile, value))
            {
                OnPropertyChanged(nameof(IsElencoPersonaleVisibile));
            }
        }
    }

    public bool IsElencoPersonaleVisibile => !IsSchedaPersonaleVisibile;

    public bool IsSchedaServizioVisibile
    {
        get => _isSchedaServizioVisibile;
        set
        {
            if (SetProperty(ref _isSchedaServizioVisibile, value))
            {
                OnPropertyChanged(nameof(IsElencoServiziVisibile));
            }
        }
    }

    public bool IsServizioApertoDaReport
    {
        get => _isServizioApertoDaReport;
        set => SetProperty(ref _isServizioApertoDaReport, value);
    }

    public bool IsElencoServiziVisibile => !IsSchedaServizioVisibile;

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
                CaricaElenco();
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
                _deleteDefinitivoCommand.RaiseCanExecuteChanged();
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

    public string DataDecorrenzaQualifica
    {
        get => _dataDecorrenzaQualifica;
        set => SetProperty(ref _dataDecorrenzaQualifica, value);
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

    public string StatoServizioPersonale
    {
        get => _statoServizioPersonale;
        set
        {
            var valoreNormalizzato = StatoServizioPersonaleCatalogo.Normalizza(value);
            if (SetProperty(ref _statoServizioPersonale, valoreNormalizzato))
            {
                if (IsPersonaleInServizio)
                {
                    DataFineServizio = string.Empty;
                }

                OnPropertyChanged(nameof(IsPersonaleInServizio));
                OnPropertyChanged(nameof(StatoServizioSchedaSintesi));
                OnPropertyChanged(nameof(DataFineServizioLabel));
            }
        }
    }

    public string DataFineServizio
    {
        get => _dataFineServizio;
        set
        {
            if (SetProperty(ref _dataFineServizio, value))
            {
                OnPropertyChanged(nameof(StatoServizioSchedaSintesi));
            }
        }
    }

    public bool IsPersonaleInServizio =>
        string.Equals(StatoServizioPersonale, StatoServizioPersonaleCatalogo.Attivo, StringComparison.OrdinalIgnoreCase);

    public string DataFineServizioLabel =>
        IsPersonaleInServizio ? "Fine servizio" : $"Data {StatoServizioPersonale.ToLowerInvariant()}";

    public string StatoServizioSchedaSintesi =>
        IsPersonaleInServizio
            ? "In servizio"
            : string.IsNullOrWhiteSpace(DataFineServizio)
                ? StatoServizioPersonale
                : $"{StatoServizioPersonale} dal {DataFineServizio}";

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
}
