namespace SMZ.Conta.App.Models;

public sealed class CategoriaRegistroItem
{
    public int CategoriaRegistroId { get; set; }

    public string Descrizione { get; set; } = string.Empty;

    public bool Attiva { get; set; } = true;

    public int Ordine { get; set; }
}

public sealed class LocalitaOperativa
{
    public int LocalitaOperativaId { get; set; }

    public string Descrizione { get; set; } = string.Empty;

    public string Provincia { get; set; } = string.Empty;

    public bool Attiva { get; set; } = true;

    public int Ordine { get; set; }
}

public sealed class ScopoImmersioneItem
{
    public int ScopoImmersioneId { get; set; }

    public string Descrizione { get; set; } = string.Empty;

    public int CategoriaRegistroId { get; set; }

    public bool Attiva { get; set; } = true;

    public int Ordine { get; set; }
}

public sealed class UnitaNavale
{
    public int UnitaNavaleId { get; set; }

    public string Descrizione { get; set; } = string.Empty;

    public string Sigla { get; set; } = string.Empty;

    public bool Attiva { get; set; } = true;

    public int Ordine { get; set; }
}

public sealed class TipologiaImmersioneOperativa
{
    public int TipologiaImmersioneOperativaId { get; set; }

    public string Codice { get; set; } = string.Empty;

    public string Descrizione { get; set; } = string.Empty;

    public int? ProfonditaMinimaMetri { get; set; }

    public int? ProfonditaMassimaMetri { get; set; }

    public bool Attiva { get; set; } = true;

    public int Ordine { get; set; }

    public string ProfonditaConsentitaDisplay =>
        ProfonditaMinimaMetri is null || ProfonditaMassimaMetri is null
            ? "Nessun range profondita configurato."
            : $"Profondita consentita: {ProfonditaMinimaMetri:0}-{ProfonditaMassimaMetri:0} m";
}

public sealed class FasciaProfondita
{
    public int FasciaProfonditaId { get; set; }

    public string Descrizione { get; set; } = string.Empty;

    public int MetriDa { get; set; }

    public int MetriA { get; set; }

    public bool Attiva { get; set; } = true;

    public int Ordine { get; set; }
}

public sealed class CategoriaContabileOre
{
    public int CategoriaContabileOreId { get; set; }

    public string Codice { get; set; } = string.Empty;

    public string Descrizione { get; set; } = string.Empty;

    public bool Attiva { get; set; } = true;

    public int Ordine { get; set; }
}

public sealed class GruppoOperativo
{
    public int GruppoOperativoId { get; set; }

    public string Codice { get; set; } = string.Empty;

    public string Descrizione { get; set; } = string.Empty;

    public bool Attiva { get; set; } = true;

    public int Ordine { get; set; }
}

public sealed class RegolaContabileImmersione
{
    public int RegolaContabileImmersioneId { get; set; }

    public int TipologiaImmersioneOperativaId { get; set; }

    public int FasciaProfonditaId { get; set; }

    public int CategoriaContabileOreId { get; set; }

    public decimal Tariffa { get; set; }

    public DateOnly? DataInizioValidita { get; set; }

    public DateOnly? DataFineValidita { get; set; }

    public bool Attiva { get; set; } = true;
}

public sealed class RuoloOperativo
{
    public int RuoloOperativoId { get; set; }

    public string Codice { get; set; } = string.Empty;

    public string Descrizione { get; set; } = string.Empty;

    public bool Attiva { get; set; } = true;

    public int Ordine { get; set; }
}

public sealed class CataloghiServizioSnapshot
{
    public List<CategoriaRegistroItem> CategorieRegistro { get; init; } = [];

    public List<LocalitaOperativa> LocalitaOperative { get; init; } = [];

    public List<ScopoImmersioneItem> ScopiImmersione { get; init; } = [];

    public List<UnitaNavale> UnitaNavali { get; init; } = [];

