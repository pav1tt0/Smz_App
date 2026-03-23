using SMZ.Conta.App.Infrastructure;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.ViewModels;

public sealed class PersonaleArchivioListItemViewModel : ObservableObject
{
    private long _personaleArchivioId;
    private int _perIdOriginale;
    private string _cognome = string.Empty;
    private string _nome = string.Empty;
    private string _codiceFiscale = string.Empty;
    private DateTime _dataArchiviazione;

    public long PersonaleArchivioId
    {
        get => _personaleArchivioId;
        set => SetProperty(ref _personaleArchivioId, value);
    }

    public int PerIdOriginale
    {
        get => _perIdOriginale;
        set
        {
            if (SetProperty(ref _perIdOriginale, value))
            {
                OnPropertyChanged(nameof(PerIdOriginaleLabel));
            }
        }
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
            }
        }
    }

    public string CodiceFiscale
    {
        get => _codiceFiscale;
        set => SetProperty(ref _codiceFiscale, value);
    }

    public DateTime DataArchiviazione
    {
        get => _dataArchiviazione;
        set
        {
            if (SetProperty(ref _dataArchiviazione, value))
            {
                OnPropertyChanged(nameof(DataArchiviazioneLabel));
            }
        }
    }

    public string Nominativo => $"{Cognome} {Nome}".Trim();

    public string PerIdOriginaleLabel => $"PerID originario {PerIdOriginale}";

    public string DataArchiviazioneLabel => $"Archiviata il {DataArchiviazione:dd/MM/yyyy HH:mm}";

    public static PersonaleArchivioListItemViewModel FromModel(PersonaleArchivioSummary model)
    {
        return new PersonaleArchivioListItemViewModel
        {
            PersonaleArchivioId = model.PersonaleArchivioId,
            PerIdOriginale = model.PerIdOriginale,
            Cognome = model.Cognome,
            Nome = model.Nome,
            CodiceFiscale = model.CodiceFiscale,
            DataArchiviazione = model.DataArchiviazione,
        };
    }
}
