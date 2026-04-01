namespace SMZ.Conta.App.Models;

public sealed class MisuraAttagliamentoDefinizione
{
    public int OrdineScheda { get; init; }

    public string NumeroScheda { get; init; } = string.Empty;

    public string Voce { get; init; } = string.Empty;

    public string EtichettaScheda { get; init; } = string.Empty;

    public string UnitaScheda { get; init; } = string.Empty;
}
