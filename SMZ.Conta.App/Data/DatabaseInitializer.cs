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
                CodiceFiscale TEXT NOT NULL,
                DataNascita TEXT NULL,
                LuogoNascita TEXT NULL,
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
            """;

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
