using Microsoft.Data.Sqlite;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.Data;

public sealed partial class PersonaleRepository
{
    public ContabilitaGiornateImpiegoSnapshot GetContabilitaGiornateImpiego(int anno, int mese)
    {
        using var connection = OpenConnection();
        var dataInizio = new DateOnly(anno, mese, 1);
        var dataFine = dataInizio.AddMonths(1).AddDays(-1);

        return new ContabilitaGiornateImpiegoSnapshot
        {
            SmzImmersioni = GetContabilitaSmzImmersioni(connection, dataInizio, dataFine),
            Sanitari = GetContabilitaSanitari(connection, dataInizio, dataFine),
            SupportiOccasionali = GetContabilitaSupportiOccasionali(connection, dataInizio, dataFine),
        };
    }

    public List<RegistroImmersioneRiga> GetRegistroImmersioniMensile(int anno, int mese)
    {
        using var connection = OpenConnection();
        var dataInizio = new DateOnly(anno, mese, 1);
        var dataFine = dataInizio.AddMonths(1).AddDays(-1);
        return GetRegistroImmersioniMensile(connection, dataInizio, dataFine);
    }

    public List<ReportPersonaleMensileRiga> GetReportPersonaleMensile(int anno, int mese)
    {
        var dataInizio = new DateOnly(anno, mese, 1);
        var dataFine = dataInizio.AddMonths(1).AddDays(-1);
        return GetReportPersonale(dataInizio, dataFine);
    }

    public List<ReportPersonaleMensileRiga> GetReportPersonale(DateOnly dataInizio, DateOnly dataFine)
    {
        using var connection = OpenConnection();

        var righe = GetRegistroImmersioniMensile(connection, dataInizio, dataFine)
            .Select(item => new ReportPersonaleMensileRiga
            {
                ServizioGiornalieroId = item.ServizioGiornalieroId,
                DataServizio = item.DataServizio,
                NumeroOrdineServizio = item.NumeroOrdineServizio,
                PerId = item.PerId,
                Qualifica = item.Qualifica,
                Nominativo = item.Nominativo,
                TipoRiga = "Immersione",
                Localita = item.Localita,
                ScopoImmersione = item.ScopoImmersione,
                NumeroImmersione = item.NumeroImmersione,
                Apparato = item.Apparato,
                ProfonditaMetri = item.ProfonditaMetri,
                OreImmersione = item.OreImmersione,
            })
            .ToList();

        righe.AddRange(GetReportPersonaleServizi(connection, dataInizio, dataFine));
        righe.AddRange(GetReportPersonaleSupporti(connection, dataInizio, dataFine));

        return righe
            .OrderBy(item => item.DataServizio)
            .ThenBy(item => item.NumeroOrdineServizio)
            .ThenBy(item => item.Nominativo)
            .ThenBy(item => item.TipoRiga)
            .ThenBy(item => item.NumeroImmersione ?? 0)
            .ToList();
    }

    public ElaborazioneMensileInfo? GetElaborazioneMensileInfo(int anno, int mese)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT em.ElaborazioneMensileId,
                   em.Anno,
                   em.Mese,
                   em.CreataIl,
                   em.AggiornataIl,
                   COALESCE(SUM(CASE WHEN emr.TipoRiga = 'SMZ' THEN 1 ELSE 0 END), 0) AS RigheSmz,
                   COALESCE(SUM(CASE WHEN emr.TipoRiga = 'SANITARIO' THEN 1 ELSE 0 END), 0) AS RigheSanitari,
                   COALESCE(SUM(CASE WHEN emr.TipoRiga = 'SUPPORTO' THEN 1 ELSE 0 END), 0) AS RigheSupporti
            FROM ElaborazioniMensili em
            LEFT JOIN ElaborazioneMensileRighe emr ON emr.ElaborazioneMensileId = em.ElaborazioneMensileId
            WHERE em.Anno = $anno
              AND em.Mese = $mese
            GROUP BY em.ElaborazioneMensileId, em.Anno, em.Mese, em.CreataIl, em.AggiornataIl;
            """;
        command.Parameters.AddWithValue("$anno", anno);
        command.Parameters.AddWithValue("$mese", mese);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new ElaborazioneMensileInfo
        {
            ElaborazioneMensileId = reader.GetInt64(0),
            Anno = reader.GetInt32(1),
            Mese = reader.GetInt32(2),
            CreataIl = DateTime.Parse(reader.GetString(3)),
            AggiornataIl = DateTime.Parse(reader.GetString(4)),
            RigheSmz = reader.GetInt32(5),
            RigheSanitari = reader.GetInt32(6),
            RigheSupporti = reader.GetInt32(7),
        };
    }

    public ContabilitaGiornateImpiegoSnapshot? GetElaborazioneMensileSnapshot(int anno, int mese)
    {
        using var connection = OpenConnection();
        var elaborazioneId = GetElaborazioneMensileId(connection, anno, mese);
        if (elaborazioneId is null)
        {
            return null;
        }

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT TipoRiga,
                   PerId,
                   DataServizio,
                   NumeroOrdineServizio,
                   Cognome,
                   Nome,
                   Nominativo,
                   Qualifica,
                   Ruolo,
                   Apparato,
                   FasciaProfondita,
                   Tariffa,
                   OreOrd,
                   OreAdd,
                   OreSper,
                   OreCi,
                   Importo,
                   GiornateImpiego,
                   UltimaDataServizio
            FROM ElaborazioneMensileRighe
            WHERE ElaborazioneMensileId = $elaborazioneMensileId
            ORDER BY TipoRiga, OrdineRiga, ElaborazioneMensileRigaId;
            """;
        command.Parameters.AddWithValue("$elaborazioneMensileId", elaborazioneId.Value);

