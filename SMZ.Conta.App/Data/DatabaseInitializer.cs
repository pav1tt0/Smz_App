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
        EnsureColumnMigrations(connection, transaction);
        SeedCataloghiServizio(connection, transaction);

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
                ProfiloPersonale TEXT NOT NULL DEFAULT 'Operatore Subacqueo',
                RuoloSanitario TEXT NULL,
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

            CREATE TABLE IF NOT EXISTS PersonaleAttagliamento (
                PersonaleAttagliamentoId INTEGER PRIMARY KEY AUTOINCREMENT,
                PerId INTEGER NOT NULL,
                Voce TEXT NOT NULL,
                TagliaMisura TEXT NULL,
                Note TEXT NULL,
                FOREIGN KEY (PerId) REFERENCES Personale (PerId) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS IX_PersonaleAttagliamento_PerId
                ON PersonaleAttagliamento (PerId);

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
                ProfiloPersonale TEXT NOT NULL DEFAULT 'Operatore Subacqueo',
                RuoloSanitario TEXT NULL,
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

            CREATE TABLE IF NOT EXISTS PersonaleAttagliamentoArchivio (
                PersonaleAttagliamentoArchivioId INTEGER PRIMARY KEY AUTOINCREMENT,
                PersonaleArchivioId INTEGER NOT NULL,
                PerIdOriginale INTEGER NOT NULL,
                Voce TEXT NOT NULL,
                TagliaMisura TEXT NULL,
                Note TEXT NULL,
                FOREIGN KEY (PersonaleArchivioId) REFERENCES PersonaleArchivio (PersonaleArchivioId) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS IX_PersonaleAttagliamentoArchivio_ArchivioId
                ON PersonaleAttagliamentoArchivio (PersonaleArchivioId);

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

            CREATE TABLE IF NOT EXISTS CategorieRegistro (
                CategoriaRegistroId INTEGER PRIMARY KEY,
                Descrizione TEXT NOT NULL,
                Attiva INTEGER NOT NULL,
                Ordine INTEGER NOT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS IX_CategorieRegistro_Descrizione
                ON CategorieRegistro (Descrizione);

            CREATE TABLE IF NOT EXISTS LocalitaOperative (
                LocalitaOperativaId INTEGER PRIMARY KEY,
                Descrizione TEXT NOT NULL,
                Provincia TEXT NULL,
                Attiva INTEGER NOT NULL,
                Ordine INTEGER NOT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS IX_LocalitaOperative_Descrizione
                ON LocalitaOperative (Descrizione);

            CREATE TABLE IF NOT EXISTS ScopiImmersione (
                ScopoImmersioneId INTEGER PRIMARY KEY,
                Descrizione TEXT NOT NULL,
                CategoriaRegistroId INTEGER NOT NULL,
                Attiva INTEGER NOT NULL,
                Ordine INTEGER NOT NULL,
                FOREIGN KEY (CategoriaRegistroId) REFERENCES CategorieRegistro (CategoriaRegistroId) ON DELETE RESTRICT
            );

            CREATE UNIQUE INDEX IF NOT EXISTS IX_ScopiImmersione_Descrizione
                ON ScopiImmersione (Descrizione);

            CREATE INDEX IF NOT EXISTS IX_ScopiImmersione_CategoriaRegistroId
                ON ScopiImmersione (CategoriaRegistroId);

            CREATE TABLE IF NOT EXISTS UnitaNavali (
                UnitaNavaleId INTEGER PRIMARY KEY,
                Descrizione TEXT NOT NULL,
                Sigla TEXT NULL,
                Attiva INTEGER NOT NULL,
                Ordine INTEGER NOT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS IX_UnitaNavali_Descrizione
                ON UnitaNavali (Descrizione);

            CREATE TABLE IF NOT EXISTS TipologieImmersioneOperative (
                TipologiaImmersioneOperativaId INTEGER PRIMARY KEY,
                Codice TEXT NOT NULL,
                Descrizione TEXT NOT NULL,
                ProfonditaMinimaMetri INTEGER NULL,
                ProfonditaMassimaMetri INTEGER NULL,
                Attiva INTEGER NOT NULL,
                Ordine INTEGER NOT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS IX_TipologieImmersioneOperative_Codice
                ON TipologieImmersioneOperative (Codice);

            CREATE TABLE IF NOT EXISTS FasceProfondita (
                FasciaProfonditaId INTEGER PRIMARY KEY,
                Descrizione TEXT NOT NULL,
                MetriDa INTEGER NOT NULL,
                MetriA INTEGER NOT NULL,
                Attiva INTEGER NOT NULL,
                Ordine INTEGER NOT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS IX_FasceProfondita_Descrizione
                ON FasceProfondita (Descrizione);

            CREATE TABLE IF NOT EXISTS CategorieContabiliOre (
                CategoriaContabileOreId INTEGER PRIMARY KEY,
                Codice TEXT NOT NULL,
                Descrizione TEXT NOT NULL,
                Attiva INTEGER NOT NULL,
                Ordine INTEGER NOT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS IX_CategorieContabiliOre_Codice
                ON CategorieContabiliOre (Codice);

            CREATE TABLE IF NOT EXISTS GruppiOperativi (
                GruppoOperativoId INTEGER PRIMARY KEY,
                Codice TEXT NOT NULL,
                Descrizione TEXT NOT NULL,
                Attiva INTEGER NOT NULL,
                Ordine INTEGER NOT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS IX_GruppiOperativi_Codice
                ON GruppiOperativi (Codice);

            CREATE TABLE IF NOT EXISTS RuoliOperativi (
                RuoloOperativoId INTEGER PRIMARY KEY,
                Codice TEXT NOT NULL,
                Descrizione TEXT NOT NULL,
                Attiva INTEGER NOT NULL,
                Ordine INTEGER NOT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS IX_RuoliOperativi_Codice
                ON RuoliOperativi (Codice);

            CREATE TABLE IF NOT EXISTS RegoleContabiliImmersione (
                RegolaContabileImmersioneId INTEGER PRIMARY KEY,
                TipologiaImmersioneOperativaId INTEGER NOT NULL,
                FasciaProfonditaId INTEGER NOT NULL,
                CategoriaContabileOreId INTEGER NOT NULL,
                Tariffa REAL NOT NULL,
                DataInizioValidita TEXT NULL,
                DataFineValidita TEXT NULL,
                Attiva INTEGER NOT NULL,
                FOREIGN KEY (TipologiaImmersioneOperativaId) REFERENCES TipologieImmersioneOperative (TipologiaImmersioneOperativaId) ON DELETE RESTRICT,
                FOREIGN KEY (FasciaProfonditaId) REFERENCES FasceProfondita (FasciaProfonditaId) ON DELETE RESTRICT,
                FOREIGN KEY (CategoriaContabileOreId) REFERENCES CategorieContabiliOre (CategoriaContabileOreId) ON DELETE RESTRICT
            );

            CREATE INDEX IF NOT EXISTS IX_RegoleContabiliImmersione_Keys
                ON RegoleContabiliImmersione (TipologiaImmersioneOperativaId, FasciaProfonditaId, CategoriaContabileOreId);

            CREATE TABLE IF NOT EXISTS ServiziGiornalieri (
                ServizioGiornalieroId INTEGER PRIMARY KEY AUTOINCREMENT,
                DataServizio TEXT NOT NULL,
                NumeroOrdineServizio TEXT NULL,
                OrarioServizio TEXT NULL,
                StraordinarioAttivo INTEGER NOT NULL DEFAULT 0,
                StraordinarioInizio TEXT NULL,
                StraordinarioFine TEXT NULL,
                TipoServizio TEXT NOT NULL DEFAULT 'InSede',
                LocalitaOperativaId INTEGER NULL,
                ScopoImmersioneId INTEGER NULL,
                UnitaNavaleId INTEGER NULL,
                FuoriSede INTEGER NOT NULL DEFAULT 0,
                IndennitaOrdinePubblico INTEGER NOT NULL DEFAULT 0,
                AttivitaSvolta TEXT NULL,
                Note TEXT NULL,
                CreatoIl TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                AggiornatoIl TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (LocalitaOperativaId) REFERENCES LocalitaOperative (LocalitaOperativaId) ON DELETE RESTRICT,
                FOREIGN KEY (ScopoImmersioneId) REFERENCES ScopiImmersione (ScopoImmersioneId) ON DELETE RESTRICT,
                FOREIGN KEY (UnitaNavaleId) REFERENCES UnitaNavali (UnitaNavaleId) ON DELETE RESTRICT
            );

            CREATE INDEX IF NOT EXISTS IX_ServiziGiornalieri_DataServizio
                ON ServiziGiornalieri (DataServizio);

            CREATE TABLE IF NOT EXISTS ServizioImmersioni (
                ServizioImmersioneId INTEGER PRIMARY KEY AUTOINCREMENT,
                ServizioGiornalieroId INTEGER NOT NULL,
                NumeroImmersione INTEGER NOT NULL,
                OrarioInizio TEXT NULL,
                OrarioFine TEXT NULL,
                DirettoreImmersionePerId INTEGER NULL,
                OperatoreSoccorsoPerId INTEGER NULL,
                AssistenteBlsdPerId INTEGER NULL,
                AssistenteSanitarioPerId INTEGER NULL,
                LocalitaOperativaId INTEGER NULL,
                ScopoImmersioneId INTEGER NULL,
                Note TEXT NULL,
                FOREIGN KEY (ServizioGiornalieroId) REFERENCES ServiziGiornalieri (ServizioGiornalieroId) ON DELETE CASCADE,
                FOREIGN KEY (DirettoreImmersionePerId) REFERENCES Personale (PerId) ON DELETE RESTRICT,
                FOREIGN KEY (OperatoreSoccorsoPerId) REFERENCES Personale (PerId) ON DELETE RESTRICT,
                FOREIGN KEY (AssistenteBlsdPerId) REFERENCES Personale (PerId) ON DELETE RESTRICT,
                FOREIGN KEY (AssistenteSanitarioPerId) REFERENCES Personale (PerId) ON DELETE RESTRICT,
                FOREIGN KEY (LocalitaOperativaId) REFERENCES LocalitaOperative (LocalitaOperativaId) ON DELETE RESTRICT,
                FOREIGN KEY (ScopoImmersioneId) REFERENCES ScopiImmersione (ScopoImmersioneId) ON DELETE RESTRICT
            );

            CREATE INDEX IF NOT EXISTS IX_ServizioImmersioni_ServizioGiornalieroId
                ON ServizioImmersioni (ServizioGiornalieroId);

            CREATE TABLE IF NOT EXISTS ServizioPartecipanti (
                ServizioPartecipanteId INTEGER PRIMARY KEY AUTOINCREMENT,
                ServizioGiornalieroId INTEGER NOT NULL,
                PerId INTEGER NOT NULL,
                GruppoOperativoId INTEGER NOT NULL,
                Presente INTEGER NOT NULL DEFAULT 1,
                RuoloOperativoId INTEGER NULL,
                Note TEXT NULL,
                FOREIGN KEY (ServizioGiornalieroId) REFERENCES ServiziGiornalieri (ServizioGiornalieroId) ON DELETE CASCADE,
                FOREIGN KEY (PerId) REFERENCES Personale (PerId) ON DELETE RESTRICT,
                FOREIGN KEY (GruppoOperativoId) REFERENCES GruppiOperativi (GruppoOperativoId) ON DELETE RESTRICT,
                FOREIGN KEY (RuoloOperativoId) REFERENCES RuoliOperativi (RuoloOperativoId) ON DELETE RESTRICT
            );

            CREATE INDEX IF NOT EXISTS IX_ServizioPartecipanti_ServizioGiornalieroId
                ON ServizioPartecipanti (ServizioGiornalieroId);

            CREATE INDEX IF NOT EXISTS IX_ServizioPartecipanti_PerId
                ON ServizioPartecipanti (PerId);

            CREATE TABLE IF NOT EXISTS ServizioPartecipantiImmersioni (
                ServizioPartecipanteImmersioneId INTEGER PRIMARY KEY AUTOINCREMENT,
                ServizioImmersioneId INTEGER NOT NULL,
                ServizioPartecipanteId INTEGER NOT NULL,
                TipologiaImmersioneOperativaId INTEGER NULL,
                ProfonditaMetri INTEGER NULL,
                FasciaProfonditaId INTEGER NULL,
                OreImmersione REAL NULL,
                CategoriaContabileOreId INTEGER NULL,
                Note TEXT NULL,
                FOREIGN KEY (ServizioImmersioneId) REFERENCES ServizioImmersioni (ServizioImmersioneId) ON DELETE CASCADE,
                FOREIGN KEY (ServizioPartecipanteId) REFERENCES ServizioPartecipanti (ServizioPartecipanteId) ON DELETE CASCADE,
                FOREIGN KEY (TipologiaImmersioneOperativaId) REFERENCES TipologieImmersioneOperative (TipologiaImmersioneOperativaId) ON DELETE RESTRICT,
                FOREIGN KEY (FasciaProfonditaId) REFERENCES FasceProfondita (FasciaProfonditaId) ON DELETE RESTRICT,
                FOREIGN KEY (CategoriaContabileOreId) REFERENCES CategorieContabiliOre (CategoriaContabileOreId) ON DELETE RESTRICT
            );

            CREATE INDEX IF NOT EXISTS IX_ServizioPartecipantiImmersioni_ServizioImmersioneId
                ON ServizioPartecipantiImmersioni (ServizioImmersioneId);

            CREATE INDEX IF NOT EXISTS IX_ServizioPartecipantiImmersioni_ServizioPartecipanteId
                ON ServizioPartecipantiImmersioni (ServizioPartecipanteId);

            CREATE TABLE IF NOT EXISTS ServizioSupportiOccasionali (
                ServizioSupportoOccasionaleId INTEGER PRIMARY KEY AUTOINCREMENT,
                ServizioGiornalieroId INTEGER NOT NULL,
                Nominativo TEXT NOT NULL,
                Qualifica TEXT NULL,
                Ruolo TEXT NULL,
                Presente INTEGER NOT NULL DEFAULT 1,
                Contatti TEXT NULL,
                Note TEXT NULL,
                FOREIGN KEY (ServizioGiornalieroId) REFERENCES ServiziGiornalieri (ServizioGiornalieroId) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS IX_ServizioSupportiOccasionali_ServizioGiornalieroId
                ON ServizioSupportiOccasionali (ServizioGiornalieroId);

            CREATE TABLE IF NOT EXISTS ElaborazioniMensili (
                ElaborazioneMensileId INTEGER PRIMARY KEY AUTOINCREMENT,
                Anno INTEGER NOT NULL,
                Mese INTEGER NOT NULL,
                Note TEXT NULL,
                CreataIl TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                AggiornataIl TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
            );

            CREATE UNIQUE INDEX IF NOT EXISTS IX_ElaborazioniMensili_Anno_Mese
                ON ElaborazioniMensili (Anno, Mese);

            CREATE TABLE IF NOT EXISTS ElaborazioneMensileRighe (
                ElaborazioneMensileRigaId INTEGER PRIMARY KEY AUTOINCREMENT,
                ElaborazioneMensileId INTEGER NOT NULL,
                TipoRiga TEXT NOT NULL,
                OrdineRiga INTEGER NOT NULL DEFAULT 0,
                PerId INTEGER NULL,
                DataServizio TEXT NULL,
                NumeroOrdineServizio TEXT NULL,
                Cognome TEXT NULL,
                Nome TEXT NULL,
                Nominativo TEXT NULL,
                Qualifica TEXT NULL,
                Ruolo TEXT NULL,
                Apparato TEXT NULL,
                FasciaProfondita TEXT NULL,
                Tariffa REAL NULL,
                OreOrd REAL NULL,
                OreAdd REAL NULL,
                OreSper REAL NULL,
                OreCi REAL NULL,
                Importo REAL NULL,
                GiornateImpiego INTEGER NULL,
                UltimaDataServizio TEXT NULL,
                FOREIGN KEY (ElaborazioneMensileId) REFERENCES ElaborazioniMensili (ElaborazioneMensileId) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS IX_ElaborazioneMensileRighe_ElaborazioneMensileId
                ON ElaborazioneMensileRighe (ElaborazioneMensileId);
            """;

        command.ExecuteNonQuery();
    }

    private static void EnsureColumnMigrations(SqliteConnection connection, SqliteTransaction transaction)
    {
        AddColumnIfMissing(connection, transaction, "Personale", "Qualifica", "TEXT NULL");
        AddColumnIfMissing(connection, transaction, "Personale", "ProfiloPersonale", "TEXT NOT NULL DEFAULT 'Operatore Subacqueo'");
        AddColumnIfMissing(connection, transaction, "Personale", "RuoloSanitario", "TEXT NULL");
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
        AddColumnIfMissing(connection, transaction, "PersonaleArchivio", "ProfiloPersonale", "TEXT NOT NULL DEFAULT 'Operatore Subacqueo'");
        AddColumnIfMissing(connection, transaction, "PersonaleArchivio", "RuoloSanitario", "TEXT NULL");
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
        NormalizzaProfiliPersonale(connection, transaction, "Personale");
        NormalizzaProfiliPersonale(connection, transaction, "PersonaleArchivio");
        MigraAttagliamentoPredefinito(connection, transaction, "PersonaleAttagliamento");
        MigraAttagliamentoPredefinito(connection, transaction, "PersonaleAttagliamentoArchivio");
        MigraAbilitazioniSubacquee(connection, transaction, "PersonaleAbilitazioni");
        MigraAbilitazioniSubacquee(connection, transaction, "PersonaleAbilitazioniArchivio");

        AddColumnIfMissing(connection, transaction, "ServiziGiornalieri", "NumeroOrdineServizio", "TEXT NULL");
        AddColumnIfMissing(connection, transaction, "ServiziGiornalieri", "OrarioServizio", "TEXT NULL");
        AddColumnIfMissing(connection, transaction, "ServiziGiornalieri", "StraordinarioAttivo", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing(connection, transaction, "ServiziGiornalieri", "StraordinarioInizio", "TEXT NULL");
        AddColumnIfMissing(connection, transaction, "ServiziGiornalieri", "StraordinarioFine", "TEXT NULL");
        AddColumnIfMissing(connection, transaction, "ServiziGiornalieri", "IndennitaOrdinePubblico", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfMissing(connection, transaction, "TipologieImmersioneOperative", "ProfonditaMinimaMetri", "INTEGER NULL");
        AddColumnIfMissing(connection, transaction, "TipologieImmersioneOperative", "ProfonditaMassimaMetri", "INTEGER NULL");
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

    private static void NormalizzaProfiliPersonale(SqliteConnection connection, SqliteTransaction transaction, string tableName)
    {
        ExecuteMigration(
            connection,
            transaction,
            $"""
            UPDATE {tableName}
            SET ProfiloPersonale = 'Operatore Subacqueo'
            WHERE ProfiloPersonale IS NULL
               OR TRIM(ProfiloPersonale) = ''
               OR TRIM(ProfiloPersonale) = 'SMZ operativo';
            """);
    }

    private static void MigraAbilitazioniSubacquee(SqliteConnection connection, SqliteTransaction transaction, string tableName)
    {
        EseguiMigrazioneAbilitazione(connection, transaction, tableName, 1, "ProfonditaMetri = 60", 23);
        EseguiMigrazioneAbilitazione(connection, transaction, tableName, 1, "ProfonditaMetri = 39", 27);
        EseguiMigrazioneAbilitazione(connection, transaction, tableName, 3, "ProfonditaMetri = 24", 24);
        EseguiMigrazioneAbilitazione(connection, transaction, tableName, 3, "ProfonditaMetri = 54", 26);
        EseguiMigrazioneAbilitazione(connection, transaction, tableName, 4, "ProfonditaMetri = 15", 28);
        EseguiMigrazioneAbilitazione(connection, transaction, tableName, 4, "ProfonditaMetri IN (30, 60)", 29);
    }

    private static void MigraAttagliamentoPredefinito(SqliteConnection connection, SqliteTransaction transaction, string tableName)
    {
        ExecuteMigration(
            connection,
            transaction,
            $"""
            UPDATE {tableName}
            SET Voce = 'Lunghezza piede'
            WHERE TRIM(COALESCE(Voce, '')) = 'Taglia calzature';
            """);
    }

    private static void EseguiMigrazioneAbilitazione(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string tableName,
        int tipoPrecedente,
        string? condizioneExtra,
        int tipoNuovo)
    {
        var whereCondizione = condizioneExtra is null
            ? $"TipoAbilitazioneId = {tipoPrecedente}"
            : $"TipoAbilitazioneId = {tipoPrecedente} AND {condizioneExtra}";

        ExecuteMigration(
            connection,
            transaction,
            $"""
            UPDATE {tableName}
            SET TipoAbilitazioneId = {tipoNuovo},
                ProfonditaMetri = NULL
            WHERE {whereCondizione};
            """);
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

    private static void SeedCataloghiServizio(SqliteConnection connection, SqliteTransaction transaction)
    {
        SeedCategorieRegistro(connection, transaction);
        SeedLocalitaOperative(connection, transaction);
        SeedScopiImmersione(connection, transaction);
        SeedUnitaNavali(connection, transaction);
        SeedTipologieImmersioneOperative(connection, transaction);
        SeedFasceProfondita(connection, transaction);
        SeedCategorieContabiliOre(connection, transaction);
        SeedGruppiOperativi(connection, transaction);
        SeedRuoliOperativi(connection, transaction);
        SeedRegoleContabiliImmersione(connection, transaction);
    }

    private static void SeedCategorieRegistro(SqliteConnection connection, SqliteTransaction transaction)
    {
        foreach (var item in CataloghiServizio.CategorieRegistro)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                """
                INSERT INTO CategorieRegistro (CategoriaRegistroId, Descrizione, Attiva, Ordine)
                VALUES ($id, $descrizione, $attiva, $ordine)
                ON CONFLICT(CategoriaRegistroId) DO UPDATE SET
                    Descrizione = excluded.Descrizione,
                    Attiva = excluded.Attiva,
                    Ordine = excluded.Ordine;
                """;
            command.Parameters.AddWithValue("$id", item.CategoriaRegistroId);
            command.Parameters.AddWithValue("$descrizione", item.Descrizione);
            command.Parameters.AddWithValue("$attiva", item.Attiva ? 1 : 0);
            command.Parameters.AddWithValue("$ordine", item.Ordine);
            command.ExecuteNonQuery();
        }
    }

    private static void SeedLocalitaOperative(SqliteConnection connection, SqliteTransaction transaction)
    {
        foreach (var item in CataloghiServizio.LocalitaOperative)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                """
                INSERT INTO LocalitaOperative (LocalitaOperativaId, Descrizione, Provincia, Attiva, Ordine)
                VALUES ($id, $descrizione, $provincia, $attiva, $ordine)
                ON CONFLICT(LocalitaOperativaId) DO UPDATE SET
                    Descrizione = excluded.Descrizione,
                    Provincia = excluded.Provincia,
                    Attiva = excluded.Attiva,
                    Ordine = excluded.Ordine;
                """;
            command.Parameters.AddWithValue("$id", item.LocalitaOperativaId);
            command.Parameters.AddWithValue("$descrizione", item.Descrizione);
            command.Parameters.AddWithValue("$provincia", string.IsNullOrWhiteSpace(item.Provincia) ? DBNull.Value : item.Provincia);
            command.Parameters.AddWithValue("$attiva", item.Attiva ? 1 : 0);
            command.Parameters.AddWithValue("$ordine", item.Ordine);
            command.ExecuteNonQuery();
        }
    }

    private static void SeedScopiImmersione(SqliteConnection connection, SqliteTransaction transaction)
    {
        foreach (var item in CataloghiServizio.ScopiImmersione)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                """
                INSERT INTO ScopiImmersione (ScopoImmersioneId, Descrizione, CategoriaRegistroId, Attiva, Ordine)
                VALUES ($id, $descrizione, $categoriaRegistroId, $attiva, $ordine)
                ON CONFLICT(ScopoImmersioneId) DO UPDATE SET
                    Descrizione = excluded.Descrizione,
                    CategoriaRegistroId = excluded.CategoriaRegistroId,
                    Attiva = excluded.Attiva,
                    Ordine = excluded.Ordine;
                """;
            command.Parameters.AddWithValue("$id", item.ScopoImmersioneId);
            command.Parameters.AddWithValue("$descrizione", item.Descrizione);
            command.Parameters.AddWithValue("$categoriaRegistroId", item.CategoriaRegistroId);
            command.Parameters.AddWithValue("$attiva", item.Attiva ? 1 : 0);
            command.Parameters.AddWithValue("$ordine", item.Ordine);
            command.ExecuteNonQuery();
        }
    }

    private static void SeedUnitaNavali(SqliteConnection connection, SqliteTransaction transaction)
    {
        foreach (var item in CataloghiServizio.UnitaNavali)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                """
                INSERT INTO UnitaNavali (UnitaNavaleId, Descrizione, Sigla, Attiva, Ordine)
                VALUES ($id, $descrizione, $sigla, $attiva, $ordine)
                ON CONFLICT(UnitaNavaleId) DO UPDATE SET
                    Descrizione = excluded.Descrizione,
                    Sigla = excluded.Sigla,
                    Attiva = excluded.Attiva,
                    Ordine = excluded.Ordine;
                """;
            command.Parameters.AddWithValue("$id", item.UnitaNavaleId);
            command.Parameters.AddWithValue("$descrizione", item.Descrizione);
            command.Parameters.AddWithValue("$sigla", string.IsNullOrWhiteSpace(item.Sigla) ? DBNull.Value : item.Sigla);
            command.Parameters.AddWithValue("$attiva", item.Attiva ? 1 : 0);
            command.Parameters.AddWithValue("$ordine", item.Ordine);
            command.ExecuteNonQuery();
        }
    }

    private static void SeedTipologieImmersioneOperative(SqliteConnection connection, SqliteTransaction transaction)
    {
        foreach (var item in CataloghiServizio.TipologieImmersione)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                """
                INSERT INTO TipologieImmersioneOperative (
                    TipologiaImmersioneOperativaId,
                    Codice,
                    Descrizione,
                    ProfonditaMinimaMetri,
                    ProfonditaMassimaMetri,
                    Attiva,
                    Ordine
                )
                VALUES ($id, $codice, $descrizione, $profonditaMinimaMetri, $profonditaMassimaMetri, $attiva, $ordine)
                ON CONFLICT(TipologiaImmersioneOperativaId) DO UPDATE SET
                    Codice = excluded.Codice,
                    Descrizione = excluded.Descrizione,
                    ProfonditaMinimaMetri = excluded.ProfonditaMinimaMetri,
                    ProfonditaMassimaMetri = excluded.ProfonditaMassimaMetri,
                    Attiva = excluded.Attiva,
                    Ordine = excluded.Ordine;
                """;
            command.Parameters.AddWithValue("$id", item.TipologiaImmersioneOperativaId);
            command.Parameters.AddWithValue("$codice", item.Codice);
            command.Parameters.AddWithValue("$descrizione", item.Descrizione);
            command.Parameters.AddWithValue("$profonditaMinimaMetri", item.ProfonditaMinimaMetri ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("$profonditaMassimaMetri", item.ProfonditaMassimaMetri ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("$attiva", item.Attiva ? 1 : 0);
            command.Parameters.AddWithValue("$ordine", item.Ordine);
            command.ExecuteNonQuery();
        }
    }

    private static void SeedFasceProfondita(SqliteConnection connection, SqliteTransaction transaction)
    {
        foreach (var item in CataloghiServizio.FasceProfondita)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                """
                INSERT INTO FasceProfondita (FasciaProfonditaId, Descrizione, MetriDa, MetriA, Attiva, Ordine)
                VALUES ($id, $descrizione, $metriDa, $metriA, $attiva, $ordine)
                ON CONFLICT(FasciaProfonditaId) DO UPDATE SET
                    Descrizione = excluded.Descrizione,
                    MetriDa = excluded.MetriDa,
                    MetriA = excluded.MetriA,
                    Attiva = excluded.Attiva,
                    Ordine = excluded.Ordine;
                """;
            command.Parameters.AddWithValue("$id", item.FasciaProfonditaId);
            command.Parameters.AddWithValue("$descrizione", item.Descrizione);
            command.Parameters.AddWithValue("$metriDa", item.MetriDa);
            command.Parameters.AddWithValue("$metriA", item.MetriA);
            command.Parameters.AddWithValue("$attiva", item.Attiva ? 1 : 0);
            command.Parameters.AddWithValue("$ordine", item.Ordine);
            command.ExecuteNonQuery();
        }
    }

    private static void SeedCategorieContabiliOre(SqliteConnection connection, SqliteTransaction transaction)
    {
        foreach (var item in CataloghiServizio.CategorieContabiliOre)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                """
                INSERT INTO CategorieContabiliOre (CategoriaContabileOreId, Codice, Descrizione, Attiva, Ordine)
                VALUES ($id, $codice, $descrizione, $attiva, $ordine)
                ON CONFLICT(CategoriaContabileOreId) DO UPDATE SET
                    Codice = excluded.Codice,
                    Descrizione = excluded.Descrizione,
                    Attiva = excluded.Attiva,
                    Ordine = excluded.Ordine;
                """;
            command.Parameters.AddWithValue("$id", item.CategoriaContabileOreId);
            command.Parameters.AddWithValue("$codice", item.Codice);
            command.Parameters.AddWithValue("$descrizione", item.Descrizione);
            command.Parameters.AddWithValue("$attiva", item.Attiva ? 1 : 0);
            command.Parameters.AddWithValue("$ordine", item.Ordine);
            command.ExecuteNonQuery();
        }
    }

    private static void SeedGruppiOperativi(SqliteConnection connection, SqliteTransaction transaction)
    {
        foreach (var item in CataloghiServizio.GruppiOperativi)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                """
                INSERT INTO GruppiOperativi (GruppoOperativoId, Codice, Descrizione, Attiva, Ordine)
                VALUES ($id, $codice, $descrizione, $attiva, $ordine)
                ON CONFLICT(GruppoOperativoId) DO UPDATE SET
                    Codice = excluded.Codice,
                    Descrizione = excluded.Descrizione,
                    Attiva = excluded.Attiva,
                    Ordine = excluded.Ordine;
                """;
            command.Parameters.AddWithValue("$id", item.GruppoOperativoId);
            command.Parameters.AddWithValue("$codice", item.Codice);
            command.Parameters.AddWithValue("$descrizione", item.Descrizione);
            command.Parameters.AddWithValue("$attiva", item.Attiva ? 1 : 0);
            command.Parameters.AddWithValue("$ordine", item.Ordine);
            command.ExecuteNonQuery();
        }
    }

    private static void SeedRuoliOperativi(SqliteConnection connection, SqliteTransaction transaction)
    {
        foreach (var item in CataloghiServizio.RuoliOperativi)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                """
                INSERT INTO RuoliOperativi (RuoloOperativoId, Codice, Descrizione, Attiva, Ordine)
                VALUES ($id, $codice, $descrizione, $attiva, $ordine)
                ON CONFLICT(RuoloOperativoId) DO UPDATE SET
                    Codice = excluded.Codice,
                    Descrizione = excluded.Descrizione,
                    Attiva = excluded.Attiva,
                    Ordine = excluded.Ordine;
                """;
            command.Parameters.AddWithValue("$id", item.RuoloOperativoId);
            command.Parameters.AddWithValue("$codice", item.Codice);
            command.Parameters.AddWithValue("$descrizione", item.Descrizione);
            command.Parameters.AddWithValue("$attiva", item.Attiva ? 1 : 0);
            command.Parameters.AddWithValue("$ordine", item.Ordine);
            command.ExecuteNonQuery();
        }
    }

    private static void SeedRegoleContabiliImmersione(SqliteConnection connection, SqliteTransaction transaction)
    {
        foreach (var item in CataloghiServizio.RegoleContabiliImmersione)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                """
                INSERT INTO RegoleContabiliImmersione (
                    RegolaContabileImmersioneId,
                    TipologiaImmersioneOperativaId,
                    FasciaProfonditaId,
                    CategoriaContabileOreId,
                    Tariffa,
                    DataInizioValidita,
                    DataFineValidita,
                    Attiva
                )
                VALUES (
                    $id,
                    $tipologiaImmersioneOperativaId,
                    $fasciaProfonditaId,
                    $categoriaContabileOreId,
                    $tariffa,
                    $dataInizioValidita,
                    $dataFineValidita,
                    $attiva
                )
                ON CONFLICT(RegolaContabileImmersioneId) DO NOTHING;
                """;
            command.Parameters.AddWithValue("$id", item.RegolaContabileImmersioneId);
            command.Parameters.AddWithValue("$tipologiaImmersioneOperativaId", item.TipologiaImmersioneOperativaId);
            command.Parameters.AddWithValue("$fasciaProfonditaId", item.FasciaProfonditaId);
            command.Parameters.AddWithValue("$categoriaContabileOreId", item.CategoriaContabileOreId);
            command.Parameters.AddWithValue("$tariffa", item.Tariffa);
            command.Parameters.AddWithValue("$dataInizioValidita", item.DataInizioValidita?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("$dataFineValidita", item.DataFineValidita?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("$attiva", item.Attiva ? 1 : 0);
            command.ExecuteNonQuery();
        }
    }
}
