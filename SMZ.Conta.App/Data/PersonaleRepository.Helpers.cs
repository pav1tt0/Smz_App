using Microsoft.Data.Sqlite;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.Data;

public sealed partial class PersonaleRepository
{
    private static void AddPersonaleParameters(SqliteCommand command, Personale personale)
    {
        command.Parameters.AddWithValue("$cognome", personale.Cognome.Trim());
        command.Parameters.AddWithValue("$nome", personale.Nome.Trim());
        command.Parameters.AddWithValue("$qualifica", DbText(personale.Qualifica));
        command.Parameters.AddWithValue("$dataDecorrenzaQualifica", ToDbValue(personale.DataDecorrenzaQualifica));
        command.Parameters.AddWithValue("$profiloPersonale", ProfiliPersonaleCatalogo.Normalizza(personale.ProfiloPersonale));
        command.Parameters.AddWithValue("$ruoloSanitario", DbText(personale.RuoloSanitario));
        command.Parameters.AddWithValue("$codiceFiscale", personale.CodiceFiscale.Trim().ToUpperInvariant());
        command.Parameters.AddWithValue("$matricolaPersonale", DbText(personale.MatricolaPersonale));
        command.Parameters.AddWithValue("$numeroBrevettoSmz", DbText(personale.NumeroBrevettoSmz));
        command.Parameters.AddWithValue("$statoServizio", StatoServizioPersonaleCatalogo.Normalizza(personale.StatoServizio));
        command.Parameters.AddWithValue("$dataFineServizio", ToDbValue(personale.DataFineServizio));
        command.Parameters.AddWithValue("$dataNascita", ToDbValue(personale.DataNascita));
        command.Parameters.AddWithValue("$luogoNascita", DbText(personale.LuogoNascita));
        command.Parameters.AddWithValue("$viaResidenza", DbText(personale.ViaResidenza));
        command.Parameters.AddWithValue("$capResidenza", DbText(personale.CapResidenza));
        command.Parameters.AddWithValue("$cittaResidenza", DbText(personale.CittaResidenza));
        command.Parameters.AddWithValue("$telefono1", DbText(personale.Telefono1));
        command.Parameters.AddWithValue("$telefono2", DbText(personale.Telefono2));
        command.Parameters.AddWithValue("$mail1Utente", DbText(personale.Mail1Utente));
        command.Parameters.AddWithValue("$mail2Utente", DbText(personale.Mail2Utente));
    }