        using var reader = command.ExecuteReader();
        var snapshot = new ContabilitaGiornateImpiegoSnapshot();

        while (reader.Read())
        {
            var tipoRiga = reader.GetString(0);
            switch (tipoRiga)
            {
                case "SMZ":
                    snapshot.SmzImmersioni.Add(new ContabilitaSmzSummary
                    {
                        PerId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                        DataServizio = DateOnly.Parse(reader.GetString(2)),
                        NumeroOrdineServizio = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                        Cognome = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                        Nome = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                        Qualifica = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                        Apparato = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                        FasciaProfondita = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                        Tariffa = reader.IsDBNull(11) ? 0m : Convert.ToDecimal(reader.GetDouble(11)),
                        OreOrd = reader.IsDBNull(12) ? 0m : Convert.ToDecimal(reader.GetDouble(12)),
                        OreAdd = reader.IsDBNull(13) ? 0m : Convert.ToDecimal(reader.GetDouble(13)),
                        OreSper = reader.IsDBNull(14) ? 0m : Convert.ToDecimal(reader.GetDouble(14)),
                        OreCi = reader.IsDBNull(15) ? 0m : Convert.ToDecimal(reader.GetDouble(15)),
                        Importo = reader.IsDBNull(16) ? 0m : Convert.ToDecimal(reader.GetDouble(16)),
                    });
                    break;

                case "SANITARIO":
                    snapshot.Sanitari.Add(new ContabilitaSanitarioSummary
                    {
                        PerId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                        Cognome = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                        Nome = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                        Qualifica = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                        RuoloSanitario = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                        GiornateImpiego = reader.IsDBNull(17) ? 0 : reader.GetInt32(17),
                        UltimaDataServizio = ParseDbDate(reader, 18),
                    });
                    break;

                case "SUPPORTO":
                    snapshot.SupportiOccasionali.Add(new ContabilitaSupportoSummary
                    {
                        Nominativo = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                        Qualifica = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                        Ruolo = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                        GiornateImpiego = reader.IsDBNull(17) ? 0 : reader.GetInt32(17),
                        UltimaDataServizio = ParseDbDate(reader, 18),
                    });
                    break;
            }
        }

        return snapshot;
    }

    public void SaveElaborazioneMensile(int anno, int mese, ContabilitaGiornateImpiegoSnapshot snapshot, string? note = null)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        var elaborazioneId = GetElaborazioneMensileId(connection, anno, mese, transaction);
        if (elaborazioneId is null)
        {
            using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText =
                """
                INSERT INTO ElaborazioniMensili (Anno, Mese, Note)
                VALUES ($anno, $mese, $note);
                SELECT last_insert_rowid();
                """;
            insert.Parameters.AddWithValue("$anno", anno);
            insert.Parameters.AddWithValue("$mese", mese);
            insert.Parameters.AddWithValue("$note", string.IsNullOrWhiteSpace(note) ? DBNull.Value : note.Trim());
            elaborazioneId = Convert.ToInt64(insert.ExecuteScalar());
        }
        else
        {
            using var update = connection.CreateCommand();
            update.Transaction = transaction;
            update.CommandText =
                """
                UPDATE ElaborazioniMensili
                SET Note = $note,
                    AggiornataIl = CURRENT_TIMESTAMP
                WHERE ElaborazioneMensileId = $elaborazioneMensileId;
                """;
            update.Parameters.AddWithValue("$elaborazioneMensileId", elaborazioneId.Value);
            update.Parameters.AddWithValue("$note", string.IsNullOrWhiteSpace(note) ? DBNull.Value : note.Trim());
            update.ExecuteNonQuery();

            using var deleteRows = connection.CreateCommand();
            deleteRows.Transaction = transaction;
            deleteRows.CommandText = "DELETE FROM ElaborazioneMensileRighe WHERE ElaborazioneMensileId = $elaborazioneMensileId;";
            deleteRows.Parameters.AddWithValue("$elaborazioneMensileId", elaborazioneId.Value);
            deleteRows.ExecuteNonQuery();
        }

        var ordine = 0;
        foreach (var item in snapshot.SmzImmersioni)
        {
            InsertElaborazioneMensileRiga(connection, transaction, elaborazioneId.Value, "SMZ", ordine++, item);
        }

        ordine = 0;
        foreach (var item in snapshot.Sanitari)
        {
            InsertElaborazioneMensileRiga(connection, transaction, elaborazioneId.Value, "SANITARIO", ordine++, item);
        }

        ordine = 0;
        foreach (var item in snapshot.SupportiOccasionali)
        {
            InsertElaborazioneMensileRiga(connection, transaction, elaborazioneId.Value, "SUPPORTO", ordine++, item);
        }

        transaction.Commit();
    }

    public void UpdateRegoleContabiliImmersione(IEnumerable<RegolaContabileImmersione> regole)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        foreach (var regola in regole)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                """
                UPDATE RegoleContabiliImmersione
                SET Tariffa = $tariffa,
                    Attiva = $attiva
                WHERE RegolaContabileImmersioneId = $regolaContabileImmersioneId;
                """;
            command.Parameters.AddWithValue("$regolaContabileImmersioneId", regola.RegolaContabileImmersioneId);
            command.Parameters.AddWithValue("$tariffa", regola.Tariffa);
            command.Parameters.AddWithValue("$attiva", regola.Attiva ? 1 : 0);

            if (command.ExecuteNonQuery() == 0)
            {
                throw new InvalidOperationException($"Regola contabile {regola.RegolaContabileImmersioneId} non trovata.");
            }
        }

        transaction.Commit();
    }
}
