using SMZ.Conta.App.Infrastructure;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.ViewModels;

public sealed class ServizioPartecipanteImmersioneDraftViewModel : ObservableObject
{
    private int _perId;
    private string _qualifica = string.Empty;
    private string _nominativo = string.Empty;
    private bool _inImmersione;
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

    public bool InImmersione
    {
        get => _inImmersione;
        set => SetProperty(ref _inImmersione, value);
    }

    public TipologiaImmersioneOperativa? TipologiaImmersioneOperativa
    {
        get => _tipologiaImmersioneOperativa;
        set => SetProperty(ref _tipologiaImmersioneOperativa, value);
    }

    public string ProfonditaMetri
    {
        get => _profonditaMetri;
        set => SetProperty(ref _profonditaMetri, value);
    }

    public FasciaProfondita? FasciaProfondita
    {
        get => _fasciaProfondita;
        set => SetProperty(ref _fasciaProfondita, value);
    }

    public string OreImmersione
    {
        get => _oreImmersione;
        set => SetProperty(ref _oreImmersione, value);
    }

    public CategoriaContabileOre? CategoriaContabileOre
    {
        get => _categoriaContabileOre;
        set => SetProperty(ref _categoriaContabileOre, value);
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
        set => SetProperty(ref _note, value);
    }

    public string NominativoConQualifica
    {
        get
        {
            var qualifica = QualificaFormatter.AbbreviaPerVisualizzazione(Qualifica);
            return string.IsNullOrWhiteSpace(qualifica) ? Nominativo : $"{qualifica} - {Nominativo}";
        }
    }

    public string TariffaPropostaDisplay => TariffaProposta?.ToString("0.##") ?? string.Empty;

    public string ImportoStimatoDisplay => ImportoStimato?.ToString("0.##") ?? string.Empty;
}
