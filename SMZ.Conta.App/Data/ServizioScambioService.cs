using System.IO;
using System.Text.Json;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.Data;

public sealed class ServizioScambioService
{
    private const string PackageType = "SMZ.ServicePackage";
    private const int PackageVersion = 1;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private readonly PersonaleRepository _repository;

    public ServizioScambioService(PersonaleRepository repository)
    {
        _repository = repository;
    }

    public void ExportServizio(long servizioGiornalieroId, string filePath)
    {
        var servizio = _repository.GetServizioGiornalieroById(servizioGiornalieroId)
            ?? throw new InvalidOperationException("Servizio giornaliero non trovato.");
        var cataloghi = _repository.GetCataloghiServizio();
        var personale = _repository.SearchPersonale(string.Empty, null, null).ToDictionary(item => item.PerId);

        var document = BuildDocument(servizio, cataloghi, personale);

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(document, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    public long ImportServizio(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            throw new FileNotFoundException("Pacchetto servizio non trovato.", filePath);
        }

        var json = File.ReadAllText(filePath);
        var document = JsonSerializer.Deserialize<ServizioScambioPackageDocument>(json, JsonOptions)
            ?? throw new InvalidOperationException("Pacchetto servizio non valido o vuoto.");

        if (!string.Equals(document.PackageType, PackageType, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Formato pacchetto non riconosciuto.");
        }

        if (document.Version != PackageVersion)
        {
            throw new InvalidOperationException($"Versione pacchetto non supportata: {document.Version}.");
        }

        var cataloghi = _repository.GetCataloghiServizio();
        var personale = _repository.SearchPersonale(string.Empty, null, null);
        var servizio = BuildServizio(document.Servizio, cataloghi, personale);
        return _repository.SaveServizioGiornaliero(servizio);
    }

    private static ServizioScambioPackageDocument BuildDocument(
        ServizioGiornaliero servizio,
        CataloghiServizioSnapshot cataloghi,
        IReadOnlyDictionary<int, Personale> personaleById)
    {
        var localitaById = cataloghi.LocalitaOperative.ToDictionary(item => item.LocalitaOperativaId);
        var scopiById = cataloghi.ScopiImmersione.ToDictionary(item => item.ScopoImmersioneId);
        var unitaById = cataloghi.UnitaNavali.ToDictionary(item => item.UnitaNavaleId);
        var gruppiById = cataloghi.GruppiOperativi.ToDictionary(item => item.GruppoOperativoId);
        var ruoliById = cataloghi.RuoliOperativi.ToDictionary(item => item.RuoloOperativoId);
        var tipologieById = cataloghi.TipologieImmersione.ToDictionary(item => item.TipologiaImmersioneOperativaId);
        var fasceById = cataloghi.FasceProfondita.ToDictionary(item => item.FasciaProfonditaId);
        var categorieById = cataloghi.CategorieContabiliOre.ToDictionary(item => item.CategoriaContabileOreId);
        var partecipantiPerServizioId = servizio.Partecipanti.ToDictionary(item => item.ServizioPartecipanteId, item => item.PerId);

        return new ServizioScambioPackageDocument
        {
            PackageType = PackageType,
            Version = PackageVersion,
            Servizio = new ServizioScambioPackageService
            {
                SourceServizioGiornalieroId = servizio.ServizioGiornalieroId,
                DataServizio = servizio.DataServizio,
                NumeroOrdineServizio = servizio.NumeroOrdineServizio,
                OrarioServizio = servizio.OrarioServizio,
                StraordinarioAttivo = servizio.StraordinarioAttivo,
                StraordinarioInizio = servizio.StraordinarioInizio,
                StraordinarioFine = servizio.StraordinarioFine,
                TipoServizio = servizio.TipoServizio,
                LocalitaDescrizione = servizio.LocalitaOperativaId is { } localitaId && localitaById.TryGetValue(localitaId, out var localita)
                    ? localita.Descrizione
                    : string.Empty,
                ScopoDescrizione = servizio.ScopoImmersioneId is { } scopoId && scopiById.TryGetValue(scopoId, out var scopo)
                    ? scopo.Descrizione
                    : string.Empty,
                UnitaNavaleDescrizione = servizio.UnitaNavaleId is { } unitaId && unitaById.TryGetValue(unitaId, out var unita)
                    ? unita.Descrizione
                    : string.Empty,
                UnitaNavaleSigla = servizio.UnitaNavaleId is { } siglaId && unitaById.TryGetValue(siglaId, out var unitaSigla)
                    ? unitaSigla.Sigla
                    : string.Empty,
                FuoriSede = servizio.FuoriSede,
                IndennitaOrdinePubblico = servizio.IndennitaOrdinePubblico,
                AttivitaSvolta = servizio.AttivitaSvolta,
                Note = servizio.Note,
                Partecipanti = servizio.Partecipanti
                    .Select(item => new ServizioScambioPackagePartecipante
                    {
                        Persona = BuildPersonRef(item.PerId, personaleById),
                        GruppoCodice = item.GruppoOperativoId is { } gruppoId && gruppiById.TryGetValue(gruppoId, out var gruppo) ? gruppo.Codice : string.Empty,
                        GruppoDescrizione = item.GruppoOperativoId is { } gruppoDescrId && gruppiById.TryGetValue(gruppoDescrId, out var gruppoDescr) ? gruppoDescr.Descrizione : string.Empty,
                        Presente = item.Presente,
                        RuoloCodice = item.RuoloOperativoId is { } ruoloId && ruoliById.TryGetValue(ruoloId, out var ruolo) ? ruolo.Codice : string.Empty,
                        RuoloDescrizione = item.RuoloOperativoId is { } ruoloDescrId && ruoliById.TryGetValue(ruoloDescrId, out var ruoloDescr) ? ruoloDescr.Descrizione : string.Empty,
                        Note = item.Note,
                    })
                    .ToList(),
                Immersioni = servizio.Immersioni
                    .OrderBy(item => item.NumeroImmersione)
                    .Select(item => new ServizioScambioPackageImmersione
                    {
                        NumeroImmersione = item.NumeroImmersione,
                        OrarioInizio = item.OrarioInizio?.ToString("HH:mm") ?? string.Empty,
                        OrarioFine = item.OrarioFine?.ToString("HH:mm") ?? string.Empty,
                        DirettoreImmersione = BuildNullablePersonRef(item.DirettoreImmersionePerId, personaleById),
                        OperatoreSoccorso = BuildNullablePersonRef(item.OperatoreSoccorsoPerId, personaleById),
                        AssistenteBlsd = BuildNullablePersonRef(item.AssistenteBlsdPerId, personaleById),
                        AssistenteSanitario = BuildNullablePersonRef(item.AssistenteSanitarioPerId, personaleById),
                        LocalitaDescrizione = item.LocalitaOperativaId is { } immersioneLocalitaId && localitaById.TryGetValue(immersioneLocalitaId, out var immersioneLocalita)
                            ? immersioneLocalita.Descrizione
                            : string.Empty,
                        ScopoDescrizione = item.ScopoImmersioneId is { } immersioneScopoId && scopiById.TryGetValue(immersioneScopoId, out var immersioneScopo)
                            ? immersioneScopo.Descrizione
                            : string.Empty,
                        Note = item.Note,
                        Partecipazioni = item.Partecipazioni
                            .Select(partecipazione => new ServizioScambioPackagePartecipazioneImmersione
                            {
                                Persona = partecipantiPerServizioId.TryGetValue(partecipazione.ServizioPartecipanteId, out var perId)
                                    ? BuildPersonRef(perId, personaleById)
                                    : new ServizioScambioPackagePersonRef(),
                                TipologiaCodice = partecipazione.TipologiaImmersioneOperativaId is { } tipologiaId && tipologieById.TryGetValue(tipologiaId, out var tipologia) ? tipologia.Codice : string.Empty,
                                TipologiaDescrizione = partecipazione.TipologiaImmersioneOperativaId is { } tipologiaDescrId && tipologieById.TryGetValue(tipologiaDescrId, out var tipologiaDescr) ? tipologiaDescr.Descrizione : string.Empty,
                                ProfonditaMetri = partecipazione.ProfonditaMetri,
                                FasciaDescrizione = partecipazione.FasciaProfonditaId is { } fasciaId && fasceById.TryGetValue(fasciaId, out var fascia) ? fascia.Descrizione : string.Empty,
                                OreImmersione = partecipazione.OreImmersione,
                                CategoriaCodice = partecipazione.CategoriaContabileOreId is { } categoriaId && categorieById.TryGetValue(categoriaId, out var categoria) ? categoria.Codice : string.Empty,
                                CategoriaDescrizione = partecipazione.CategoriaContabileOreId is { } categoriaDescrId && categorieById.TryGetValue(categoriaDescrId, out var categoriaDescr) ? categoriaDescr.Descrizione : string.Empty,
                                Note = partecipazione.Note,
                            })
                            .ToList(),
                    })
                    .ToList(),
                SupportiOccasionali = servizio.SupportiOccasionali
                    .Select(item => new ServizioScambioPackageSupportoOccasionale
                    {
                        Nominativo = item.Nominativo,
                        Qualifica = item.Qualifica,
                        Ruolo = item.Ruolo,
                        Presente = item.Presente,
                        Contatti = item.Contatti,
                        Note = item.Note,
                    })
                    .ToList(),
            },
        };
    }

    private ServizioGiornaliero BuildServizio(
        ServizioScambioPackageService package,
        CataloghiServizioSnapshot cataloghi,
        IReadOnlyList<Personale> personale)
    {
        var peopleResolver = new PeopleResolver(personale);

        var gruppiByCode = cataloghi.GruppiOperativi
            .Where(item => !string.IsNullOrWhiteSpace(item.Codice))
            .ToDictionary(item => Normalize(item.Codice));
        var gruppiByDescription = cataloghi.GruppiOperativi
            .Where(item => !string.IsNullOrWhiteSpace(item.Descrizione))
            .ToDictionary(item => Normalize(item.Descrizione));
        var ruoliByCode = cataloghi.RuoliOperativi
            .Where(item => !string.IsNullOrWhiteSpace(item.Codice))
            .ToDictionary(item => Normalize(item.Codice));
        var ruoliByDescription = cataloghi.RuoliOperativi
            .Where(item => !string.IsNullOrWhiteSpace(item.Descrizione))
            .ToDictionary(item => Normalize(item.Descrizione));
        var tipologieByCode = cataloghi.TipologieImmersione
            .Where(item => !string.IsNullOrWhiteSpace(item.Codice))
            .ToDictionary(item => Normalize(item.Codice));
        var tipologieByDescription = cataloghi.TipologieImmersione
            .Where(item => !string.IsNullOrWhiteSpace(item.Descrizione))
            .ToDictionary(item => Normalize(item.Descrizione));
        var fasceByDescription = cataloghi.FasceProfondita
            .Where(item => !string.IsNullOrWhiteSpace(item.Descrizione))
            .ToDictionary(item => Normalize(item.Descrizione));
        var categorieByCode = cataloghi.CategorieContabiliOre
            .Where(item => !string.IsNullOrWhiteSpace(item.Codice))
            .ToDictionary(item => Normalize(item.Codice));
        var categorieByDescription = cataloghi.CategorieContabiliOre
            .Where(item => !string.IsNullOrWhiteSpace(item.Descrizione))
            .ToDictionary(item => Normalize(item.Descrizione));
        var localitaByDescription = cataloghi.LocalitaOperative
            .Where(item => !string.IsNullOrWhiteSpace(item.Descrizione))
            .ToDictionary(item => Normalize(item.Descrizione));
        var scopiByDescription = cataloghi.ScopiImmersione
            .Where(item => !string.IsNullOrWhiteSpace(item.Descrizione))
            .ToDictionary(item => Normalize(item.Descrizione));
        var unitaBySigla = cataloghi.UnitaNavali
            .Where(item => !string.IsNullOrWhiteSpace(item.Sigla))
            .ToDictionary(item => Normalize(item.Sigla));
        var unitaByDescription = cataloghi.UnitaNavali
            .Where(item => !string.IsNullOrWhiteSpace(item.Descrizione))
            .ToDictionary(item => Normalize(item.Descrizione));

        var partecipanti = package.Partecipanti
            .Select(item =>
            {
                var perId = peopleResolver.Resolve(item.Persona);
                var gruppo = ResolveGruppo(item, gruppiByCode, gruppiByDescription);
                var ruolo = ResolveRuolo(item, ruoliByCode, ruoliByDescription);

                return new ServizioPartecipante
                {
                    PerId = perId,
                    GruppoOperativoId = gruppo.GruppoOperativoId,
                    Presente = item.Presente,
                    RuoloOperativoId = ruolo?.RuoloOperativoId,
                    Note = item.Note?.Trim() ?? string.Empty,
                };
            })
            .ToList();

        var partecipantiByPerId = partecipanti.ToDictionary(item => item.PerId);

        var immersioni = package.Immersioni
            .OrderBy(item => item.NumeroImmersione)
            .Select(item => new ServizioImmersione
            {
                NumeroImmersione = item.NumeroImmersione,
                OrarioInizio = ParseNullableTime(item.OrarioInizio),
                OrarioFine = ParseNullableTime(item.OrarioFine),
                DirettoreImmersionePerId = ResolveRolePersonId(item.DirettoreImmersione, peopleResolver, partecipantiByPerId, "direttore immersione"),
                OperatoreSoccorsoPerId = ResolveRolePersonId(item.OperatoreSoccorso, peopleResolver, partecipantiByPerId, "operatore soccorso"),
                AssistenteBlsdPerId = ResolveRolePersonId(item.AssistenteBlsd, peopleResolver, partecipantiByPerId, "assistenza BLSD"),
                AssistenteSanitarioPerId = ResolveRolePersonId(item.AssistenteSanitario, peopleResolver, partecipantiByPerId, "assistenza sanitaria"),
                LocalitaOperativaId = ResolveOptionalLocalitaId(item.LocalitaDescrizione, localitaByDescription),
                ScopoImmersioneId = ResolveOptionalScopoId(item.ScopoDescrizione, scopiByDescription),
                Note = item.Note?.Trim() ?? string.Empty,
                Partecipazioni = item.Partecipazioni
                    .Select(partecipazione =>
                    {
                        var perId = peopleResolver.Resolve(partecipazione.Persona);
                        if (!partecipantiByPerId.ContainsKey(perId))
                        {
                            throw new InvalidOperationException($"La persona {DescribePerson(partecipazione.Persona)} compare in immersione ma non tra i partecipanti del servizio.");
                        }

                        var tipologia = ResolveTipologia(partecipazione, tipologieByCode, tipologieByDescription);
                        var fascia = ResolveFascia(partecipazione, fasceByDescription);
                        var categoria = ResolveCategoria(partecipazione, categorieByCode, categorieByDescription);

                        return new ServizioPartecipanteImmersione
                        {
                            ServizioPartecipanteId = perId,
                            TipologiaImmersioneOperativaId = tipologia.TipologiaImmersioneOperativaId,
                            ProfonditaMetri = partecipazione.ProfonditaMetri,
                            FasciaProfonditaId = fascia.FasciaProfonditaId,
                            OreImmersione = partecipazione.OreImmersione,
                            CategoriaContabileOreId = categoria.CategoriaContabileOreId,
                            Note = partecipazione.Note?.Trim() ?? string.Empty,
                        };
                    })
                    .ToList(),
            })
            .ToList();

        return new ServizioGiornaliero
        {
            DataServizio = package.DataServizio,
            NumeroOrdineServizio = package.NumeroOrdineServizio?.Trim() ?? string.Empty,
            OrarioServizio = package.OrarioServizio?.Trim() ?? string.Empty,
            StraordinarioAttivo = package.StraordinarioAttivo,
            StraordinarioInizio = package.StraordinarioInizio?.Trim() ?? string.Empty,
            StraordinarioFine = package.StraordinarioFine?.Trim() ?? string.Empty,
            TipoServizio = string.IsNullOrWhiteSpace(package.TipoServizio) ? "InSede" : package.TipoServizio.Trim(),
            LocalitaOperativaId = ResolveOptionalLocalitaId(package.LocalitaDescrizione, localitaByDescription),
            ScopoImmersioneId = ResolveOptionalScopoId(package.ScopoDescrizione, scopiByDescription),
            UnitaNavaleId = ResolveOptionalUnitaId(package.UnitaNavaleSigla, package.UnitaNavaleDescrizione, unitaBySigla, unitaByDescription),
            FuoriSede = package.FuoriSede,
            IndennitaOrdinePubblico = package.IndennitaOrdinePubblico,
            AttivitaSvolta = package.AttivitaSvolta?.Trim() ?? string.Empty,
            Note = package.Note?.Trim() ?? string.Empty,
            Partecipanti = partecipanti,
            Immersioni = immersioni,
            SupportiOccasionali = package.SupportiOccasionali
                .Select(item => new ServizioSupportoOccasionale
                {
                    Nominativo = item.Nominativo?.Trim() ?? string.Empty,
                    Qualifica = item.Qualifica?.Trim() ?? string.Empty,
                    Ruolo = item.Ruolo?.Trim() ?? string.Empty,
                    Presente = item.Presente,
                    Contatti = item.Contatti?.Trim() ?? string.Empty,
                    Note = item.Note?.Trim() ?? string.Empty,
                })
                .ToList(),
        };
    }

    private static ServizioScambioPackagePersonRef BuildPersonRef(
        int perId,
        IReadOnlyDictionary<int, Personale> personaleById,
        int? fallbackPerId = null)
    {
        if (personaleById.TryGetValue(perId, out var personale))
        {
            return new ServizioScambioPackagePersonRef
            {
                PerId = personale.PerId,
                CodiceFiscale = personale.CodiceFiscale,
                MatricolaPersonale = personale.MatricolaPersonale,
                NumeroBrevettoSmz = personale.NumeroBrevettoSmz,
                Cognome = personale.Cognome,
                Nome = personale.Nome,
                Qualifica = personale.Qualifica,
            };
        }

        return new ServizioScambioPackagePersonRef
        {
            PerId = fallbackPerId ?? perId,
        };
    }

    private static ServizioScambioPackagePersonRef? BuildNullablePersonRef(
        int? perId,
        IReadOnlyDictionary<int, Personale> personaleById) =>
        perId is not > 0 ? null : BuildPersonRef(perId.Value, personaleById);

    private static GruppoOperativo ResolveGruppo(
        ServizioScambioPackagePartecipante item,
        IReadOnlyDictionary<string, GruppoOperativo> byCode,
        IReadOnlyDictionary<string, GruppoOperativo> byDescription)
    {
        if (!string.IsNullOrWhiteSpace(item.GruppoCodice) && byCode.TryGetValue(Normalize(item.GruppoCodice), out var byGroupCode))
        {
            return byGroupCode;
        }

        if (!string.IsNullOrWhiteSpace(item.GruppoDescrizione) && byDescription.TryGetValue(Normalize(item.GruppoDescrizione), out var byGroupDescription))
        {
            return byGroupDescription;
        }

        throw new InvalidOperationException($"Gruppo operativo non riconosciuto per {DescribePerson(item.Persona)}.");
    }

    private static RuoloOperativo? ResolveRuolo(
        ServizioScambioPackagePartecipante item,
        IReadOnlyDictionary<string, RuoloOperativo> byCode,
        IReadOnlyDictionary<string, RuoloOperativo> byDescription)
    {
        if (string.IsNullOrWhiteSpace(item.RuoloCodice) && string.IsNullOrWhiteSpace(item.RuoloDescrizione))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(item.RuoloCodice) && byCode.TryGetValue(Normalize(item.RuoloCodice), out var byRoleCode))
        {
            return byRoleCode;
        }

        if (!string.IsNullOrWhiteSpace(item.RuoloDescrizione) && byDescription.TryGetValue(Normalize(item.RuoloDescrizione), out var byRoleDescription))
        {
            return byRoleDescription;
        }

        throw new InvalidOperationException($"Ruolo operativo non riconosciuto per {DescribePerson(item.Persona)}.");
    }

    private static TipologiaImmersioneOperativa ResolveTipologia(
        ServizioScambioPackagePartecipazioneImmersione item,
        IReadOnlyDictionary<string, TipologiaImmersioneOperativa> byCode,
        IReadOnlyDictionary<string, TipologiaImmersioneOperativa> byDescription)
    {
        if (!string.IsNullOrWhiteSpace(item.TipologiaCodice) && byCode.TryGetValue(Normalize(item.TipologiaCodice), out var byTipologiaCode))
        {
            return byTipologiaCode;
        }

        if (!string.IsNullOrWhiteSpace(item.TipologiaDescrizione) && byDescription.TryGetValue(Normalize(item.TipologiaDescrizione), out var byTipologiaDescription))
        {
            return byTipologiaDescription;
        }

        throw new InvalidOperationException($"Tipologia immersione non riconosciuta per {DescribePerson(item.Persona)}.");
    }

    private static FasciaProfondita ResolveFascia(
        ServizioScambioPackagePartecipazioneImmersione item,
        IReadOnlyDictionary<string, FasciaProfondita> byDescription)
    {
        if (!string.IsNullOrWhiteSpace(item.FasciaDescrizione) && byDescription.TryGetValue(Normalize(item.FasciaDescrizione), out var fascia))
        {
            return fascia;
        }

        throw new InvalidOperationException($"Fascia profondita non riconosciuta per {DescribePerson(item.Persona)}.");
    }

    private static CategoriaContabileOre ResolveCategoria(
        ServizioScambioPackagePartecipazioneImmersione item,
        IReadOnlyDictionary<string, CategoriaContabileOre> byCode,
        IReadOnlyDictionary<string, CategoriaContabileOre> byDescription)
    {
        if (!string.IsNullOrWhiteSpace(item.CategoriaCodice) && byCode.TryGetValue(Normalize(item.CategoriaCodice), out var byCategoryCode))
        {
            return byCategoryCode;
        }

        if (!string.IsNullOrWhiteSpace(item.CategoriaDescrizione) && byDescription.TryGetValue(Normalize(item.CategoriaDescrizione), out var byCategoryDescription))
        {
            return byCategoryDescription;
        }

        throw new InvalidOperationException($"Categoria contabile non riconosciuta per {DescribePerson(item.Persona)}.");
    }

    private static int? ResolveOptionalLocalitaId(
        string? descrizione,
        IReadOnlyDictionary<string, LocalitaOperativa> byDescription)
    {
        if (string.IsNullOrWhiteSpace(descrizione))
        {
            return null;
        }

        if (byDescription.TryGetValue(Normalize(descrizione), out var localita))
        {
            return localita.LocalitaOperativaId;
        }

        throw new InvalidOperationException($"Localita operativa non riconosciuta: {descrizione.Trim()}.");
    }

    private static int? ResolveOptionalScopoId(
        string? descrizione,
        IReadOnlyDictionary<string, ScopoImmersioneItem> byDescription)
    {
        if (string.IsNullOrWhiteSpace(descrizione))
        {
            return null;
        }

        if (byDescription.TryGetValue(Normalize(descrizione), out var scopo))
        {
            return scopo.ScopoImmersioneId;
        }

        throw new InvalidOperationException($"Scopo immersione non riconosciuto: {descrizione.Trim()}.");
    }

    private static int? ResolveOptionalUnitaId(
        string? sigla,
        string? descrizione,
        IReadOnlyDictionary<string, UnitaNavale> bySigla,
        IReadOnlyDictionary<string, UnitaNavale> byDescription)
    {
        if (!string.IsNullOrWhiteSpace(sigla) && bySigla.TryGetValue(Normalize(sigla), out var unitaByCode))
        {
            return unitaByCode.UnitaNavaleId;
        }

        if (!string.IsNullOrWhiteSpace(descrizione) && byDescription.TryGetValue(Normalize(descrizione), out var unitaByDescr))
        {
            return unitaByDescr.UnitaNavaleId;
        }

        if (string.IsNullOrWhiteSpace(sigla) && string.IsNullOrWhiteSpace(descrizione))
        {
            return null;
        }

        throw new InvalidOperationException($"Unita navale non riconosciuta: {(string.IsNullOrWhiteSpace(sigla) ? descrizione?.Trim() : sigla.Trim())}.");
    }

    private static int? ResolveRolePersonId(
        ServizioScambioPackagePersonRef? person,
        PeopleResolver resolver,
        IReadOnlyDictionary<int, ServizioPartecipante> partecipantiByPerId,
        string ruolo)
    {
        if (person is null)
        {
            return null;
        }

        var perId = resolver.Resolve(person);
        if (!partecipantiByPerId.ContainsKey(perId))
        {
            throw new InvalidOperationException($"La persona {DescribePerson(person)} e indicata come {ruolo} ma non compare tra i partecipanti del servizio.");
        }

        return perId;
    }

    private static TimeOnly? ParseNullableTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return TimeOnly.Parse(value.Trim());
    }

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();

