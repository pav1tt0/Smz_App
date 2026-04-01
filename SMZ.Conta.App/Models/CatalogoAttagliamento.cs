namespace SMZ.Conta.App.Models;

public static class CatalogoAttagliamento
{
    public static IReadOnlyList<MisuraAttagliamentoDefinizione> MisurePredefinite { get; } =
    [
        new MisuraAttagliamentoDefinizione { OrdineScheda = 1, NumeroScheda = "1", Voce = "Misura petto", EtichettaScheda = "Misura petto", UnitaScheda = "cm." },
        new MisuraAttagliamentoDefinizione { OrdineScheda = 2, NumeroScheda = "2", Voce = "Misura vita", EtichettaScheda = "Misura vita", UnitaScheda = "cm." },
        new MisuraAttagliamentoDefinizione { OrdineScheda = 3, NumeroScheda = "3", Voce = "Misura fianchi", EtichettaScheda = "Misura fianchi", UnitaScheda = "cm." },
        new MisuraAttagliamentoDefinizione { OrdineScheda = 4, NumeroScheda = "4", Voce = "Lunghezza gamba interna", EtichettaScheda = "Lunghezza gamba interna", UnitaScheda = "cm." },
        new MisuraAttagliamentoDefinizione { OrdineScheda = 5, NumeroScheda = "5", Voce = "Altezza", EtichettaScheda = "Altezza", UnitaScheda = "cm." },
        new MisuraAttagliamentoDefinizione { OrdineScheda = 6, NumeroScheda = "6", Voce = "Misura testa", EtichettaScheda = "Misura testa", UnitaScheda = "cm." },
        new MisuraAttagliamentoDefinizione { OrdineScheda = 7, NumeroScheda = "7", Voce = "Lunghezza piede", EtichettaScheda = "Lunghezza piede", UnitaScheda = "cm." },
    ];

    public static bool IsPredefinita(string? voce) =>
        MisurePredefinite.Any(item => string.Equals(item.Voce, voce?.Trim(), StringComparison.OrdinalIgnoreCase));

    public static MisuraAttagliamentoDefinizione? TrovaPerVoce(string? voce) =>
        MisurePredefinite.FirstOrDefault(item => string.Equals(item.Voce, voce?.Trim(), StringComparison.OrdinalIgnoreCase));
}
