namespace SMZ.Conta.App.Models;

public sealed class TipoAbilitazione
{
    public int TipoAbilitazioneId { get; set; }

    public string Codice { get; set; } = string.Empty;

    public string Descrizione { get; set; } = string.Empty;

    public string Categoria { get; set; } = string.Empty;

    public bool RichiedeLivello { get; set; }

    public bool RichiedeScadenza { get; set; }

    public bool RichiedeProfondita { get; set; }

    public string EtichettaCompleta => $"{Descrizione} [{Codice}]";
}
