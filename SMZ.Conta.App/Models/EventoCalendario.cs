namespace SMZ.Conta.App.Models;

public sealed class EventoCalendario
{
    public long EventoId { get; set; }
    public string Titolo { get; set; } = string.Empty;
    public string Categoria { get; set; } = "Appuntamento";
    public DateTime Data { get; set; } = DateTime.Today;
    public TimeSpan? Ora { get; set; }
    public string Note { get; set; } = string.Empty;
    public bool Completata { get; set; }

    public bool IsScaduta => Categoria == "Scadenza" && !Completata && Data.Date < DateTime.Today;
    public string Colore => Completata ? "#64748B" : IsScaduta ? "#BD2735"
        : Categoria == "Scadenza" ? "#B76500" : "#0E5AA7";
    public string Stato => Completata ? "Scadenza completata" : IsScaduta ? "Scadenza superata" : Categoria;
    public string OrarioTesto => Ora?.ToString(@"hh\:mm") ?? "Senza orario";
}
