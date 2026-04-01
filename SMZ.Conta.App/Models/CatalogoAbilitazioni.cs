namespace SMZ.Conta.App.Models;

public static class CatalogoAbilitazioni
{
    public static IReadOnlyList<TipoAbilitazione> Tutte { get; } =
    [
        new TipoAbilitazione { TipoAbilitazioneId = 23, Codice = "SMZ_ARA60_ARO12", Descrizione = "SMZ ARA 60 - ARO 12", Categoria = "Subacquea", RichiedeLivello = false, RichiedeScadenza = false, RichiedeProfondita = false },
        new TipoAbilitazione { TipoAbilitazioneId = 24, Codice = "SMZ_ARA60_ARO12_ARM24", Descrizione = "SMZ ARA 60 - ARO 12 - ARM 24", Categoria = "Subacquea", RichiedeLivello = false, RichiedeScadenza = false, RichiedeProfondita = false },
        new TipoAbilitazione { TipoAbilitazioneId = 26, Codice = "SMZ_ARA60_ARO12_ARM54", Descrizione = "SMZ ARA 60 - ARO 12 - ARM 54", Categoria = "Subacquea", RichiedeLivello = false, RichiedeScadenza = false, RichiedeProfondita = false },
        new TipoAbilitazione { TipoAbilitazioneId = 27, Codice = "SMZ_ARA39_ARO12", Descrizione = "SMZ ARA 39 - ARO 12", Categoria = "Subacquea", RichiedeLivello = false, RichiedeScadenza = false, RichiedeProfondita = false },
        new TipoAbilitazione { TipoAbilitazioneId = 28, Codice = "SMZ_ASAS_15", Descrizione = "SMZ ASAS 15", Categoria = "Subacquea", RichiedeLivello = false, RichiedeScadenza = false, RichiedeProfondita = false },
        new TipoAbilitazione { TipoAbilitazioneId = 29, Codice = "SMZ_ASAS_60", Descrizione = "SMZ ASAS 60", Categoria = "Subacquea", RichiedeLivello = false, RichiedeScadenza = false, RichiedeProfondita = false },
        new TipoAbilitazione { TipoAbilitazioneId = 30, Codice = "OSSALC_ARA_15", Descrizione = "OSSALC ARA 15", Categoria = "Subacquea", RichiedeLivello = false, RichiedeScadenza = false, RichiedeProfondita = false },
        new TipoAbilitazione { TipoAbilitazioneId = 5, Codice = "EOR", Descrizione = "EOR", Categoria = "Subacquea", RichiedeLivello = false, RichiedeScadenza = false, RichiedeProfondita = false },
        new TipoAbilitazione { TipoAbilitazioneId = 6, Codice = "TECNICO_IPERBARICO", Descrizione = "Tecnico iperbarico", Categoria = "Tecnica", RichiedeLivello = false, RichiedeScadenza = false, RichiedeProfondita = false },
        new TipoAbilitazione { TipoAbilitazioneId = 7, Codice = "CINE_FOTO", Descrizione = "Cine-foto operatore", Categoria = "Tecnica", RichiedeLivello = false, RichiedeScadenza = false, RichiedeProfondita = false },
        new TipoAbilitazione { TipoAbilitazioneId = 8, Codice = "BLSD", Descrizione = "BLS-D", Categoria = "Sanitaria", RichiedeLivello = false, RichiedeScadenza = true, RichiedeProfondita = false },
        new TipoAbilitazione { TipoAbilitazioneId = 15, Codice = "COMANDANTE_COSTIERO", Descrizione = "Comandante costiero", Categoria = "Nautica", RichiedeLivello = false, RichiedeScadenza = false, RichiedeProfondita = false },
        new TipoAbilitazione { TipoAbilitazioneId = 16, Codice = "MOTORISTA_NAVALE", Descrizione = "Motorista navale", Categoria = "Nautica", RichiedeLivello = false, RichiedeScadenza = false, RichiedeProfondita = false },
        new TipoAbilitazione { TipoAbilitazioneId = 18, Codice = "ISTRUTTORE_TIRO", Descrizione = "Istruttore di tiro", Categoria = "Istruttore", RichiedeLivello = false, RichiedeScadenza = false, RichiedeProfondita = false },
        new TipoAbilitazione { TipoAbilitazioneId = 19, Codice = "ISTRUTTORE_TECNICHE_OPERATIVE", Descrizione = "Istruttore tecniche operative", Categoria = "Istruttore", RichiedeLivello = false, RichiedeScadenza = false, RichiedeProfondita = false },
        new TipoAbilitazione { TipoAbilitazioneId = 20, Codice = "ISTRUTTORE_DIFESA_PERSONALE", Descrizione = "Istruttore difesa personale", Categoria = "Istruttore", RichiedeLivello = false, RichiedeScadenza = false, RichiedeProfondita = false },
        new TipoAbilitazione { TipoAbilitazioneId = 9, Codice = "CORDA", Descrizione = "Esperto manovratore di corda", Categoria = "Tecnica", RichiedeLivello = false, RichiedeScadenza = false, RichiedeProfondita = false },
        new TipoAbilitazione { TipoAbilitazioneId = 12, Codice = "MAESTRO_NUOTO", Descrizione = "Maestro nuoto", Categoria = "Didattica", RichiedeLivello = false, RichiedeScadenza = false, RichiedeProfondita = false },
        new TipoAbilitazione { TipoAbilitazioneId = 11, Codice = "ISTRUTTORE_NUOTO", Descrizione = "Istruttore nuoto", Categoria = "Didattica", RichiedeLivello = false, RichiedeScadenza = false, RichiedeProfondita = false },
        new TipoAbilitazione { TipoAbilitazioneId = 13, Codice = "ASSISTENTE_BAGNANTI", Descrizione = "Assistente bagnanti", Categoria = "Didattica", RichiedeLivello = false, RichiedeScadenza = true, RichiedeProfondita = false },
        new TipoAbilitazione { TipoAbilitazioneId = 17, Codice = "CONDUTTORE_ACQUASCOOTER", Descrizione = "Conduttore acquascooter", Categoria = "Nautica", RichiedeLivello = false, RichiedeScadenza = false, RichiedeProfondita = false },
        new TipoAbilitazione { TipoAbilitazioneId = 14, Codice = "PATENTE_GUIDA", Descrizione = "Patente di guida", Categoria = "Guida", RichiedeLivello = true, RichiedeScadenza = true, RichiedeProfondita = false, LivelliSuggeriti = ["1", "2", "3", "4", "5"] },
        new TipoAbilitazione { TipoAbilitazioneId = 21, Codice = "GRU_BANDIERA", Descrizione = "Abilitazione utilizzo gru bandiera", Categoria = "Mezzi", RichiedeLivello = false, RichiedeScadenza = false, RichiedeProfondita = false },
        new TipoAbilitazione { TipoAbilitazioneId = 22, Codice = "GRU_SEMOVENTE", Descrizione = "Abilitazione gru semovente", Categoria = "Mezzi", RichiedeLivello = false, RichiedeScadenza = false, RichiedeProfondita = false },
        new TipoAbilitazione { TipoAbilitazioneId = 10, Codice = "ALPINISTA", Descrizione = "Alpinista", Categoria = "Tecnica", RichiedeLivello = false, RichiedeScadenza = false, RichiedeProfondita = false },
    ];

    public static TipoAbilitazione DaCodice(string codice)
    {
        return Tutte.Single(tipo => tipo.Codice == codice);
    }

    public static TipoAbilitazione ApplicaSuggerimenti(TipoAbilitazione tipo)
    {
        var riferimento = Tutte.FirstOrDefault(item => item.TipoAbilitazioneId == tipo.TipoAbilitazioneId)
            ?? Tutte.FirstOrDefault(item => string.Equals(item.Codice, tipo.Codice, StringComparison.OrdinalIgnoreCase));

        tipo.LivelliSuggeriti = riferimento?.LivelliSuggeriti ?? [];
        tipo.ProfonditaSuggerite = riferimento?.ProfonditaSuggerite ?? [];
        return tipo;
    }

    public static int GetOrdineVisualizzazione(TipoAbilitazione tipo)
    {
        for (var index = 0; index < Tutte.Count; index++)
        {
            if (Tutte[index].TipoAbilitazioneId == tipo.TipoAbilitazioneId)
            {
                return index;
            }
        }

        return int.MaxValue;
    }
}