    public List<TipologiaImmersioneOperativa> TipologieImmersione { get; init; } = [];

    public List<FasciaProfondita> FasceProfondita { get; init; } = [];

    public List<CategoriaContabileOre> CategorieContabiliOre { get; init; } = [];

    public List<GruppoOperativo> GruppiOperativi { get; init; } = [];

    public List<RegolaContabileImmersione> RegoleContabiliImmersione { get; init; } = [];

    public List<RuoloOperativo> RuoliOperativi { get; init; } = [];
}

public static class CataloghiServizio
{
    public static IReadOnlyList<CategoriaRegistroItem> CategorieRegistro { get; } =
    [
        new CategoriaRegistroItem { CategoriaRegistroId = 1, Descrizione = "Immersioni addestrative a mare e in bacino delimitato", Ordine = 1 },
        new CategoriaRegistroItem { CategoriaRegistroId = 2, Descrizione = "Immersioni ordinarie", Ordine = 2 },
        new CategoriaRegistroItem { CategoriaRegistroId = 3, Descrizione = "Immersioni per sperimentazione attrezzature e materiali subacquei", Ordine = 3 },
        new CategoriaRegistroItem { CategoriaRegistroId = 4, Descrizione = "Immersioni in camera iperbarica", Ordine = 4 },
        new CategoriaRegistroItem { CategoriaRegistroId = 5, Descrizione = "Altro", Ordine = 5 },
    ];

