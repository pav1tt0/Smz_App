using SMZ.Conta.App.Data;
using SMZ.Conta.App.ViewModels;

namespace SMZ.Conta.Tests;

internal static partial class Program
{
    private static void TestIncarichiNelRiepilogo() => ConDatabaseServiziIsolato(repository =>
    {
        foreach (var id in new[] { 501, 502, 503, 504 })
            repository.SavePersonale(CreaPersonale(id, "COLLAUDO", $"Ruolo {id}", $"RUOLO{id}", $"ruolo{id}"), true);
        repository.SavePersonale(CreaSanitario(505, "COLLAUDO", "Sanitario", "RUOLO505", "ruolo505"), true);
        var vm = new MainWindowViewModel { ServizioData = "01/09/2026" };
        foreach (var persona in vm.ServizioPartecipantiBozza) persona.Presente = true;
        PersonaleListItemViewModel Persona(int id) => vm.OperatoriServizioPresentiDisponibili.Single(item => item.PerId == id);
        var prima = vm.ServizioImmersioniBozza[0];
        var seconda = vm.ServizioImmersioniBozza[1];
        prima.DirettoreImmersione = Persona(501);
        prima.OperatoreSoccorso = Persona(502);
        prima.AssistenteBlsd = Persona(503);
        prima.AssistenteSanitario = Persona(505);
        seconda.DirettoreImmersione = Persona(504);
        seconda.AssistenteBlsd = Persona(501);
        ServizioPartecipanteImmersioneUnicoDraftViewModel Riga(int id) => vm.ServizioPartecipazioniContabiliUnicheBozza.Single(item => item.PerId == id);
        AssertEqual($"1{Environment.NewLine}2", Riga(501).NumeriImmersioneRuolo, "Immersioni degli incarichi");
        AssertEqual($"Direttore immersione{Environment.NewLine}Assistenza BLSD", Riga(501).RuoliImmersione, "Descrizioni complete senza numeri");
        AssertEqual("1", Riga(502).NumeriImmersioneRuolo, "Immersione operatore soccorso");
        AssertEqual("Operatore soccorso", Riga(502).RuoliImmersione, "Descrizione soccorso");
        AssertEqual("Assistenza BLSD", Riga(503).RuoliImmersione, "Descrizione BLSD");
        AssertEqual("2", Riga(504).NumeriImmersioneRuolo, "Immersione secondo direttore");
        AssertEqual("Assistenza sanitaria", Riga(505).RuoliImmersione, "Assistenza sanitaria nel riepilogo");
        AssertTrue(Riga(505).IsSoloAssistenza && !Riga(505).MostraSceltaImmersione, "Il sanitario deve essere mostrato senza partecipazioni contabili.");
        AssertTrue(!Riga(501).MostraSceltaImmersione, "Il numero di un incarico non deve essere sostituito dalla scelta di un'immersione operativa.");
        var modello = LeggiBozzaServizio(vm);
        AssertTrue(modello.Immersioni.All(item => item.Partecipazioni.Count == 0), "Incarichi trasformati in ore d'immersione.");
        var idServizio = repository.SaveServizioGiornaliero(modello);
        vm.SelectedServizioSalvato = repository.GetServiziGiornalieriRecenti().Single(item => item.ServizioGiornalieroId == idServizio);
        vm.OpenServizioCommand.Execute(null);
        AssertEqual($"1{Environment.NewLine}2", Riga(501).NumeriImmersioneRuolo, "Numeri incarichi dopo riapertura");
        AssertEqual("Assistenza sanitaria", Riga(505).RuoliImmersione, "Sanitario dopo riapertura");
        vm.ServizioImmersioniBozza[1].AssistenteBlsd = null;
        vm.ServizioImmersioniBozza[1].DirettoreImmersione = Persona(501);
        vm.ServizioImmersioniBozza[0].DirettoreImmersione = null;
        AssertEqual("2", Riga(501).NumeriImmersioneRuolo, "Numero aggiornato cambiando i menu superiori");
        AssertEqual("Direttore immersione", Riga(501).RuoliImmersione, "Ruolo aggiornato cambiando i menu superiori");
        AssertTrue(Riga(504).MostraSceltaImmersione && !Riga(504).HasIncarichiImmersione, "Operatore senza incarico non torna selezionabile.");
        vm.ServizioImmersioniBozza[0].AssistenteSanitario = null;
        AssertTrue(vm.ServizioPartecipazioniContabiliUnicheBozza.All(item => item.PerId != 505), "Incarico sanitario rimosso ancora presente nel riepilogo.");
    });

    private static void TestIncaricoEPartecipazioneSeparati() => ConDatabaseServiziIsolato(repository =>
    {
        repository.SavePersonale(CreaPersonale(601, "COLLAUDO", "Ruolo e immersione", "RUOLO601", "ruolo601"), true);
        var vm = new MainWindowViewModel { ServizioData = "02/09/2026" };
        vm.ServizioPartecipantiBozza.Single().Presente = true;
        vm.ServizioImmersioniBozza[0].DirettoreImmersione = vm.OperatoriServizioPresentiDisponibili.Single(item => item.PerId == 601);
        var row = vm.ServizioPartecipazioniContabiliUnicheBozza.Single();
        AssertEqual("1", row.NumeriImmersioneRuolo, "Immersione assegnata come direttore");
        AssertTrue(!row.MostraSceltaImmersione, "Scelta operativa mostrata prima della compilazione.");
        row.TipologiaImmersioneOperativa = vm.TipologieImmersioneOperativeCatalogo.Single(item => item.Codice == "ARA_ASAS");
        row.ProfonditaMetri = "10";
        row.OreImmersione = "1";
        row.CategoriaContabileOre = vm.CategorieContabiliOreCatalogo.Single(item => item.Codice == "ORD");
        row.NumeroImmersione = 2;
        AssertTrue(row.MostraSceltaImmersione && row.MostraRuoloOperatore, "Partecipazione operativa distinta non mostrata.");
        var modello = LeggiBozzaServizio(vm);
        AssertEqual(601, modello.Immersioni.Single(item => item.NumeroImmersione == 1).DirettoreImmersionePerId, "Direttore conservato");
        AssertEqual(0, modello.Immersioni.Single(item => item.NumeroImmersione == 1).Partecipazioni.Count, "Partecipazione attribuita all'incarico");
        AssertEqual(1, modello.Immersioni.Single(item => item.NumeroImmersione == 2).Partecipazioni.Count, "Partecipazione operativa persa");
        var id = repository.SaveServizioGiornaliero(modello);
        vm.SelectedServizioSalvato = repository.GetServiziGiornalieriRecenti().Single(item => item.ServizioGiornalieroId == id);
        vm.OpenServizioCommand.Execute(null);
        row = vm.ServizioPartecipazioniContabiliUnicheBozza.Single();
        AssertEqual("1", row.NumeriImmersioneRuolo, "Numero incarico dopo riapertura");
        AssertEqual(2, row.NumeroImmersione, "Numero partecipazione dopo riapertura");
        AssertTrue(row.MostraRuoloOperatore, "Distinzione dei ruoli persa dopo riapertura.");
    });
}
