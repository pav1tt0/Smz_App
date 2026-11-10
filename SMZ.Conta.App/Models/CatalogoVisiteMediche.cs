namespace SMZ.Conta.App.Models;

public static class CatalogoVisiteMediche
{
    public static IReadOnlyList<TipoVisitaMedica> Tutte { get; } =
    [
        new TipoVisitaMedica
        {
            Codice = "MANTENIMENTO_BREVETTO_MM",
            Descrizione = "Visita periodica mantenimento brevetto M.M.",
            MesiValidita = 24,
        },
        new TipoVisitaMedica
        {
            Codice = "DLGS_81_08",
            Descrizione = "Visita periodica D.Lgs. 81/08",
            MesiValidita = 12,
        },
        new TipoVisitaMedica
        {
            Codice = "BIMESTRALE",
            Descrizione = "Visita periodica bimestrale",
            DescrizioniAlternative = ["Bimestrale", "Visita bimestrale", "Visite bimestrali"],
            MesiValidita = 2,
            // Impostare a true per riabilitare menu, scheda e scadenze.
            Attiva = false,
        },
    ];

    public static IEnumerable<TipoVisitaMedica> Attive => Tutte.Where(tipo => tipo.Attiva);

    public static bool IsAttiva(string descrizione) => TrovaPerDescrizione(descrizione)?.Attiva ?? true;

    public static IEnumerable<string> DescrizioniDisattivate => Tutte
        .Where(tipo => !tipo.Attiva)
        .SelectMany(tipo => tipo.DescrizioniAlternative.Prepend(tipo.Descrizione));

    public static TipoVisitaMedica? TrovaPerDescrizione(string descrizione)
    {
        var valore = descrizione.Trim();
        return Tutte.FirstOrDefault(tipo =>
            tipo.Descrizione.Equals(valore, StringComparison.OrdinalIgnoreCase)
            || tipo.DescrizioniAlternative.Contains(valore, StringComparer.OrdinalIgnoreCase));
    }
}
