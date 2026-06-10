using Microsoft.Data.Sqlite;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.Data;

public sealed partial class PersonaleRepository
{
    private static Personale MapPersonale(SqliteDataReader reader)
    {
        return new Personale
        {
            PerId = reader.GetInt32(0),
            Cognome = reader.GetString(1),
            Nome = reader.GetString(2),
            Qualifica = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
            DataDecorrenzaQualifica = ParseDbDate(reader, 4),
            ProfiloPersonale = ProfiliPersonaleCatalogo.Normalizza(reader.IsDBNull(5) ? null : reader.GetString(5)),
            RuoloSanitario = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
            CodiceFiscale = reader.GetString(7),
            MatricolaPersonale = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
            NumeroBrevettoSmz = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
            StatoServizio = StatoServizioPersonaleCatalogo.Normalizza(reader.IsDBNull(10) ? null : reader.GetString(10)),
            DataFineServizio = ParseDbDate(reader, 11),
            DataNascita = ParseDbDate(reader, 12),
            LuogoNascita = reader.IsDBNull(13) ? string.Empty : reader.GetString(13),
            ViaResidenza = reader.IsDBNull(14) ? string.Empty : reader.GetString(14),
            CapResidenza = reader.IsDBNull(15) ? string.Empty : reader.GetString(15),
            CittaResidenza = reader.IsDBNull(16) ? string.Empty : reader.GetString(16),
            Telefono1 = reader.IsDBNull(17) ? string.Empty : reader.GetString(17),
            Telefono2 = reader.IsDBNull(18) ? string.Empty : reader.GetString(18),
            Mail1Utente = reader.IsDBNull(19) ? string.Empty : reader.GetString(19),
            Mail2Utente = reader.IsDBNull(20) ? string.Empty : reader.GetString(20),
        };
    }

    private static PersonaleArchivio? GetArchivioById(SqliteConnection connection, SqliteTransaction transaction, long archiveId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            SELECT PersonaleArchivioId,
                   PerIdOriginale,
                   Cognome,
                   Nome,
                   Qualifica,
                   DataDecorrenzaQualifica,
                   ProfiloPersonale,
                   RuoloSanitario,
                   CodiceFiscale,
                   MatricolaPersonale,
                   NumeroBrevettoSmz,
                   StatoServizio,
                   DataFineServizio,
                   DataNascita,
                   LuogoNascita,
                   ViaResidenza,
                   CapResidenza,
                   CittaResidenza,
                   Telefono1,
                   Telefono2,
                   Mail1Utente,
                   Mail2Utente,
                   DataArchiviazione
            FROM PersonaleArchivio
            WHERE PersonaleArchivioId = $archiveId;
            """;
        command.Parameters.AddWithValue("$archiveId", archiveId);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new PersonaleArchivio
        {
            PersonaleArchivioId = reader.GetInt64(0),
            PerIdOriginale = reader.GetInt32(1),
            Cognome = reader.GetString(2),
            Nome = reader.GetString(3),
            Qualifica = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
            DataDecorrenzaQualifica = ParseDbDate(reader, 5),
            ProfiloPersonale = ProfiliPersonaleCatalogo.Normalizza(reader.IsDBNull(6) ? null : reader.GetString(6)),
            RuoloSanitario = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
            CodiceFiscale = reader.GetString(8),
            MatricolaPersonale = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
            NumeroBrevettoSmz = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
            StatoServizio = StatoServizioPersonaleCatalogo.Normalizza(reader.IsDBNull(11) ? null : reader.GetString(11)),
            DataFineServizio = ParseDbDate(reader, 12),
            DataNascita = ParseDbDate(reader, 13),
            LuogoNascita = reader.IsDBNull(14) ? string.Empty : reader.GetString(14),
            ViaResidenza = reader.IsDBNull(15) ? string.Empty : reader.GetString(15),
            CapResidenza = reader.IsDBNull(16) ? string.Empty : reader.GetString(16),
            CittaResidenza = reader.IsDBNull(17) ? string.Empty : reader.GetString(17),
            Telefono1 = reader.IsDBNull(18) ? string.Empty : reader.GetString(18),
            Telefono2 = reader.IsDBNull(19) ? string.Empty : reader.GetString(19),
            Mail1Utente = reader.IsDBNull(20) ? string.Empty : reader.GetString(20),
            Mail2Utente = reader.IsDBNull(21) ? string.Empty : reader.GetString(21),
            DataArchiviazione = DateTime.Parse(reader.GetString(22)),
        };
    }

    private static bool ExistsActiveCodiceFiscale(SqliteConnection connection, SqliteTransaction transaction, string codiceFiscale)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COUNT(1) FROM Personale WHERE CodiceFiscale = $codiceFiscale;";
        command.Parameters.AddWithValue("$codiceFiscale", codiceFiscale.Trim().ToUpperInvariant());
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private static bool ExistsActivePerId(SqliteConnection connection, SqliteTransaction transaction, int perId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COUNT(1) FROM Personale WHERE PerId = $perId;";
        command.Parameters.AddWithValue("$perId", perId);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private static int GetNextAvailablePerId(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COALESCE(MAX(PerId), 0) + 1 FROM Personale;";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static void RestoreAbilitazioniArchivio(SqliteConnection connection, SqliteTransaction transaction, long archiveId, int perId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO PersonaleAbilitazioni (
                PerId,
                TipoAbilitazioneId,
                Livello,
                ProfonditaMetri,
                DataConseguimento,
                DataScadenza,
                Note
            )
            SELECT $perId,
                   TipoAbilitazioneId,
                   Livello,
                   ProfonditaMetri,
                   DataConseguimento,
                   DataScadenza,
                   Note
            FROM PersonaleAbilitazioniArchivio
            WHERE PersonaleArchivioId = $archiveId;
            """;
        command.Parameters.AddWithValue("$perId", perId);
        command.Parameters.AddWithValue("$archiveId", archiveId);
        command.ExecuteNonQuery();
    }

