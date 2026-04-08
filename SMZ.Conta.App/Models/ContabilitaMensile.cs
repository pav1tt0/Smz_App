namespace SMZ.Conta.App.Models;

public sealed class ContabilitaMeseItem
{
    public int NumeroMese { get; init; }

    public string Descrizione { get; init; } = string.Empty;
}

public sealed class ContabilitaSanitarioSummary
{
    public int PerId { get; set; }

    public string Cognome { get; set; } = string.Empty;

    public string Nome { get; set; } = string.Empty;

    public string Qualifica { get; set; } = string.Empty;

    public string RuoloSanitario { get; set; } = string.Empty;

    public int GiornateImpiego { get; set; }

    public DateOnly? UltimaDataServizio { get; set; }

    public int TrentesimiMaturati => GiornateImpiego;

    public string Nominativo => $"{Cognome} {Nome}".Trim();

    public string UltimaDataServizioDescrizione => UltimaDataServizio?.ToString("dd/MM/yyyy") ?? string.Empty;

    public string QualificaDisplay => QualificaFormatter.AbbreviaPerVisualizzazione(Qualifica);

    public string QualificaConRuolo =>
        string.IsNullOrWhiteSpace(RuoloSanitario)
            ? QualificaDisplay
            : string.IsNullOrWhiteSpace(QualificaDisplay)
                ? RuoloSanitario
                : $"{QualificaDisplay} | {RuoloSanitario}";
}

public sealed class ContabilitaSupportoSummary
{
    public string Nominativo { get; set; } = string.Empty;

    public string Qualifica { get; set; } = string.Empty;

    public string Ruolo { get; set; } = string.Empty;

    public int GiornateImpiego { get; set; }

    public DateOnly? UltimaDataServizio { get; set; }

    public int TrentesimiMaturati => GiornateImpiego;

    public string UltimaDataServizioDescrizione => UltimaDataServizio?.ToString("dd/MM/yyyy") ?? string.Empty;

    public string QualificaDisplay => QualificaFormatter.AbbreviaPerVisualizzazione(Qualifica);

    public string QualificaConRuolo =>
        string.IsNullOrWhiteSpace(Ruolo)
            ? QualificaDisplay
            : string.IsNullOrWhiteSpace(QualificaDisplay)
                ? Ruolo
                : $"{QualificaDisplay} | {Ruolo}";
}

public sealed class ContabilitaGiornateImpiegoSnapshot
{
    public List<ContabilitaSmzSummary> SmzImmersioni { get; init; } = [];

    public List<ContabilitaSanitarioSummary> Sanitari { get; init; } = [];

    public List<ContabilitaSupportoSummary> SupportiOccasionali { get; init; } = [];
}

public sealed class ElaborazioneMensileInfo
{
    public long ElaborazioneMensileId { get; set; }

    public int Anno { get; set; }

    public int Mese { get; set; }

    public DateTime CreataIl { get; set; }

    public DateTime AggiornataIl { get; set; }

    public int RigheSmz { get; set; }

    public int RigheSanitari { get; set; }

    public int RigheSupporti { get; set; }

    public string AggiornataIlDescrizione => AggiornataIl.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
}

public sealed class ContabilitaSmzSummary
{
    public int PerId { get; set; }

    public DateOnly DataServizio { get; set; }

    public string NumeroOrdineServizio { get; set; } = string.Empty;

    public string Cognome { get; set; } = string.Empty;

    public string Nome { get; set; } = string.Empty;

    public string Qualifica { get; set; } = string.Empty;

    public string Apparato { get; set; } = string.Empty;

    public string FasciaProfondita { get; set; } = string.Empty;

    public decimal Tariffa { get; set; }

    public decimal OreOrd { get; set; }

    public decimal OreAdd { get; set; }

    public decimal OreSper { get; set; }

    public decimal OreCi { get; set; }

    public decimal Importo { get; set; }

    public string Nominativo => $"{Cognome} {Nome}".Trim();

    public string QualificaDisplay => QualificaFormatter.AbbreviaPerVisualizzazione(Qualifica);

    public string DataServizioDescrizione => DataServizio.ToString("dd/MM/yyyy");

    public string TariffaDisplay => Tariffa.ToString("0.##");

    public string OreOrdDisplay => OreOrd == 0 ? string.Empty : OreOrd.ToString("0.##");

    public string OreAddDisplay => OreAdd == 0 ? string.Empty : OreAdd.ToString("0.##");

    public string OreSperDisplay => OreSper == 0 ? string.Empty : OreSper.ToString("0.##");

    public string OreCiDisplay => OreCi == 0 ? string.Empty : OreCi.ToString("0.##");

    public string ImportoDisplay => Importo.ToString("0.##");
}
