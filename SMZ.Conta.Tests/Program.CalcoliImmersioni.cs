using Microsoft.Data.Sqlite;
using SMZ.Conta.App.Data;
using SMZ.Conta.App.Models;
using SMZ.Conta.App.ViewModels;

namespace SMZ.Conta.Tests;

internal static partial class Program
{
    private static void PreparaOperatoreCalcoli(PersonaleRepository repository) =>
        repository.SavePersonale(CreaPersonale(401, "COLLAUDO", "Calcoli", "CALCOLI401", "calcoli401"), true);

    private static (long Id, MainWindowViewModel Vm, decimal? Anteprima) CreaCasoCalcoli(
        PersonaleRepository repository, RegolaContabileImmersione regola, decimal ore)
    {
        var vm = new MainWindowViewModel { ServizioData = "10/08/2026" };
        vm.ServizioPartecipantiBozza.Single().Presente = true;
        var row = vm.ServizioPartecipazioniContabiliUnicheBozza.Single();
        row.NumeroImmersione = 1;
        row.TipologiaImmersioneOperativa = vm.TipologieImmersioneOperativeCatalogo.Single(item => item.TipologiaImmersioneOperativaId == regola.TipologiaImmersioneOperativaId);
        var fascia = vm.FasceProfonditaCatalogo.Single(item => item.FasciaProfonditaId == regola.FasciaProfonditaId);
        row.ProfonditaMetri = fascia.MetriDa.ToString();
        row.OreImmersione = ore.ToString();
        row.CategoriaContabileOre = vm.CategorieContabiliOreCatalogo.Single(item => item.CategoriaContabileOreId == regola.CategoriaContabileOreId);
        var servizio = LeggiBozzaServizio(vm);
        servizio.OperatoriSubEsterni.Add(new ServizioOperatoreSubEsterno
        {
            PerId = 402, Nominativo = "COLLAUDO Esterno", Reparto = "Reparto test", GruppoOperativoId = 1,
        });
        servizio.Immersioni.Single().PartecipazioniEsterne.Add(new ServizioOperatoreSubEsternoImmersione
        {
            ServizioOperatoreSubEsternoId = 402,
            TipologiaImmersioneOperativaId = regola.TipologiaImmersioneOperativaId,
            FasciaProfonditaId = regola.FasciaProfonditaId,
            ProfonditaMetri = fascia.MetriDa,
            OreImmersione = ore,
            CategoriaContabileOreId = regola.CategoriaContabileOreId,
        });
        return (repository.SaveServizioGiornaliero(servizio), vm, row.ImportoStimato);
    }

    private static void TestTariffeFinaliImmersioni() => ConDatabaseServiziIsolato(repository =>
    {
        PreparaOperatoreCalcoli(repository);
        decimal[] aria = [5m, 10m, 20m, 28m, 38m];
        decimal[] miscele = [10m, 15m, 18m, 24m];
        decimal[] camera = [4m, 8m, 16m, 22.4m];
        var regole = repository.GetCataloghiServizio().RegoleContabiliImmersione;
        AssertEqual(34, regole.Count, "Numero casi tariffari");
        foreach (var regola in regole)
        {
            var baseOrdinaria = regola.TipologiaImmersioneOperativaId switch
            {
                1 => aria[regola.FasciaProfonditaId - 1],
                2 => 30m,
                3 => miscele[regola.FasciaProfonditaId - 1],
                _ => 0m,
            };
            var tariffaAttesa = regola.CategoriaContabileOreId switch
            {
                2 => baseOrdinaria / 2m,
                3 => baseOrdinaria * 1.25m,
                4 => camera[regola.FasciaProfonditaId - 1],
                _ => baseOrdinaria,
            };
            AssertEqual(tariffaAttesa, regola.Tariffa, $"Tariffa finale regola {regola.RegolaContabileImmersioneId}");
            var ore = regola.RegolaContabileImmersioneId % 2 == 0 ? 1.5m : 1m;
            var caso = CreaCasoCalcoli(repository, regola, ore);
            var importoAtteso = tariffaAttesa * ore;
            AssertEqual(Math.Round(importoAtteso, 2, MidpointRounding.AwayFromZero), caso.Anteprima, "Importo nel riepilogo");
            var mensile = repository.GetContabilitaGiornateImpiego(2026, 8).SmzImmersioni;
            AssertEqual(2, mensile.Count, "Operatore interno ed esterno nel mensile");
            foreach (var item in mensile)
            {
                AssertEqual(importoAtteso, item.Importo, $"Importo mensile PerID {item.PerId}, regola {regola.RegolaContabileImmersioneId}");
                AssertEqual(ore, item.OreOrd + item.OreAdd + item.OreSper + item.OreCi, "Ore contabili non ridotte");
            }
            repository.DeleteServizioGiornaliero(caso.Id);
        }
    });

