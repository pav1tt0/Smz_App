namespace SMZ.Conta.App.Infrastructure;

public static class MailPoliziaHelper
{
    public const string DominioFisso = "@poliziadistato.it";

    public static string Compose(string userPart)
    {
        var normalized = NormalizeUserPart(userPart, null);
        return string.IsNullOrWhiteSpace(normalized) ? string.Empty : normalized + DominioFisso;
    }

    public static string NormalizeUserPart(string value, string? fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        var atIndex = trimmed.IndexOf('@');
        if (atIndex < 0)
        {
            return trimmed;
        }

        if (!trimmed.EndsWith(DominioFisso, StringComparison.OrdinalIgnoreCase))
        {
            var prefix = string.IsNullOrWhiteSpace(fieldName) ? "Mail" : fieldName;
            throw new InvalidOperationException($"{prefix}: usare solo il nome utente oppure un indirizzo {DominioFisso}.");
        }

        return trimmed[..atIndex];
    }

    public static string ExtractUserPart(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        var atIndex = trimmed.IndexOf('@');
        return atIndex > 0 ? trimmed[..atIndex] : trimmed;
    }
}
