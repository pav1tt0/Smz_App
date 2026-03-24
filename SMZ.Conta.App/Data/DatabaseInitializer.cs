using System.IO;
using Microsoft.Data.Sqlite;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.Data;

public static class DatabaseInitializer
{
    public static void EnsureDatabase()
    {
        Directory.CreateDirectory(DatabasePaths.AppDataDirectory);

        using var connection = new SqliteConnection(DatabasePaths.ConnectionString);
        connection.Open();

        using var transaction = connection.BeginTransaction();

        CreateSchema(connection, transaction);
        EnsureColumnMigrations(connection, transaction);
        SeedTipiAbilitazione(connection, transaction);

        transaction.Commit();
    }

    private static void CreateSchema(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            PRAGMA foreign_keys = ON;

            CREATE TABLE IF NOT EXISTS Personale (
                PerId INTEGER PRIMARY KEY AUTOINCREMENT,
                Cognome TEXT NOT NULL,
                Nome TEXT NOT NULL,
                Qualifica TEXT NULL,
                CodiceFiscale TEXT NOT NULL,
                MatricolaPersonale TEXT NULL,
                NumeroBrevettoSmz TEXT NULL,
                DataNascita TEXT NULL,
                LuogoNascita TEXT NULL,
                ViaResidenza TEXT NULL,
                CapResidenza TEXT NULL,
                CittaResidenza TEXT NULL,
                Telefono1 TEXT NULL,
                Telefono2 TEXT NULL,
                Mail1Utente TEXT NULL,
                Mail2Utente TEXT NULL,
                IndirizzoResidenza TEXT NULL,
                Telefono TEXT NULL,
                Mail TEXT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS IX_Personale_CodiceFiscale
                ON Personale (CodiceFiscale);

            CREATE INDEX IF NOT EXISTS IX_Personale_Cognome_Nome
                ON Personale (Cognome, Nome);

            CREATE TABLE IF NOT EXISTS TipiAbilitazione (
                TipoAbilitazioneId INTEGER PRIMARY KEY,
                Codice TEXT NOT NULL,
                Descrizione TEXT NOT NULL,
                Categoria TEXT NOT NULL,
                RichiedeLivello INTEGER NOT NULL,
                RichiedeScadenza INTEGER NOT NULL,
                RichiedeProfondita INTEGER NOT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS IX_TipiAbilitazione_Codice
                ON TipiAbilitazione (Codice);

            CREATE TABLE IF NOT EXISTS PersonaleAbilitazioni (
                PersonaleAbilitazioneId INTEGER PRIMARY KEY AUTOINCREMENT,
                PerId INTEGER NOT NULL,
                TipoAbilitazioneId INTEGER NOT NULL,
                Livello TEXT NULL,
                ProfonditaMetri INTEGER NULL,
                DataConseguimento TEXT NULL,
                DataScadenza TEXT NULL,
                Note TEXT NULL,
                FOREIGN KEY (PerId) REFERENCES Personale (PerId) ON DELETE CASCADE,
                FOREIGN KEY (TipoAbilitazioneId) REFERENCES TipiAbilitazione (TipoAbilitazioneId) ON DELETE RESTRICT
            );

            CREATE INDEX IF NOT EXISTS IX_PersonaleAbilitazioni_PerId
                ON PersonaleAbilitazioni (PerId);

            CREATE INDEX IF NOT EXISTS IX_PersonaleAbilitazioni_TipoAbilitazioneId
                ON PersonaleAbilitazioni (TipoAbilitazioneId);

            CREATE TABLE IF NOT EXISTS VisiteMediche (
                VisitaMedicaId INTEGER PRIMARY KEY AUTOINCREMENT,
                PerId INTEGER NOT NULL,
                TipoVisita TEXT NOT NULL,
                DataUltimaVisita TEXT NULL,
                DataScadenza TEXT NULL,
                Esito TEXT NULL,
                Note TEXT NULL,
                FOREIGN KEY (PerId) REFERENCES Personale (PerId) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS IX_VisiteMediche_PerId
                ON VisiteMediche (PerId);

            CREATE INDEX IF NOT EXISTS IX_VisiteMediche_DataScadenza
                ON VisiteMediche (DataScadenza);

            CREATE TABLE IF NOT EXISTS PersonaleArchivio (
                PersonaleArchivioId INTEGER PRIMARY KEY AUTOINCREMENT,
                PerIdOriginale INTEGER NOT NULL,
                Cognome TEXT NOT NULL,
                Nome TEXT NOT NULL,
                Qualifica TEXT NULL,
                CodiceFiscale TEXT NOT NULL,
                MatricolaPersonale TEXT NULL,
                NumeroBrevettoSmz TEXT NULL,
                DataNascita TEXT NULL,
                LuogoNascita TEXT NULL,
                ViaResidenza TEXT NULL,
                CapResidenza TEXT NULL,
                CittaResidenza TEXT NULL,
                Telefono1 TEXT NULL,
                Telefono2 TEXT NULL,
                Mail1Utente TEXT NULL,
                Mail2Utente TEXT NULL,
                IndirizzoResidenza TEXT NULL,
                Telefono TEXT NULL,
                Mail TEXT NULL,
                DataArchiviazione TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_PersonaleArchivio_PerIdOriginale
                ON PersonaleArchivio (PerIdOriginale);

            CREATE INDEX IF NOT EXISTS IX_PersonaleArchivio_DataArchiviazione
                ON PersonaleArchivio (DataArchiviazione);

            CREATE TABLE IF NOT EXISTS PersonaleAbilitazioniArchivio (
                PersonaleAbilitazioneArchivioId INTEGER PRIMARY KEY AUTOINCREMENT,
                PersonaleArchivioId INTEGER NOT NULL,
                PerIdOriginale INTEGER NOT NULL,
                TipoAbilitazioneId INTEGER NOT NULL,
                Livello TEXT NULL,
                ProfonditaMetri INTEGER NULL,
                DataConseguimento TEXT NULL,
                DataScadenza TEXT NULL,
                Note TEXT NULL,
                FOREIGN KEY (PersonaleArchivioId) REFERENCES PersonaleArchivio (PersonaleArchivioId) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS IX_PersonaleAbilitazioniArchivio_ArchivioId
                ON PersonaleAbilitazioniArchivio (PersonaleArchivioId);

            CREATE TABLE IF NOT EXISTS VisiteMedicheArchivio (
                VisitaMedicaArchivioId INTEGER PRIMARY KEY AUTOINCREMENT,
                PersonaleArchivioId INTEGER NOT NULL,
                PerIdOriginale INTEGER NOT NULL,
                TipoVisita TEXT NOT NULL,
                DataUltimaVisita TEXT NULL,
                DataScadenza TEXT NULL,
                Esito TEXT NULL,
                Note TEXT NULL,
                FOREIGN KEY (PersonaleArchivioId) REFERENCES PersonaleArchivio (PersonaleArchivioId) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS IX_VisiteMedicheArchivio_ArchivioId
                ON VisiteMedicheArchivio (PersonaleArchivioId);
            """;

        command.ExecuteNonQuery();
    }

    private static void EnsureColumnMigrations(SqliteConnection connection, SqliteTransaction transaction)
    {
        AddColumnIfMissing(connection, transaction, "Personale", "Qualifica", "TEXT NULL");
        AddColumnIfMissing(connection, transaction, "Personale", "MatricolaPersonale", "TEXT NULL");
        AddColumnIfMissing(connection, transaction, "Personale", "NumeroBrevettoSmz", "TEXT NULL");
        AddColumnIfMissing(connection, transaction, "Personale", "ViaResidenza", "TEXT NULL");
        AddColumnIfMissing(connection, transaction, "Personale", "CapResidenza", "TEXT NULL");
        AddColumnIfMissing(connection, transaction, "Personale", "CittaResidenza", "TEXT NULL");
        AddColumnIfMissing(connection, transaction, "Personale", "Telefono1", "TEXT NULL");
        AddColumnIfMissing(connection, transaction, "Personale", "Telefono2", "TEXT NULL");
        AddColumnIfMissing(connection, transaction, "Personale", "Mail1Utente", "TEXT NULL");
        AddColumnIfMissing(connection, transaction, "Personale", "Mail2Utente", "TEXT NULL");

        AddColumnIfMissing(connection, transaction, "PersonaleArchivio", "Qualifica", "TEXT NULL");
        AddColumnIfMissing(connection, transaction, "PersonaleArchivio", "MatricolaPersonale", "TEXT NULL");
        AddColumnIfMissing(connection, transaction, "PersonaleArchivio", "NumeroBrevettoSmz", "TEXT NULL");
        AddColumnIfMissing(connection, transaction, "PersonaleArchivio", "ViaResidenza", "TEXT NULL");
        AddColumnIfMissing(connection, transaction, "PersonaleArchivio", "CapResidenza", "TEXT NULL");
        AddColumnIfMissing(connection, transaction, "PersonaleArchivio", "CittaResidenza", "TEXT NULL");
        AddColumnIfMissing(connection, transaction, "PersonaleArchivio", "Telefono1", "TEXT NULL");
        AddColumnIfMissing(connection, transaction, "PersonaleArchivio", "Telefono2", "TEXT NULL");
        AddColumnIfMissing(connection, transaction, "PersonaleArchivio", "Mail1Utente", "TEXT NULL");
        AddColumnIfMissing(connection, transaction, "PersonaleArchivio", "Mail2Utente", "TEXT NULL");

        MigrateLegacyAnagraficaData(connection, transaction, "Personale");
        MigrateLegacyAnagraficaData(connection, transaction, "PersonaleArchivio");
    }

    private static void AddColumnIfMissing(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string tableName,
        string columnName,
        string columnDefinition)
    {
        if (ColumnExists(connection, transaction, tableName, columnName))
        {
            return;
        }

        using var alterCommand = connection.CreateCommand();
        alterCommand.Transaction = transaction;
        alterCommand.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition};";
        alterCommand.ExecuteNonQuery();
    }

    private static bool ColumnExists(SqliteConnection connection, SqliteTransaction transaction, string tableName, string columnName)
    {
        using var existsCommand = connection.CreateCommand();
        existsCommand.Transaction = transaction;
        existsCommand.CommandText = $"PRAGMA table_info({tableName});";

        using var reader = existsCommand.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void MigrateLegacyAnagraficaData(SqliteConnection connection, SqliteTransaction transaction, string tableName)
    {
        if (ColumnExists(connection, transaction, tableName, "IndirizzoResidenza"))
        {
            ExecuteMigration(
                connection,
                transaction,
                $"UPDATE {tableName} SET ViaResidenza = TRIM(IndirizzoResidenza) WHERE (ViaResidenza IS NULL OR TRIM(ViaResidenza) = '') AND IndirizzoResidenza IS NOT NULL AND TRIM(IndirizzoResidenza) <> '';");
        }

        if (ColumnExists(connection, transaction, tableName, "Telefono"))
        {
            ExecuteMigration(
                connection,
                transaction,
                $"UPDATE {tableName} SET Telefono1 = TRIM(Telefono) WHERE (Telefono1 IS NULL OR TRIM(Telefono1) = '') AND Telefono IS NOT NULL AND TRIM(Telefono) <> '';");
        }

        if (ColumnExists(connection, transaction, tableName, "Mail"))
        {
            ExecuteMigration(
                connection,
                transaction,
                $"""
                UPDATE {tableName}
                SET Mail1Utente = TRIM(
                    CASE
                        WHEN instr(Mail, '@') > 1 THEN substr(Mail, 1, instr(Mail, '@') - 1)
                        ELSE Mail
                    END)
                WHERE (Mail1Utente IS NULL OR TRIM(Mail1Utente) = '')
                  AND Mail IS NOT NULL
                  AND TRIM(Mail) <> '';
                """);
        }
    }

    private static void ExecuteMigration(SqliteConnection connection, SqliteTransaction transaction, string commandText)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = commandText;
        command.ExecuteNonQuery();
    }

    private static void SeedTipiAbilitazione(SqliteConnection connection, SqliteTransaction transaction)
    {
        foreach (var tipo in CatalogoAbilitazioni.Tutte)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                """
                INSERT INTO TipiAbilitazione (
                    TipoAbilitazioneId,
                    Codice,
                    Descrizione,
                    Categoria,
                    RichiedeLivello,
                    RichiedeScadenza,
                    RichiedeProfondita
                )
                VALUES (
                    $tipoAbilitazioneId,
                    $codice,
                    $descrizione,
                    $categoria,
                    $richiedeLivello,
                    $richiedeScadenza,
                    $richiedeProfondita
                )
                ON CONFLICT(TipoAbilitazioneId) DO UPDATE SET
                    Codice = excluded.Codice,
                    Descrizione = excluded.Descrizione,
                    Categoria = excluded.Categoria,
                    RichiedeLivello = excluded.RichiedeLivello,
                    RichiedeScadenza = excluded.RichiedeScadenza,
                    RichiedeProfondita = excluded.RichiedeProfondita;
                """;

            command.Parameters.AddWithValue("$tipoAbilitazioneId", tipo.TipoAbilitazioneId);
            command.Parameters.AddWithValue("$codice", tipo.Codice);
            command.Parameters.AddWithValue("$descrizione", tipo.Descrizione);
            command.Parameters.AddWithValue("$categoria", tipo.Categoria);
            command.Parameters.AddWithValue("$richiedeLivello", tipo.RichiedeLivello ? 1 : 0);
            command.Parameters.AddWithValue("$richiedeScadenza", tipo.RichiedeScadenza ? 1 : 0);
            command.Parameters.AddWithValue("$richiedeProfondita", tipo.RichiedeProfondita ? 1 : 0);
            command.ExecuteNonQuery();
        }
    }
}
