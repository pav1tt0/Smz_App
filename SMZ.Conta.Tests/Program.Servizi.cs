using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Data.Sqlite;
using SMZ.Conta.App.Data;
using SMZ.Conta.App.Infrastructure;
using SMZ.Conta.App.Models;
using SMZ.Conta.App.Printing;
using SMZ.Conta.App.ViewModels;

namespace SMZ.Conta.Tests;

internal static partial class Program
{
    private static void ConDatabaseServiziIsolato(Action<PersonaleRepository> test)
    {
        var previous = Environment.GetEnvironmentVariable(DatabasePaths.AppDataDirectoryEnvironmentVariable);
        var directory = Path.Combine(previous!, "servizi-" + Guid.NewGuid().ToString("N"));
        Environment.SetEnvironmentVariable(DatabasePaths.AppDataDirectoryEnvironmentVariable, directory);
        try
        {
            DatabaseInitializer.EnsureDatabase();
            test(new PersonaleRepository());
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Environment.SetEnvironmentVariable(DatabasePaths.AppDataDirectoryEnvironmentVariable, previous);
        }
    }

    private static void TestOrdinePersonalePerQualifica() => ConDatabaseServiziIsolato(repository =>
    {
        var agente = CreaPersonale(301, "Alfa", "Agente", "ORDINE301", "ordine301");
        agente.Qualifica = "Agente";
        var capoRecente = CreaPersonale(302, "Beta", "Recente", "ORDINE302", "ordine302");
        capoRecente.Qualifica = "Commissario Capo";
        capoRecente.DataDecorrenzaQualifica = new DateOnly(2025, 1, 1);
        var capoAnziano = CreaPersonale(303, "Zeta", "Anziano", "ORDINE303", "ordine303");
        capoAnziano.Qualifica = "Commissario Capo";
        capoAnziano.DataDecorrenzaQualifica = new DateOnly(2020, 1, 1);
        var commissario = CreaPersonale(304, "Gamma", "Commissario", "ORDINE304", "ordine304");
        commissario.Qualifica = "Commissario";
        commissario.DataDecorrenzaQualifica = new DateOnly(1900, 1, 1);
        foreach (var persona in new[] { agente, capoRecente, capoAnziano, commissario }) repository.SavePersonale(persona, true);
        var expected = "303,302,304,301";
        AssertEqual(expected, string.Join(",", repository.SearchPersonale("", null, null).Select(item => item.PerId)), "Ordine anagrafica");
        var vm = new MainWindowViewModel();
        AssertEqual(expected, string.Join(",", vm.ServizioPartecipantiBozza.Select(item => item.PerId)), "Ordine personale servizio");
        AssertEqual(expected, string.Join(",", vm.OperatoriServizioDisponibili.Where(item => item.PerId > 0).Select(item => item.PerId)), "Ordine tendine operatori");
        AssertTrue(QualificaFormatter.GetChiaveOrdinamento("Commissario Capo") < QualificaFormatter.GetChiaveOrdinamento("Commissario", new DateOnly(1900, 1, 1)), "Decorrenza mancante altera la gerarchia.");

        vm.ServizioData = "03/07/2026";
        vm.ServizioFuoriSede = true;
        foreach (var partecipante in vm.ServizioPartecipantiBozza) partecipante.Presente = true;
        foreach (var row in vm.ServizioPartecipazioniContabiliUnicheBozza)
        {
            row.TipologiaImmersioneOperativa = vm.TipologieImmersioneOperativeCatalogo.Single(item => item.Codice == "ARA_ASAS");
            row.ProfonditaMetri = "10";
            row.OreImmersione = "1";
            row.CategoriaContabileOre = vm.CategorieContabiliOreCatalogo.Single(item => item.Codice == "ORD");
        }
        repository.SaveServizioGiornaliero(LeggiBozzaServizio(vm));
        AssertEqual("302,303,304,301", string.Join(",", repository.GetContabilitaGiornateImpiego(2026, 7).SmzImmersioni.Select(item => item.PerId)), "Ordine contabilita per qualifica e nome");
        AssertEqual("302,303,304,301", string.Join(",", repository.GetRegistroImmersioniMensile(2026, 7).Select(item => item.PerId)), "Ordine registro per qualifica e nome");
        AssertEqual("302,303,304,301", string.Join(",", repository.GetIndennitaFuoriSedeMensile(2026, 7).Select(item => item.PerId)), "Ordine indennita per qualifica e nome");
        AssertEqual("302,303,304,301", string.Join(",", repository.GetReportPersonaleMensile(2026, 7).Select(item => item.PerId).Distinct()), "Ordine report per qualifica e nome");
    });

