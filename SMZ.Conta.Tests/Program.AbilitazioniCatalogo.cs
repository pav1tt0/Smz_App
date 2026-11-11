using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;
using SMZ.Conta.App.Data;
using SMZ.Conta.App.Models;
using SMZ.Conta.App.ViewModels;
using SMZ.Conta.App.Views;

namespace SMZ.Conta.Tests;

internal static partial class Program
{
    private static void TestCatalogoAbilitazioniAdmin() => ConDatabaseServiziIsolato(repository =>
    {
        var vm = new MainWindowViewModel();
        var patente = vm.AbilitazioniGestioneCatalogo.Single(tipo => tipo.Codice == "PATENTE_GUIDA");
        patente.Descrizione = "Patente di servizio";
        patente.Categoria = "Guida mezzi";
        patente.Ordine = -1;
        AssertEqual("Patente di guida", vm.TipiAbilitazioneCatalogo.Single(tipo => tipo.Codice == "PATENTE_GUIDA").Descrizione,
            "Modifiche del catalogo isolate prima di Salva");
        vm.SaveTipiAbilitazioneCommand.Execute(null);
        AssertEqual("Abilitazioni aggiornate.", vm.Stato, "Salvataggio catalogo da Admin");
        AssertEqual("PATENTE_GUIDA", vm.TipiAbilitazioneCatalogo.First().Codice, "Ordine applicato nei menu");

        vm.NuovaAbilitazione = "Abilitazione personalizzata";
        vm.AddTipoAbilitazioneCommand.Execute(null);
        var nuova = vm.AbilitazioniGestioneCatalogo.Single(tipo => tipo.Descrizione == "Abilitazione personalizzata");
        nuova.Categoria = "Tecnica";
        nuova.RichiedeLivello = true;
        nuova.RichiedeScadenza = true;
        nuova.RichiedeProfondita = true;
        vm.SaveTipiAbilitazioneCommand.Execute(null);
        DatabaseInitializer.EnsureDatabase();
        var persistita = repository.GetTipiAbilitazione().Single(tipo => tipo.TipoAbilitazioneId == nuova.TipoAbilitazioneId);
        AssertTrue(persistita.RichiedeLivello && persistita.RichiedeScadenza && persistita.RichiedeProfondita,
            "Campi personalizzati conservati dopo riavvio");
        AssertEqual("Patente di servizio", repository.GetTipiAbilitazione().First().Descrizione, "Modifica predefinita conservata dopo riavvio");

        var persona = CreaPersonale(901, "Catalogo", "Abilitazioni", "CATALOGO901", "catalogo901");
        persona.Abilitazioni = [new() { TipoAbilitazioneId = nuova.TipoAbilitazioneId, Livello = "Avanzato", ProfonditaMetri = 20,
            DataScadenza = new DateOnly(2028, 5, 6), Note = "Storico" }];
        repository.SavePersonale(persona, true);
        vm.SearchCommand.Execute(null);
        vm.Nome = "Bozza non salvata";
        nuova = vm.AbilitazioniGestioneCatalogo.Single(tipo => tipo.TipoAbilitazioneId == nuova.TipoAbilitazioneId);
        nuova.Attiva = false;
        vm.SaveTipiAbilitazioneCommand.Execute(null);
        AssertEqual("Bozza non salvata", vm.Nome, "Salvataggio catalogo conserva la bozza anagrafica");
        AssertTrue(!vm.TipiAbilitazioneSelezionabili.Any(tipo => tipo.TipoAbilitazioneId == nuova.TipoAbilitazioneId),
            "Voce disattivata esclusa dalle nuove assegnazioni");
        var caricata = repository.GetPersonaleById(901)!;
        AssertEqual("Storico", caricata.Abilitazioni.Single().Note, "Voce disattivata ancora nella scheda");
        AssertTrue(!caricata.Abilitazioni.Single().Tipo!.Attiva, "Stato disattivato riletto nella scheda");
        vm.SelectedAbilitazione = vm.Abilitazioni.Single();
        AssertEqual(nuova.TipoAbilitazioneId, vm.AbilitazioneTipoSelezionato!.TipoAbilitazioneId, "Voce storica modificabile");
        var build = typeof(MainWindowViewModel).GetMethod("BuildAbilitazioni", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        AssertEqual(1, ((List<PersonaleAbilitazione>)build.Invoke(vm, null)!).Count, "Voce disattivata conservata dal salvataggio della scheda");

        var archivioId = repository.DeletePersonale(901);
        AssertEqual("Storico", repository.GetArchivioById(archivioId)!.Abilitazioni.Single().Note, "Nuova abilitazione conservata nell'archivio");
        repository.RestorePersonaleArchivio(archivioId);
        AssertEqual(20, repository.GetPersonaleById(901)!.Abilitazioni.Single().ProfonditaMetri, "Ripristino conserva la nuova abilitazione");

        var baseVm = new MainWindowViewModel(new AccessSession(0, "Base", AccessRole.Base, false));
        baseVm.NuovaAbilitazione = "Non autorizzata";
        AssertTrue(!baseVm.AddTipoAbilitazioneCommand.CanExecute(null) && !baseVm.SaveTipiAbilitazioneCommand.CanExecute(null),
            "Gestione riservata agli amministratori");
        baseVm.AddTipoAbilitazioneCommand.Execute(null);
        AssertTrue(!repository.GetTipiAbilitazione(true).Any(tipo => tipo.Descrizione == "Non autorizzata"), "Controllo accessi anche su esecuzione diretta");
    });

    private static void TestCatalogoAbilitazioniMigrazione() => ConDatabaseServiziIsolato(repository =>
    {
        using (var connection = new SqliteConnection(DatabasePaths.ConnectionString))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = """
                ALTER TABLE TipiAbilitazione DROP COLUMN Attiva;
                ALTER TABLE TipiAbilitazione DROP COLUMN Ordine;
                INSERT INTO TipiAbilitazione VALUES (1, 'ARA_LEGACY', 'Vecchia abilitazione', 'Subacquea', 0, 0, 1);
                """;
            command.ExecuteNonQuery();
        }
        DatabaseInitializer.EnsureDatabase();
        AssertTrue(!repository.GetTipiAbilitazione().Any(tipo => tipo.TipoAbilitazioneId == 1), "Voce legacy resta disattivata dopo migrazione");
        AssertTrue(repository.GetTipiAbilitazione(true).Any(tipo => tipo.TipoAbilitazioneId == 1), "Voce legacy conservata nel database");
        AssertEqual(CatalogoAbilitazioni.Tutte.First().TipoAbilitazioneId, repository.GetTipiAbilitazione().First().TipoAbilitazioneId,
            "Ordine originale conservato dopo migrazione");

        var tipi = repository.GetTipiAbilitazione();
        var originale = tipi[0].Descrizione;
        tipi[0].Descrizione = "Modifica da annullare";
        tipi[1].Descrizione = " ";
        AssertThrows(() => repository.UpdateTipiAbilitazione(tipi), "Descrizione vuota accettata");
        AssertEqual(originale, repository.GetTipiAbilitazione().First().Descrizione, "Nessun salvataggio parziale dopo errore");
        tipi[1].Descrizione = " modifica da annullare ";
        AssertThrows(() => repository.UpdateTipiAbilitazione(tipi), "Descrizione duplicata accettata");
        AssertEqual(originale, repository.GetTipiAbilitazione().First().Descrizione, "Rollback transazione su duplicati");
        AssertThrows(() => repository.AddTipoAbilitazione(" " + originale.ToUpperInvariant() + " "), "Aggiunta duplicata accettata");
    });

    private static void TestCatalogoAbilitazioniLayout() => ConDatabaseServiziIsolato(repository =>
    {
        var vm = new MainWindowViewModel();
        var view = new SettingsView { DataContext = vm };
        var grid = (DataGrid)view.FindName("AbilitazioniCatalogoGrid");
        foreach (var width in new[] { 400d, 860d, 1240d, 400d })
        {
            view.Measure(new Size(width, double.PositiveInfinity));
            view.Arrange(new Rect(0, 0, width, view.DesiredSize.Height));
            view.UpdateLayout();
            AssertTrue(grid.ActualWidth > 0 && grid.ActualWidth <= width, "Tabella abilitazioni entro la larghezza disponibile");
            AssertTrue(grid.Columns.All(column => column.MinWidth >= 60), "Colonne leggibili con scorrimento orizzontale");
        }
        AssertEqual(vm.AbilitazioniGestioneCatalogo.Count, grid.Items.Count, "Binding del catalogo Admin");
    });
}