    public static IReadOnlyList<LocalitaOperativa> LocalitaOperative { get; } =
    [
        new LocalitaOperativa { LocalitaOperativaId = 1, Descrizione = "BASE NAVALE (SP)", Provincia = "SP", Ordine = 1 },
        new LocalitaOperativa { LocalitaOperativaId = 2, Descrizione = "B.N. - SENO DI PANIGAGLIA (SP)", Provincia = "SP", Ordine = 2 },
        new LocalitaOperativa { LocalitaOperativaId = 3, Descrizione = "LA SPEZIA (SP)", Provincia = "SP", Ordine = 3 },
        new LocalitaOperativa { LocalitaOperativaId = 4, Descrizione = "ISOLA DEL TINO (SP)", Provincia = "SP", Ordine = 4 },
        new LocalitaOperativa { LocalitaOperativaId = 5, Descrizione = "ISOLA DEL TINETTO (SP)", Provincia = "SP", Ordine = 5 },
        new LocalitaOperativa { LocalitaOperativaId = 6, Descrizione = "ISOLA PALMARIA (SP)", Provincia = "SP", Ordine = 6 },
        new LocalitaOperativa { LocalitaOperativaId = 7, Descrizione = "PORTOVENERE (SP)", Provincia = "SP", Ordine = 7 },
        new LocalitaOperativa { LocalitaOperativaId = 8, Descrizione = "RIOMAGGIORE (SP)", Provincia = "SP", Ordine = 8 },
        new LocalitaOperativa { LocalitaOperativaId = 9, Descrizione = "MANAROLA (SP)", Provincia = "SP", Ordine = 9 },
        new LocalitaOperativa { LocalitaOperativaId = 10, Descrizione = "CORNIGLIA (SP)", Provincia = "SP", Ordine = 10 },
        new LocalitaOperativa { LocalitaOperativaId = 11, Descrizione = "VERNAZZA (SP)", Provincia = "SP", Ordine = 11 },
        new LocalitaOperativa { LocalitaOperativaId = 12, Descrizione = "MONTEROSSO (SP)", Provincia = "SP", Ordine = 12 },
        new LocalitaOperativa { LocalitaOperativaId = 13, Descrizione = "LERICI (SP)", Provincia = "SP", Ordine = 13 },
        new LocalitaOperativa { LocalitaOperativaId = 14, Descrizione = "LERICI (SP) - Diga Foranea", Provincia = "SP", Ordine = 14 },
        new LocalitaOperativa { LocalitaOperativaId = 15, Descrizione = "SARZANA (SP)", Provincia = "SP", Ordine = 15 },
        new LocalitaOperativa { LocalitaOperativaId = 16, Descrizione = "AMEGLIA (SP)", Provincia = "SP", Ordine = 16 },
        new LocalitaOperativa { LocalitaOperativaId = 17, Descrizione = "ALASSIO (SV)", Provincia = "SV", Ordine = 17 },
        new LocalitaOperativa { LocalitaOperativaId = 18, Descrizione = "BARI (BA)", Provincia = "BA", Ordine = 18 },
        new LocalitaOperativa { LocalitaOperativaId = 19, Descrizione = "BOLOGNA (BO)", Provincia = "BO", Ordine = 19 },
        new LocalitaOperativa { LocalitaOperativaId = 20, Descrizione = "COMO (CO)", Provincia = "CO", Ordine = 20 },
        new LocalitaOperativa { LocalitaOperativaId = 21, Descrizione = "FIRENZE (FI)", Provincia = "FI", Ordine = 21 },
        new LocalitaOperativa { LocalitaOperativaId = 22, Descrizione = "GENOVA (GE)", Provincia = "GE", Ordine = 22 },
        new LocalitaOperativa { LocalitaOperativaId = 23, Descrizione = "GROSSETO (GR)", Provincia = "GR", Ordine = 23 },
        new LocalitaOperativa { LocalitaOperativaId = 24, Descrizione = "IMPERIA (IM)", Provincia = "IM", Ordine = 24 },
        new LocalitaOperativa { LocalitaOperativaId = 25, Descrizione = "LIVORNO (LI)", Provincia = "LI", Ordine = 25 },
        new LocalitaOperativa { LocalitaOperativaId = 26, Descrizione = "MASSA-CARRARA (MS)", Provincia = "MS", Ordine = 26 },
        new LocalitaOperativa { LocalitaOperativaId = 27, Descrizione = "MODENA (TN)", Provincia = "TN", Ordine = 27 },
        new LocalitaOperativa { LocalitaOperativaId = 28, Descrizione = "NAPOLI (NA)", Provincia = "NA", Ordine = 28 },
        new LocalitaOperativa { LocalitaOperativaId = 29, Descrizione = "PALERMO (PA)", Provincia = "PA", Ordine = 29 },
        new LocalitaOperativa { LocalitaOperativaId = 30, Descrizione = "PISA (PI)", Provincia = "PI", Ordine = 30 },
        new LocalitaOperativa { LocalitaOperativaId = 31, Descrizione = "ROMA (RM)", Provincia = "RM", Ordine = 31 },
        new LocalitaOperativa { LocalitaOperativaId = 32, Descrizione = "SAVONA (SV)", Provincia = "SV", Ordine = 32 },
        new LocalitaOperativa { LocalitaOperativaId = 33, Descrizione = "SASSARI (SS)", Provincia = "SS", Ordine = 33 },
    ];

