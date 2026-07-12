using Microsoft.Data.Sqlite;
using SMZ.Conta.App.Data;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.Tests;

internal static class Program
{
    private static int Main()
    {
        var testRoot = Path.Combine(Path.GetTempPath(), "smz-conta-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testRoot);
        Environment.SetEnvironmentVariable(DatabasePaths.AppDataDirectoryEnvironmentVariable, testRoot);

        try
        {
            DatabaseInitializer.EnsureDatabase();

            Run("database isolato", TestDatabaseIsolato);
            Run("salvataggio e lettura anagrafica", TestSalvataggioELetturaPersonale);
            Run("salvataggio e lettura servizio con immersione", TestSalvataggioELetturaServizio);

            Console.WriteLine("Tutti i test SMZ sono passati.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }
        finally
        {
            Environment.SetEnvironmentVariable(DatabasePaths.AppDataDirectoryEnvironmentVariable, null);
            SqliteConnection.ClearAllPools();

            try
            {
                if (Directory.Exists(testRoot))
                {
                    Directory.Delete(testRoot, recursive: true);
                }
            }
            catch
            {
                Console.Error.WriteLine($"Cartella temporanea test non eliminata: {testRoot}");
            }
        }
    }

    private static void Run(string name, Action test)
    {
        test();
        Console.WriteLine($"OK - {name}");
    }

    private static void TestDatabaseIsolato()
    {
        AssertTrue(File.Exists(DatabasePaths.DatabasePath), "Il database di test non e stato creato.");
        AssertTrue(
            DatabasePaths.DatabasePath.Contains(Path.Combine("smz-conta-tests"), StringComparison.OrdinalIgnoreCase),
            $"Il database non punta alla cartella temporanea: {DatabasePaths.DatabasePath}");
    }

    private static void TestSalvataggioELetturaPersonale()
    {
        var repository = new PersonaleRepository();
        var personale = CreaPersonale(101, "Rossi", "Mario", "RSSMRA80A01H501U", "mario.rossi");

        var perId = repository.SavePersonale(personale, isNewRecord: true);
        var loaded = repository.GetPersonaleById(perId) ?? throw new InvalidOperationException("Personale non riletto.");

        AssertEqual(101, loaded.PerId, "PerId");
        AssertEqual("Rossi", loaded.Cognome, "Cognome");
        AssertEqual("Mario", loaded.Nome, "Nome");
        AssertEqual("Sovrintendente", loaded.Qualifica, "Qualifica");
        AssertEqual("RSSMRA80A01H501U", loaded.CodiceFiscale, "Codice fiscale");
        AssertEqual("mario.rossi", loaded.Mail1Utente, "Mail primaria");

        var search = repository.SearchPersonale("rossi", null, null);
        AssertTrue(search.Any(item => item.PerId == 101), "Ricerca personale non trova la scheda salvata.");
    }

    private static void TestSalvataggioELetturaServizio()
    {
        var repository = new PersonaleRepository();
        repository.SavePersonale(CreaPersonale(201, "Bianchi", "Luca", "BNCLCU81A01H501J", "luca.bianchi"), isNewRecord: true);
        repository.SavePersonale(CreaPersonale(202, "Verdi", "Anna", "VRDNNA82A41H501K", "anna.verdi"), isNewRecord: true);
        repository.SavePersonale(CreaSanitario(203, "Neri", "Paolo", "NREPLA83A01H501L", "paolo.neri"), isNewRecord: true);

        var cataloghi = repository.GetCataloghiServizio();
        var gruppoSmz = cataloghi.GruppiOperativi.Single(item => item.Codice == "SMZ");
        var ruoloOperatore = cataloghi.RuoliOperativi.Single(item => item.Codice == "OPERATORE");
        var localita = cataloghi.LocalitaOperative.First();
        var scopo = cataloghi.ScopiImmersione.First();
        var unita = cataloghi.UnitaNavali.First();
        var tipologia = cataloghi.TipologieImmersione.Single(item => item.Codice == "ARA_ASAS");
        var fascia = cataloghi.FasceProfondita.Single(item => item.Descrizione == "00/12");
        var categoria = cataloghi.CategorieContabiliOre.Single(item => item.Codice == "ORD");

        var servizio = new ServizioGiornaliero
        {
            DataServizio = new DateOnly(2026, 3, 18),
            NumeroOrdineServizio = "TEST-001",
            OrarioServizio = "08.00/14.00",
            TipoServizio = "FuoriSede",
            LocalitaOperativaId = localita.LocalitaOperativaId,
            ScopoImmersioneId = scopo.ScopoImmersioneId,
            UnitaNavaleId = unita.UnitaNavaleId,
            ResponsabileServizioPerId = 201,
            FuoriSede = true,
            AttivitaSvolta = "Test automatico",
            Partecipanti =
            [
                new ServizioPartecipante
                {
                    PerId = 201,
                    GruppoOperativoId = gruppoSmz.GruppoOperativoId,
                    Presente = true,
                    RuoloOperativoId = ruoloOperatore.RuoloOperativoId,
                },
                new ServizioPartecipante
                {
                    PerId = 202,
                    GruppoOperativoId = gruppoSmz.GruppoOperativoId,
                    Presente = true,
                    RuoloOperativoId = ruoloOperatore.RuoloOperativoId,
                },
                new ServizioPartecipante
                {
                    PerId = 203,
                    GruppoOperativoId = gruppoSmz.GruppoOperativoId,
                    Presente = true,
                    RuoloOperativoId = ruoloOperatore.RuoloOperativoId,
                },
            ],
            Immersioni =
            [
                new ServizioImmersione
                {
                    NumeroImmersione = 1,
                    OrarioInizio = new TimeOnly(9, 15),
                    OrarioFine = new TimeOnly(10, 5),
                    DirettoreImmersionePerId = 201,
                    LocalitaOperativaId = localita.LocalitaOperativaId,
                    ScopoImmersioneId = scopo.ScopoImmersioneId,
                    Partecipazioni =
                    [
                        new ServizioPartecipanteImmersione
                        {
                            ServizioPartecipanteId = 202,
                            TipologiaImmersioneOperativaId = tipologia.TipologiaImmersioneOperativaId,
                            ProfonditaMetri = 10,
                            FasciaProfonditaId = fascia.FasciaProfonditaId,
                            OreImmersione = 1.5m,
                            CategoriaContabileOreId = categoria.CategoriaContabileOreId,
                        },
                    ],
                },
            ],
            SupportiOccasionali =
            [
                new ServizioSupportoOccasionale
                {
                    Nominativo = "Gialli Sara",
                    Qualifica = "Assistente",
                    Ruolo = "Assistenza SMZ",
                    Presente = true,
                },
            ],
        };

        var servizioId = repository.SaveServizioGiornaliero(servizio);
        var loaded = repository.GetServizioGiornalieroById(servizioId)
            ?? throw new InvalidOperationException("Servizio non riletto.");

        AssertEqual(new DateOnly(2026, 3, 18), loaded.DataServizio, "Data servizio");
        AssertEqual("TEST-001", loaded.NumeroOrdineServizio, "Numero ordine servizio");
        AssertTrue(loaded.FuoriSede, "Indennita fuori sede non riletta dal servizio.");
        AssertEqual(3, loaded.Partecipanti.Count, "Numero partecipanti");
        AssertEqual(1, loaded.Immersioni.Count, "Numero immersioni");
        AssertEqual(1, loaded.SupportiOccasionali.Count, "Numero supporti occasionali");

        var immersione = loaded.Immersioni.Single();
        AssertEqual(new TimeOnly(9, 15), immersione.OrarioInizio, "Orario inizio immersione");
        AssertEqual(new TimeOnly(10, 5), immersione.OrarioFine, "Orario fine immersione");
        AssertEqual(201, immersione.DirettoreImmersionePerId, "Direttore immersione");
        AssertEqual(1, immersione.Partecipazioni.Count, "Numero partecipazioni immersione");
        AssertEqual(10, immersione.Partecipazioni.Single().ProfonditaMetri, "Profondita immersione");
        AssertEqual(1.5m, immersione.Partecipazioni.Single().OreImmersione, "Ore immersione");

        var fuoriSede = repository.GetIndennitaFuoriSedeMensile(2026, 3);
        AssertEqual(3, fuoriSede.Count, "Operatori fuori sede");
        AssertTrue(fuoriSede.All(item => item.GiornateImpiego == 1), "Conteggio giornate fuori sede non corretto.");
        AssertTrue(fuoriSede.All(item => item.DateServizio.Single() == new DateOnly(2026, 3, 18)), "Date fuori sede non corrette.");

        var contabilita = repository.GetContabilitaGiornateImpiego(2026, 3);
        AssertTrue(contabilita.Sanitari.Any(item => item.PerId == 203 && item.TrentesimiMaturati == 1), "Sanitario non conteggiato nei trentesimi.");
        AssertTrue(contabilita.SupportiOccasionali.Any(item => item.Nominativo == "Gialli Sara" && item.TrentesimiMaturati == 1), "Assistenza SMZ non conteggiata nei trentesimi.");
    }

    private static Personale CreaPersonale(int perId, string cognome, string nome, string codiceFiscale, string mail)
    {
        return new Personale
        {
            PerId = perId,
            Cognome = cognome,
            Nome = nome,
            Qualifica = "Sovrintendente",
            ProfiloPersonale = ProfiliPersonaleCatalogo.OperatoreSubacqueo,
            CodiceFiscale = codiceFiscale,
            StatoServizio = StatoServizioPersonaleCatalogo.Attivo,
            Mail1Utente = mail,
        };
    }

    private static Personale CreaSanitario(int perId, string cognome, string nome, string codiceFiscale, string mail)
    {
        var personale = CreaPersonale(perId, cognome, nome, codiceFiscale, mail);
        personale.ProfiloPersonale = ProfiliPersonaleCatalogo.Sanitario;
        personale.RuoloSanitario = "Sanitario";
        return personale;
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void AssertEqual<T>(T expected, T actual, string fieldName)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{fieldName}: atteso '{expected}', trovato '{actual}'.");
        }
    }
}
