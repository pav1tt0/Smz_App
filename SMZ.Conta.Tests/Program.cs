using System.IO.Compression;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using SMZ.Conta.App.Infrastructure;
using Microsoft.Data.Sqlite;
using SMZ.Conta.App.Data;
using SMZ.Conta.App.Models;
using SMZ.Conta.App.Printing;
using SMZ.Conta.App.ViewModels;

namespace SMZ.Conta.Tests;

internal static partial class Program
{
    private static readonly Version MinimumSupportedSqliteVersion = new(3, 50, 2);

    [STAThread]
    private static int Main()
    {
        _ = new Application();
        var testRoot = Path.Combine(Path.GetTempPath(), "smz-conta-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testRoot);
        Environment.SetEnvironmentVariable(DatabasePaths.AppDataDirectoryEnvironmentVariable, testRoot);

        try
        {
            DatabaseInitializer.EnsureDatabase();

            Run("database isolato", TestDatabaseIsolato);
            Run("separazione dati pacchetto test", TestProfiloDatiPacchetto);
            Run("ridisposizione campi e ripristino layout", TestAdaptiveGrid);
            Run("schede adattate alla larghezza", TestAdaptiveCards);
            Run("preferenza dimensione interfaccia isolata", TestDisplayPreferences);
            Run("versione SQLite sicura", TestVersioneSqliteSicura);
            Run("numero ordine servizio automatico", TestNumeroOrdineServizioAutomatico);
            Run("abbreviazioni qualifiche e gerarchia", TestAbbreviazioniQualifiche);
            Run("salvataggio e lettura anagrafica", TestSalvataggioELetturaPersonale);
            Run("date abilitazioni facoltative", TestDateAbilitazioniFacoltative);
            Run("abilitazione unica per persona nell'editor", TestAbilitazioniUnicheEditor);
            Run("rifiuto duplicati prima del salvataggio", TestAbilitazioniUnicheRepository);
            Run("visita bimestrale disattivata", TestVisitaBimestraleDisattivata);
            Run("avvisi bimestrali con nomi alternativi", TestAvvisiBimestraliNomiAlternativi);
            Run("visite facoltative nell'anagrafica", TestVisiteFacoltative);
            Run("conservazione visite gia registrate", TestConservazioneVisite);
            Run("salvataggio e lettura servizio con immersione", TestSalvataggioELetturaServizio);
            Run("personale ordinato per qualifica in elenchi e servizi", TestOrdinePersonalePerQualifica);
            Run("personale su due colonne in ordine verticale", TestPersonaleColonneVerticali);
            Run("tre natanti salvati riaperti stampati e scambiati", TestTreNatantiServizio);
            Run("scelta immersione nel riepilogo e riapertura", TestSceltaImmersioneRiepilogo);
            Run("numeri e descrizioni degli incarichi nel riepilogo", TestIncarichiNelRiepilogo);
            Run("incarico e partecipazione in immersioni diverse", TestIncaricoEPartecipazioneSeparati);
            Run("tariffe finali in riepilogo e mensile per interni ed esterni", TestTariffeFinaliImmersioni);
            Run("tariffe personalizzate senza doppi coefficienti", TestTariffeFinaliPersonalizzate);
            Run("aggiornamento del solo catalogo tariffario originale", TestAggiornamentoTariffeOriginali);
            Run("conservazione dei cataloghi tariffari personalizzati", TestConservazioneTariffePersonalizzate);
            Run("chiusure mensili conservate e rigenerate su richiesta", TestRigenerazioneImportiMensili);
            Run("ripartizione straordinario stampa servizio", TestRipartizioneStraordinarioStampa);
            Run("autenticazione e ruoli di accesso", TestAccessi);
            Run("ripristino backup sicuro", TestRipristinoBackupSicuro);
            Run("installazione con dati e account esistenti", TestOperationalDatabaseSetup);
            Run("importazione comprende dati SQLite WAL", TestOperationalDatabaseWal);
            Run("avvio usa il database installato senza ripiego", TestInstalledDatabaseConfiguration);

            Console.WriteLine("Tutti i test SMZ sono passati.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
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

    private static void TestProfiloDatiPacchetto()
    {
        var originalOverride = Environment.GetEnvironmentVariable(DatabasePaths.AppDataDirectoryEnvironmentVariable);
        var originalProfile = AppContext.GetData(DatabasePaths.DataProfileAppContextKey);
        try
        {
            Environment.SetEnvironmentVariable(DatabasePaths.AppDataDirectoryEnvironmentVariable, null);
            AppContext.SetData(DatabasePaths.DataProfileAppContextKey, null);
            var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            AssertEqual(Path.Combine(localData, "SMZ", "Conta"), DatabasePaths.AppDataDirectory, "Percorso versione ordinaria");

            AppContext.SetData(DatabasePaths.DataProfileAppContextKey, "Test-MarioRossi-12345");
            AssertEqual(Path.Combine(localData, "SMZ", "Test-MarioRossi-12345"), DatabasePaths.AppDataDirectory, "Percorso pacchetto test");

            Environment.SetEnvironmentVariable(DatabasePaths.AppDataDirectoryEnvironmentVariable, originalOverride);
            AssertEqual(originalOverride, DatabasePaths.AppDataDirectory, "Priorita del database isolato");

            Environment.SetEnvironmentVariable(DatabasePaths.AppDataDirectoryEnvironmentVariable, null);
            AppContext.SetData(DatabasePaths.DataProfileAppContextKey, "..\\Conta");
            AssertThrows(() => _ = DatabasePaths.AppDataDirectory, "Accettato un profilo dati esterno alla cartella SMZ.");
        }
        finally
        {
            AppContext.SetData(DatabasePaths.DataProfileAppContextKey, originalProfile);
            Environment.SetEnvironmentVariable(DatabasePaths.AppDataDirectoryEnvironmentVariable, originalOverride);
        }
    }

    private static void TestVersioneSqliteSicura()
    {
        using var connection = new SqliteConnection($"Data Source={DatabasePaths.DatabasePath}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT sqlite_version();";
        var versionText = Convert.ToString(command.ExecuteScalar()) ?? string.Empty;

        AssertTrue(
            Version.TryParse(versionText, out var sqliteVersion),
            $"Versione SQLite non riconosciuta: {versionText}");
        AssertTrue(
            sqliteVersion >= MinimumSupportedSqliteVersion,
            $"SQLite {sqliteVersion} non e sicuro: richiesta almeno la versione {MinimumSupportedSqliteVersion}.");
        Console.WriteLine($"SQLite runtime: {sqliteVersion}");
    }

    private static void TestAdaptiveGrid()
    {
        var grid = new AdaptiveGrid { CollapseBelow = 760, MinItemWidth = 240, MaxColumns = 2 };
        var originalColumn = new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) };
        var originalRow = new RowDefinition { Height = GridLength.Auto };
        grid.ColumnDefinitions.Add(originalColumn);
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.RowDefinitions.Add(originalRow);
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var person = new Personale { Nome = "Nome iniziale" };
        var editor = new TextBox { DataContext = person, Margin = new Thickness(1, 2, 3, 4) };
        editor.SetBinding(TextBox.TextProperty, new Binding(nameof(Personale.Nome)) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
        var second = new TextBox();
        Grid.SetColumn(second, 2);
        var footer = new TextBlock { Text = "Indicazioni" };
        Grid.SetRow(footer, 1);
        Grid.SetColumnSpan(footer, 3);
        grid.Children.Add(editor);
        grid.Children.Add(second);
        grid.Children.Add(footer);

        foreach (var width in new[] { 400, 600, 1200, 400, 1200 })
        {
            grid.Measure(new Size(width, double.PositiveInfinity));
            grid.Arrange(new Rect(0, 0, width, grid.DesiredSize.Height));
            if (width == 400)
            {
                AssertEqual(1, grid.ColumnDefinitions.Count, "Campi su una colonna");
                AssertEqual(1, Grid.GetRow(second), "Secondo campo sotto il primo");
            }
            else if (width == 600)
            {
                AssertEqual(2, grid.ColumnDefinitions.Count, "Campi su due colonne");
                AssertEqual(2, Grid.GetColumnSpan(footer), "Indicazioni su tutta la riga");
            }
            else
            {
                AssertTrue(ReferenceEquals(originalColumn, grid.ColumnDefinitions[0]), "Definizioni colonne non ripristinate.");
                AssertTrue(ReferenceEquals(originalRow, grid.RowDefinitions[0]), "Definizioni righe non ripristinate.");
                AssertEqual(2, Grid.GetColumn(second), "Posizione originale ripristinata");
                AssertEqual(new Thickness(1, 2, 3, 4), editor.Margin, "Margini originali ripristinati");
            }
        }
        editor.Text = "Nome modificato";
        editor.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
        AssertEqual("Nome modificato", person.Nome, "Binding conservato durante i ridimensionamenti");
    }

    private static void TestAdaptiveCards()
    {
        var panel = new AdaptiveUniformGrid { MaxColumns = 5, MinItemWidth = 230 };
        for (var i = 0; i < 5; i++) panel.Children.Add(new Button { Content = $"Modulo {i}" });
        foreach (var (width, columns) in new[] { (1200, 5), (900, 3), (400, 1), (1200, 5) })
        {
            panel.Measure(new Size(width, double.PositiveInfinity));
            panel.Arrange(new Rect(0, 0, width, panel.DesiredSize.Height));
            AssertEqual(columns, panel.Columns, "Numero colonne delle schede");
            AssertTrue(panel.Children.Cast<FrameworkElement>().All(item => item.ActualWidth >= 230), "Scheda compressa.");
        }
    }

    private static void TestDisplayPreferences()
    {
        var previous = Environment.GetEnvironmentVariable(DatabasePaths.AppDataDirectoryEnvironmentVariable);
        var isolated = Path.Combine(previous!, "display-preferences");
        Environment.SetEnvironmentVariable(DatabasePaths.AppDataDirectoryEnvironmentVariable, isolated);
        try
        {
            AssertEqual(100, DisplayPreferences.LoadZoom(), "Dimensione iniziale");
            DisplayPreferences.SaveZoom(120);
            AssertEqual(120, DisplayPreferences.LoadZoom(), "Preferenza riletta");
            DisplayPreferences.SaveZoom(999);
            AssertEqual(150, DisplayPreferences.LoadZoom(), "Limite superiore");
            DisplayPreferences.SaveZoom(1);
            AssertEqual(70, DisplayPreferences.LoadZoom(), "Limite inferiore");
            File.WriteAllText(Path.Combine(isolated, "display-settings.json"), "file incompleto");
            AssertEqual(100, DisplayPreferences.LoadZoom(), "Ripristino dopo file incompleto");
            File.WriteAllText(Path.Combine(isolated, "display-settings.json"), "{}");
            AssertEqual(100, DisplayPreferences.LoadZoom(), "Ripristino dopo preferenza mancante");
        }
        finally
        {
            Environment.SetEnvironmentVariable(DatabasePaths.AppDataDirectoryEnvironmentVariable, previous);
        }
    }

    private static void TestNumeroOrdineServizioAutomatico()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.ServizioData = "01/01/2026";
        AssertEqual("001", viewModel.ServizioNumeroOrdine, "Numero ordine primo giorno dell'anno");

        viewModel.ServizioData = "29/02/2024";
        AssertEqual("060", viewModel.ServizioNumeroOrdine, "Numero ordine in anno bisestile");

        viewModel.ServizioData = "31/12/2024";
        AssertEqual("366", viewModel.ServizioNumeroOrdine, "Numero ordine ultimo giorno di anno bisestile");

        viewModel.ServizioNumeroOrdine = "MANUALE";
        viewModel.ServizioData = "01/03/2024";
        AssertEqual("MANUALE", viewModel.ServizioNumeroOrdine, "Numero ordine modificato manualmente");
    }

    private static void TestAbbreviazioniQualifiche()
    {
        AssertEqual(string.Empty, QualificaFormatter.Abbrevia(null), "Qualifica assente");
        AssertEqual("Isp.", QualificaFormatter.Abbrevia("Ispettore"), "Sigla ispettore");
        AssertEqual("Ass.C.C.", QualificaFormatter.Abbrevia(" Assistente Capo Coordinatore "), "Alias coordinatore");
        AssertEqual("Sost.C.C.T.", QualificaFormatter.Abbrevia("Sostituto Commissario Coordinatore Tecnico"), "Qualifica tecnica coordinatore");
        AssertEqual("Qualifica esterna", QualificaFormatter.Abbrevia("Qualifica esterna"), "Qualifica fuori catalogo conservata");
        AssertEqual(10, QualificaFormatter.GetGerarchiaOrdine("Ispettore", false), "Gerarchia distinta dalle sigle");
        AssertEqual(20, QualificaFormatter.GetGerarchiaOrdine("Assistente Capo Coordinatore", false), "Gerarchia coordinatore");
        AssertEqual(10_000, QualificaFormatter.GetGerarchiaOrdine("Medico Capo", true), "Gerarchia sanitaria");
        AssertTrue(QualificaFormatter.GetGerarchiaOrdine("Primo Dirigente", false) < QualificaFormatter.GetGerarchiaOrdine("Vice Questore", false), "Gerarchia nuove qualifiche errata.");
        AssertTrue(QualificaFormatter.GetGerarchiaOrdine("Vice Questore", false) < QualificaFormatter.GetGerarchiaOrdine("Vice Questore Aggiunto", false), "Vice Questore fuori ordine.");

        var viewModel = new MainWindowViewModel();
        foreach (var qualifica in viewModel.QualificheDisponibili)
        {
            AssertTrue(QualificaFormatter.Abbrevia(qualifica) != qualifica, $"Sigla mancante per {qualifica}.");
        }

        var row = new ServizioPartecipanteDraftViewModel { Qualifica = "Ispettore" };
        var notificata = false;
        row.PropertyChanged += (_, e) => notificata |= e.PropertyName == nameof(row.QualificaBreve);
        row.Qualifica = "Assistente Capo";
        AssertEqual("Ass.C.", row.QualificaBreve, "Aggiornamento sigla nel selettore");
        AssertEqual("Assistente Capo", row.Qualifica, "Qualifica estesa conservata");
        AssertTrue(notificata, "La sigla non notifica l'aggiornamento dell'interfaccia.");
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

    private static void TestDateAbilitazioniFacoltative()
    {
        var viewModel = new MainWindowViewModel
        {
            PerIdInput = "109",
            Cognome = "Test",
            Nome = "Abilitazioni senza date",
            CodiceFiscale = "TSTABL80A01H501A",
        };

        foreach (var tipo in viewModel.TipiAbilitazioneCatalogo.ToList())
        {
            viewModel.AbilitazioneTipoSelezionato = tipo;
            if (tipo.RichiedeScadenza)
            {
                AssertTrue(viewModel.AbilitazioneIndicazioni.Contains("Data scadenza facoltativa"), "Indicazioni scadenza non aggiornate.");
            }

            viewModel.SaveAbilitazioneCommand.Execute(null);
            AssertTrue(viewModel.Abilitazioni.Any(item => item.TipoAbilitazioneId == tipo.TipoAbilitazioneId), $"Abilitazione senza date non aggiunta: {tipo.Descrizione}.");
        }

        var loaded = SalvaSchedaPerTest(viewModel);
        AssertEqual(viewModel.TipiAbilitazioneCatalogo.Count, loaded.Abilitazioni.Count, "Abilitazioni senza date salvate");
        AssertTrue(loaded.Abilitazioni.All(item => item.DataConseguimento is null && item.DataScadenza is null), "Date vuote non conservate.");

        foreach (var tipo in viewModel.TipiAbilitazioneCatalogo.Where(item => item.RichiedeScadenza).ToList())
        {
            viewModel.SelectedAbilitazione = viewModel.Abilitazioni.Single(item => item.TipoAbilitazioneId == tipo.TipoAbilitazioneId);
            viewModel.AbilitazioneDataConseguimento = "01/01/2020";
            viewModel.AbilitazioneDataScadenza = "01/01/2021";
            viewModel.SaveAbilitazioneCommand.Execute(null);
        }

        loaded = SalvaSchedaPerTest(viewModel);
        foreach (var abilitazione in loaded.Abilitazioni.Where(item => item.Tipo!.RichiedeScadenza))
        {
            AssertEqual<DateOnly?>(new DateOnly(2020, 1, 1), abilitazione.DataConseguimento, "Conservazione data conseguimento");
            AssertEqual<DateOnly?>(new DateOnly(2021, 1, 1), abilitazione.DataScadenza, "Conservazione scadenza passata");
            viewModel.SelectedAbilitazione = viewModel.Abilitazioni.Single(item => item.TipoAbilitazioneId == abilitazione.TipoAbilitazioneId);
            viewModel.AbilitazioneDataConseguimento = string.Empty;
            viewModel.AbilitazioneDataScadenza = string.Empty;
            viewModel.SaveAbilitazioneCommand.Execute(null);
        }

        loaded = SalvaSchedaPerTest(viewModel);
        AssertEqual(viewModel.TipiAbilitazioneCatalogo.Count, loaded.Abilitazioni.Count, "Abilitazioni conservate dopo rimozione date");
        AssertTrue(loaded.Abilitazioni.All(item => item.DataConseguimento is null && item.DataScadenza is null), "Rimozione date non salvata.");
    }

    private static void TestAbilitazioniUnicheEditor()
    {
        var viewModel = new MainWindowViewModel
        {
            PerIdInput = "113", Cognome = "Test", Nome = "Abilitazione unica", CodiceFiscale = "TSTDUP80A01H501A",
        };
        var tipo = viewModel.TipiAbilitazioneCatalogo.First();
        var altroTipo = viewModel.TipiAbilitazioneCatalogo.Skip(1).First();
        viewModel.AbilitazioneTipoSelezionato = tipo;
        viewModel.AbilitazioneNote = "Nota originale";
        viewModel.SaveAbilitazioneCommand.Execute(null);
        var originale = viewModel.Abilitazioni.Single();

        viewModel.AbilitazioneTipoSelezionato = tipo;
        viewModel.AbilitazioneNote = "Non sovrascrivere";
        AssertTrue(viewModel.AbilitazioneIndicazioni.Contains("gia presente"), "Indicazione duplicato assente.");
        viewModel.SaveAbilitazioneCommand.Execute(null);
        AssertEqual(1, viewModel.Abilitazioni.Count, "Nessuna seconda riga dello stesso tipo");
        AssertEqual("Nota originale", originale.Note, "La riga originale resta intatta");

        viewModel.SelectedAbilitazione = originale;
        viewModel.AbilitazioneNote = "Nota aggiornata";
        viewModel.SaveAbilitazioneCommand.Execute(null);
        AssertEqual(1, viewModel.Abilitazioni.Count, "Modifica della riga esistente consentita");
        AssertEqual("Nota aggiornata", viewModel.Abilitazioni.Single().Note, "Modifica conservata");

        viewModel.AbilitazioneTipoSelezionato = altroTipo;
        viewModel.SaveAbilitazioneCommand.Execute(null);
        viewModel.SelectedAbilitazione = viewModel.Abilitazioni.Single(item => item.TipoAbilitazioneId == tipo.TipoAbilitazioneId);
        viewModel.AbilitazioneTipoSelezionato = altroTipo;
        viewModel.SaveAbilitazioneCommand.Execute(null);
        AssertEqual(2, viewModel.Abilitazioni.Select(item => item.TipoAbilitazioneId).Distinct().Count(), "Cambio tipo verso un duplicato rifiutato");

        viewModel.RemoveAbilitazioneCommand.Execute(null);
        viewModel.AbilitazioneTipoSelezionato = tipo;
        viewModel.SaveAbilitazioneCommand.Execute(null);
        AssertEqual(2, SalvaSchedaPerTest(viewModel).Abilitazioni.Count, "Reinserimento dopo rimozione consentito");
    }

    private static void TestAbilitazioniUnicheRepository()
    {
        var repository = new PersonaleRepository();
        var tipo = repository.GetTipiAbilitazione().First();
        var personale = CreaPersonale(114, "Test", "Duplicati", "TSTDBL80A01H501A", string.Empty);
        personale.Abilitazioni = [new() { TipoAbilitazioneId = tipo.TipoAbilitazioneId }, new() { TipoAbilitazioneId = tipo.TipoAbilitazioneId, Note = "Seconda riga" }];
        var exception = CaptureException(() => repository.SavePersonale(personale, true), "Duplicato salvato su nuova scheda.");
        AssertTrue(exception.Message.Contains("presente piu volte"), "Errore duplicato non riconoscibile.");
        AssertTrue(repository.GetPersonaleById(114) is null, "Scheda parziale creata dopo il rifiuto.");

        personale.Abilitazioni.RemoveAt(1);
        repository.SavePersonale(personale, true);
        personale.Nome = "Modifica da non salvare";
        personale.Abilitazioni.Add(new() { TipoAbilitazioneId = tipo.TipoAbilitazioneId });
        AssertThrows(() => repository.SavePersonale(personale, false), "Duplicato salvato su scheda esistente.");
        var loaded = repository.GetPersonaleById(114)!;
        AssertEqual("Duplicati", loaded.Nome, "Dati originali conservati dopo rifiuto");
        AssertEqual(1, loaded.Abilitazioni.Count, "Abilitazione originale conservata");

        var altraPersona = CreaPersonale(115, "Test", "Altra persona", "TSTALT80A01H501A", string.Empty);
        altraPersona.Abilitazioni = [new() { TipoAbilitazioneId = tipo.TipoAbilitazioneId }];
        repository.SavePersonale(altraPersona, true);
        AssertEqual(1, repository.GetPersonaleById(115)!.Abilitazioni.Count, "Stessa abilitazione consentita a persone diverse");
    }

    private static void TestVisitaBimestraleDisattivata()
    {
        var bimestrale = CatalogoVisiteMediche.Tutte.Single(tipo => tipo.Codice == "BIMESTRALE");
        AssertTrue(!bimestrale.Attiva, "Visita bimestrale ancora attiva.");
        AssertEqual<int?>(2, bimestrale.MesiValidita, "Regola bimestrale conservata per riattivazione");

        var repository = new PersonaleRepository();
        var personale = CreaPersonale(108, "Test", "Bimestrale disattivata", "TSTBMS80A01H501A", string.Empty);
        personale.VisiteMediche =
        [
            new VisitaMedica
            {
                TipoVisita = bimestrale.Descrizione,
                DataUltimaVisita = new DateOnly(2020, 1, 1),
                DataScadenza = new DateOnly(2020, 3, 1),
                Note = "Dato da conservare senza mostrare scadenze",
            },
        ];
        repository.SavePersonale(personale, isNewRecord: true);

        var oggi = DateOnly.FromDateTime(DateTime.Today);
        AssertTrue(!repository.GetScadenzeProssime(oggi, oggi.AddDays(90)).Any(item => item.PerId == 108), "Bimestrale presente nello scadenzario.");
        AssertTrue(!repository.SearchPersonale(string.Empty, null, oggi).Any(item => item.PerId == 108), "Bimestrale presente nel filtro visite in scadenza.");

        var viewModel = new MainWindowViewModel();
        viewModel.SelectedPersonale = viewModel.PersonaleItems.Single(item => item.PerId == 108);
        AssertEqual(2, viewModel.TipiVisitaMedicaCatalogo.Count, "Tipi visita attivi");
        AssertTrue(viewModel.TipiVisitaMedicaCatalogo.All(tipo => tipo.Codice != "BIMESTRALE"), "Bimestrale presente nel menu.");
        AssertTrue(viewModel.VisiteMediche.All(item => item.TipoVisita != bimestrale.Descrizione), "Bimestrale presente in anagrafica.");
        AssertEqual(0, viewModel.SchedaVisiteTotali, "Visite disattivate escluse dal riepilogo");
        AssertEqual(0, viewModel.SchedaScadenzeTotali, "Scadenze disattivate escluse dalla scheda");
        AssertEqual(0, viewModel.SchedaScaduteTotali, "Visite disattivate escluse dagli avvisi");
        AssertTrue(viewModel.ScadenzeProssime.All(item => item.PerId != 108), "Bimestrale presente nella dashboard.");
        AssertTrue(!viewModel.RegoleVisiteDescrizione.Contains("bimestrale", StringComparison.OrdinalIgnoreCase), "Bimestrale ancora descritta nelle regole operative.");

        viewModel.Telefono1 = "123456";
        var loaded = SalvaSchedaPerTest(viewModel);
        var visita = loaded.VisiteMediche.Single();
        AssertEqual(bimestrale.Descrizione, visita.TipoVisita, "Tipo disattivato conservato nel database");
        AssertEqual(personale.VisiteMediche[0].DataScadenza, visita.DataScadenza, "Scadenza disattivata conservata");
        AssertEqual(personale.VisiteMediche[0].Note, visita.Note, "Note disattivate conservate");

        viewModel.NewCommand.Execute(null);
        viewModel.PerIdInput = "107";
        viewModel.Cognome = "Test";
        viewModel.Nome = "Nuova scheda senza bimestrale";
        viewModel.CodiceFiscale = "TSTNVA80A01H501A";
        loaded = SalvaSchedaPerTest(viewModel);
        AssertEqual(0, loaded.VisiteMediche.Count, "Nuova scheda senza dati disattivati di un'altra persona");
    }

    private static void TestAvvisiBimestraliNomiAlternativi()
    {
        var repository = new PersonaleRepository();
        var oggi = DateOnly.FromDateTime(DateTime.Today);
        var tipo = CatalogoVisiteMediche.Tutte.Single(item => item.Codice == "BIMESTRALE");
        var personale = CreaPersonale(108, "Test", "Bimestrale disattivata", "TSTBMS80A01H501A", string.Empty);
        personale.VisiteMediche = tipo.DescrizioniAlternative.Prepend(tipo.Descrizione)
            .SelectMany(nome => new[] { -10, 3, 30 }.Select(giorni => new VisitaMedica
            {
                TipoVisita = $"  {nome.ToUpperInvariant()}  ",
                DataScadenza = oggi.AddDays(giorni),
                Note = "Dato storico da conservare",
            }))
            .ToList();
        repository.SavePersonale(personale, isNewRecord: false);

        AssertTrue(personale.VisiteMediche.All(visita => !CatalogoVisiteMediche.IsAttiva(visita.TipoVisita)), "Nome alternativo bimestrale riconosciuto come attivo.");
        AssertTrue(!repository.GetScadenzeProssime(oggi, oggi.AddDays(90)).Any(item => item.PerId == 108), "Avviso bimestrale con nome alternativo nello scadenzario.");
        AssertTrue(!repository.SearchPersonale(string.Empty, null, oggi.AddDays(90)).Any(item => item.PerId == 108), "Nome alternativo nel filtro visite.");

        var viewModel = new MainWindowViewModel();
        AssertTrue(viewModel.DashboardTopScadenze.All(item => item.PerId != 108), "Avviso bimestrale nella dashboard.");
        AssertEqual(0, viewModel.DashboardVisiteScadutePersonale, "Contatore visite scadute senza bimestrali");
        AssertEqual(0, viewModel.DashboardVisiteInScadenzaPersonale, "Contatore visite in scadenza senza bimestrali");
        AssertEqual(0, viewModel.ScadenzeUrgenti, "Contatore urgenze senza bimestrali");

        viewModel.SelectedPersonale = viewModel.PersonaleItems.Single(item => item.PerId == 108);
        AssertEqual(0, viewModel.SchedaScaduteTotali, "Nomi alternativi esclusi dagli avvisi scheda");
        viewModel.Telefono1 = "654321";
        var loaded = SalvaSchedaPerTest(viewModel);
        AssertEqual(personale.VisiteMediche.Count, loaded.VisiteMediche.Count, "Conservazione visite con nomi alternativi");
        AssertTrue(loaded.VisiteMediche.All(item => item.Note == "Dato storico da conservare"), "Note storiche non conservate.");
    }

    private static void TestVisiteFacoltative()
    {
        var viewModel = new MainWindowViewModel
        {
            PerIdInput = "110",
            Cognome = "Test",
            Nome = "Visite facoltative",
            CodiceFiscale = "TSTVST80A01H501A",
        };

        var loaded = SalvaSchedaPerTest(viewModel);
        AssertEqual(0, loaded.VisiteMediche.Count, "Nuova scheda senza visite");
        viewModel.VisiteMediche.Clear();
        loaded = SalvaSchedaPerTest(viewModel);
        AssertEqual(0, loaded.VisiteMediche.Count, "Scheda senza righe visita");

        foreach (var tipo in CatalogoVisiteMediche.Attive)
        {
            viewModel.VisitaTipoSelezionato = tipo;
            viewModel.VisitaDataUltimaVisita = "01/01/2020";
            viewModel.VisitaEsito = "Da verificare";
            viewModel.VisitaNote = "Visita scaduta di test";
            viewModel.SaveVisitaCommand.Execute(null);
            loaded = SalvaSchedaPerTest(viewModel);
            var visita = loaded.VisiteMediche.Single(item => item.TipoVisita == tipo.Descrizione);
            var mesi = tipo.Codice switch
            {
                "DLGS_81_08" => 12,
                _ => 24,
            };
            AssertEqual<DateOnly?>(new DateOnly(2020, 1, 1).AddMonths(mesi), visita.DataScadenza, "Scadenza informativa");
            AssertEqual("Visita scaduta di test", visita.Note, "Note visita scaduta");
        }

        AssertEqual(2, loaded.VisiteMediche.Count, "Scheda con entrambe le visite attive scadute");
        AssertEqual(2, new PersonaleRepository().GetScadenzeProssime(DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today).AddDays(90)).Count(item => item.PerId == 110), "Scadenze delle visite attive");
        AssertTrue(new PersonaleRepository().SearchPersonale(string.Empty, null, DateOnly.FromDateTime(DateTime.Today)).Any(item => item.PerId == 110), "Visite attive escluse dal filtro scadenze.");
        AssertTrue(viewModel.OperatoriServizioDisponibili.Any(item => item.PerId == 110), "Operatore escluso dai servizi per visite scadute.");

        viewModel.VisitaDataUltimaVisita = string.Empty;
        viewModel.VisitaEsito = string.Empty;
        viewModel.VisitaNote = "Annotazione senza data";
        viewModel.SaveVisitaCommand.Execute(null);
        loaded = SalvaSchedaPerTest(viewModel);
        var senzaData = loaded.VisiteMediche.Single(item => item.TipoVisita == viewModel.VisitaTipoSelezionato!.Descrizione);
        AssertTrue(senzaData.DataUltimaVisita is null && senzaData.DataScadenza is null, "La visita senza data ha conservato date precedenti.");
        AssertEqual("Annotazione senza data", senzaData.Note, "Note senza data");

        foreach (var tipo in CatalogoVisiteMediche.Attive)
        {
            viewModel.VisitaTipoSelezionato = tipo;
            viewModel.RemoveVisitaCommand.Execute(null);
        }

        loaded = SalvaSchedaPerTest(viewModel);
        AssertEqual(0, loaded.VisiteMediche.Count, "Rimozione di tutte le visite");
        AssertTrue(viewModel.OperatoriServizioDisponibili.Any(item => item.PerId == 110), "Operatore escluso dai servizi senza visite.");
    }

    private static void TestConservazioneVisite()
    {
        var repository = new PersonaleRepository();
        var perId = 111;
        foreach (var profilo in ProfiliPersonaleCatalogo.Tutti)
        {
            var personale = CreaPersonale(perId, "Test", "Conservazione visite", $"TEST{perId}", string.Empty);
            personale.ProfiloPersonale = profilo;
            personale.RuoloSanitario = ProfiliPersonaleCatalogo.IsSanitario(profilo) ? "Sanitario" : string.Empty;
            personale.VisiteMediche =
            [
                new VisitaMedica
                {
                    TipoVisita = CatalogoVisiteMediche.Tutte.Single(item => item.Codice == "DLGS_81_08").Descrizione,
                    DataUltimaVisita = new DateOnly(2020, 1, 1),
                    DataScadenza = new DateOnly(2021, 1, 1),
                    Esito = "Esito preesistente",
                    Note = "Scadenza gia registrata",
                },
                new VisitaMedica
                {
                    TipoVisita = CatalogoVisiteMediche.Tutte.Single(item => item.Codice == "BIMESTRALE").Descrizione,
                    DataScadenza = new DateOnly(2020, 3, 1),
                    Note = "Data ultima visita mancante",
                },
                new VisitaMedica { TipoVisita = "Visita storica fuori catalogo", Note = "Da conservare" },
            ];
            repository.SavePersonale(personale, isNewRecord: true);
            var viewModel = new MainWindowViewModel();
            viewModel.SelectedPersonale = viewModel.PersonaleItems.Single(item => item.PerId == perId);
            viewModel.Telefono1 = "123456";
            var loaded = SalvaSchedaPerTest(viewModel);

            AssertEqual("123456", loaded.Telefono1, "Modifica anagrafica con visite incomplete");
            AssertEqual(personale.VisiteMediche.Count, loaded.VisiteMediche.Count, "Numero visite conservate");
            foreach (var originale in personale.VisiteMediche)
            {
                var visita = loaded.VisiteMediche.Single(item => item.TipoVisita == originale.TipoVisita);
                AssertEqual(originale.DataUltimaVisita, visita.DataUltimaVisita, "Data visita conservata");
                AssertEqual(originale.DataScadenza, visita.DataScadenza, "Scadenza conservata");
                AssertEqual(originale.Esito, visita.Esito, "Esito conservato");
                AssertEqual(originale.Note, visita.Note, "Note conservate");
            }

            perId++;
        }
    }

    private static Personale SalvaSchedaPerTest(MainWindowViewModel viewModel)
    {
        // Esegui prima la validazione senza finestre modali, per fallire subito in caso di regressione.
        var buildModel = typeof(MainWindowViewModel).GetMethod("BuildModelFromEditor", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var personale = (Personale)buildModel.Invoke(viewModel, null)!;
        viewModel.SaveCommand.Execute(null);
        AssertEqual($"Scheda salvata con PerID {personale.PerId}", viewModel.Stato, "Salvataggio da interfaccia");
        return new PersonaleRepository().GetPersonaleById(personale.PerId)
            ?? throw new InvalidOperationException("Scheda non riletta dopo il salvataggio da interfaccia.");
    }

    private static void TestSalvataggioELetturaServizio()
    {
        var repository = new PersonaleRepository();
        repository.SavePersonale(CreaPersonale(201, "Bianchi", "Luca", "BNCLCU81A01H501J", "luca.bianchi"), isNewRecord: true);
        var personaleConVisiteScadute = CreaPersonale(202, "Verdi", "Anna", "VRDNNA82A41H501K", "anna.verdi");
        personaleConVisiteScadute.VisiteMediche = CatalogoVisiteMediche.Tutte.Select(tipo => new VisitaMedica
        {
            TipoVisita = tipo.Descrizione,
            DataUltimaVisita = new DateOnly(2020, 1, 1),
            DataScadenza = new DateOnly(2020, 1, 1).AddMonths(tipo.MesiValidita!.Value),
            Esito = "Da verificare",
        }).ToList();
        repository.SavePersonale(personaleConVisiteScadute, isNewRecord: true);
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
        viewModel.ServizioData = "19/03/2026";
        AssertEqual("TEST-001", viewModel.ServizioNumeroOrdine, "Numero ordine del servizio esistente");
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

    private static void TestRipristinoBackupSicuro()
    {
        const int presenteNelBackupPerId = 901;
        const int aggiuntoDopoBackupPerId = 902;
        const string exportFileName = "restore-test.txt";

        var repository = new PersonaleRepository();
        repository.SavePersonale(
            CreaPersonale(presenteNelBackupPerId, "Backup", "Originale", "BCKRGN80A01H501M", "backup.originale"),
            isNewRecord: true);

        Directory.CreateDirectory(DatabasePaths.ExportDirectory);
        var exportFilePath = Path.Combine(DatabasePaths.ExportDirectory, exportFileName);
        File.WriteAllText(exportFilePath, "contenuto-del-backup");

        var backupService = new BackupService();
        var validBackup = backupService.CreateLocalBackup("test-restore");

        repository.SavePersonale(
            CreaPersonale(aggiuntoDopoBackupPerId, "Corrente", "DaPreservare", "CRRDPR80A01H501N", "corrente.preservare"),
            isNewRecord: true);
        File.WriteAllText(exportFilePath, "contenuto-corrente");

        var unsafeBackupPath = Path.Combine(DatabasePaths.BackupsDirectory, "percorso-non-sicuro.smzbak");
        using (var archive = ZipFile.Open(unsafeBackupPath, ZipArchiveMode.Create))
        {
            var unsafeEntry = archive.CreateEntry("../intrusione.txt");
            using var writer = new StreamWriter(unsafeEntry.Open());
            writer.Write("contenuto non autorizzato");
        }

        var backupCountBeforeUnsafeRestore = Directory
            .EnumerateFiles(DatabasePaths.LocalBackupDirectory, "*.smzbak", SearchOption.TopDirectoryOnly)
            .Count();
        var unsafeRestoreException = CaptureException(
            () => backupService.RestoreBackup(unsafeBackupPath),
            "Un archivio con attraversamento di percorso e stato accettato.");
        AssertTrue(
            unsafeRestoreException.Message.Contains("percorso non sicuro", StringComparison.OrdinalIgnoreCase),
            "Il percorso non sicuro non e stato riconosciuto esplicitamente.");
        AssertEqual(
            backupCountBeforeUnsafeRestore,
            Directory.EnumerateFiles(DatabasePaths.LocalBackupDirectory, "*.smzbak", SearchOption.TopDirectoryOnly).Count(),
            "Backup di sicurezza creati per un archivio non valido");
        AssertTrue(
            repository.GetPersonaleById(aggiuntoDopoBackupPerId) is not null,
            "Un backup non valido ha modificato il database corrente.");
        AssertEqual(
            "contenuto-corrente",
            File.ReadAllText(exportFilePath),
            "Export dopo rifiuto backup non valido");

        Directory.Delete(DatabasePaths.ExportDirectory, recursive: true);
        File.WriteAllText(DatabasePaths.ExportDirectory, "blocco controllato del test");

        var rollbackException = CaptureException(
            () => backupService.RestoreBackup(validBackup.BackupPath),
            "Un errore durante l'applicazione del backup non e stato segnalato.");
        AssertTrue(
            rollbackException.Message.Contains("ripristinati automaticamente", StringComparison.OrdinalIgnoreCase),
            "Il test non ha raggiunto il rollback successivo allo scambio del database.");
        AssertTrue(
            repository.GetPersonaleById(aggiuntoDopoBackupPerId) is not null,
            "Il rollback non ha ripristinato il database precedente.");
        AssertTrue(
            File.Exists(DatabasePaths.ExportDirectory),
            "Il rollback ha alterato il file usato per simulare l'errore sugli export.");

        File.Delete(DatabasePaths.ExportDirectory);
        Directory.CreateDirectory(DatabasePaths.ExportDirectory);
        File.WriteAllText(exportFilePath, "contenuto-corrente");

        var restoreResult = backupService.RestoreBackup(validBackup.BackupPath);
        var restoredRepository = new PersonaleRepository();
        AssertTrue(
            restoredRepository.GetPersonaleById(presenteNelBackupPerId) is not null,
            "Il record presente nel backup non e stato ripristinato.");
        AssertTrue(
            restoredRepository.GetPersonaleById(aggiuntoDopoBackupPerId) is null,
            "Il database attivo non e stato sostituito dal backup validato.");
        AssertEqual(
            "contenuto-del-backup",
            File.ReadAllText(exportFilePath),
            "Export ripristinato");
        AssertTrue(
            File.Exists(restoreResult.SafetyBackupPath),
            "Il backup di sicurezza precedente al restore non e stato conservato.");
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

    private static void TestOperationalDatabaseSetup()
    {
        SqliteConnection.ClearAllPools();
        var root = DatabasePaths.AppDataDirectory;
        var source = DatabasePaths.DatabasePath;
        var sourceHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(source)));
        var fingerprint = DatabaseFingerprint(source);
        var target = Path.Combine(root, "Installazione con spazi", "Dati reali simulati");
        var result = OperationalDatabaseSetup.Prepare(source, target);
        AssertTrue(result.Imported && result.PersonaleCount > 0 && result.HasUsers, "Dati o account non importati.");
        AssertTrue(File.Exists(result.SafetyBackupPath), "Copia di sicurezza mancante.");
        AssertEqual(fingerprint, DatabaseFingerprint(result.DatabasePath), "Tutte le righe e gli account conservati");
        AssertEqual(sourceHash, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(source))), "Sorgente intatta");

        var reused = OperationalDatabaseSetup.Prepare(result.DatabasePath, target);
        AssertTrue(!reused.Imported && File.Exists(reused.SafetyBackupPath), "Database in uso non riutilizzato con backup.");
        AssertEqual(fingerprint, DatabaseFingerprint(reused.DatabasePath), "Riutilizzo non modifica il database");
        AssertThrows(() => OperationalDatabaseSetup.Prepare(source, target), "Sostituzione di un database esistente consentita.");
        AssertEqual(fingerprint, DatabaseFingerprint(result.DatabasePath), "Destinazione conservata dopo il rifiuto");

        var invalid = Path.Combine(root, "database-non-valido.db");
        File.WriteAllText(invalid, "non e un database");
        var invalidTarget = Path.Combine(root, "Importazione rifiutata");
        AssertThrows(() => OperationalDatabaseSetup.Prepare(invalid, invalidTarget), "Database non valido accettato.");
        AssertTrue(!Directory.Exists(invalidTarget), "Cartella dati parziale creata.");

        Directory.CreateDirectory(DatabasePaths.ExportDirectory);
        File.WriteAllText(Path.Combine(DatabasePaths.ExportDirectory, "collaudo-installazione.txt"), "Export da conservare");
        var backup = new BackupService().CreateLocalBackup("collaudo-installazione");
        var restored = OperationalDatabaseSetup.Prepare(backup.BackupPath, Path.Combine(root, "Importazione backup"));
        AssertEqual(fingerprint, DatabaseFingerprint(restored.DatabasePath), "Dati del backup conservati senza inizializzazione");
        AssertEqual("Export da conservare", File.ReadAllText(Path.Combine(Path.GetDirectoryName(restored.DatabasePath)!, "Export", "collaudo-installazione.txt")), "Export del backup conservato");
    }

    private static void TestOperationalDatabaseWal()
    {
        var root = DatabasePaths.AppDataDirectory;
        var source = OperationalDatabaseSetup.Prepare(DatabasePaths.DatabasePath, Path.Combine(root, "Sorgente WAL")).DatabasePath;
        using var writer = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = source, Pooling = false }.ToString());
        writer.Open();
        using var command = writer.CreateCommand();
        command.CommandText = "PRAGMA journal_mode=WAL; PRAGMA wal_autocheckpoint=0; UPDATE Personale SET Telefono1='WAL-COMMIT-COLLAUDO' WHERE PerId=(SELECT MIN(PerId) FROM Personale);";
        command.ExecuteNonQuery();
        AssertTrue(File.Exists(source + "-wal") && new FileInfo(source + "-wal").Length > 0, "La prova non contiene dati WAL.");
        var result = OperationalDatabaseSetup.Prepare(source, Path.Combine(root, "Destinazione WAL"));
        AssertEqual(DatabaseFingerprint(source), DatabaseFingerprint(result.DatabasePath), "Righe confermate nel WAL importate");
    }