    private static void TestTariffeFinaliPersonalizzate() => ConDatabaseServiziIsolato(repository =>
    {
        PreparaOperatoreCalcoli(repository);
        var regole = repository.GetCataloghiServizio().RegoleContabiliImmersione;
        var personalizzate = new Dictionary<int, decimal> { [12] = 3.33m, [21] = 4.75m, [23] = 7.77m };
        foreach (var (id, tariffa) in personalizzate) regole.Single(item => item.RegolaContabileImmersioneId == id).Tariffa = tariffa;
        repository.UpdateRegoleContabiliImmersione(regole);
        DatabaseInitializer.EnsureDatabase();
        foreach (var (id, tariffa) in personalizzate)
        {
            var regola = repository.GetCataloghiServizio().RegoleContabiliImmersione.Single(item => item.RegolaContabileImmersioneId == id);
            AssertEqual(tariffa, regola.Tariffa, "Tariffa personalizzata dopo riavvio");
            var caso = CreaCasoCalcoli(repository, regola, 1m);
            AssertEqual(tariffa, caso.Anteprima, "Tariffa personalizzata nel riepilogo");
            AssertTrue(repository.GetContabilitaGiornateImpiego(2026, 8).SmzImmersioni.All(item => item.Importo == tariffa), "Coefficiente applicato nuovamente alla tariffa personalizzata.");

            // Anche l'editor del servizio aperto deve riflettere il salvataggio delle tariffe.
            var editor = caso.Vm.RegoleContabiliEditorItems.Single(item => item.RegolaContabileImmersioneId == id);
            editor.Tariffa = (tariffa + 1m).ToString();
            caso.Vm.SaveTariffeContabiliCommand.Execute(null);
            AssertEqual(tariffa + 1m, caso.Vm.ServizioPartecipazioniContabiliUnicheBozza.Single().ImportoStimato, "Riepilogo aggiornato dopo modifica tariffa");
            repository.DeleteServizioGiornaliero(caso.Id);
        }
    });

    private static void RipristinaCatalogoOriginalePerTest()
    {
        using var connection = new SqliteConnection(DatabasePaths.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE RegoleContabiliImmersione AS r
            SET Tariffa = CASE WHEN CategoriaContabileOreId = 4 THEN 2.48 ELSE (
                SELECT ord.Tariffa FROM RegoleContabiliImmersione ord
                WHERE ord.TipologiaImmersioneOperativaId = r.TipologiaImmersioneOperativaId
                  AND ord.FasciaProfonditaId = r.FasciaProfonditaId AND ord.CategoriaContabileOreId = 1
            ) END WHERE CategoriaContabileOreId <> 1;
            """;
        command.ExecuteNonQuery();
    }

    private static void TestAggiornamentoTariffeOriginali() => ConDatabaseServiziIsolato(repository =>
    {
        RipristinaCatalogoOriginalePerTest();
        DatabaseInitializer.EnsureDatabase();
        foreach (var attesa in CataloghiServizio.RegoleContabiliImmersione)
        {
            var letta = repository.GetCataloghiServizio().RegoleContabiliImmersione.Single(item => item.RegolaContabileImmersioneId == attesa.RegolaContabileImmersioneId);
            AssertEqual(attesa.Tariffa, letta.Tariffa, "Aggiornamento catalogo originale");
        }
        DatabaseInitializer.EnsureDatabase();
        AssertEqual(2.5m, repository.GetCataloghiServizio().RegoleContabiliImmersione.Single(item => item.RegolaContabileImmersioneId == 12).Tariffa, "Aggiornamento non ripetuto al secondo avvio");
    });

    private static string ImprontaTariffePerTest(PersonaleRepository repository) => string.Join(";",
        repository.GetCataloghiServizio().RegoleContabiliImmersione.OrderBy(item => item.RegolaContabileImmersioneId)
            .Select(item => $"{item.RegolaContabileImmersioneId}|{item.Tariffa}|{item.Attiva}|{item.DataInizioValidita}|{item.DataFineValidita}"));

    private static void TestConservazioneTariffePersonalizzate() => ConDatabaseServiziIsolato(repository =>
    {
        foreach (var sql in new[]
        {
            "UPDATE RegoleContabiliImmersione SET Tariffa = 3.21 WHERE RegolaContabileImmersioneId = 12;",
            "UPDATE RegoleContabiliImmersione SET Attiva = 0 WHERE RegolaContabileImmersioneId = 12;",
            "UPDATE RegoleContabiliImmersione SET DataInizioValidita = '2026-01-01' WHERE RegolaContabileImmersioneId = 12;",
        })
        {
            RipristinaCatalogoOriginalePerTest();
            using (var connection = new SqliteConnection(DatabasePaths.ConnectionString))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "UPDATE RegoleContabiliImmersione SET Attiva = 1, DataInizioValidita = NULL, DataFineValidita = NULL;" + sql;
                command.ExecuteNonQuery();
            }
            var prima = ImprontaTariffePerTest(repository);
            DatabaseInitializer.EnsureDatabase();
            AssertEqual(prima, ImprontaTariffePerTest(repository), "Catalogo personalizzato riscritto all'avvio");
        }
    });

    private static void TestRigenerazioneImportiMensili() => ConDatabaseServiziIsolato(repository =>
    {
        PreparaOperatoreCalcoli(repository);
        var regola = repository.GetCataloghiServizio().RegoleContabiliImmersione.Single(item => item.RegolaContabileImmersioneId == 12);
        CreaCasoCalcoli(repository, regola, 1m);
        var precedente = repository.GetContabilitaGiornateImpiego(2026, 8);
        foreach (var item in precedente.SmzImmersioni) item.Importo = 1.25m;
        repository.SaveElaborazioneMensile(2026, 8, precedente);
        DatabaseInitializer.EnsureDatabase();
        AssertTrue(repository.GetElaborazioneMensileSnapshot(2026, 8)!.SmzImmersioni.All(item => item.Importo == 1.25m), "Chiusura congelata alterata automaticamente.");
        var corrente = repository.GetContabilitaGiornateImpiego(2026, 8);
        AssertTrue(corrente.SmzImmersioni.All(item => item.Importo == 2.5m), "Il calcolo corrente applica ancora la doppia riduzione.");
        repository.SaveElaborazioneMensile(2026, 8, corrente);
        AssertTrue(repository.GetElaborazioneMensileSnapshot(2026, 8)!.SmzImmersioni.All(item => item.Importo == 2.5m), "Importi rigenerati non salvati.");
    });
}
