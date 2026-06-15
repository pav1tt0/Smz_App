using SMZ.Conta.App.Infrastructure;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.ViewModels;

public sealed class ServizioOperatoreSubEsternoDraftViewModel : ObservableObject
{
    private string _perId = string.Empty;
    private string _qualifica = string.Empty;
    private string _nominativo = string.Empty;
    private string _reparto = string.Empty;
    private GruppoOperativo? _gruppoOperativo;
    private string _note = string.Empty;

    public string PerId
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
                OnPropertyChanged(nameof(QualificaDisplay));
            }
        }
    }

    public string Nominativo
    {
        get => _nominativo;
        set => SetProperty(ref _nominativo, value);
    }

    public string Reparto
    {
        get => _reparto;
        set => SetProperty(ref _reparto, value);
    }

    public GruppoOperativo? GruppoOperativo
    {
        get => _gruppoOperativo;
        set => SetProperty(ref _gruppoOperativo, value);
    }

    public string Note
    {
        get => _note;
        set => SetProperty(ref _note, value);
    }

    public string QualificaDisplay => Qualifica.ToUpperInvariant();
}
