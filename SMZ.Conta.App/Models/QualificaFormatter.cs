namespace SMZ.Conta.App.Models;

public static class QualificaFormatter
{
    // Sigle di visualizzazione: la qualifica estesa resta il valore salvato.
    public static IReadOnlyDictionary<string, string> Abbreviazioni { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Primo Dirigente"] = "P.D.",
            ["Vice Questore"] = "V.Q.",
            ["Vice Questore Aggiunto"] = "V.Q.A.",
            ["Commissario Capo"] = "Comm.C.",
            ["Commissario"] = "Comm.",
            ["Vice Commissario"] = "V.Comm.",
            ["Sostituto Commissario C."] = "Sost.C.C.",
            ["Sostituto Commissario"] = "Sost.C.",
            ["Sostituto Commissario C. Tecnico"] = "Sost.C.C.T.",
            ["Sostituto Commissario Tecnico"] = "Sost.C.T.",
            ["Ispettore Superiore"] = "Isp.S.",
            ["Ispettore Capo"] = "Isp.C.",
            ["Ispettore"] = "Isp.",
            ["Vice Ispettore"] = "V.Isp.",
            ["Ispettore Superiore Tecnico"] = "Isp.S.T.",
            ["Ispettore Capo Tecnico"] = "Isp.C.T.",
            ["Ispettore Tecnico"] = "Isp.T.",
            ["Vice Ispettore Tecnico"] = "V.Isp.T.",
            ["Sovrintendente Capo C."] = "Sov.C.C.",
            ["Sovrintendente Capo"] = "Sov.C.",
            ["Sovrintendente"] = "Sov.",
            ["Vice Sovrintendente"] = "V.Sov.",
            ["Assistente Capo C."] = "Ass.C.C.",
            ["Assistente Capo"] = "Ass.C.",
            ["Assistente"] = "Ass.",
            ["Agente Scelto"] = "Agt.S.",
            ["Agente"] = "Agt.",
            ["Medico Capo"] = "MED.C.",
            ["Medico Principale"] = "MED.P.",
            ["Medico"] = "MED.",
            ["Infermiere"] = "INF.",
        };

    public static string Abbrevia(string? qualifica)
    {
        var estesa = AbbreviaPerVisualizzazione(qualifica).Trim();
        return Abbreviazioni.TryGetValue(estesa, out var sigla) ? sigla : estesa;
    }

    private static readonly Dictionary<string, int> GerarchiaOrdine = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Primo Dirigente"] = -2,
        ["Vice Questore"] = -1,
        ["Vice Questore Aggiunto"] = 0,
        ["Commissario Capo"] = 1,
        ["Commissario"] = 2,
        ["Vice Commissario"] = 3,
        ["Sostituto Commissario C."] = 4,
        ["Sostituto Commissario"] = 5,
        ["Sostituto Commissario C. Tecnico"] = 6,
        ["Sostituto Commissario Tecnico"] = 7,
        ["Ispettore Superiore"] = 8,
        ["Ispettore Capo"] = 9,
        ["Ispettore"] = 10,
        ["Vice Ispettore"] = 11,
        ["Ispettore Superiore Tecnico"] = 12,
        ["Ispettore Capo Tecnico"] = 13,
        ["Ispettore Tecnico"] = 14,
        ["Vice Ispettore Tecnico"] = 15,
        ["Sovrintendente Capo C."] = 16,
        ["Sovrintendente Capo"] = 17,
        ["Sovrintendente"] = 18,
        ["Vice Sovrintendente"] = 19,
        ["Assistente Capo C."] = 20,
        ["Assistente Capo"] = 21,
        ["Assistente"] = 22,
        ["Agente Scelto"] = 23,
        ["Agente"] = 24,
    };

    private static readonly Dictionary<string, int> RuoloSanitarioOrdine = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Medico Capo"] = 0,
        ["Medico Principale"] = 1,
        ["Medico"] = 2,
        ["Infermiere"] = 3,
    };

    public static string AbbreviaPerVisualizzazione(string? qualifica)
    {
        if (string.IsNullOrWhiteSpace(qualifica))
        {
            return string.Empty;
        }

        return qualifica.Replace(" Coordinatore", " C.", StringComparison.OrdinalIgnoreCase);
    }

    public static int GetGerarchiaOrdine(string? qualifica, bool isSanitario, string? ruoloSanitario = null)
    {
        if (isSanitario || RuoloSanitarioOrdine.ContainsKey(AbbreviaPerVisualizzazione(qualifica).Trim()))
        {
            return 10_000 + GetOrdineSanitario(qualifica, ruoloSanitario);
        }

        var chiave = AbbreviaPerVisualizzazione(qualifica);
        return !string.IsNullOrWhiteSpace(chiave) && GerarchiaOrdine.TryGetValue(chiave.Trim(), out var ordine)
            ? ordine
            : 1_000;
    }

    public static long GetChiaveOrdinamento(string? qualifica, DateOnly? decorrenza = null,
        bool isSanitario = false, string? ruoloSanitario = null) =>
        GetGerarchiaOrdine(qualifica, isSanitario, ruoloSanitario) * 4_000_000L
        + (decorrenza ?? DateOnly.MaxValue).DayNumber;

    private static int GetOrdineSanitario(string? qualifica, string? ruoloSanitario)
    {
        var chiaveQualifica = AbbreviaPerVisualizzazione(qualifica);
        if (!string.IsNullOrWhiteSpace(chiaveQualifica) && RuoloSanitarioOrdine.TryGetValue(chiaveQualifica.Trim(), out var ordineQualifica))
        {
            return ordineQualifica;
        }

        return !string.IsNullOrWhiteSpace(ruoloSanitario) && RuoloSanitarioOrdine.TryGetValue(ruoloSanitario.Trim(), out var ordineRuolo)
            ? ordineRuolo
            : 99;
    }
}
