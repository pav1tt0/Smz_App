using SMZ.Conta.App.Infrastructure;

namespace SMZ.Conta.App.ViewModels;

public sealed class RegolaContabileEditorRowViewModel : ObservableObject
{
    private string _tipologiaDescrizione = string.Empty;
    private string _fasciaDescrizione = string.Empty;
    private string _categoriaDescrizione = string.Empty;
    private string _tariffa = string.Empty;
    private bool _attiva = true;

    public int RegolaContabileImmersioneId { get; set; }

    public string TipologiaDescrizione
    {
        get => _tipologiaDescrizione;
        set => SetProperty(ref _tipologiaDescrizione, value);
    }

    public string FasciaDescrizione
    {
        get => _fasciaDescrizione;
        set => SetProperty(ref _fasciaDescrizione, value);
    }

    public string CategoriaDescrizione
    {
        get => _categoriaDescrizione;
        set => SetProperty(ref _categoriaDescrizione, value);
    }

    public string Tariffa
    {
        get => _tariffa;
        set => SetProperty(ref _tariffa, value);
    }

    public bool Attiva
    {
        get => _attiva;
        set => SetProperty(ref _attiva, value);
    }
}
