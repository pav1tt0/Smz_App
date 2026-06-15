namespace SMZ.Conta.App.Models;

public sealed class ReportPersonaleMensileRiga
{
    public long ServizioGiornalieroId { get; set; }

    public DateOnly DataServizio { get; set; }

    public string NumeroOrdineServizio { get; set; } = string.Empty;

    public int? PerId { get; set; }

    public string Qualifica { get; set; } = string.Empty;

    public string Nominativo { get; set; } = string.Empty;

    public string TipoRiga { get; set; } = string.Empty;

    public string GruppoRuolo { get; set; } = string.Empty;

    public string Localita { get; set; } = string.Empty;

    public string ScopoImmersione { get; set; } = string.Empty;

    public int? NumeroImmersione { get; set; }

    public string Apparato { get; set; } = string.Empty;

    public int? ProfonditaMetri { get; set; }

    public decimal OreImmersione { get; set; }

    public string DataServizioDescrizione => DataServizio.ToString("dd/MM/yyyy");

    public string PerIdDisplay => PerId?.ToString() ?? string.Empty;

    public string QualificaDisplay => QualificaFormatter.AbbreviaPerVisualizzazione(Qualifica);

    public string NumeroImmersioneDisplay => NumeroImmersione?.ToString() ?? string.Empty;

    public string ProfonditaDisplay => ProfonditaMetri is null ? string.Empty : $"{ProfonditaMetri} m";

    public string OreImmersioneDisplay => OreImmersione == 0 ? string.Empty : OreImmersione.ToString("0.##");
}
