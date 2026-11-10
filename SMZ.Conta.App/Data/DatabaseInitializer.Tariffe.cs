using Microsoft.Data.Sqlite;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.Data;

public static partial class DatabaseInitializer
{
    // Aggiorna esclusivamente il catalogo originale completo e mai personalizzato.
    // Una singola differenza, anche di validità o attivazione, conserva tutto il catalogo.
    private static void AggiornaCatalogoTariffeOriginale(SqliteConnection connection, SqliteTransaction transaction)
    {
        var attuali = CataloghiServizio.RegoleContabiliImmersione.ToDictionary(item => item.RegolaContabileImmersioneId);
        var ordinarie = attuali.Values.Where(item => item.CategoriaContabileOreId == 1)
            .ToDictionary(item => (item.TipologiaImmersioneOperativaId, item.FasciaProfonditaId), item => item.Tariffa);

        using (var select = connection.CreateCommand())
        {
            select.Transaction = transaction;
            select.CommandText = """
                SELECT RegolaContabileImmersioneId, TipologiaImmersioneOperativaId, FasciaProfonditaId,
                       CategoriaContabileOreId, Tariffa, DataInizioValidita, DataFineValidita, Attiva
                FROM RegoleContabiliImmersione;
                """;
            using var reader = select.ExecuteReader();
            var count = 0;
            while (reader.Read())
            {
                if (!attuali.TryGetValue(reader.GetInt32(0), out var regola)
                    || reader.GetInt32(1) != regola.TipologiaImmersioneOperativaId
                    || reader.GetInt32(2) != regola.FasciaProfonditaId
                    || reader.GetInt32(3) != regola.CategoriaContabileOreId
                    || !reader.IsDBNull(5) || !reader.IsDBNull(6) || reader.GetInt32(7) != 1)
                {
                    return;
                }

                var tariffaOriginale = regola.CategoriaContabileOreId == 4
                    ? 2.48m
                    : ordinarie[(regola.TipologiaImmersioneOperativaId, regola.FasciaProfonditaId)];
                if (reader.GetDecimal(4) != tariffaOriginale) return;
                count++;
            }

            if (count != attuali.Count) return;
        }

        foreach (var regola in attuali.Values.Where(item => item.CategoriaContabileOreId != 1))
        {
            using var update = connection.CreateCommand();
            update.Transaction = transaction;
            update.CommandText = "UPDATE RegoleContabiliImmersione SET Tariffa = $tariffa WHERE RegolaContabileImmersioneId = $id;";
            update.Parameters.AddWithValue("$tariffa", regola.Tariffa);
            update.Parameters.AddWithValue("$id", regola.RegolaContabileImmersioneId);
            update.ExecuteNonQuery();
        }
    }
}