    public static IReadOnlyList<ScopoImmersioneItem> ScopiImmersione { get; } =
    [
        new ScopoImmersioneItem { ScopoImmersioneId = 1, Descrizione = "ADDESTRATIVA IN B.D.", CategoriaRegistroId = 1, Ordine = 1 },
        new ScopoImmersioneItem { ScopoImmersioneId = 2, Descrizione = "ADDESTRATIVA A MARE", CategoriaRegistroId = 1, Ordine = 2 },
        new ScopoImmersioneItem { ScopoImmersioneId = 3, Descrizione = "ADDESTRATIVA IN BACINO LACUALE IN ALTITUDINE", CategoriaRegistroId = 1, Ordine = 3 },
        new ScopoImmersioneItem { ScopoImmersioneId = 4, Descrizione = "IMMERSIONE IN CAMERA IPERBARICA", CategoriaRegistroId = 4, Ordine = 4 },
        new ScopoImmersioneItem { ScopoImmersioneId = 5, Descrizione = "ASSISTENZA SUB. MANIFESTAZIONI SPORTIVE/CULTURALI", CategoriaRegistroId = 2, Ordine = 5 },
        new ScopoImmersioneItem { ScopoImmersioneId = 6, Descrizione = "ASSISTENZA SUB. MANIFESTAZIONI RELIGIOSE", CategoriaRegistroId = 2, Ordine = 6 },
        new ScopoImmersioneItem { ScopoImmersioneId = 7, Descrizione = "COLLABORAZIONE CON ALTRI ENTI", CategoriaRegistroId = 2, Ordine = 7 },
        new ScopoImmersioneItem { ScopoImmersioneId = 8, Descrizione = "DIMOSTRAZIONE/RAPPRESENTANZA SPECIALITA'", CategoriaRegistroId = 2, Ordine = 8 },
        new ScopoImmersioneItem { ScopoImmersioneId = 9, Descrizione = "MANUTENZIONE/CONTROLLO CATENARIE D'ORMEGGIO", CategoriaRegistroId = 2, Ordine = 9 },
        new ScopoImmersioneItem { ScopoImmersioneId = 10, Descrizione = "MANTENIMENTO BREVETTO OPER. SUB. ALTRI UFFICI", CategoriaRegistroId = 1, Ordine = 10 },
        new ScopoImmersioneItem { ScopoImmersioneId = 11, Descrizione = "RICERCA/RECUPERO P.G.", CategoriaRegistroId = 2, Ordine = 11 },
        new ScopoImmersioneItem { ScopoImmersioneId = 12, Descrizione = "RICERCA ARCHEOLOGIA", CategoriaRegistroId = 2, Ordine = 12 },
        new ScopoImmersioneItem { ScopoImmersioneId = 13, Descrizione = "RICERCA/RECUPERO MATERIALI DISPERSI", CategoriaRegistroId = 2, Ordine = 13 },
        new ScopoImmersioneItem { ScopoImmersioneId = 14, Descrizione = "RICERCA RESIDUATI BELLICI", CategoriaRegistroId = 2, Ordine = 14 },
        new ScopoImmersioneItem { ScopoImmersioneId = 15, Descrizione = "RIPRESE VIDEO/SERVIZIO TELEVISIVO", CategoriaRegistroId = 2, Ordine = 15 },
        new ScopoImmersioneItem { ScopoImmersioneId = 16, Descrizione = "SICUREZZA E PREVENZIONE SUBACQUEA", CategoriaRegistroId = 2, Ordine = 16 },
        new ScopoImmersioneItem { ScopoImmersioneId = 17, Descrizione = "SPERIMENTAZIONE E COLLAUDO ATTREZZATURA SUBACQUEA", CategoriaRegistroId = 3, Ordine = 17 },
        new ScopoImmersioneItem { ScopoImmersioneId = 18, Descrizione = "SVOLGIMENTO STAGE PROPEDEUTICO AL CORSO O.S.S.P.", CategoriaRegistroId = 1, Ordine = 18 },
        new ScopoImmersioneItem { ScopoImmersioneId = 19, Descrizione = "TUTELA AMBIENTALE", CategoriaRegistroId = 2, Ordine = 19 },
        new ScopoImmersioneItem { ScopoImmersioneId = 20, Descrizione = "PROVE FUNZIONALI ATTREZZATURA SUBACQUEA", CategoriaRegistroId = 3, Ordine = 20 },
        new ScopoImmersioneItem { ScopoImmersioneId = 21, Descrizione = "VIGILANZA PARCHI MARINI E PREVENZIONE PESCA DI FRODO", CategoriaRegistroId = 2, Ordine = 21 },
        new ScopoImmersioneItem { ScopoImmersioneId = 22, Descrizione = "ALTRO", CategoriaRegistroId = 5, Ordine = 22 },
        new ScopoImmersioneItem { ScopoImmersioneId = 23, Descrizione = "ASSISTENZA SUB PROVE SELETTIVE CORSO SMZ", CategoriaRegistroId = 2, Ordine = 23 },
        new ScopoImmersioneItem { ScopoImmersioneId = 24, Descrizione = "SVOLGIMENTO ASSISTENZA CORSO RIQUALIFICA SMZ", CategoriaRegistroId = 2, Ordine = 24 },
        new ScopoImmersioneItem { ScopoImmersioneId = 25, Descrizione = "PROVE FUNZIONALI PER ESERCITAZIONE SUBACQUEA", CategoriaRegistroId = 3, Ordine = 25 },
    ];

