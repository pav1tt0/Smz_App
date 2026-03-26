using SMZ.Conta.App.Infrastructure;

namespace SMZ.Conta.App.ViewModels;

public sealed class ServizioSupportoOccasionaleDraftViewModel : ObservableObject
{
    private string _nominativo = string.Empty;
    private string _qualifica = string.Empty;
    private string _ruolo = string.Empty;
    private bool _presente = true;
    private string _contatti = string.Empty;
    private string _note = string.Empty;

    public string Nominativo
    {
        get => _nominativo;
        set => SetProperty(ref _nominativo, value);
    }

    public string Qualifica
    {
        get => _qualifica;
        set => SetProperty(ref _qualifica, value);
    }

    public string Ruolo
    {
        get => _ruolo;
        set => SetProperty(ref _ruolo, value);
    }

    public bool Presente
    {
        get => _presente;
        set => SetProperty(ref _presente, value);
    }

    public string Contatti
    {
        get => _contatti;
        set => SetProperty(ref _contatti, value);
    }

    public string Note
    {
        get => _note;
        set => SetProperty(ref _note, value);
    }
}
