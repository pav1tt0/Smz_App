namespace SMZ.Conta.App.Models;

public sealed class PersonaleArchivio
{
    public long PersonaleArchivioId { get; set; }

    public int PerIdOriginale { get; set; }

    public DateTime DataArchiviazione { get; set; }

    public string Cognome { get; set; } = string.Empty;

    public string Nome { get; set; } = string.Empty;

    public string Qualifica { get; set; } = string.Empty;

    public string CodiceFiscale { get; set; } = string.Empty;

    public string MatricolaPersonale { get; set; } = string.Empty;

    public string NumeroBrevettoSmz { get; set; } = string.Empty;

    public DateOnly? DataNascita { get; set; }

    public string LuogoNascita { get; set; } = string.Empty;

    public string IndirizzoResidenza { get; set; } = string.Empty;

    public string Telefono { get; set; } = string.Empty;

    public string Mail { get; set; } = string.Empty;

    public List<PersonaleAbilitazione> Abilitazioni { get; set; } = [];

    public List<VisitaMedica> VisiteMediche { get; set; } = [];

    public string NominativoCompleto => $"{Cognome} {Nome}".Trim();
}