    private static void TestPersonaleColonneVerticali()
    {
        foreach (var count in new[] { 1, 4, 5, 6 })
        {
            var panel = new ColumnFirstUniformGrid { Columns = 2 };
            for (var i = 0; i < count; i++) panel.Children.Add(new Border { Height = 40 });
            panel.Measure(new Size(900, double.PositiveInfinity));
            panel.Arrange(new Rect(0, 0, 900, panel.DesiredSize.Height));
            var rows = (count + 1) / 2;
            for (var i = 0; i < count; i++)
            {
                var position = VisualTreeHelper.GetOffset(panel.Children[i]);
                AssertEqual(i / rows * 450d, position.X, "Colonna personale");
                AssertEqual(i % rows * 40d, position.Y, "Riga personale");
            }
        }
    }

    private static ServizioGiornaliero LeggiBozzaServizio(MainWindowViewModel vm) =>
        (ServizioGiornaliero)typeof(MainWindowViewModel)
            .GetMethod("BuildServizioGiornalieroModel", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(vm, null)!;

    private static void TestTreNatantiServizio() => ConDatabaseServiziIsolato(repository =>
    {
        repository.SavePersonale(CreaPersonale(311, "Test", "Natanti", "NATANTI311", "natanti311"), true);
        var natanti = new[] { "NATANTE TEST UNO", "NATANTE TEST DUE", "NATANTE TEST TRE" }.Select(repository.AddUnitaNavale).ToList();
        var vm = new MainWindowViewModel { ServizioData = "01/07/2026" };
        vm.ServizioPartecipantiBozza.Single().Presente = true;
        vm.ServizioUnitaNavaleSelezionata = vm.UnitaNavaliCatalogo.Single(item => item.UnitaNavaleId == natanti[0].UnitaNavaleId);
        vm.ServizioSecondaUnitaNavale = vm.UnitaNavaliCatalogo.Single(item => item.UnitaNavaleId == natanti[1].UnitaNavaleId);
        vm.ServizioTerzaUnitaNavale = vm.UnitaNavaliCatalogo.Single(item => item.UnitaNavaleId == natanti[2].UnitaNavaleId);
        var id = repository.SaveServizioGiornaliero(LeggiBozzaServizio(vm));
        var loaded = repository.GetServizioGiornalieroById(id)!;
        AssertEqual(string.Join(",", natanti.Select(item => item.UnitaNavaleId)), string.Join(",", loaded.GetUnitaNavaliIds()), "Natanti riletti");
        var summary = repository.GetServiziGiornalieriRecenti().Single();
        AssertEqual(string.Join(", ", natanti.Select(item => item.Descrizione)), summary.UnitaNavaleDescrizione, "Natanti nel riepilogo");
        var print = new ServizioGiornalieroPrintService(repository);
        var document = (FlowDocument)typeof(ServizioGiornalieroPrintService).GetMethod("BuildDocument", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(print, [loaded])!;
        var printText = new TextRange(document.ContentStart, document.ContentEnd).Text;
        foreach (var natante in natanti) AssertTrue(printText.Contains(natante.Descrizione), "Natante assente dalla stampa: " + natante.Descrizione);
        var exchange = new ServizioScambioService(repository);
        var file = Path.Combine(DatabasePaths.AppDataDirectory, "tre-natanti.smzservice");
        exchange.ExportServizio(id, file);
        var imported = repository.GetServizioGiornalieroById(exchange.ImportServizio(file))!;
        AssertEqual(string.Join(",", loaded.GetUnitaNavaliIds()), string.Join(",", imported.GetUnitaNavaliIds()), "Natanti dopo scambio");
        vm.SelectedServizioSalvato = summary;
        vm.OpenServizioCommand.Execute(null);
        AssertEqual(natanti[1].UnitaNavaleId, vm.ServizioSecondaUnitaNavale?.UnitaNavaleId, "Secondo natante riaperto");
        AssertEqual(natanti[2].UnitaNavaleId, vm.ServizioTerzaUnitaNavale?.UnitaNavaleId, "Terzo natante riaperto");
        vm.ServizioSecondaUnitaNavale = vm.ServizioUnitaNavaleSelezionata;
        AssertEqual(2, LeggiBozzaServizio(vm).GetUnitaNavaliIds().Count(), "Esclusione duplicati");
        vm.ServizioSecondaUnitaNavale = null;
        vm.ServizioTerzaUnitaNavale = null;
        repository.SaveServizioGiornaliero(LeggiBozzaServizio(vm));
        AssertEqual(1, repository.GetServizioGiornalieroById(id)!.GetUnitaNavaliIds().Count(), "Rimozione natanti aggiuntivi");
        DatabaseInitializer.EnsureDatabase();
        AssertEqual(3, repository.GetServizioGiornalieroById(imported.ServizioGiornalieroId)!.GetUnitaNavaliIds().Count(), "Natanti dopo riavvio database");
    });

    private static void TestSceltaImmersioneRiepilogo() => ConDatabaseServiziIsolato(repository =>
    {
        repository.SavePersonale(CreaPersonale(321, "Test", "Immersione", "IMMERSIONE321", "immersione321"), true);
        var vm = new MainWindowViewModel { ServizioData = "02/07/2026" };
        vm.ServizioImmersioniBozza[0].OrarioInizio = "09:00";
        vm.ServizioPartecipantiBozza.Single().Presente = true;
        var row = vm.ServizioPartecipazioniContabiliUnicheBozza.Single();
        var combo = new ComboBox { DataContext = row };
        combo.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(nameof(row.ImmersioniDisponibili)));
        combo.SetBinding(System.Windows.Controls.Primitives.Selector.SelectedItemProperty, new Binding(nameof(row.NumeroImmersione)) { Mode = BindingMode.TwoWay });
        combo.SelectedItem = 2;
        AssertEqual(2, row.NumeroImmersione, "Numero scelto dalla tendina");
        row.TipologiaImmersioneOperativa = vm.TipologieImmersioneOperativeCatalogo.Single(item => item.Codice == "ARA_ASAS");
        row.ProfonditaMetri = "10";
        row.OreImmersione = "1";
        row.CategoriaContabileOre = vm.CategorieContabiliOreCatalogo.Single(item => item.Codice == "ORD");
        AssertTrue(!vm.ServizioImmersioniBozza[0].Partecipazioni.Single().InImmersione, "Partecipazione attribuita erroneamente all'immersione 1.");
        AssertTrue(vm.ServizioImmersioniBozza[1].Partecipazioni.Single().InImmersione, "Partecipazione assente dall'immersione 2.");
        var id = repository.SaveServizioGiornaliero(LeggiBozzaServizio(vm));
        vm.SelectedServizioSalvato = repository.GetServiziGiornalieriRecenti().Single();
        vm.OpenServizioCommand.Execute(null);
        row = vm.ServizioPartecipazioniContabiliUnicheBozza.Single();
        AssertEqual(2, row.NumeroImmersione, "Immersione scelta dopo riapertura");
        AssertEqual(2, repository.GetServizioGiornalieroById(id)!.Immersioni.Single(item => item.Partecipazioni.Count > 0).NumeroImmersione, "Immersione persistita");
        vm.AddImmersioneCommand.Execute(null);
        AssertEqual(2, row.NumeroImmersione, "Scelta mantenuta aggiungendo un'immersione");
        vm.RemoveImmersioneCommand.Execute(vm.ServizioImmersioniBozza[0]);
        AssertEqual(1, row.NumeroImmersione, "Scelta segue la rinumerazione");
        vm.ServizioImmersioniBozza[0].DirettoreImmersione = vm.OperatoriServizioPresentiDisponibili.Single(item => item.PerId == 321);
        AssertTrue(!row.ImmersioniDisponibili.Contains(1), "Immersione in cui e direttore ancora selezionabile.");
        row.NumeroImmersione = null;
        AssertTrue(vm.ServizioImmersioniBozza.SelectMany(item => item.Partecipazioni).All(item => !item.InImmersione), "Partecipazione ancora attiva dopo rimozione della scelta.");
        AssertThrows(() => LeggiBozzaServizio(vm), "Dettaglio compilato senza numero immersione ignorato al salvataggio.");
    });
}
