namespace SMZ.Conta.App.Models;

public sealed class VisitaMedica
{
    public int VisitaMedicaId { get; set; }

    public int PerId { get; set; }

    public string TipoVisita { get; set; } = string.Empty;

    public DateOnly? DataUltimaVisita { get; set; }

    public DateOnly? DataScadenza { get; set; }

    public string Esito { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;
}
