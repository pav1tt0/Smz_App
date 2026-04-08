namespace SMZ.Conta.App.Models;

public sealed class ServizioScambioPackageDocument
{
    public string PackageType { get; set; } = "SMZ.ServicePackage";

    public int Version { get; set; } = 1;

    public Guid PackageId { get; set; } = Guid.NewGuid();

    public DateTime ExportedAtUtc { get; set; } = DateTime.UtcNow;

    public ServizioScambioPackageService Servizio { get; set; } = new();
}

public sealed class ServizioScambioPackageService
{
    public long SourceServizioGiornalieroId { get; set; }

    public DateOnly DataServizio { get; set; }

    public string NumeroOrdineServizio { get; set; } = string.Empty;

    public string OrarioServizio { get; set; } = string.Empty;

    public bool StraordinarioAttivo { get; set; }

    public string StraordinarioInizio { get; set; } = string.Empty;

    public string StraordinarioFine { get; set; } = string.Empty;

    public string TipoServizio { get; set; } = "InSede";

    public string LocalitaDescrizione { get; set; } = string.Empty;

    public string ScopoDescrizione { get; set; } = string.Empty;

    public string UnitaNavaleDescrizione { get; set; } = string.Empty;

    public string UnitaNavaleSigla { get; set; } = string.Empty;

    public bool FuoriSede { get; set; }

    public bool IndennitaOrdinePubblico { get; set; }

    public string AttivitaSvolta { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;

    public List<ServizioScambioPackagePartecipante> Partecipanti { get; set; } = [];

    public List<ServizioScambioPackageImmersione> Immersioni { get; set; } = [];

    public List<ServizioScambioPackageSupportoOccasionale> SupportiOccasionali { get; set; } = [];
}

public sealed class ServizioScambioPackagePartecipante
{
    public ServizioScambioPackagePersonRef Persona { get; set; } = new();

    public string GruppoCodice { get; set; } = string.Empty;

    public string GruppoDescrizione { get; set; } = string.Empty;

    public bool Presente { get; set; }

    public string RuoloCodice { get; set; } = string.Empty;

    public string RuoloDescrizione { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;
}

public sealed class ServizioScambioPackageImmersione
{
    public int NumeroImmersione { get; set; }

    public string OrarioInizio { get; set; } = string.Empty;

    public string OrarioFine { get; set; } = string.Empty;

    public ServizioScambioPackagePersonRef? DirettoreImmersione { get; set; }

    public ServizioScambioPackagePersonRef? OperatoreSoccorso { get; set; }

    public ServizioScambioPackagePersonRef? AssistenteBlsd { get; set; }

    public ServizioScambioPackagePersonRef? AssistenteSanitario { get; set; }

    public string LocalitaDescrizione { get; set; } = string.Empty;

    public string ScopoDescrizione { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;

    public List<ServizioScambioPackagePartecipazioneImmersione> Partecipazioni { get; set; } = [];
}

public sealed class ServizioScambioPackagePartecipazioneImmersione
{
    public ServizioScambioPackagePersonRef Persona { get; set; } = new();

    public string TipologiaCodice { get; set; } = string.Empty;

    public string TipologiaDescrizione { get; set; } = string.Empty;

    public int? ProfonditaMetri { get; set; }

    public string FasciaDescrizione { get; set; } = string.Empty;

    public decimal? OreImmersione { get; set; }

    public string CategoriaCodice { get; set; } = string.Empty;

    public string CategoriaDescrizione { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;
}

public sealed class ServizioScambioPackageSupportoOccasionale
{
    public string Nominativo { get; set; } = string.Empty;

    public string Qualifica { get; set; } = string.Empty;

    public string Ruolo { get; set; } = string.Empty;

    public bool Presente { get; set; }

    public string Contatti { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;
}

public sealed class ServizioScambioPackagePersonRef
{
    public int? PerId { get; set; }

    public string CodiceFiscale { get; set; } = string.Empty;

    public string MatricolaPersonale { get; set; } = string.Empty;

    public string NumeroBrevettoSmz { get; set; } = string.Empty;

    public string Cognome { get; set; } = string.Empty;

    public string Nome { get; set; } = string.Empty;

    public string Qualifica { get; set; } = string.Empty;
}
