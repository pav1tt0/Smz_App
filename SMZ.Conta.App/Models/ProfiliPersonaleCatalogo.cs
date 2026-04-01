namespace SMZ.Conta.App.Models;

public static class ProfiliPersonaleCatalogo
{
    public const string OperatoreSubacqueo = "Operatore Subacqueo";
    public const string Sanitario = "Sanitario";
    public const string LegacySmzOperativo = "SMZ operativo";

    public static IReadOnlyList<string> Tutti { get; } =
    [
        OperatoreSubacqueo,
        Sanitario,
    ];

    public static bool IsSanitario(string? profiloPersonale) =>
        string.Equals(Normalizza(profiloPersonale), Sanitario, StringComparison.OrdinalIgnoreCase);

    public static bool IsOperatoreSubacqueo(string? profiloPersonale) =>
        string.Equals(Normalizza(profiloPersonale), OperatoreSubacqueo, StringComparison.OrdinalIgnoreCase);

    public static string Normalizza(string? profiloPersonale)
    {
        if (string.IsNullOrWhiteSpace(profiloPersonale)
            || string.Equals(profiloPersonale, LegacySmzOperativo, StringComparison.OrdinalIgnoreCase))
        {
            return OperatoreSubacqueo;
        }

        return profiloPersonale.Trim();
    }
}
