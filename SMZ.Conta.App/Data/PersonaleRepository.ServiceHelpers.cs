using Microsoft.Data.Sqlite;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.Data;

public sealed partial class PersonaleRepository
{
    private static List<ServizioPartecipante> GetServizioPartecipanti(SqliteConnection connection, long servizioGiornalieroId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT sp.ServizioPartecipanteId,
                   sp.ServizioGiornalieroId,
                   sp.PerId,
                   sp.GruppoOperativoId,
                   sp.Presente,
                   sp.RuoloOperativoId,
                   sp.Note
            FROM ServizioPartecipanti sp
            INNER JOIN Personale p ON p.PerId = sp.PerId
            WHERE sp.ServizioGiornalieroId = $servizioGiornalieroId
            ORDER BY p.Cognome, p.Nome, sp.ServizioPartecipanteId;
            """;
        command.Parameters.AddWithValue("$servizioGiornalieroId", servizioGiornalieroId);

        using var reader = command.ExecuteReader();
        var items = new List<ServizioPartecipante>();

        while (reader.Read())
        {
            items.Add(new ServizioPartecipante
            {
                ServizioPartecipanteId = reader.GetInt64(0),
                ServizioGiornalieroId = reader.GetInt64(1),
                PerId = reader.GetInt32(2),
                GruppoOperativoId = reader.GetInt32(3),
                Presente = reader.GetInt32(4) == 1,
                RuoloOperativoId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                Note = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
            });
        }

        return items;
    }

    private static List<ServizioImmersione> GetServizioImmersioni(SqliteConnection connection, long servizioGiornalieroId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT ServizioImmersioneId,
                   ServizioGiornalieroId,
                   NumeroImmersione,
                   OrarioInizio,
                   OrarioFine,
                   DirettoreImmersionePerId,
                   OperatoreSoccorsoPerId,
                   AssistenteBlsdPerId,
                   AssistenteSanitarioPerId,
                   LocalitaOperativaId,
                   ScopoImmersioneId,
                   Note
            FROM ServizioImmersioni
            WHERE ServizioGiornalieroId = $servizioGiornalieroId
            ORDER BY NumeroImmersione, ServizioImmersioneId;
            """;
        command.Parameters.AddWithValue("$servizioGiornalieroId", servizioGiornalieroId);

        using var reader = command.ExecuteReader();
        var items = new List<ServizioImmersione>();

        while (reader.Read())
        {
            items.Add(new ServizioImmersione
            {
                ServizioImmersioneId = reader.GetInt64(0),
                ServizioGiornalieroId = reader.GetInt64(1),
                NumeroImmersione = reader.GetInt32(2),
                OrarioInizio = ParseDbTime(reader, 3),
                OrarioFine = ParseDbTime(reader, 4),
                DirettoreImmersionePerId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                OperatoreSoccorsoPerId = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                AssistenteBlsdPerId = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                AssistenteSanitarioPerId = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                LocalitaOperativaId = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                ScopoImmersioneId = reader.IsDBNull(10) ? null : reader.GetInt32(10),
                Note = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
            });
        }

        return items;
    }