    private static void RestoreVisiteArchivio(SqliteConnection connection, SqliteTransaction transaction, long archiveId, int perId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO VisiteMediche (
                PerId,
                TipoVisita,
                DataUltimaVisita,
                DataScadenza,
                Esito,
                Note
            )
            SELECT $perId,
                   TipoVisita,
                   DataUltimaVisita,
                   DataScadenza,
                   Esito,
                   Note
            FROM VisiteMedicheArchivio
            WHERE PersonaleArchivioId = $archiveId;
            """;
        command.Parameters.AddWithValue("$perId", perId);
        command.Parameters.AddWithValue("$archiveId", archiveId);
        command.ExecuteNonQuery();
    }

    private static void RestoreAttagliamentoArchivio(SqliteConnection connection, SqliteTransaction transaction, long archiveId, int perId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO PersonaleAttagliamento (
                PerId,
                Voce,
                TagliaMisura,
                Note
            )
            SELECT $perId,
                   Voce,
                   TagliaMisura,
                   Note
            FROM PersonaleAttagliamentoArchivio
            WHERE PersonaleArchivioId = $archiveId;
            """;
        command.Parameters.AddWithValue("$perId", perId);
        command.Parameters.AddWithValue("$archiveId", archiveId);
        command.ExecuteNonQuery();
    }

    private static void DeleteArchivio(SqliteConnection connection, SqliteTransaction transaction, long archiveId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "DELETE FROM PersonaleArchivio WHERE PersonaleArchivioId = $archiveId;";
        command.Parameters.AddWithValue("$archiveId", archiveId);

        if (command.ExecuteNonQuery() == 0)
        {
            throw new InvalidOperationException("Scheda archiviata non trovata.");
        }
    }

    private static SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(DatabasePaths.ConnectionString);
        connection.Open();
        return connection;
    }

    private sealed record PersonaleServiceUsage(int ServiziComePartecipante, int ImmersioniConRuoli)
    {
        public bool HasReferences => ServiziComePartecipante > 0 || ImmersioniConRuoli > 0;
    }

    private static int GetNextIntegerId(SqliteConnection connection, SqliteTransaction transaction, string tableName, string columnName)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT COALESCE(MAX({columnName}), 0) + 1 FROM {tableName};";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static string? ToDbDate(DateOnly? value) => value?.ToString("yyyy-MM-dd");

    private static object ToDbValue(DateOnly? value) => value is null ? DBNull.Value : value.Value.ToString("yyyy-MM-dd");

    private static DateOnly? ParseDbDate(SqliteDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        return DateOnly.Parse(reader.GetString(ordinal));
    }
}