    public static IReadOnlyList<UnitaNavale> UnitaNavali { get; } =
    [
        new UnitaNavale { UnitaNavaleId = 1, Descrizione = "P.S. 1230 arimar", Sigla = "1230", Ordine = 1 },
        new UnitaNavale { UnitaNavaleId = 2, Descrizione = "P.S. 1190 stilmar", Sigla = "1190", Ordine = 2 },
        new UnitaNavale { UnitaNavaleId = 3, Descrizione = "P.S. 1162 stilmar", Sigla = "1162", Ordine = 3 },
        new UnitaNavale { UnitaNavaleId = 4, Descrizione = "P.S. 1347 orion", Sigla = "1347", Ordine = 4 },
        new UnitaNavale { UnitaNavaleId = 5, Descrizione = "P.S. 1174 MDN", Sigla = "1174", Ordine = 5 },
        new UnitaNavale { UnitaNavaleId = 6, Descrizione = "P.S. 1287 zodiac420", Sigla = "1287", Ordine = 6 },
        new UnitaNavale { UnitaNavaleId = 7, Descrizione = "P.S. 1289 zodiac470", Sigla = "1289", Ordine = 7 },
        new UnitaNavale { UnitaNavaleId = 8, Descrizione = "P.S. 1437 med55", Sigla = "1437", Ordine = 8 },
        new UnitaNavale { UnitaNavaleId = 9, Descrizione = "P.S. 1438 med55", Sigla = "1438", Ordine = 9 },
        new UnitaNavale { UnitaNavaleId = 10, Descrizione = "P.S. 1272 vizianello", Sigla = "1272", Ordine = 10 },
        new UnitaNavale { UnitaNavaleId = 11, Descrizione = "P.S. 1447 Whally", Sigla = "1447", Ordine = 11 },
    ];

    public static IReadOnlyList<TipologiaImmersioneOperativa> TipologieImmersione { get; } =
    [
        new TipologiaImmersioneOperativa { TipologiaImmersioneOperativaId = 1, Codice = "ARA_ASAS", Descrizione = "A.R.A./ASAS", ProfonditaMinimaMetri = 0, ProfonditaMassimaMetri = 80, Ordine = 1 },
        new TipologiaImmersioneOperativa { TipologiaImmersioneOperativaId = 2, Codice = "ARO", Descrizione = "A.R.O.", ProfonditaMinimaMetri = 0, ProfonditaMassimaMetri = 12, Ordine = 2 },
        new TipologiaImmersioneOperativa { TipologiaImmersioneOperativaId = 3, Codice = "ARM", Descrizione = "A.R.M.", ProfonditaMinimaMetri = 0, ProfonditaMassimaMetri = 55, Ordine = 3 },
        new TipologiaImmersioneOperativa { TipologiaImmersioneOperativaId = 4, Codice = "CI", Descrizione = "C.I.", ProfonditaMinimaMetri = 0, ProfonditaMassimaMetri = 55, Ordine = 4 },
    ];

