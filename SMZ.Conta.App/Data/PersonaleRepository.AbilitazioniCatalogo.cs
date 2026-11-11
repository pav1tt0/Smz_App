using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.Data;

public sealed partial class PersonaleRepository
{
    public TipoAbilitazione AddTipoAbilitazione(string descrizione)
    {
        var descrizionePulita = descrizione.Trim();
        if (string.IsNullOrWhiteSpace(descrizionePulita))
            throw new InvalidOperationException("Inserisci una descrizione per l'abilitazione.");

        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        using (var existing = connection.CreateCommand())
        {
            existing.Transaction = transaction;
            existing.CommandText = "SELECT COUNT(*) FROM TipiAbilitazione WHERE UPPER(TRIM(Descrizione)) = UPPER($descrizione);";
            existing.Parameters.AddWithValue("$descrizione", descrizionePulita);
            if (Convert.ToInt32(existing.ExecuteScalar()) > 0)
                throw new InvalidOperationException("Abilitazione gia presente: modifica la riga esistente.");
        }

        var id = GetNextIntegerId(connection, transaction, "TipiAbilitazione", "TipoAbilitazioneId");
        var ordine = GetNextIntegerId(connection, transaction, "TipiAbilitazione", "Ordine");
        var tipo = new TipoAbilitazione
        {
            TipoAbilitazioneId = id, Codice = $"PERSONALIZZATA_{id}",
            Descrizione = descrizionePulita, Categoria = "Altre", Ordine = ordine,
        };
        using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = """
            INSERT INTO TipiAbilitazione
                (TipoAbilitazioneId, Codice, Descrizione, Categoria, RichiedeLivello, RichiedeScadenza, RichiedeProfondita, Attiva, Ordine)
            VALUES ($id, $codice, $descrizione, $categoria, 0, 0, 0, 1, $ordine);
            """;
        insert.Parameters.AddWithValue("$id", tipo.TipoAbilitazioneId);
        insert.Parameters.AddWithValue("$codice", tipo.Codice);
        insert.Parameters.AddWithValue("$descrizione", tipo.Descrizione);
        insert.Parameters.AddWithValue("$categoria", tipo.Categoria);
        insert.Parameters.AddWithValue("$ordine", tipo.Ordine);
        insert.ExecuteNonQuery();
        transaction.Commit();
        return tipo;
    }

    public void UpdateTipiAbilitazione(IEnumerable<TipoAbilitazione> items)
    {
        var tipi = items.ToList();
        if (tipi.Any(tipo => tipo.TipoAbilitazioneId <= 0 || string.IsNullOrWhiteSpace(tipo.Descrizione)
            || string.IsNullOrWhiteSpace(tipo.Categoria)))
            throw new InvalidOperationException("Ogni abilitazione deve avere una descrizione e una categoria.");

        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        foreach (var tipo in tipi)
        {
            using var update = connection.CreateCommand();
            update.Transaction = transaction;
            update.CommandText = """
                UPDATE TipiAbilitazione
                SET Descrizione = $descrizione, Categoria = $categoria,
                    RichiedeLivello = $livello, RichiedeScadenza = $scadenza,
                    RichiedeProfondita = $profondita, Attiva = $attiva, Ordine = $ordine
                WHERE TipoAbilitazioneId = $id;
                """;
            update.Parameters.AddWithValue("$id", tipo.TipoAbilitazioneId);
            update.Parameters.AddWithValue("$descrizione", tipo.Descrizione.Trim());
            update.Parameters.AddWithValue("$categoria", tipo.Categoria.Trim());
            update.Parameters.AddWithValue("$livello", tipo.RichiedeLivello ? 1 : 0);
            update.Parameters.AddWithValue("$scadenza", tipo.RichiedeScadenza ? 1 : 0);
            update.Parameters.AddWithValue("$profondita", tipo.RichiedeProfondita ? 1 : 0);
            update.Parameters.AddWithValue("$attiva", tipo.Attiva ? 1 : 0);
            update.Parameters.AddWithValue("$ordine", tipo.Ordine);
            if (update.ExecuteNonQuery() != 1)
                throw new InvalidOperationException("Abilitazione non trovata. Riapri la pagina Admin.");
        }

        using var duplicates = connection.CreateCommand();
        duplicates.Transaction = transaction;
        duplicates.CommandText = """
            SELECT Descrizione FROM TipiAbilitazione
            GROUP BY UPPER(TRIM(Descrizione)) HAVING COUNT(*) > 1 LIMIT 1;
            """;
        if (duplicates.ExecuteScalar() is string descrizione)
            throw new InvalidOperationException($"Abilitazione duplicata: {descrizione}.");
        transaction.Commit();
    }
}
