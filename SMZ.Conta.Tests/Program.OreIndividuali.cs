using System.Globalization;
using SMZ.Conta.App.Models;
using SMZ.Conta.App.ViewModels;

namespace SMZ.Conta.Tests;

internal static partial class Program
{
    private static void TestOreIndividualiServizio() => ConDatabaseServiziIsolato(repository =>
    {
        foreach (var id in new[] { 711, 712, 713 })
            repository.SavePersonale(CreaPersonale(id, "COLLAUDO", $"Ore {id}", $"ORE{id}", $"ore{id}"), true);
        var vm = new MainWindowViewModel { ServizioData = "11/11/2026" };
        foreach (var persona in vm.ServizioPartecipantiBozza) persona.Presente = true;
        vm.AddOperatoreSubEsternoCommand.Execute(null);
        var esterno = vm.ServizioOperatoriSubEsterniBozza.Single();
        esterno.PerId = "714";
        esterno.Nominativo = "COLLAUDO Esterno";
        esterno.Reparto = "Reparto test";

        var prima = vm.ServizioPartecipazioniContabiliUnicheBozza.First();
        prima.TipologiaImmersioneOperativa = vm.TipologieImmersioneOperativeCatalogo.Single(tipo => tipo.Codice == "ARA_ASAS");
        prima.ProfonditaMetri = "10";
        prima.CategoriaContabileOre = vm.CategorieContabiliOreCatalogo.Single(categoria => categoria.Codice == "ORD");
        prima.OreImmersione = "2";
        AssertEqual(4, vm.ServizioPartecipazioniContabiliUnicheBozza.Count, "Tre interni e un esterno nel dettaglio");
        AssertTrue(vm.ServizioPartecipazioniContabiliUnicheBozza.All(row => row.OreImmersione == "2"),
            "Prima compilazione delle ore riportata sulle righe sottostanti");
        var attese = vm.ServizioPartecipazioniContabiliUnicheBozza.ToDictionary(row => row.PerId, _ => 2m);

        void ModificaOre(int perId, decimal ore)
        {
            vm.ServizioPartecipazioniContabiliUnicheBozza.Single(row => row.PerId == perId).OreImmersione = ore.ToString(CultureInfo.CurrentCulture);
            attese[perId] = ore;
            VerificaBozza();
        }

        void VerificaBozza()
        {
            foreach (var row in vm.ServizioPartecipazioniContabiliUnicheBozza)
                AssertEqual(attese[row.PerId], decimal.Parse(row.OreImmersione.Replace(',', '.'), CultureInfo.InvariantCulture),
                    $"Ore individuali in bozza PerID {row.PerId}");
        }

        void SalvaERiapri(bool nuovaFinestra = false)
        {
            _ = LeggiBozzaServizio(vm); // Validazione prima del comando, senza finestre modali in caso di errore.
            vm.SaveServizioCommand.Execute(null);
            var summary = repository.GetServiziGiornalieriRecenti().Single();
            var salvato = repository.GetServizioGiornalieroById(summary.ServizioGiornalieroId)!;
            foreach (var partecipante in salvato.Partecipanti)
            {
                var ore = salvato.Immersioni.SelectMany(immersione => immersione.Partecipazioni)
                    .Single(row => row.ServizioPartecipanteId == partecipante.ServizioPartecipanteId).OreImmersione;
                AssertEqual(attese[partecipante.PerId], ore, $"Ore salvate interno {partecipante.PerId}");
            }
            var subEsterno = salvato.OperatoriSubEsterni.Single();
            AssertEqual(attese[subEsterno.PerId], salvato.Immersioni.SelectMany(immersione => immersione.PartecipazioniEsterne).Single().OreImmersione,
                "Ore salvate esterno");
            if (nuovaFinestra) vm = new MainWindowViewModel();
            vm.SelectedServizioSalvato = summary;
            vm.OpenServizioCommand.Execute(null);
            VerificaBozza();
        }

        // Una modifica alla riga intermedia deve lasciare intatte le altre, anche alla riapertura.
        ModificaOre(712, 3m);
        SalvaERiapri();
        ModificaOre(prima.PerId, 1.5m);
        SalvaERiapri();
        ModificaOre(714, 2.5m);
        vm.AddImmersioneCommand.Execute(null);
        vm.ServizioPartecipazioniContabiliUnicheBozza.Single(row => row.PerId == 714).NumeroImmersione = 2;
        SalvaERiapri(nuovaFinestra: true);
        AssertEqual(2, vm.ServizioPartecipazioniContabiliUnicheBozza.Single(row => row.PerId == 714).NumeroImmersione,
            "Ore individuali conservate anche su un'altra immersione");
    });
}
