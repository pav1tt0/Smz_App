namespace SMZ.Conta.App.Models;

public static class StatoServizioPersonaleCatalogo
{
    public const string Attivo = "Attivo";
    public const string Trasferito = "Trasferito";
    public const string Cessato = "Cessato";

    public static IReadOnlyList<string> Tutti { get; } =
    [
        Attivo,
        Trasferito,
        Cessato,
    ];

    public static string Normalizza(string? statoServizio)
    {
        if (string.IsNullOrWhiteSpace(statoServizio))
        {
            return Attivo;
        }

        var value = statoServizio.Trim();
        return Tutti.Contains(value, StringComparer.OrdinalIgnoreCase)
            ? Tutti.First(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase))
            : Attivo;
    }
}
