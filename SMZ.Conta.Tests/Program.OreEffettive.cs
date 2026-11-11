using SMZ.Conta.App.Data;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.Tests;

internal static partial class Program
{
    private static void TestOreImmersioneEffettive() => ConDatabaseServiziIsolato(repository =>
    {
        repository.SavePersonale(CreaPersonale(801, "Alfa", "Sub", "PROCAPITE801", "procapite801"), true);
        repository.SavePersonale(CreaPersonale(802, "Beta", "Supporto", "PROCAPITE802", "procapite802"), true);
        var gruppo = repository.GetCataloghiServizio().GruppiOperativi.First().GruppoOperativoId;
        var servizio = new ServizioGiornaliero
        {
            DataServizio = new DateOnly(2026, 11, 11),
            NumeroOrdineServizio = "PRO-CAPITE",
            Partecipanti =
            [
                new() { PerId = 801, GruppoOperativoId = gruppo, Presente = true },
                new() { PerId = 802, GruppoOperativoId = gruppo, Presente = true },
            ],
            SupportiOccasionali = [new() { Nominativo = "Supporto occasionale", Presente = true }],
        };

        ServizioGiornalieroSummary SalvaERileggi()
        {
            servizio.ServizioGiornalieroId = repository.SaveServizioGiornaliero(servizio);
            return repository.GetServiziGiornalieriRecenti().Single();
        }

        var riepilogo = SalvaERileggi();
        AssertEqual("", riepilogo.OreImmersioneEffettiveDescrizione, "Ore senza immersioni");
        AssertEqual("Nessuna immersione registrata.", riepilogo.DettaglioImmersioniDescrizione, "Dettaglio senza immersioni");

        servizio.Immersioni =
        [
            new()
            {
                NumeroImmersione = 1,
                Partecipazioni = [new() { ServizioPartecipanteId = 801, OreImmersione = 2m }],
            },
        ];
        riepilogo = SalvaERileggi();
        AssertEqual("2", riepilogo.OreImmersioneEffettiveDescrizione, "Ore con un solo subacqueo");

        servizio.OperatoriSubEsterni = [new() { PerId = 803, Nominativo = "Sub esterno", GruppoOperativoId = gruppo }];
        servizio.Immersioni[0].PartecipazioniEsterne =
            [new() { ServizioOperatoreSubEsternoId = 803, OreImmersione = 2m }];
        riepilogo = SalvaERileggi();
        AssertEqual(4m, riepilogo.OreImmersioneTotali, "Totale interno ed esterno");
        AssertEqual("2", riepilogo.OreImmersioneEffettiveDescrizione, "Valori uguali mostrati una sola volta");
        AssertTrue(riepilogo.DettaglioImmersioniDescrizione.Contains("Ore: 2  |"), "Ore effettive nel messaggio dell'elenco.");

        servizio.Immersioni[0].PartecipazioniEsterne[0].OreImmersione = 3m;
        riepilogo = SalvaERileggi();
        AssertEqual("2-3", riepilogo.OreImmersioneEffettiveDescrizione, "Ore diverse mostrate senza media");
        AssertTrue(riepilogo.DettaglioImmersioniDescrizione.Contains("Ore: 2-3  |"), "Ore diverse nel messaggio dell'elenco.");

        servizio.Immersioni.Add(new ServizioImmersione
        {
            NumeroImmersione = 2,
            Partecipazioni = [new() { ServizioPartecipanteId = 801, OreImmersione = 2m }],
            PartecipazioniEsterne = [new() { ServizioOperatoreSubEsternoId = 803, OreImmersione = 1.5m }],
        });
        riepilogo = SalvaERileggi();
        AssertEqual($"{1.5m:0.##}-2-3", riepilogo.OreImmersioneEffettiveDescrizione, "Ore registrate ordinate anche su piu immersioni, senza somme");
    });
}