    private static void AddServizioParameters(SqliteCommand command, ServizioGiornaliero servizio)
    {
        command.Parameters.AddWithValue("$dataServizio", servizio.DataServizio.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("$numeroOrdineServizio", DbText(servizio.NumeroOrdineServizio));
        command.Parameters.AddWithValue("$orarioServizio", DbText(servizio.OrarioServizio));
        command.Parameters.AddWithValue("$straordinarioAttivo", servizio.StraordinarioAttivo ? 1 : 0);
        command.Parameters.AddWithValue("$straordinarioInizio", DbText(servizio.StraordinarioInizio));
        command.Parameters.AddWithValue("$straordinarioFine", DbText(servizio.StraordinarioFine));
        command.Parameters.AddWithValue("$tipoServizio", servizio.TipoServizio.Trim());
        command.Parameters.AddWithValue("$localitaOperativaId", servizio.LocalitaOperativaId is null ? DBNull.Value : servizio.LocalitaOperativaId.Value);
        command.Parameters.AddWithValue("$scopoImmersioneId", servizio.ScopoImmersioneId is null ? DBNull.Value : servizio.ScopoImmersioneId.Value);
        command.Parameters.AddWithValue("$unitaNavaleId", servizio.UnitaNavaleId is null ? DBNull.Value : servizio.UnitaNavaleId.Value);
        command.Parameters.AddWithValue("$responsabileServizioPerId", servizio.ResponsabileServizioPerId is null ? DBNull.Value : servizio.ResponsabileServizioPerId.Value);
        command.Parameters.AddWithValue("$fuoriSede", servizio.FuoriSede ? 1 : 0);
        command.Parameters.AddWithValue("$indennitaOrdinePubblico", servizio.IndennitaOrdinePubblico ? 1 : 0);
        command.Parameters.AddWithValue("$attivitaSvolta", DbText(servizio.AttivitaSvolta));
        command.Parameters.AddWithValue("$note", DbText(servizio.Note));
    }

    private static void DeleteChildRows(SqliteConnection connection, SqliteTransaction transaction, string tableName, int perId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"DELETE FROM {tableName} WHERE PerId = $perId;";
        command.Parameters.AddWithValue("$perId", perId);
        command.ExecuteNonQuery();
    }

    private static PersonaleServiceUsage GetPersonaleServiceUsage(SqliteConnection connection, SqliteTransaction transaction, int perId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            SELECT
                (
                    SELECT COUNT(DISTINCT ServizioGiornalieroId)
                    FROM ServizioPartecipanti
                    WHERE PerId = $perId
                ) AS ServiziComePartecipante,
                (
                    SELECT COUNT(1)
                    FROM ServizioImmersioni
                    WHERE DirettoreImmersionePerId = $perId
                       OR OperatoreSoccorsoPerId = $perId
                       OR AssistenteBlsdPerId = $perId
                       OR AssistenteSanitarioPerId = $perId
                ) AS ImmersioniConRuoli;
            """;
        command.Parameters.AddWithValue("$perId", perId);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return new PersonaleServiceUsage(0, 0);
        }

        return new PersonaleServiceUsage(reader.GetInt32(0), reader.GetInt32(1));
    }

    private static void InsertAbilitazioni(SqliteConnection connection, SqliteTransaction transaction, int perId, IEnumerable<PersonaleAbilitazione> abilitazioni)
    {
        foreach (var abilitazione in abilitazioni)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                """
                INSERT INTO PersonaleAbilitazioni (PerId, TipoAbilitazioneId, Livello, ProfonditaMetri, DataConseguimento, DataScadenza, Note)
                VALUES ($perId, $tipoAbilitazioneId, $livello, $profonditaMetri, $dataConseguimento, $dataScadenza, $note);
                """;
            command.Parameters.AddWithValue("$perId", perId);
            command.Parameters.AddWithValue("$tipoAbilitazioneId", abilitazione.TipoAbilitazioneId);
            command.Parameters.AddWithValue("$livello", DbText(abilitazione.Livello));
            command.Parameters.AddWithValue("$profonditaMetri", abilitazione.ProfonditaMetri is null ? DBNull.Value : abilitazione.ProfonditaMetri.Value);
            command.Parameters.AddWithValue("$dataConseguimento", ToDbValue(abilitazione.DataConseguimento));
            command.Parameters.AddWithValue("$dataScadenza", ToDbValue(abilitazione.DataScadenza));
            command.Parameters.AddWithValue("$note", DbText(abilitazione.Note));
            command.ExecuteNonQuery();
        }
    }

    private static long ArchivePersonale(SqliteConnection connection, SqliteTransaction transaction, int perId, string archivedAt)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO PersonaleArchivio (
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
            )
            SELECT PerId,
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
                   $dataArchiviazione
            FROM Personale
            WHERE PerId = $perId;
            """;
        command.Parameters.AddWithValue("$perId", perId);
        command.Parameters.AddWithValue("$dataArchiviazione", archivedAt);

        if (command.ExecuteNonQuery() == 0)
        {
            throw new InvalidOperationException($"Scheda con PerID {perId} non trovata.");
        }

        using var lastInsertCommand = connection.CreateCommand();
        lastInsertCommand.Transaction = transaction;
        lastInsertCommand.CommandText = "SELECT last_insert_rowid();";
        return Convert.ToInt64(lastInsertCommand.ExecuteScalar());
    }

    private static void ArchiveAbilitazioni(SqliteConnection connection, SqliteTransaction transaction, long archiveId, int perId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO PersonaleAbilitazioniArchivio (
                PersonaleArchivioId,
                PerIdOriginale,
                TipoAbilitazioneId,
                Livello,
                ProfonditaMetri,
                DataConseguimento,
                DataScadenza,
                Note
            )
            SELECT $personaleArchivioId,
                   PerId,
                   TipoAbilitazioneId,
                   Livello,
                   ProfonditaMetri,
                   DataConseguimento,
                   DataScadenza,
                   Note
            FROM PersonaleAbilitazioni
            WHERE PerId = $perId;
            """;
        command.Parameters.AddWithValue("$personaleArchivioId", archiveId);
        command.Parameters.AddWithValue("$perId", perId);
        command.ExecuteNonQuery();
    }

    private static void ArchiveVisite(SqliteConnection connection, SqliteTransaction transaction, long archiveId, int perId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO VisiteMedicheArchivio (
                PersonaleArchivioId,
                PerIdOriginale,
                TipoVisita,
                DataUltimaVisita,
                DataScadenza,
                Esito,
                Note
            )
            SELECT $personaleArchivioId,
                   PerId,
                   TipoVisita,
                   DataUltimaVisita,
                   DataScadenza,
                   Esito,
                   Note
            FROM VisiteMediche
            WHERE PerId = $perId;
            """;
        command.Parameters.AddWithValue("$personaleArchivioId", archiveId);
        command.Parameters.AddWithValue("$perId", perId);
        command.ExecuteNonQuery();
    }

    private static void ArchiveAttagliamento(SqliteConnection connection, SqliteTransaction transaction, long archiveId, int perId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO PersonaleAttagliamentoArchivio (
                PersonaleArchivioId,
                PerIdOriginale,
                Voce,
                TagliaMisura,
                Note
            )
            SELECT $personaleArchivioId,
                   PerId,
                   Voce,
                   TagliaMisura,
                   Note
            FROM PersonaleAttagliamento
            WHERE PerId = $perId;
            """;
        command.Parameters.AddWithValue("$personaleArchivioId", archiveId);
        command.Parameters.AddWithValue("$perId", perId);
        command.ExecuteNonQuery();
    }

    private static void InsertVisite(SqliteConnection connection, SqliteTransaction transaction, int perId, IEnumerable<VisitaMedica> visite)
    {
        foreach (var visita in visite)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                """
                INSERT INTO VisiteMediche (PerId, TipoVisita, DataUltimaVisita, DataScadenza, Esito, Note)
                VALUES ($perId, $tipoVisita, $dataUltimaVisita, $dataScadenza, $esito, $note);
                """;
            command.Parameters.AddWithValue("$perId", perId);
            command.Parameters.AddWithValue("$tipoVisita", visita.TipoVisita.Trim());
            command.Parameters.AddWithValue("$dataUltimaVisita", ToDbValue(visita.DataUltimaVisita));
            command.Parameters.AddWithValue("$dataScadenza", ToDbValue(visita.DataScadenza));
            command.Parameters.AddWithValue("$esito", DbText(visita.Esito));
            command.Parameters.AddWithValue("$note", DbText(visita.Note));
            command.ExecuteNonQuery();
        }
    }

    private static void InsertAttagliamento(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int perId,
        IEnumerable<PersonaleAttagliamento> attagliamento)
    {
        foreach (var riga in attagliamento)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                """
                INSERT INTO PersonaleAttagliamento (PerId, Voce, TagliaMisura, Note)
                VALUES ($perId, $voce, $tagliaMisura, $note);
                """;
            command.Parameters.AddWithValue("$perId", perId);
            command.Parameters.AddWithValue("$voce", riga.Voce.Trim());
            command.Parameters.AddWithValue("$tagliaMisura", DbText(riga.TagliaMisura));
            command.Parameters.AddWithValue("$note", DbText(riga.Note));
            command.ExecuteNonQuery();
        }
    }

    private static List<PersonaleAbilitazione> GetAbilitazioniArchivio(SqliteConnection connection, long archiveId)
    {
        var tipiAttivi = CatalogoAbilitazioni.Tutte
            .Select(item => item.TipoAbilitazioneId)
            .ToHashSet();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT paa.TipoAbilitazioneId,
                   paa.Livello,
                   paa.ProfonditaMetri,
                   paa.DataConseguimento,
                   paa.DataScadenza,
                   paa.Note,
                   ta.Codice,
                   ta.Descrizione,
                   ta.Categoria,
                   ta.RichiedeLivello,
                   ta.RichiedeScadenza,
                   ta.RichiedeProfondita
            FROM PersonaleAbilitazioniArchivio paa
            INNER JOIN TipiAbilitazione ta ON ta.TipoAbilitazioneId = paa.TipoAbilitazioneId
            WHERE paa.PersonaleArchivioId = $archiveId
            ORDER BY ta.Categoria, ta.Descrizione;
            """;
        command.Parameters.AddWithValue("$archiveId", archiveId);

        using var reader = command.ExecuteReader();
        var items = new List<PersonaleAbilitazione>();

        while (reader.Read())
        {
            if (!tipiAttivi.Contains(reader.GetInt32(0)))
            {
                continue;
            }

            items.Add(new PersonaleAbilitazione
            {
                TipoAbilitazioneId = reader.GetInt32(0),
                Livello = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                ProfonditaMetri = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                DataConseguimento = ParseDbDate(reader, 3),
                DataScadenza = ParseDbDate(reader, 4),
                Note = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                Tipo = new TipoAbilitazione
                {
                    TipoAbilitazioneId = reader.GetInt32(0),
                    Codice = reader.GetString(6),
                    Descrizione = reader.GetString(7),
                    Categoria = reader.GetString(8),
                    RichiedeLivello = reader.GetInt32(9) == 1,
                    RichiedeScadenza = reader.GetInt32(10) == 1,
                    RichiedeProfondita = reader.GetInt32(11) == 1,
                },
            });
        }

        return items;
    }

    private static List<VisitaMedica> GetVisiteArchivio(SqliteConnection connection, long archiveId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT TipoVisita, DataUltimaVisita, DataScadenza, Esito, Note
            FROM VisiteMedicheArchivio
            WHERE PersonaleArchivioId = $archiveId
            ORDER BY TipoVisita;
            """;
        command.Parameters.AddWithValue("$archiveId", archiveId);

        using var reader = command.ExecuteReader();
        var items = new List<VisitaMedica>();

        while (reader.Read())
        {
            items.Add(new VisitaMedica
            {
                TipoVisita = reader.GetString(0),
                DataUltimaVisita = ParseDbDate(reader, 1),
                DataScadenza = ParseDbDate(reader, 2),
                Esito = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                Note = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
            });
        }

        return items;
    }

    private static List<PersonaleAttagliamento> GetAttagliamentoArchivio(SqliteConnection connection, long archiveId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Voce, TagliaMisura, Note
            FROM PersonaleAttagliamentoArchivio
            WHERE PersonaleArchivioId = $archiveId
            ORDER BY Voce;
            """;
        command.Parameters.AddWithValue("$archiveId", archiveId);

        using var reader = command.ExecuteReader();
        var items = new List<PersonaleAttagliamento>();

        while (reader.Read())
        {
            items.Add(new PersonaleAttagliamento
            {
                Voce = reader.GetString(0),
                TagliaMisura = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                Note = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
            });
        }

        return items;
    }

    private static List<PersonaleAbilitazione> GetAbilitazioni(SqliteConnection connection, int perId)
    {
        var tipiAttivi = CatalogoAbilitazioni.Tutte
            .Select(item => item.TipoAbilitazioneId)
            .ToHashSet();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT pa.PersonaleAbilitazioneId,
                   pa.PerId,
                   pa.TipoAbilitazioneId,
                   pa.Livello,
                   pa.ProfonditaMetri,
                   pa.DataConseguimento,
                   pa.DataScadenza,
                   pa.Note,
                   ta.Codice,
                   ta.Descrizione,
                   ta.Categoria,
                   ta.RichiedeLivello,
                   ta.RichiedeScadenza,
                   ta.RichiedeProfondita
            FROM PersonaleAbilitazioni pa
            INNER JOIN TipiAbilitazione ta ON ta.TipoAbilitazioneId = pa.TipoAbilitazioneId
            WHERE pa.PerId = $perId
            ORDER BY ta.Categoria, ta.Descrizione;
            """;
        command.Parameters.AddWithValue("$perId", perId);

        using var reader = command.ExecuteReader();
        var items = new List<PersonaleAbilitazione>();

        while (reader.Read())
        {
            if (!tipiAttivi.Contains(reader.GetInt32(2)))
            {
                continue;
            }

            items.Add(new PersonaleAbilitazione
            {
                PersonaleAbilitazioneId = reader.GetInt32(0),
                PerId = reader.GetInt32(1),
                TipoAbilitazioneId = reader.GetInt32(2),
                Livello = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                ProfonditaMetri = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                DataConseguimento = ParseDbDate(reader, 5),
                DataScadenza = ParseDbDate(reader, 6),
                Note = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                Tipo = new TipoAbilitazione
                {
                    TipoAbilitazioneId = reader.GetInt32(2),
                    Codice = reader.GetString(8),
                    Descrizione = reader.GetString(9),
                    Categoria = reader.GetString(10),
                    RichiedeLivello = reader.GetInt32(11) == 1,
                    RichiedeScadenza = reader.GetInt32(12) == 1,
                    RichiedeProfondita = reader.GetInt32(13) == 1,
                },
            });
        }

        return items;
    }

    private static List<PersonaleAttagliamento> GetAttagliamento(SqliteConnection connection, int perId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT PersonaleAttagliamentoId, PerId, Voce, TagliaMisura, Note
            FROM PersonaleAttagliamento
            WHERE PerId = $perId
            ORDER BY Voce;
            """;
        command.Parameters.AddWithValue("$perId", perId);

        using var reader = command.ExecuteReader();
        var items = new List<PersonaleAttagliamento>();

        while (reader.Read())
        {
            items.Add(new PersonaleAttagliamento
            {
                PersonaleAttagliamentoId = reader.GetInt32(0),
                PerId = reader.GetInt32(1),
                Voce = reader.GetString(2),
                TagliaMisura = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                Note = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
            });
        }

        return items;
    }
}
