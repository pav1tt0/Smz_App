using SMZ.Conta.App.Infrastructure;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.ViewModels;

public sealed class VisitaMedicaRowViewModel : ObservableObject
{
    private int? _visitaMedicaId;
    private string _tipoVisita = string.Empty;
    private string _dataUltimaVisita = string.Empty;
    private string _dataScadenza = string.Empty;
    private string _esito = string.Empty;
    private string _note = string.Empty;

    public int? VisitaMedicaId
    {
        get => _visitaMedicaId;
        set => SetProperty(ref _visitaMedicaId, value);
    }

    public string TipoVisita
    {
        get => _tipoVisita;
        set => SetProperty(ref _tipoVisita, value);
    }

    public string DataUltimaVisita
    {
        get => _dataUltimaVisita;
        set => SetProperty(ref _dataUltimaVisita, value);
    }

    public string DataScadenza
    {
        get => _dataScadenza;
        set => SetProperty(ref _dataScadenza, value);
    }

    public string Esito
    {
        get => _esito;
        set => SetProperty(ref _esito, value);
    }

    public string Note
    {
        get => _note;
        set => SetProperty(ref _note, value);
    }

    public static VisitaMedicaRowViewModel FromModel(VisitaMedica model)
    {
        return new VisitaMedicaRowViewModel
        {
            VisitaMedicaId = model.VisitaMedicaId,
            TipoVisita = model.TipoVisita,
            DataUltimaVisita = FormatDate(model.DataUltimaVisita),
            DataScadenza = FormatDate(model.DataScadenza),
            Esito = model.Esito,
            Note = model.Note,
        };
    }

    private static string FormatDate(DateOnly? value) => value?.ToString("dd/MM/yyyy") ?? string.Empty;
}