    private static List<ServizioSupportoOccasionale> GetServizioSupportiOccasionali(SqliteConnection connection, long servizioGiornalieroId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT ServizioSupportoOccasionaleId,
                   ServizioGiornalieroId,
                   Nominativo,
                   Qualifica,
                   Ruolo,
                   Presente,
                   Contatti,
                   Note
            FROM ServizioSupportiOccasionali
            WHERE ServizioGiornalieroId = $servizioGiornalieroId
            ORDER BY ServizioSupportoOccasionaleId;
            """;
        command.Parameters.AddWithValue("$servizioGiornalieroId", servizioGiornalieroId);

        using var reader = command.ExecuteReader();
        var items = new List<ServizioSupportoOccasionale>();

        while (reader.Read())
        {
            items.Add(new ServizioSupportoOccasionale
            {
                ServizioSupportoOccasionaleId = reader.GetInt64(0),
                ServizioGiornalieroId = reader.GetInt64(1),
                Nominativo = reader.GetString(2),
                Qualifica = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                Ruolo = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                Presente = reader.GetInt32(5) == 1,
                Contatti = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                Note = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
            });
        }

        return items;
    }

    private static List<ServizioOperatoreSubEsterno> GetServizioOperatoriSubEsterni(SqliteConnection connection, long servizioGiornalieroId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT ServizioOperatoreSubEsternoId,
                   ServizioGiornalieroId,
                   PerId,
                   Qualifica,
                   Nominativo,
                   Reparto,
                   GruppoOperativoId,
                   Note
            FROM ServizioOperatoriSubEsterni
            WHERE ServizioGiornalieroId = $servizioGiornalieroId
            ORDER BY ServizioOperatoreSubEsternoId;
            """;
        command.Parameters.AddWithValue("$servizioGiornalieroId", servizioGiornalieroId);

        using var reader = command.ExecuteReader();
        var items = new List<ServizioOperatoreSubEsterno>();

        while (reader.Read())
        {
            items.Add(new ServizioOperatoreSubEsterno
            {
                ServizioOperatoreSubEsternoId = reader.GetInt64(0),
                ServizioGiornalieroId = reader.GetInt64(1),
                PerId = reader.GetInt32(2),
                Qualifica = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                Nominativo = reader.GetString(4),
                Reparto = reader.GetString(5),
                GruppoOperativoId = reader.GetInt32(6),
                Note = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
            });
        }

        return items;
    }

    private static List<ServizioPartecipanteImmersione> GetServizioPartecipantiImmersioni(
        SqliteConnection connection,
        long servizioGiornalieroId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT spi.ServizioPartecipanteImmersioneId,
                   spi.ServizioImmersioneId,
                   spi.ServizioPartecipanteId,
                   spi.TipologiaImmersioneOperativaId,
                   spi.ProfonditaMetri,
                   spi.FasciaProfonditaId,
                   spi.OreImmersione,
                   spi.CategoriaContabileOreId,
                   spi.Note
            FROM ServizioPartecipantiImmersioni spi
            INNER JOIN ServizioImmersioni si ON si.ServizioImmersioneId = spi.ServizioImmersioneId
            WHERE si.ServizioGiornalieroId = $servizioGiornalieroId
            ORDER BY si.NumeroImmersione, spi.ServizioPartecipanteImmersioneId;
            """;
        command.Parameters.AddWithValue("$servizioGiornalieroId", servizioGiornalieroId);

        using var reader = command.ExecuteReader();
        var items = new List<ServizioPartecipanteImmersione>();

        while (reader.Read())
        {
            items.Add(new ServizioPartecipanteImmersione
            {
                ServizioPartecipanteImmersioneId = reader.GetInt64(0),
                ServizioImmersioneId = reader.GetInt64(1),
                ServizioPartecipanteId = reader.GetInt64(2),
                TipologiaImmersioneOperativaId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                ProfonditaMetri = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                FasciaProfonditaId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                OreImmersione = reader.IsDBNull(6) ? null : Convert.ToDecimal(reader.GetDouble(6)),
                CategoriaContabileOreId = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                Note = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
            });
        }

        return items;
    }

    private static List<ServizioOperatoreSubEsternoImmersione> GetServizioOperatoriSubEsterniImmersioni(
        SqliteConnection connection,
        long servizioGiornalieroId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT sei.ServizioOperatoreSubEsternoImmersioneId,
                   sei.ServizioImmersioneId,
                   sei.ServizioOperatoreSubEsternoId,
                   sei.TipologiaImmersioneOperativaId,
                   sei.ProfonditaMetri,
                   sei.FasciaProfonditaId,
                   sei.OreImmersione,
                   sei.CategoriaContabileOreId,
                   sei.Note
            FROM ServizioOperatoriSubEsterniImmersioni sei
            INNER JOIN ServizioImmersioni si ON si.ServizioImmersioneId = sei.ServizioImmersioneId
            WHERE si.ServizioGiornalieroId = $servizioGiornalieroId
            ORDER BY si.NumeroImmersione, sei.ServizioOperatoreSubEsternoImmersioneId;
            """;
        command.Parameters.AddWithValue("$servizioGiornalieroId", servizioGiornalieroId);

        using var reader = command.ExecuteReader();
        var items = new List<ServizioOperatoreSubEsternoImmersione>();

        while (reader.Read())
        {
            items.Add(new ServizioOperatoreSubEsternoImmersione
            {
                ServizioOperatoreSubEsternoImmersioneId = reader.GetInt64(0),
                ServizioImmersioneId = reader.GetInt64(1),
                ServizioOperatoreSubEsternoId = reader.GetInt64(2),
                TipologiaImmersioneOperativaId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                ProfonditaMetri = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                FasciaProfonditaId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                OreImmersione = reader.IsDBNull(6) ? null : Convert.ToDecimal(reader.GetDouble(6)),
                CategoriaContabileOreId = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                Note = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
            });
        }

        return items;
    }