    public static IReadOnlyList<FasciaProfondita> FasceProfondita { get; } =
    [
        new FasciaProfondita { FasciaProfonditaId = 1, Descrizione = "00/12", MetriDa = 0, MetriA = 12, Ordine = 1 },
        new FasciaProfondita { FasciaProfonditaId = 2, Descrizione = "13/25", MetriDa = 13, MetriA = 25, Ordine = 2 },
        new FasciaProfondita { FasciaProfonditaId = 3, Descrizione = "26/40", MetriDa = 26, MetriA = 40, Ordine = 3 },
        new FasciaProfondita { FasciaProfonditaId = 4, Descrizione = "41/55", MetriDa = 41, MetriA = 55, Ordine = 4 },
        new FasciaProfondita { FasciaProfonditaId = 5, Descrizione = "56/80", MetriDa = 56, MetriA = 80, Ordine = 5 },
    ];

    public static IReadOnlyList<CategoriaContabileOre> CategorieContabiliOre { get; } =
    [
        new CategoriaContabileOre { CategoriaContabileOreId = 1, Codice = "ORD", Descrizione = "ORE ORD", Ordine = 1 },
        new CategoriaContabileOre { CategoriaContabileOreId = 2, Codice = "ADD", Descrizione = "ORE ADD", Ordine = 2 },
        new CategoriaContabileOre { CategoriaContabileOreId = 3, Codice = "SPER", Descrizione = "ORE SPER", Ordine = 3 },
        new CategoriaContabileOre { CategoriaContabileOreId = 4, Codice = "CI", Descrizione = "ORE C.I.", Ordine = 4 },
    ];

    public static IReadOnlyList<GruppoOperativo> GruppiOperativi { get; } =
    [
        new GruppoOperativo { GruppoOperativoId = 1, Codice = "SMZ", Descrizione = "SMZ", Ordine = 1 },
        new GruppoOperativo { GruppoOperativoId = 2, Codice = "OSSALC", Descrizione = "Personale O.S.S.A.L.C.", Ordine = 2 },
        new GruppoOperativo { GruppoOperativoId = 3, Codice = "SANITARIA", Descrizione = "Personale assistenza sanitaria", Ordine = 3 },
        new GruppoOperativo { GruppoOperativoId = 4, Codice = "SUPPORTO", Descrizione = "Supporto", Ordine = 4 },
    ];

    public static IReadOnlyList<RuoloOperativo> RuoliOperativi { get; } =
    [
        new RuoloOperativo { RuoloOperativoId = 1, Codice = "OPERATORE", Descrizione = "Operatore", Ordine = 1 },
        new RuoloOperativo { RuoloOperativoId = 2, Codice = "ASSISTENZA", Descrizione = "Assistenza", Ordine = 2 },
        new RuoloOperativo { RuoloOperativoId = 3, Codice = "SANITARIO", Descrizione = "Sanitario", Ordine = 3 },
        new RuoloOperativo { RuoloOperativoId = 4, Codice = "DIRETTORE", Descrizione = "Direttore immersione", Ordine = 4 },
        new RuoloOperativo { RuoloOperativoId = 5, Codice = "SOCCORSO", Descrizione = "Operatore soccorso", Ordine = 5 },
        new RuoloOperativo { RuoloOperativoId = 6, Codice = "BLSD", Descrizione = "Assistenza BLSD", Ordine = 6 },
        new RuoloOperativo { RuoloOperativoId = 7, Codice = "SUPPORTO", Descrizione = "Supporto", Ordine = 7 },
    ];

