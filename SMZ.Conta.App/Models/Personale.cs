using SMZ.Conta.App.Infrastructure;

namespace SMZ.Conta.App.Models;

public sealed class Personale
{
    public int PerId { get; set; }

    public string Cognome { get; set; } = string.Empty;

    public string Nome { get; set; } = string.Empty;

    public string Qualifica { get; set; } = string.Empty;

    public string ProfiloPersonale { get; set; } = ProfiliPersonaleCatalogo.OperatoreSubacqueo;

    public string RuoloSanitario { get; set; } = string.Empty;

    public string CodiceFiscale { get; set; } = string.Empty;

    public string MatricolaPersonale { get; set; } = string.Empty;

    public string NumeroBrevettoSmz { get; set; } = string.Empty;

    public string StatoServizio { get; set; } = StatoServizioPersonaleCatalogo.Attivo;

    public DateOnly? DataFineServizio { get; set; }

    public DateOnly? DataNascita { get; set; }

    public string LuogoNascita { get; set; } = string.Empty;

    public string ViaResidenza { get; set; } = string.Empty;

    public string CapResidenza { get; set; } = string.Empty;

    public string CittaResidenza { get; set; } = string.Empty;

    public string Telefono1 { get; set; } = string.Empty;

    public string Telefono2 { get; set; } = string.Empty;

    public string Mail1Utente { get; set; } = string.Empty;

    public string Mail2Utente { get; set; } = string.Empty;

    public List<PersonaleAbilitazione> Abilitazioni { get; set; } = [];

    public List<VisitaMedica> VisiteMediche { get; set; } = [];

    public List<PersonaleAttagliamento> Attagliamento { get; set; } = [];

    public bool IsProfiloSanitario => ProfiliPersonaleCatalogo.IsSanitario(ProfiloPersonale);

    public bool IsAttivo => string.Equals(
        StatoServizioPersonaleCatalogo.Normalizza(StatoServizio),
        StatoServizioPersonaleCatalogo.Attivo,
        StringComparison.OrdinalIgnoreCase);

    public bool IsUtilizzabileInData(DateOnly dataServizio) =>
        IsAttivo || (DataFineServizio is not null && dataServizio <= DataFineServizio.Value);

    public string NominativoCompleto => $"{Cognome} {Nome}".Trim();

    public string IndirizzoResidenzaCompleto
    {
        get
        {
            var parti = new List<string>();

            if (!string.IsNullOrWhiteSpace(ViaResidenza))
            {
                parti.Add(ViaResidenza.Trim());
            }

            var capCitta = string.Join(
                " ",
                new[] { CapResidenza.Trim(), CittaResidenza.Trim() }.Where(value => !string.IsNullOrWhiteSpace(value)));

            if (!string.IsNullOrWhiteSpace(capCitta))
            {
                parti.Add(capCitta);
            }

            return string.Join(", ", parti);
        }
    }

    public string ContattiSintesi
    {
        get
        {
            var parti = new[]
            {
                Telefono1.Trim(),
                Telefono2.Trim(),
                MailPoliziaHelper.Compose(Mail1Utente),
                Mail2Utente.Trim(),
            };

            return string.Join(" | ", parti.Where(value => !string.IsNullOrWhiteSpace(value)));
        }
    }
}
