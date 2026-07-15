namespace SMZ.Conta.App.Models;

public enum AccessRole
{
    Base,
    Administrator,
}

public static class AccessRoleExtensions
{
    public static string ToDatabaseValue(this AccessRole role) => role switch
    {
        AccessRole.Base => "Base",
        AccessRole.Administrator => "Amministratore",
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };

    public static string ToDisplayName(this AccessRole role) => role switch
    {
        AccessRole.Base => "Base",
        AccessRole.Administrator => "Amministratore",
        _ => role.ToString(),
    };

    public static AccessRole ParseDatabaseValue(string value) => value switch
    {
        "Base" => AccessRole.Base,
        "Amministratore" => AccessRole.Administrator,
        _ => throw new InvalidOperationException($"Ruolo di accesso non riconosciuto: {value}"),
    };
}
