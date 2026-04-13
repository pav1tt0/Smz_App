using SMZ.Conta.App.Infrastructure;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.ViewModels;

public sealed class PersonaleListItemViewModel : ObservableObject
{
    private int _perId;
    private string _cognome = string.Empty;
    private string _nome = string.Empty;
    private string _qualifica = string.Empty;
    private string _profiloPersonale = ProfiliPersonaleCatalogo.OperatoreSubacqueo;
    private string _ruoloSanitario = string.Empty;
    private string _codiceFiscale = string.Empty;
    private string _contatti = string.Empty;
    private string _statoServizio = StatoServizioPersonaleCatalogo.Attivo;
    private string _dataFineServizio = string.Empty;

    public int PerId
    {
        get => _perId;
        set => SetProperty(ref _perId, value);
    }

    public string Cognome
    {
        get => _cognome;
        set
        {
            if (SetProperty(ref _cognome, value))
            {
                OnPropertyChanged(nameof(Nominativo));
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
                OnPropertyChanged(nameof(Nominativo));
                OnPropertyChanged(nameof(NominativoConQualifica));
            }
        }
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

    public string ProfiloPersonale
    {
        get => _profiloPersonale;
        set
        {
            if (SetProperty(ref _profiloPersonale, value))
            {
                OnPropertyChanged(nameof(NominativoConQualifica));
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
                OnPropertyChanged(nameof(NominativoConQualifica));
            }
        }
    }

    public string CodiceFiscale
    {
        get => _codiceFiscale;
        set => SetProperty(ref _codiceFiscale, value);
    }

    public string Contatti
    {
        get => _contatti;
        set => SetProperty(ref _contatti, value);
    }

    public string StatoServizio
    {
        get => _statoServizio;
        set
        {
            if (SetProperty(ref _statoServizio, StatoServizioPersonaleCatalogo.Normalizza(value)))
            {
                OnPropertyChanged(nameof(StatoServizioSintesi));
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
                OnPropertyChanged(nameof(StatoServizioSintesi));
            }
        }
    }

    public bool IsProfiloSanitario => ProfiliPersonaleCatalogo.IsSanitario(ProfiloPersonale);

    public string Nominativo => $"{Cognome} {Nome}".Trim();

    public string StatoServizioSintesi =>
        string.Equals(StatoServizio, StatoServizioPersonaleCatalogo.Attivo, StringComparison.OrdinalIgnoreCase)
            ? "In servizio"
            : string.IsNullOrWhiteSpace(DataFineServizio)
                ? StatoServizio
                : $"{StatoServizio} dal {DataFineServizio}";

    public string NominativoConQualifica
    {
        get
        {
            var qualifica = QualificaFormatter.AbbreviaPerVisualizzazione(Qualifica);
            var prefisso = string.IsNullOrWhiteSpace(qualifica) ? Nominativo : $"{qualifica} - {Nominativo}";
            return ProfiliPersonaleCatalogo.IsSanitario(ProfiloPersonale) && !string.IsNullOrWhiteSpace(RuoloSanitario)
                ? $"{prefisso} (Sanitario - {RuoloSanitario})"
                : prefisso;
        }
    }

    public static PersonaleListItemViewModel FromModel(Personale personale)
    {
        return new PersonaleListItemViewModel
        {
            PerId = personale.PerId,
            Cognome = personale.Cognome,
            Nome = personale.Nome,
            Qualifica = personale.Qualifica,
            ProfiloPersonale = personale.ProfiloPersonale,
            RuoloSanitario = personale.RuoloSanitario,
            CodiceFiscale = personale.CodiceFiscale,
            Contatti = personale.ContattiSintesi,
            StatoServizio = personale.StatoServizio,
            DataFineServizio = personale.DataFineServizio?.ToString("dd/MM/yyyy") ?? string.Empty,
        };
    }
}