    private static void TestInstalledDatabaseConfiguration()
    {
        var appDirectory = Path.Combine(DatabasePaths.AppDataDirectory, "App installata");
        Directory.CreateDirectory(appDirectory);
        AssertTrue(DatabasePaths.ResolveInstalledDataDirectory(appDirectory) is null, "Configurazione inesistente riconosciuta.");
        var settingsPath = Path.Combine(appDirectory, "installazione.json");
        File.WriteAllText(settingsPath, System.Text.Json.JsonSerializer.Serialize(new { PackageKind = "Operational", DataDirectory = DatabasePaths.AppDataDirectory }));
        AssertEqual(DatabasePaths.AppDataDirectory, DatabasePaths.ResolveInstalledDataDirectory(appDirectory), "Percorso installato risolto");
        var missing = Path.Combine(appDirectory, "Database assente");
        File.WriteAllText(settingsPath, System.Text.Json.JsonSerializer.Serialize(new { PackageKind = "Operational", DataDirectory = missing }));
        AssertThrows(() => DatabasePaths.ResolveInstalledDataDirectory(appDirectory), "Database mancante ignorato.");
        AssertTrue(!Directory.Exists(missing), "Creato un database alternativo vuoto.");

        var previous = Environment.GetEnvironmentVariable(DatabasePaths.AppDataDirectoryEnvironmentVariable);
        var requirement = AppContext.GetData("SMZ.RequireInstalledDatabase");
        try
        {
            Environment.SetEnvironmentVariable(DatabasePaths.AppDataDirectoryEnvironmentVariable, null);
            AppContext.SetData("SMZ.RequireInstalledDatabase", true);
            AssertThrows(() => _ = DatabasePaths.AppDataDirectory, "Distribuzione operativa avviata senza dati selezionati.");
            Environment.SetEnvironmentVariable(DatabasePaths.AppDataDirectoryEnvironmentVariable, missing);
            AssertThrows(() => _ = DatabasePaths.AppDataDirectory, "Percorso ambiente mancante accettato dalla distribuzione operativa.");
        }
        finally
        {
            Environment.SetEnvironmentVariable(DatabasePaths.AppDataDirectoryEnvironmentVariable, previous);
            AppContext.SetData("SMZ.RequireInstalledDatabase", requirement);
        }
    }

    private static string DatabaseFingerprint(string path)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_schema WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name;";
        var tables = new List<string>();
        using (var reader = command.ExecuteReader()) while (reader.Read()) tables.Add(reader.GetString(0));
        var text = new System.Text.StringBuilder();
        foreach (var table in tables)
        {
            text.AppendLine(table);
            command.CommandText = $"SELECT * FROM \"{table.Replace("\"", "\"\"")}\" ORDER BY rowid;";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var values = new object[reader.FieldCount];
                reader.GetValues(values);
                text.AppendLine(System.Text.Json.JsonSerializer.Serialize(values));
            }
        }
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text.ToString())));
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

    private static Exception CaptureException(Action action, string message)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            return ex;
        }

        throw new InvalidOperationException(message);
    }
}
