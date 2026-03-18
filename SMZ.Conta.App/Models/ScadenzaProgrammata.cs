namespace SMZ.Conta.App.Models;

public sealed class ScadenzaProgrammata
{
    public int PerId { get; set; }

    public string Cognome { get; set; } = string.Empty;

    public string Nome { get; set; } = string.Empty;

    public string Origine { get; set; } = string.Empty;

    public string Titolo { get; set; } = string.Empty;

    public string Dettaglio { get; set; } = string.Empty;

    public DateOnly DataScadenza { get; set; }

    public int GiorniResidui { get; set; }

    public string Nominativo => $"{Cognome} {Nome}".Trim();
}