    private static string DescribePerson(ServizioScambioPackagePersonRef person)
    {
        var nominativo = $"{person.Cognome} {person.Nome}".Trim();
        if (!string.IsNullOrWhiteSpace(nominativo))
        {
            return nominativo;
        }

        if (!string.IsNullOrWhiteSpace(person.CodiceFiscale))
        {
            return person.CodiceFiscale.Trim().ToUpperInvariant();
        }

        return person.PerId is { } perId ? $"PerID {perId}" : "persona non identificata";
    }

    private sealed class PeopleResolver
    {
        private readonly Dictionary<int, Personale> _byPerId;
        private readonly Dictionary<string, Personale> _byCodiceFiscale;
        private readonly Dictionary<string, Personale> _byMatricola;
        private readonly Dictionary<string, Personale> _byBrevetto;
        private readonly Dictionary<string, Personale> _byNomeUnivoco;

        public PeopleResolver(IReadOnlyList<Personale> personale)
        {
            _byPerId = personale.ToDictionary(item => item.PerId);
            _byCodiceFiscale = BuildUniqueMap(personale, item => item.CodiceFiscale);
            _byMatricola = BuildUniqueMap(personale, item => item.MatricolaPersonale);
            _byBrevetto = BuildUniqueMap(personale, item => item.NumeroBrevettoSmz);
            _byNomeUnivoco = personale
                .GroupBy(item => Normalize($"{item.Cognome}|{item.Nome}"))
                .Where(group => group.Count() == 1)
                .ToDictionary(group => group.Key, group => group.First());
        }

