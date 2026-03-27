namespace SMZ.Conta.App.Models;

public sealed class RegistroImmersioneRiga
{
    public long ServizioGiornalieroId { get; set; }

    public long ServizioImmersioneId { get; set; }

    public DateOnly DataServizio { get; set; }

    public string NumeroOrdineServizio { get; set; } = string.Empty;

    public int NumeroImmersione { get; set; }

    public string Localita { get; set; } = string.Empty;

    public string ScopoImmersione { get; set; } = string.Empty;

    public string CategoriaRegistro { get; set; } = string.Empty;

    public int PerId { get; set; }

    public string Cognome { get; set; } = string.Empty;

    public string Nome { get; set; } = string.Empty;

    public string Qualifica { get; set; } = string.Empty;

    public string Apparato { get; set; } = string.Empty;

    public int? ProfonditaMetri { get; set; }

    public string FasciaProfondita { get; set; } = string.Empty;

    public decimal OreImmersione { get; set; }

    public TimeOnly? OrarioInizio { get; set; }

    public TimeOnly? OrarioFine { get; set; }

    public string Nominativo => $"{Cognome} {Nome}".Trim();

    public string DataServizioDescrizione => DataServizio.ToString("dd/MM/yyyy");

    public string ProfonditaDisplay => ProfonditaMetri is null ? string.Empty : $"{ProfonditaMetri} m";

    public string OreImmersioneDisplay => OreImmersione == 0 ? string.Empty : OreImmersione.ToString("0.##");

    public string OrarioDisplay
    {
        get
        {
            var inizio = OrarioInizio?.ToString("HH:mm") ?? string.Empty;
            var fine = OrarioFine?.ToString("HH:mm") ?? string.Empty;

            return (inizio, fine) switch
            {
                ("", "") => string.Empty,
                (_, "") => inizio,
                ("", _) => fine,
                _ => $"{inizio} - {fine}",
            };
        }
    }
}

public sealed class RegistroImmersioneCategoriaSummary
{
    public string CategoriaRegistro { get; set; } = string.Empty;

    public int ImmersioniTotali { get; set; }

    public int RigheOperatoreTotali { get; set; }

    public decimal OreTotali { get; set; }

    public string OreTotaliDisplay => OreTotali.ToString("0.##");
}
