namespace SMZ.Conta.App.Models;

public sealed class PersonaleArchivioSummary
{
    public long PersonaleArchivioId { get; set; }

    public int PerIdOriginale { get; set; }

    public string Cognome { get; set; } = string.Empty;

    public string Nome { get; set; } = string.Empty;

    public string CodiceFiscale { get; set; } = string.Empty;

    public DateTime DataArchiviazione { get; set; }

    public string NominativoCompleto => $"{Cognome} {Nome}".Trim();
}
