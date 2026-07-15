using Microsoft.Data.Sqlite;
using SMZ.Conta.App.Data;
using SMZ.Conta.App.Models;
using SMZ.Conta.App.Printing;
using SMZ.Conta.App.ViewModels;

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
            Run("ripartizione straordinario stampa servizio", TestRipartizioneStraordinarioStampa);
            Run("autenticazione e ruoli di accesso", TestAccessi);

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

        var riepilogoSalvato = repository.GetServiziGiornalieriRecenti()
            .Single(item => item.ServizioGiornalieroId == servizioId);
        AssertEqual("A.R.A./ASAS", riepilogoSalvato.ApparatiDescrizione, "Apparati nel riepilogo servizi salvati");
        AssertEqual(10, riepilogoSalvato.ProfonditaMassimaMetri, "Profondita nel riepilogo servizi salvati");
        AssertEqual(1.5m, riepilogoSalvato.OreImmersioneTotali, "Ore nel riepilogo servizi salvati");
        AssertEqual("ORE ORD", riepilogoSalvato.CategorieOreDescrizione, "Categoria nel riepilogo servizi salvati");

        var viewModel = new MainWindowViewModel
        {
            SelectedServizioSalvato = riepilogoSalvato,
        };
        viewModel.OpenServizioCommand.Execute(null);
        var dettaglioRiaperto = viewModel.ServizioPartecipazioniContabiliUnicheBozza.Single(item => item.PerId == 202);
        AssertEqual(tipologia.TipologiaImmersioneOperativaId, dettaglioRiaperto.TipologiaImmersioneOperativa?.TipologiaImmersioneOperativaId, "Apparato dopo riapertura servizio");
        AssertEqual("10", dettaglioRiaperto.ProfonditaMetri, "Profondita dopo riapertura servizio");
        AssertEqual("1,5", dettaglioRiaperto.OreImmersione.Replace('.', ','), "Ore dopo riapertura servizio");
        AssertEqual(categoria.CategoriaContabileOreId, dettaglioRiaperto.CategoriaContabileOre?.CategoriaContabileOreId, "Categoria dopo riapertura servizio");

        var fuoriSede = repository.GetIndennitaFuoriSedeMensile(2026, 3);
        AssertEqual(3, fuoriSede.Count, "Operatori fuori sede");
        AssertTrue(fuoriSede.All(item => item.GiornateImpiego == 1), "Conteggio giornate fuori sede non corretto.");
        AssertTrue(fuoriSede.All(item => item.DateServizio.Single() == new DateOnly(2026, 3, 18)), "Date fuori sede non corrette.");

        var contabilita = repository.GetContabilitaGiornateImpiego(2026, 3);
        AssertTrue(contabilita.Sanitari.Any(item => item.PerId == 203 && item.TrentesimiMaturati == 1), "Sanitario non conteggiato nei trentesimi.");
        AssertTrue(contabilita.SupportiOccasionali.Any(item => item.Nominativo == "Gialli Sara" && item.TrentesimiMaturati == 1), "Assistenza SMZ non conteggiata nei trentesimi.");
    }

    private static void TestRipartizioneStraordinarioStampa()
    {
        var feriale = ServizioGiornalieroPrintService.CalcolaStraordinario(new ServizioGiornaliero
        {
            DataServizio = new DateOnly(2026, 3, 18),
            StraordinarioAttivo = true,
            StraordinarioInizio = "20:00",
            StraordinarioFine = "23:30",
        });
        AssertEqual(2m, feriale.Feriali, "Straordinario feriale");
        AssertEqual(1.5m, feriale.Notturne, "Straordinario notturno");
        AssertEqual(0m, feriale.Festive, "Straordinario festivo non dovuto");

        var festivoConPassaggioGiorno = ServizioGiornalieroPrintService.CalcolaStraordinario(new ServizioGiornaliero
        {
            DataServizio = new DateOnly(2026, 3, 22),
            StraordinarioAttivo = true,
            StraordinarioInizio = "23:00",
            StraordinarioFine = "07:00",
        });
        AssertEqual(1m, festivoConPassaggioGiorno.FestiveNotturne, "Straordinario festivo notturno");
        AssertEqual(6m, festivoConPassaggioGiorno.Notturne, "Straordinario notturno dopo mezzanotte");
        AssertEqual(1m, festivoConPassaggioGiorno.Feriali, "Straordinario feriale diurno dopo mezzanotte");

        var pasquetta = ServizioGiornalieroPrintService.CalcolaStraordinario(new ServizioGiornaliero
        {
            DataServizio = new DateOnly(2026, 4, 6),
            StraordinarioAttivo = true,
            StraordinarioInizio = "08:00",
            StraordinarioFine = "10:00",
        });
        AssertEqual(2m, pasquetta.Festive, "Straordinario nel lunedi di Pasqua");
    }

    private static void TestAccessi()
    {
        AssertEqual("PerID 80262", new AccessSession(80262, "PerID 80262", AccessRole.Administrator, false).UserDisplayName,
            "Visualizzazione PerID senza anagrafica");
        var accessService = new AccessService();
        AssertTrue(!accessService.HasUsers(), "Il database di test non dovrebbe contenere account iniziali.");

        var administrator = accessService.CreateFirstAdministrator(101, "PasswordAdmin!2026");
        AssertTrue(administrator.IsAdministrator, "Il primo account non e amministratore.");
        AssertThrows(() => accessService.Authenticate(101, "password-errata"), "Una password errata e stata accettata.");

        var authenticatedAdministrator = accessService.Authenticate(101, "PasswordAdmin!2026");
        AssertEqual(101, authenticatedAdministrator.PerId, "PerID amministratore autenticato");

        accessService.CreateUser(101, 201, AccessRole.Base, "PasswordBase!2026");
        var baseSession = accessService.Authenticate(201, "PasswordBase!2026");
        AssertTrue(!baseSession.IsAdministrator, "L'account Base risulta amministratore.");
        AssertTrue(baseSession.MustChangePassword, "Il cambio password iniziale non e richiesto.");

        accessService.ChangePassword(201, "PasswordBase!2026", "NuovaPasswordBase!2026");
        baseSession = accessService.Authenticate(201, "NuovaPasswordBase!2026");
        AssertTrue(!baseSession.MustChangePassword, "Il cambio password richiesto non e stato azzerato.");

        var baseViewModel = new MainWindowViewModel(baseSession);
        baseViewModel.SezioneAttivaIndex = 0;
        AssertEqual(2, baseViewModel.SezioneAttivaIndex, "Sezione consentita al profilo Base");
        AssertTrue(!baseViewModel.IsWelcomeVisible, "Il profilo Base non entra direttamente nella sezione Personale.");
        AssertTrue(!baseViewModel.DeleteCommand.CanExecute(null), "Il profilo Base puo cessare il personale.");

        accessService.SetUserRole(101, 201, AccessRole.Administrator);
        AssertTrue(accessService.GetUsers().Single(user => user.PerId == 201).Role == AccessRole.Administrator,
            "Il cambio ruolo non e stato salvato.");
        accessService.SetUserActive(101, 201, false);
        AssertThrows(() => accessService.Authenticate(201, "NuovaPasswordBase!2026"), "Un account sospeso ha effettuato l'accesso.");
        AssertThrows(() => accessService.SetUserActive(101, 101, false), "L'amministratore ha sospeso il proprio account.");
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

    private static void AssertThrows(Action action, string message)
    {
        try
        {
            action();
        }
        catch
        {
            return;
        }

        throw new InvalidOperationException(message);
    }
}