    public static IReadOnlyList<RegolaContabileImmersione> RegoleContabiliImmersione { get; } =
    [
        new RegolaContabileImmersione { RegolaContabileImmersioneId = 1, TipologiaImmersioneOperativaId = 2, FasciaProfonditaId = 1, CategoriaContabileOreId = 1, Tariffa = 30m, Attiva = true },
        new RegolaContabileImmersione { RegolaContabileImmersioneId = 2, TipologiaImmersioneOperativaId = 1, FasciaProfonditaId = 1, CategoriaContabileOreId = 1, Tariffa = 5m, Attiva = true },
        new RegolaContabileImmersione { RegolaContabileImmersioneId = 3, TipologiaImmersioneOperativaId = 1, FasciaProfonditaId = 2, CategoriaContabileOreId = 1, Tariffa = 10m, Attiva = true },
        new RegolaContabileImmersione { RegolaContabileImmersioneId = 4, TipologiaImmersioneOperativaId = 1, FasciaProfonditaId = 3, CategoriaContabileOreId = 1, Tariffa = 20m, Attiva = true },
        new RegolaContabileImmersione { RegolaContabileImmersioneId = 5, TipologiaImmersioneOperativaId = 1, FasciaProfonditaId = 4, CategoriaContabileOreId = 1, Tariffa = 28m, Attiva = true },
        new RegolaContabileImmersione { RegolaContabileImmersioneId = 6, TipologiaImmersioneOperativaId = 1, FasciaProfonditaId = 5, CategoriaContabileOreId = 1, Tariffa = 38m, Attiva = true },
        new RegolaContabileImmersione { RegolaContabileImmersioneId = 7, TipologiaImmersioneOperativaId = 3, FasciaProfonditaId = 1, CategoriaContabileOreId = 1, Tariffa = 10m, Attiva = true },
        new RegolaContabileImmersione { RegolaContabileImmersioneId = 8, TipologiaImmersioneOperativaId = 3, FasciaProfonditaId = 2, CategoriaContabileOreId = 1, Tariffa = 15m, Attiva = true },
        new RegolaContabileImmersione { RegolaContabileImmersioneId = 9, TipologiaImmersioneOperativaId = 3, FasciaProfonditaId = 3, CategoriaContabileOreId = 1, Tariffa = 18m, Attiva = true },
        new RegolaContabileImmersione { RegolaContabileImmersioneId = 10, TipologiaImmersioneOperativaId = 3, FasciaProfonditaId = 4, CategoriaContabileOreId = 1, Tariffa = 24m, Attiva = true },
        new RegolaContabileImmersione { RegolaContabileImmersioneId = 11, TipologiaImmersioneOperativaId = 2, FasciaProfonditaId = 1, CategoriaContabileOreId = 2, Tariffa = 30m, Attiva = true },
        new RegolaContabileImmersione { RegolaContabileImmersioneId = 12, TipologiaImmersioneOperativaId = 1, FasciaProfonditaId = 1, CategoriaContabileOreId = 2, Tariffa = 5m, Attiva = true },
        new RegolaContabileImmersione { RegolaContabileImmersioneId = 13, TipologiaImmersioneOperativaId = 1, FasciaProfonditaId = 2, CategoriaContabileOreId = 2, Tariffa = 10m, Attiva = true },
        new RegolaContabileImmersione { RegolaContabileImmersioneId = 14, TipologiaImmersioneOperativaId = 1, FasciaProfonditaId = 3, CategoriaContabileOreId = 2, Tariffa = 20m, Attiva = true },
        new RegolaContabileImmersione { RegolaContabileImmersioneId = 15, TipologiaImmersioneOperativaId = 1, FasciaProfonditaId = 4, CategoriaContabileOreId = 2, Tariffa = 28m, Attiva = true },
        new RegolaContabileImmersione { RegolaContabileImmersioneId = 16, TipologiaImmersioneOperativaId = 1, FasciaProfonditaId = 5, CategoriaContabileOreId = 2, Tariffa = 38m, Attiva = true },
        new RegolaContabileImmersione { RegolaContabileImmersioneId = 17, TipologiaImmersioneOperativaId = 3, FasciaProfonditaId = 1, CategoriaContabileOreId = 2, Tariffa = 10m, Attiva = true },
        new RegolaContabileImmersione { RegolaContabileImmersioneId = 18, TipologiaImmersioneOperativaId = 3, FasciaProfonditaId = 2, CategoriaContabileOreId = 2, Tariffa = 15m, Attiva = true },
        new RegolaContabileImmersione { RegolaContabileImmersioneId = 19, TipologiaImmersioneOperativaId = 3, FasciaProfonditaId = 3, CategoriaContabileOreId = 2, Tariffa = 18m, Attiva = true },
        new RegolaContabileImmersione { RegolaContabileImmersioneId = 20, TipologiaImmersioneOperativaId = 3, FasciaProfonditaId = 4, CategoriaContabileOreId = 2, Tariffa = 24m, Attiva = true },
        new RegolaContabileImmersione { RegolaContabileImmersioneId = 21, TipologiaImmersioneOperativaId = 4, FasciaProfonditaId = 1, CategoriaContabileOreId = 4, Tariffa = 2.48m, Attiva = true },
        new RegolaContabileImmersione { RegolaContabileImmersioneId = 22, TipologiaImmersioneOperativaId = 2, FasciaProfonditaId = 1, CategoriaContabileOreId = 3, Tariffa = 30m, Attiva = true },
        new RegolaContabileImmersione { RegolaContabileImmersioneId = 23, TipologiaImmersioneOperativaId = 1, FasciaProfonditaId = 1, CategoriaContabileOreId = 3, Tariffa = 5m, Attiva = true },
        new RegolaContabileImmersione { RegolaContabileImmersioneId = 24, TipologiaImmersioneOperativaId = 1, FasciaProfonditaId = 2, CategoriaContabileOreId = 3, Tariffa = 10m, Attiva = true },
        new RegolaContabileImmersione { RegolaContabileImmersioneId = 25, TipologiaImmersioneOperativaId = 1, FasciaProfonditaId = 3, CategoriaContabileOreId = 3, Tariffa = 20m, Attiva = true },
        new RegolaContabileImmersione { RegolaContabileImmersioneId = 26, TipologiaImmersioneOperativaId = 1, FasciaProfonditaId = 4, CategoriaContabileOreId = 3, Tariffa = 28m, Attiva = true },
        new RegolaContabileImmersione { RegolaContabileImmersioneId = 27, TipologiaImmersioneOperativaId = 1, FasciaProfonditaId = 5, CategoriaContabileOreId = 3, Tariffa = 38m, Attiva = true },
        new RegolaContabileImmersione { RegolaContabileImmersioneId = 28, TipologiaImmersioneOperativaId = 3, FasciaProfonditaId = 1, CategoriaContabileOreId = 3, Tariffa = 10m, Attiva = true },
        new RegolaContabileImmersione { RegolaContabileImmersioneId = 29, TipologiaImmersioneOperativaId = 3, FasciaProfonditaId = 2, CategoriaContabileOreId = 3, Tariffa = 15m, Attiva = true },
        new RegolaContabileImmersione { RegolaContabileImmersioneId = 30, TipologiaImmersioneOperativaId = 3, FasciaProfonditaId = 3, CategoriaContabileOreId = 3, Tariffa = 18m, Attiva = true },
        new RegolaContabileImmersione { RegolaContabileImmersioneId = 31, TipologiaImmersioneOperativaId = 3, FasciaProfonditaId = 4, CategoriaContabileOreId = 3, Tariffa = 24m, Attiva = true },
        new RegolaContabileImmersione { RegolaContabileImmersioneId = 32, TipologiaImmersioneOperativaId = 4, FasciaProfonditaId = 2, CategoriaContabileOreId = 4, Tariffa = 2.48m, Attiva = true },
        new RegolaContabileImmersione { RegolaContabileImmersioneId = 33, TipologiaImmersioneOperativaId = 4, FasciaProfonditaId = 3, CategoriaContabileOreId = 4, Tariffa = 2.48m, Attiva = true },
        new RegolaContabileImmersione { RegolaContabileImmersioneId = 34, TipologiaImmersioneOperativaId = 4, FasciaProfonditaId = 4, CategoriaContabileOreId = 4, Tariffa = 2.48m, Attiva = true },
    ];
}