    private static void DeleteServizioChildRows(SqliteConnection connection, SqliteTransaction transaction, long servizioGiornalieroId)
    {
        using var deleteSupporti = connection.CreateCommand();
        deleteSupporti.Transaction = transaction;
        deleteSupporti.CommandText = "DELETE FROM ServizioSupportiOccasionali WHERE ServizioGiornalieroId = $servizioGiornalieroId;";
        deleteSupporti.Parameters.AddWithValue("$servizioGiornalieroId", servizioGiornalieroId);
        deleteSupporti.ExecuteNonQuery();

        using var deleteOperatoriEsterni = connection.CreateCommand();
        deleteOperatoriEsterni.Transaction = transaction;
        deleteOperatoriEsterni.CommandText = "DELETE FROM ServizioOperatoriSubEsterni WHERE ServizioGiornalieroId = $servizioGiornalieroId;";
        deleteOperatoriEsterni.Parameters.AddWithValue("$servizioGiornalieroId", servizioGiornalieroId);
        deleteOperatoriEsterni.ExecuteNonQuery();

        using var deleteImmersioni = connection.CreateCommand();
        deleteImmersioni.Transaction = transaction;
        deleteImmersioni.CommandText = "DELETE FROM ServizioImmersioni WHERE ServizioGiornalieroId = $servizioGiornalieroId;";
        deleteImmersioni.Parameters.AddWithValue("$servizioGiornalieroId", servizioGiornalieroId);
        deleteImmersioni.ExecuteNonQuery();

        using var deletePartecipanti = connection.CreateCommand();
        deletePartecipanti.Transaction = transaction;
        deletePartecipanti.CommandText = "DELETE FROM ServizioPartecipanti WHERE ServizioGiornalieroId = $servizioGiornalieroId;";
        deletePartecipanti.Parameters.AddWithValue("$servizioGiornalieroId", servizioGiornalieroId);
        deletePartecipanti.ExecuteNonQuery();
    }

    private static Dictionary<int, long> InsertServizioPartecipanti(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long servizioGiornalieroId,
        IEnumerable<ServizioPartecipante> partecipanti)
    {
        var map = new Dictionary<int, long>();

        foreach (var partecipante in partecipanti)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                """
                INSERT INTO ServizioPartecipanti (
                    ServizioGiornalieroId,
                    PerId,
                    GruppoOperativoId,
                    Presente,
                    RuoloOperativoId,
                    Note
                )
                VALUES (
                    $servizioGiornalieroId,
                    $perId,
                    $gruppoOperativoId,
                    $presente,
                    $ruoloOperativoId,
                    $note
                );
                SELECT last_insert_rowid();
                """;
            command.Parameters.AddWithValue("$servizioGiornalieroId", servizioGiornalieroId);
            command.Parameters.AddWithValue("$perId", partecipante.PerId);
            command.Parameters.AddWithValue("$gruppoOperativoId", partecipante.GruppoOperativoId);
            command.Parameters.AddWithValue("$presente", partecipante.Presente ? 1 : 0);
            command.Parameters.AddWithValue("$ruoloOperativoId", partecipante.RuoloOperativoId is null ? DBNull.Value : partecipante.RuoloOperativoId.Value);
            command.Parameters.AddWithValue("$note", DbText(partecipante.Note));
            var servizioPartecipanteId = Convert.ToInt64(command.ExecuteScalar());
            map[partecipante.PerId] = servizioPartecipanteId;
        }

