namespace SMZ.Conta.App.Models;

public sealed class ServizioGiornaliero
{
    public long ServizioGiornalieroId { get; set; }

    public DateOnly DataServizio { get; set; }

    public string TipoServizio { get; set; } = "InSede";

    public int? LocalitaOperativaId { get; set; }

    public int? ScopoImmersioneId { get; set; }

    public int? UnitaNavaleId { get; set; }

    public bool FuoriSede { get; set; }

    public string AttivitaSvolta { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;

    public List<ServizioImmersione> Immersioni { get; set; } = [];

    public List<ServizioPartecipante> Partecipanti { get; set; } = [];
}

public sealed class ServizioImmersione
{
    public long ServizioImmersioneId { get; set; }

    public long ServizioGiornalieroId { get; set; }

    public int NumeroImmersione { get; set; }

    public TimeOnly? OrarioInizio { get; set; }

    public TimeOnly? OrarioFine { get; set; }

    public int? DirettoreImmersionePerId { get; set; }

    public int? OperatoreSoccorsoPerId { get; set; }

    public int? AssistenteBlsdPerId { get; set; }

    public int? AssistenteSanitarioPerId { get; set; }

    public int? LocalitaOperativaId { get; set; }

    public int? ScopoImmersioneId { get; set; }

    public string Note { get; set; } = string.Empty;

    public List<ServizioPartecipanteImmersione> Partecipazioni { get; set; } = [];
}

public sealed class ServizioPartecipante
{
    public long ServizioPartecipanteId { get; set; }

    public long ServizioGiornalieroId { get; set; }

    public int PerId { get; set; }

    public int GruppoOperativoId { get; set; }

    public bool Presente { get; set; }

    public int? RuoloOperativoId { get; set; }

    public string Note { get; set; } = string.Empty;

    public List<ServizioPartecipanteImmersione> Immersioni { get; set; } = [];
}

public sealed class ServizioPartecipanteImmersione
{
    public long ServizioPartecipanteImmersioneId { get; set; }

    public long ServizioImmersioneId { get; set; }

    public long ServizioPartecipanteId { get; set; }

    public int? TipologiaImmersioneOperativaId { get; set; }

    public int? ProfonditaMetri { get; set; }

    public int? FasciaProfonditaId { get; set; }

    public decimal? OreImmersione { get; set; }

    public int? CategoriaContabileOreId { get; set; }

    public string Note { get; set; } = string.Empty;
}
