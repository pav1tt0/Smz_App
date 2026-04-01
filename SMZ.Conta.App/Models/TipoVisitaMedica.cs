namespace SMZ.Conta.App.Models;

public sealed class TipoVisitaMedica
{
    public string Codice { get; set; } = string.Empty;

    public string Descrizione { get; set; } = string.Empty;

    public int? MesiValidita { get; set; }

    public string RegolaScadenza
    {
        get
        {
            return MesiValidita switch
            {
                12 => "Scadenza automatica a 12 mesi",
                24 => "Scadenza automatica a 24 mesi",
                2 => "Scadenza automatica a 2 mesi",
                _ => "Scadenza libera",
            };
        }
    }
}
