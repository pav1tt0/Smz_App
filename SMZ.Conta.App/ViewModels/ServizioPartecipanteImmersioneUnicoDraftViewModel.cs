using System.Collections.ObjectModel;
using SMZ.Conta.App.Infrastructure;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.ViewModels;

public sealed class ServizioPartecipanteImmersioneUnicoDraftViewModel : ObservableObject
{
    private int _perId;
    private string _qualifica = string.Empty;
    private DateOnly? _dataDecorrenzaQualifica;
    private string _nominativo = string.Empty;
    private bool _isEsterno;
    private string _reparto = string.Empty;
    private string _ruoliImmersione = string.Empty;
    private string _numeriImmersioneRuolo = string.Empty;
    private bool _isSoloAssistenza;
    private int? _numeroImmersione;

    public ObservableCollection<int?> ImmersioniDisponibili { get; } = [];

    public int? NumeroImmersione
    {
        get => _numeroImmersione;
        set => SetProperty(ref _numeroImmersione, value);
    }
    private TipologiaImmersioneOperativa? _tipologiaImmersioneOperativa;
    private string _profonditaMetri = string.Empty;
    private FasciaProfondita? _fasciaProfondita;
    private string _oreImmersione = string.Empty;
    private CategoriaContabileOre? _categoriaContabileOre;
    private decimal? _tariffaProposta;
    private decimal? _importoStimato;
    private string _note = string.Empty;

    public int PerId
    {
        get => _perId;
        set => SetProperty(ref _perId, value);
    }

    public string Qualifica
    {
        get => _qualifica;
        set
        {
            if (SetProperty(ref _qualifica, value))
            {
                OnPropertyChanged(nameof(NominativoConQualifica));
            }
        }
    }

    public DateOnly? DataDecorrenzaQualifica
    {
        get => _dataDecorrenzaQualifica;
        set => SetProperty(ref _dataDecorrenzaQualifica, value);
    }

    public string Nominativo
    {
        get => _nominativo;
        set
        {
            if (SetProperty(ref _nominativo, value))
            {
                OnPropertyChanged(nameof(NominativoConQualifica));
            }
        }
    }

    public bool IsEsterno
    {
        get => _isEsterno;
        set => SetProperty(ref _isEsterno, value);
    }

    public string Reparto
    {
        get => _reparto;
        set
        {
            if (SetProperty(ref _reparto, value))
            {
                OnPropertyChanged(nameof(NominativoConQualifica));
            }
        }
    }

    public string RuoliImmersione
    {
        get => _ruoliImmersione;
        set
        {
            if (SetProperty(ref _ruoliImmersione, value))
            {
                OnPropertyChanged(nameof(HasRuoliImmersione));
            }
        }
    }

    public string NumeriImmersioneRuolo
    {
        get => _numeriImmersioneRuolo;
        set
        {
            if (SetProperty(ref _numeriImmersioneRuolo, value))
            {
                OnPropertyChanged(nameof(HasIncarichiImmersione));
                AggiornaSceltaImmersione();
            }
        }
    }

    public bool IsSoloAssistenza
    {
        get => _isSoloAssistenza;
        set
        {
            if (SetProperty(ref _isSoloAssistenza, value)) AggiornaSceltaImmersione();
        }
    }

    public bool HasIncarichiImmersione => !string.IsNullOrWhiteSpace(NumeriImmersioneRuolo);

    public bool MostraSceltaImmersione => !IsSoloAssistenza && (!HasIncarichiImmersione || HasDettaglioContabile);

    public bool MostraRuoloOperatore => HasIncarichiImmersione && MostraSceltaImmersione;

    private bool HasDettaglioContabile => TipologiaImmersioneOperativa is not null
        || !string.IsNullOrWhiteSpace(ProfonditaMetri) || FasciaProfondita is not null
        || !string.IsNullOrWhiteSpace(OreImmersione) || CategoriaContabileOre is not null
        || !string.IsNullOrWhiteSpace(Note);

    private void AggiornaSceltaImmersione()
    {
        OnPropertyChanged(nameof(MostraSceltaImmersione));
        OnPropertyChanged(nameof(MostraRuoloOperatore));
    }

    public TipologiaImmersioneOperativa? TipologiaImmersioneOperativa
    {
        get => _tipologiaImmersioneOperativa;
        set { if (SetProperty(ref _tipologiaImmersioneOperativa, value)) AggiornaSceltaImmersione(); }
    }

    public string ProfonditaMetri
    {
        get => _profonditaMetri;
        set { if (SetProperty(ref _profonditaMetri, value)) AggiornaSceltaImmersione(); }
    }

    public FasciaProfondita? FasciaProfondita
    {
        get => _fasciaProfondita;
        set { if (SetProperty(ref _fasciaProfondita, value)) AggiornaSceltaImmersione(); }
    }

    public string OreImmersione
    {
        get => _oreImmersione;
        set { if (SetProperty(ref _oreImmersione, value)) AggiornaSceltaImmersione(); }
    }

    public CategoriaContabileOre? CategoriaContabileOre
    {
        get => _categoriaContabileOre;
        set { if (SetProperty(ref _categoriaContabileOre, value)) AggiornaSceltaImmersione(); }
    }

    public decimal? TariffaProposta
    {
        get => _tariffaProposta;
        set
        {
            if (SetProperty(ref _tariffaProposta, value))
            {
                OnPropertyChanged(nameof(TariffaPropostaDisplay));
            }
        }
    }

    public decimal? ImportoStimato
    {
        get => _importoStimato;
        set
        {
            if (SetProperty(ref _importoStimato, value))
            {
                OnPropertyChanged(nameof(ImportoStimatoDisplay));
            }
        }
    }

    public string Note
    {
        get => _note;
        set { if (SetProperty(ref _note, value)) AggiornaSceltaImmersione(); }
    }

    public string NominativoConQualifica
    {
        get
        {
            var qualifica = QualificaFormatter.AbbreviaPerVisualizzazione(Qualifica);
            var nominativo = IsEsterno && !string.IsNullOrWhiteSpace(Reparto)
                ? $"{Nominativo} ({Reparto})"
                : Nominativo;
            return string.IsNullOrWhiteSpace(qualifica) ? nominativo : $"{qualifica} - {nominativo}";
        }
    }

    public bool HasRuoliImmersione => !string.IsNullOrWhiteSpace(RuoliImmersione);

    public string TariffaPropostaDisplay => TariffaProposta?.ToString("0.##") ?? string.Empty;

    public string ImportoStimatoDisplay => ImportoStimato?.ToString("0.##") ?? string.Empty;
}
