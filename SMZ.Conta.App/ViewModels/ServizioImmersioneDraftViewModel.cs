using SMZ.Conta.App.Infrastructure;

namespace SMZ.Conta.App.ViewModels;

public sealed class ServizioImmersioneDraftViewModel : ObservableObject
{
    private int _numeroImmersione;
    private string _orarioInizio = string.Empty;
    private string _orarioFine = string.Empty;
    private PersonaleListItemViewModel? _direttoreImmersione;
    private PersonaleListItemViewModel? _operatoreSoccorso;
    private PersonaleListItemViewModel? _assistenteBlsd;
    private PersonaleListItemViewModel? _assistenteSanitario;
    private string _note = string.Empty;

    public int NumeroImmersione
    {
        get => _numeroImmersione;
        set => SetProperty(ref _numeroImmersione, value);
    }

    public string OrarioInizio
    {
        get => _orarioInizio;
        set => SetProperty(ref _orarioInizio, value);
    }

    public string OrarioFine
    {
        get => _orarioFine;
        set => SetProperty(ref _orarioFine, value);
    }

    public PersonaleListItemViewModel? DirettoreImmersione
    {
        get => _direttoreImmersione;
        set => SetProperty(ref _direttoreImmersione, value);
    }

    public PersonaleListItemViewModel? OperatoreSoccorso
    {
        get => _operatoreSoccorso;
        set => SetProperty(ref _operatoreSoccorso, value);
    }

    public PersonaleListItemViewModel? AssistenteBlsd
    {
        get => _assistenteBlsd;
        set => SetProperty(ref _assistenteBlsd, value);
    }

    public PersonaleListItemViewModel? AssistenteSanitario
    {
        get => _assistenteSanitario;
        set => SetProperty(ref _assistenteSanitario, value);
    }

    public string Note
    {
        get => _note;
        set => SetProperty(ref _note, value);
    }
}
