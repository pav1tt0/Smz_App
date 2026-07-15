namespace SMZ.Conta.App.Models;

public sealed record AccessSession(
    int PerId,
    string DisplayName,
    AccessRole Role,
    bool MustChangePassword)
{
    public bool IsAdministrator => Role == AccessRole.Administrator;

    public string RoleDisplayName => Role.ToDisplayName();

    public string UserDisplayName => PerId <= 0
        ? DisplayName
        : string.Equals(DisplayName.Trim(), $"PerID {PerId}", StringComparison.OrdinalIgnoreCase)
            ? $"PerID {PerId}"
            : $"{DisplayName} · PerID {PerId}";

    internal static AccessSession SystemAdministrator { get; } =
        new(0, "Sistema", AccessRole.Administrator, false);
}