        public int Resolve(ServizioScambioPackagePersonRef reference)
        {
            if (!string.IsNullOrWhiteSpace(reference.CodiceFiscale)
                && _byCodiceFiscale.TryGetValue(Normalize(reference.CodiceFiscale), out var byCodiceFiscale))
            {
                return byCodiceFiscale.PerId;
            }

            if (!string.IsNullOrWhiteSpace(reference.MatricolaPersonale)
                && _byMatricola.TryGetValue(Normalize(reference.MatricolaPersonale), out var byMatricola))
            {
                return byMatricola.PerId;
            }

            if (!string.IsNullOrWhiteSpace(reference.NumeroBrevettoSmz)
                && _byBrevetto.TryGetValue(Normalize(reference.NumeroBrevettoSmz), out var byBrevetto))
            {
                return byBrevetto.PerId;
            }

            if (reference.PerId is { } perId && _byPerId.ContainsKey(perId))
            {
                return perId;
            }

            var nomeKey = Normalize($"{reference.Cognome}|{reference.Nome}");
            if (!string.IsNullOrWhiteSpace(reference.Cognome)
                && !string.IsNullOrWhiteSpace(reference.Nome)
                && _byNomeUnivoco.TryGetValue(nomeKey, out var byNome))
            {
                return byNome.PerId;
            }

            throw new InvalidOperationException($"Personale non trovato in anagrafica: {DescribePerson(reference)}.");
        }

        private static Dictionary<string, Personale> BuildUniqueMap(
            IEnumerable<Personale> personale,
            Func<Personale, string> keySelector)
        {
            return personale
                .Where(item => !string.IsNullOrWhiteSpace(keySelector(item)))
                .GroupBy(item => Normalize(keySelector(item)))
                .Where(group => group.Count() == 1)
                .ToDictionary(group => group.Key, group => group.First());
        }
    }
}