        return map;
    }

    private static Dictionary<int, long> InsertServizioOperatoriSubEsterni(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long servizioGiornalieroId,
        IEnumerable<ServizioOperatoreSubEsterno> operatori)
    {
        var map = new Dictionary<int, long>();

        foreach (var operatore in operatori)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                """
                INSERT INTO ServizioOperatoriSubEsterni (
                    ServizioGiornalieroId,
                    PerId,
                    Qualifica,
                    Nominativo,
                    Reparto,
                    GruppoOperativoId,
                    Note
                )
                VALUES (
                    $servizioGiornalieroId,
                    $perId,
                    $qualifica,
                    $nominativo,
                    $reparto,
                    $gruppoOperativoId,
                    $note
                );
                SELECT last_insert_rowid();
                """;
            command.Parameters.AddWithValue("$servizioGiornalieroId", servizioGiornalieroId);
            command.Parameters.AddWithValue("$perId", operatore.PerId);
            command.Parameters.AddWithValue("$qualifica", DbText(operatore.Qualifica));
            command.Parameters.AddWithValue("$nominativo", operatore.Nominativo.Trim());
            command.Parameters.AddWithValue("$reparto", operatore.Reparto.Trim());
            command.Parameters.AddWithValue("$gruppoOperativoId", operatore.GruppoOperativoId);
            command.Parameters.AddWithValue("$note", DbText(operatore.Note));
            var servizioOperatoreSubEsternoId = Convert.ToInt64(command.ExecuteScalar());
            map[operatore.PerId] = servizioOperatoreSubEsternoId;
        }

        return map;
    }

    private static Dictionary<int, long> InsertServizioImmersioni(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long servizioGiornalieroId,
        IEnumerable<ServizioImmersione> immersioni,
        int? defaultLocalitaOperativaId,
        int? defaultScopoImmersioneId)
    {
        var map = new Dictionary<int, long>();

        foreach (var immersione in immersioni)
        {
            var localitaOperativaId = immersione.LocalitaOperativaId ?? defaultLocalitaOperativaId;
            var scopoImmersioneId = immersione.ScopoImmersioneId ?? defaultScopoImmersioneId;

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                """
                INSERT INTO ServizioImmersioni (
                    ServizioGiornalieroId,
                    NumeroImmersione,
                    OrarioInizio,
                    OrarioFine,
                    DirettoreImmersionePerId,
                    OperatoreSoccorsoPerId,
                    AssistenteBlsdPerId,
                    AssistenteSanitarioPerId,
                    LocalitaOperativaId,
                    ScopoImmersioneId,
                    Note
                )
                VALUES (
                    $servizioGiornalieroId,
                    $numeroImmersione,
                    $orarioInizio,
                    $orarioFine,
                    $direttoreImmersionePerId,
                    $operatoreSoccorsoPerId,
                    $assistenteBlsdPerId,
                    $assistenteSanitarioPerId,
                    $localitaOperativaId,
                    $scopoImmersioneId,
                    $note
                );
                SELECT last_insert_rowid();
                """;
            command.Parameters.AddWithValue("$servizioGiornalieroId", servizioGiornalieroId);
            command.Parameters.AddWithValue("$numeroImmersione", immersione.NumeroImmersione);
            command.Parameters.AddWithValue("$orarioInizio", ToDbValue(immersione.OrarioInizio));
            command.Parameters.AddWithValue("$orarioFine", ToDbValue(immersione.OrarioFine));
            command.Parameters.AddWithValue("$direttoreImmersionePerId", immersione.DirettoreImmersionePerId is null ? DBNull.Value : immersione.DirettoreImmersionePerId.Value);
            command.Parameters.AddWithValue("$operatoreSoccorsoPerId", immersione.OperatoreSoccorsoPerId is null ? DBNull.Value : immersione.OperatoreSoccorsoPerId.Value);
            command.Parameters.AddWithValue("$assistenteBlsdPerId", immersione.AssistenteBlsdPerId is null ? DBNull.Value : immersione.AssistenteBlsdPerId.Value);
            command.Parameters.AddWithValue("$assistenteSanitarioPerId", immersione.AssistenteSanitarioPerId is null ? DBNull.Value : immersione.AssistenteSanitarioPerId.Value);
            command.Parameters.AddWithValue("$localitaOperativaId", localitaOperativaId is null ? DBNull.Value : localitaOperativaId.Value);
            command.Parameters.AddWithValue("$scopoImmersioneId", scopoImmersioneId is null ? DBNull.Value : scopoImmersioneId.Value);
            command.Parameters.AddWithValue("$note", DbText(immersione.Note));
            var servizioImmersioneId = Convert.ToInt64(command.ExecuteScalar());
            map[immersione.NumeroImmersione] = servizioImmersioneId;
        }

        return map;
    }

    private static void InsertServizioPartecipantiImmersioni(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IEnumerable<ServizioImmersione> immersioni,
        IReadOnlyDictionary<int, long> immersioniMap,
        IReadOnlyDictionary<int, long> partecipantiMap)
    {
        foreach (var immersione in immersioni)
        {
            if (!immersioniMap.TryGetValue(immersione.NumeroImmersione, out var servizioImmersioneId))
            {
                continue;
            }

            foreach (var partecipazione in immersione.Partecipazioni)
            {
                if (!partecipantiMap.TryGetValue((int)partecipazione.ServizioPartecipanteId, out var servizioPartecipanteId))
                {
                    continue;
                }

                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText =
                    """
                    INSERT INTO ServizioPartecipantiImmersioni (
                        ServizioImmersioneId,
                        ServizioPartecipanteId,
                        TipologiaImmersioneOperativaId,
                        ProfonditaMetri,
                        FasciaProfonditaId,
                        OreImmersione,
                        CategoriaContabileOreId,
                        Note
                    )
                    VALUES (
                        $servizioImmersioneId,
                        $servizioPartecipanteId,
                        $tipologiaImmersioneOperativaId,
                        $profonditaMetri,
                        $fasciaProfonditaId,
                        $oreImmersione,
                        $categoriaContabileOreId,
                        $note
                    );
                    """;
                command.Parameters.AddWithValue("$servizioImmersioneId", servizioImmersioneId);
                command.Parameters.AddWithValue("$servizioPartecipanteId", servizioPartecipanteId);
                command.Parameters.AddWithValue("$tipologiaImmersioneOperativaId", partecipazione.TipologiaImmersioneOperativaId is null ? DBNull.Value : partecipazione.TipologiaImmersioneOperativaId.Value);
                command.Parameters.AddWithValue("$profonditaMetri", partecipazione.ProfonditaMetri is null ? DBNull.Value : partecipazione.ProfonditaMetri.Value);
                command.Parameters.AddWithValue("$fasciaProfonditaId", partecipazione.FasciaProfonditaId is null ? DBNull.Value : partecipazione.FasciaProfonditaId.Value);
                command.Parameters.AddWithValue("$oreImmersione", partecipazione.OreImmersione is null ? DBNull.Value : Convert.ToDouble(partecipazione.OreImmersione.Value));
                command.Parameters.AddWithValue("$categoriaContabileOreId", partecipazione.CategoriaContabileOreId is null ? DBNull.Value : partecipazione.CategoriaContabileOreId.Value);
                command.Parameters.AddWithValue("$note", DbText(partecipazione.Note));
                command.ExecuteNonQuery();
            }
        }
    }

    private static void InsertServizioOperatoriSubEsterniImmersioni(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IEnumerable<ServizioImmersione> immersioni,
        IReadOnlyDictionary<int, long> immersioniMap,
        IReadOnlyDictionary<int, long> operatoriEsterniMap)
    {
        foreach (var immersione in immersioni)
        {
            if (!immersioniMap.TryGetValue(immersione.NumeroImmersione, out var servizioImmersioneId))
            {
                continue;
            }

            foreach (var partecipazione in immersione.PartecipazioniEsterne)
            {
                if (!operatoriEsterniMap.TryGetValue((int)partecipazione.ServizioOperatoreSubEsternoId, out var servizioOperatoreSubEsternoId))
                {
                    continue;
                }

                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText =
                    """
                    INSERT INTO ServizioOperatoriSubEsterniImmersioni (
                        ServizioImmersioneId,
                        ServizioOperatoreSubEsternoId,
                        TipologiaImmersioneOperativaId,
                        ProfonditaMetri,
                        FasciaProfonditaId,
                        OreImmersione,
                        CategoriaContabileOreId,
                        Note
                    )
                    VALUES (
                        $servizioImmersioneId,
                        $servizioOperatoreSubEsternoId,
                        $tipologiaImmersioneOperativaId,
                        $profonditaMetri,
                        $fasciaProfonditaId,
                        $oreImmersione,
                        $categoriaContabileOreId,
                        $note
                    );
                    """;
                command.Parameters.AddWithValue("$servizioImmersioneId", servizioImmersioneId);
                command.Parameters.AddWithValue("$servizioOperatoreSubEsternoId", servizioOperatoreSubEsternoId);
                command.Parameters.AddWithValue("$tipologiaImmersioneOperativaId", partecipazione.TipologiaImmersioneOperativaId is null ? DBNull.Value : partecipazione.TipologiaImmersioneOperativaId.Value);
                command.Parameters.AddWithValue("$profonditaMetri", partecipazione.ProfonditaMetri is null ? DBNull.Value : partecipazione.ProfonditaMetri.Value);
                command.Parameters.AddWithValue("$fasciaProfonditaId", partecipazione.FasciaProfonditaId is null ? DBNull.Value : partecipazione.FasciaProfonditaId.Value);
                command.Parameters.AddWithValue("$oreImmersione", partecipazione.OreImmersione is null ? DBNull.Value : Convert.ToDouble(partecipazione.OreImmersione.Value));
                command.Parameters.AddWithValue("$categoriaContabileOreId", partecipazione.CategoriaContabileOreId is null ? DBNull.Value : partecipazione.CategoriaContabileOreId.Value);
                command.Parameters.AddWithValue("$note", DbText(partecipazione.Note));
                command.ExecuteNonQuery();
            }
        }
    }

    private static void InsertServizioSupportiOccasionali(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long servizioGiornalieroId,
        IEnumerable<ServizioSupportoOccasionale> supportiOccasionali)
    {
        foreach (var supporto in supportiOccasionali)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                """
                INSERT INTO ServizioSupportiOccasionali (
                    ServizioGiornalieroId,
                    Nominativo,
                    Qualifica,
                    Ruolo,
                    Presente,
                    Contatti,
                    Note
                )
                VALUES (
                    $servizioGiornalieroId,
                    $nominativo,
                    $qualifica,
                    $ruolo,
                    $presente,
                    $contatti,
                    $note
                );
                """;
            command.Parameters.AddWithValue("$servizioGiornalieroId", servizioGiornalieroId);
            command.Parameters.AddWithValue("$nominativo", supporto.Nominativo.Trim());
            command.Parameters.AddWithValue("$qualifica", DbText(supporto.Qualifica));
            command.Parameters.AddWithValue("$ruolo", DbText(supporto.Ruolo));
            command.Parameters.AddWithValue("$presente", supporto.Presente ? 1 : 0);
            command.Parameters.AddWithValue("$contatti", DbText(supporto.Contatti));
            command.Parameters.AddWithValue("$note", DbText(supporto.Note));
            command.ExecuteNonQuery();
        }
    }
}
