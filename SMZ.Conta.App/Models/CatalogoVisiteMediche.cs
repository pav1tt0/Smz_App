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
            MesiValidita = 2,
        },
    ];

    public static TipoVisitaMedica? TrovaPerDescrizione(string descrizione)
    {
        return Tutte.FirstOrDefault(tipo => tipo.Descrizione.Equals(descrizione.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
