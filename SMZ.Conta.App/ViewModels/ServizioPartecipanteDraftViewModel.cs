using SMZ.Conta.App.Infrastructure;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.ViewModels;

public sealed class ServizioPartecipanteDraftViewModel : ObservableObject
{
    private int _perId;
    private string _qualifica = string.Empty;
    private string _nominativo = string.Empty;
    private string _contatti = string.Empty;
    private int? _defaultGruppoOperativoId;
    private int? _defaultRuoloOperativoId;
    private GruppoOperativo? _gruppoOperativo;
    private bool _presente = true;
    private RuoloOperativo? _ruoloOperativo;
    private string _note = string.Empty;

    public int PerId
    {
        get => _perId;
        set => SetProperty(ref _perId, value);
    }

    public string Qualifica
    {
        get => _qualifica;
        set => SetProperty(ref _qualifica, value);
    }

    public string Nominativo
    {
        get => _nominativo;
        set => SetProperty(ref _nominativo, value);
    }

    public string Contatti
    {
        get => _contatti;
        set => SetProperty(ref _contatti, value);
    }

    public int? DefaultGruppoOperativoId
    {
        get => _defaultGruppoOperativoId;
        set => SetProperty(ref _defaultGruppoOperativoId, value);
    }

    public int? DefaultRuoloOperativoId
    {
        get => _defaultRuoloOperativoId;
        set => SetProperty(ref _defaultRuoloOperativoId, value);
    }

    public GruppoOperativo? GruppoOperativo
    {
        get => _gruppoOperativo;
        set => SetProperty(ref _gruppoOperativo, value);
    }

    public bool Presente
    {
        get => _presente;
        set => SetProperty(ref _presente, value);
    }

    public RuoloOperativo? RuoloOperativo
    {
        get => _ruoloOperativo;
        set => SetProperty(ref _ruoloOperativo, value);
    }

    public string Note
    {
        get => _note;
        set => SetProperty(ref _note, value);
    }
}
