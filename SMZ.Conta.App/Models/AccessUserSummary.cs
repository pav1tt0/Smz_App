namespace SMZ.Conta.App.Models;

public sealed class AccessUserSummary
{
    public int PerId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public AccessRole Role { get; init; }
    public bool IsActive { get; init; }
    public bool MustChangePassword { get; init; }
    public DateTime? LastAccess { get; init; }
    public string RoleDisplayName => Role.ToDisplayName();
    public string StatusDisplayName => IsActive ? "Attivo" : "Sospeso";
    public string LastAccessDisplay => LastAccess?.ToString("dd/MM/yyyy HH:mm") ?? "Mai effettuato";
}
