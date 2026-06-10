using Microsoft.Data.Sqlite;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.Data;

public sealed partial class PersonaleRepository
{
    private static long? GetElaborazioneMensileId(
        SqliteConnection connection,
        int anno,
        int mese,
        SqliteTransaction? transaction = null)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            SELECT ElaborazioneMensileId
            FROM ElaborazioniMensili
            WHERE Anno = $anno
              AND Mese = $mese;
            """;
        command.Parameters.AddWithValue("$anno", anno);
        command.Parameters.AddWithValue("$mese", mese);

        var result = command.ExecuteScalar();
        return result is null || result is DBNull ? null : Convert.ToInt64(result);
    }

    private static void InsertElaborazioneMensileRiga(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long elaborazioneMensileId,
        string tipoRiga,
        int ordineRiga,
        ContabilitaSmzSummary item)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO ElaborazioneMensileRighe (
                ElaborazioneMensileId,
                TipoRiga,
                OrdineRiga,
                PerId,
                DataServizio,
                NumeroOrdineServizio,
                Cognome,
                Nome,
                Qualifica,
                Apparato,
                FasciaProfondita,
                Tariffa,
                OreOrd,
                OreAdd,
                OreSper,
                OreCi,
                Importo
            )
            VALUES (
                $elaborazioneMensileId,
                $tipoRiga,
                $ordineRiga,
                $perId,
                $dataServizio,
                $numeroOrdineServizio,
                $cognome,
                $nome,
                $qualifica,
                $apparato,
                $fasciaProfondita,
                $tariffa,
                $oreOrd,
                $oreAdd,
                $oreSper,
                $oreCi,
                $importo
            );
            """;
        command.Parameters.AddWithValue("$elaborazioneMensileId", elaborazioneMensileId);
        command.Parameters.AddWithValue("$tipoRiga", tipoRiga);
        command.Parameters.AddWithValue("$ordineRiga", ordineRiga);
        command.Parameters.AddWithValue("$perId", item.PerId);
        command.Parameters.AddWithValue("$dataServizio", item.DataServizio.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("$numeroOrdineServizio", DbText(item.NumeroOrdineServizio));
        command.Parameters.AddWithValue("$cognome", DbText(item.Cognome));
        command.Parameters.AddWithValue("$nome", DbText(item.Nome));
        command.Parameters.AddWithValue("$qualifica", DbText(item.Qualifica));
        command.Parameters.AddWithValue("$apparato", DbText(item.Apparato));
        command.Parameters.AddWithValue("$fasciaProfondita", DbText(item.FasciaProfondita));
        command.Parameters.AddWithValue("$tariffa", Convert.ToDouble(item.Tariffa));
        command.Parameters.AddWithValue("$oreOrd", Convert.ToDouble(item.OreOrd));
        command.Parameters.AddWithValue("$oreAdd", Convert.ToDouble(item.OreAdd));
        command.Parameters.AddWithValue("$oreSper", Convert.ToDouble(item.OreSper));
        command.Parameters.AddWithValue("$oreCi", Convert.ToDouble(item.OreCi));
        command.Parameters.AddWithValue("$importo", Convert.ToDouble(item.Importo));
        command.ExecuteNonQuery();
    }

    private static void InsertElaborazioneMensileRiga(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long elaborazioneMensileId,
        string tipoRiga,
        int ordineRiga,
        ContabilitaSanitarioSummary item)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO ElaborazioneMensileRighe (
                ElaborazioneMensileId,
                TipoRiga,
                OrdineRiga,
                PerId,
                Cognome,
                Nome,
                Qualifica,
                Ruolo,
                GiornateImpiego,
                UltimaDataServizio
            )
            VALUES (
                $elaborazioneMensileId,
                $tipoRiga,
                $ordineRiga,
                $perId,
                $cognome,
                $nome,
                $qualifica,
                $ruolo,
                $giornateImpiego,
                $ultimaDataServizio
            );
            """;
        command.Parameters.AddWithValue("$elaborazioneMensileId", elaborazioneMensileId);
        command.Parameters.AddWithValue("$tipoRiga", tipoRiga);
        command.Parameters.AddWithValue("$ordineRiga", ordineRiga);
        command.Parameters.AddWithValue("$perId", item.PerId);
        command.Parameters.AddWithValue("$cognome", DbText(item.Cognome));
        command.Parameters.AddWithValue("$nome", DbText(item.Nome));
        command.Parameters.AddWithValue("$qualifica", DbText(item.Qualifica));
        command.Parameters.AddWithValue("$ruolo", DbText(item.RuoloSanitario));
        command.Parameters.AddWithValue("$giornateImpiego", item.GiornateImpiego);
        command.Parameters.AddWithValue("$ultimaDataServizio", item.UltimaDataServizio?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);
        command.ExecuteNonQuery();
    }

    private static void InsertElaborazioneMensileRiga(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long elaborazioneMensileId,
        string tipoRiga,
        int ordineRiga,
        ContabilitaSupportoSummary item)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO ElaborazioneMensileRighe (
                ElaborazioneMensileId,
                TipoRiga,
                OrdineRiga,
                Nominativo,
                Qualifica,
                Ruolo,
                GiornateImpiego,
                UltimaDataServizio
            )
            VALUES (
                $elaborazioneMensileId,
                $tipoRiga,
                $ordineRiga,
                $nominativo,
                $qualifica,
                $ruolo,
                $giornateImpiego,
                $ultimaDataServizio
            );
            """;
        command.Parameters.AddWithValue("$elaborazioneMensileId", elaborazioneMensileId);
        command.Parameters.AddWithValue("$tipoRiga", tipoRiga);
        command.Parameters.AddWithValue("$ordineRiga", ordineRiga);
        command.Parameters.AddWithValue("$nominativo", DbText(item.Nominativo));
        command.Parameters.AddWithValue("$qualifica", DbText(item.Qualifica));
        command.Parameters.AddWithValue("$ruolo", DbText(item.Ruolo));
        command.Parameters.AddWithValue("$giornateImpiego", item.GiornateImpiego);
        command.Parameters.AddWithValue("$ultimaDataServizio", item.UltimaDataServizio?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);
        command.ExecuteNonQuery();
    }

    private static TimeOnly? ParseDbTime(SqliteDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        return TimeOnly.Parse(reader.GetString(ordinal));
    }

    private static object DbText(string value) => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

    private static object ToDbValue(TimeOnly? value) => value is null ? DBNull.Value : value.Value.ToString("HH:mm");
}
