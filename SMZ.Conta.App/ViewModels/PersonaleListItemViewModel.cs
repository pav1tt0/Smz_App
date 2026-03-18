using SMZ.Conta.App.Infrastructure;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.ViewModels;

public sealed class PersonaleListItemViewModel : ObservableObject
{
    private int _perId;
    private string _cognome = string.Empty;
    private string _nome = string.Empty;
    private string _codiceFiscale = string.Empty;
    private string _telefono = string.Empty;
    private string _mail = string.Empty;

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
            }
        }
    }

    public string CodiceFiscale
    {
        get => _codiceFiscale;
        set => SetProperty(ref _codiceFiscale, value);
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

    public string Nominativo => $"{Cognome} {Nome}".Trim();

    public static PersonaleListItemViewModel FromModel(Personale personale)
    {
        return new PersonaleListItemViewModel
        {
            PerId = personale.PerId,
            Cognome = personale.Cognome,
            Nome = personale.Nome,
            CodiceFiscale = personale.CodiceFiscale,
            Telefono = personale.Telefono,
            Mail = personale.Mail,
        };
    }
}
