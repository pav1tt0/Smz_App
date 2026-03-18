using Microsoft.Data.Sqlite;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.Data;

public sealed class PersonaleRepository
{
    public List<string> GetSearchSuggestions()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Cognome, Nome
            FROM Personale
            ORDER BY Cognome, Nome;
            """;

        using var reader = command.ExecuteReader();
        var items = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (reader.Read())
        {
            var cognome = reader.GetString(0).Trim();
            var nome = reader.GetString(1).Trim();
            var nominativo = $"{cognome} {nome}".Trim();

            if (!string.IsNullOrWhiteSpace(cognome))
            {
                items.Add(cognome);
            }

            if (!string.IsNullOrWhiteSpace(nome))
            {
                items.Add(nome);
            }

            if (!string.IsNullOrWhiteSpace(nominativo))
            {
                items.Add(nominativo);
            }
        }

        return items.OrderBy(item => item).ToList();
    }

    public bool ExistsPersonale(int perId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM Personale WHERE PerId = $perId;";
        command.Parameters.AddWithValue("$perId", perId);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    public List<TipoAbilitazione> GetTipiAbilitazione()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT TipoAbilitazioneId, Codice, Descrizione, Categoria, RichiedeLivello, RichiedeScadenza, RichiedeProfondita
            FROM TipiAbilitazione
            ORDER BY Categoria, Descrizione;
            """;

        using var reader = command.ExecuteReader();
        var items = new List<TipoAbilitazione>();

        while (reader.Read())
        {
            items.Add(new TipoAbilitazione
            {
                TipoAbilitazioneId = reader.GetInt32(0),
                Codice = reader.GetString(1),
                Descrizione = reader.GetString(2),
                Categoria = reader.GetString(3),
                RichiedeLivello = reader.GetInt32(4) == 1,
                RichiedeScadenza = reader.GetInt32(5) == 1,
                RichiedeProfondita = reader.GetInt32(6) == 1,
            });
        }

        return items;
    }

    public List<ScadenzaProgrammata> GetScadenzeProssime(DateOnly daData, DateOnly aData)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT p.PerId,
                   p.Cognome,
                   p.Nome,
                   'Abilitazione' AS Origine,
                   ta.Descrizione AS Titolo,
                   TRIM(
                       COALESCE(NULLIF(pa.Livello, ''), '') ||
                       CASE
                           WHEN pa.ProfonditaMetri IS NOT NULL AND pa.Livello IS NOT NULL AND pa.Livello <> '' THEN ' | '
                           ELSE ''
                       END ||
                       COALESCE(CASE WHEN pa.ProfonditaMetri IS NOT NULL THEN CAST(pa.ProfonditaMetri AS TEXT) || ' m' END, '')
                   ) AS Dettaglio,
                   pa.DataScadenza AS DataScadenza
            FROM PersonaleAbilitazioni pa
            INNER JOIN Personale p ON p.PerId = pa.PerId
            INNER JOIN TipiAbilitazione ta ON ta.TipoAbilitazioneId = pa.TipoAbilitazioneId
            WHERE pa.DataScadenza IS NOT NULL
              AND pa.DataScadenza >= $daData
              AND pa.DataScadenza <= $aData

            UNION ALL

            SELECT p.PerId,
                   p.Cognome,
                   p.Nome,
                   'Visita medica' AS Origine,
                   vm.TipoVisita AS Titolo,
                   COALESCE(NULLIF(vm.Esito, ''), '') AS Dettaglio,
                   vm.DataScadenza AS DataScadenza
            FROM VisiteMediche vm
            INNER JOIN Personale p ON p.PerId = vm.PerId
            WHERE vm.DataScadenza IS NOT NULL
              AND vm.DataScadenza >= $daData
              AND vm.DataScadenza <= $aData

            ORDER BY DataScadenza, Cognome, Nome, Titolo;
            """;
        command.Parameters.AddWithValue("$daData", daData.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("$aData", aData.ToString("yyyy-MM-dd"));

        using var reader = command.ExecuteReader();
        var items = new List<ScadenzaProgrammata>();

        while (reader.Read())
        {
            var dataScadenza = DateOnly.Parse(reader.GetString(6));
            items.Add(new ScadenzaProgrammata
            {
                PerId = reader.GetInt32(0),
                Cognome = reader.GetString(1),
                Nome = reader.GetString(2),
                Origine = reader.GetString(3),
                Titolo = reader.GetString(4),
                Dettaglio = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                DataScadenza = dataScadenza,
                GiorniResidui = dataScadenza.DayNumber - daData.DayNumber,
            });
        }

        return items;
    }

    public List<Personale> SearchPersonale(string cognomeFiltro, int? tipoAbilitazioneIdFiltro, DateOnly? visiteEntro)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        var clauses = new List<string>();

        if (!string.IsNullOrWhiteSpace(cognomeFiltro))
        {
            clauses.Add("(p.Cognome LIKE $cognome OR p.Nome LIKE $cognome)");
            command.Parameters.AddWithValue("$cognome", $"%{cognomeFiltro.Trim()}%");
        }

        if (tipoAbilitazioneIdFiltro is not null)
        {
            clauses.Add(
                """
                EXISTS (
                    SELECT 1
                    FROM PersonaleAbilitazioni pa
                    WHERE pa.PerId = p.PerId
                      AND pa.TipoAbilitazioneId = $tipoAbilitazioneId
                )
                """);
            command.Parameters.AddWithValue("$tipoAbilitazioneId", tipoAbilitazioneIdFiltro.Value);
        }

        if (visiteEntro is not null)
        {
            clauses.Add(
                """
                EXISTS (
                    SELECT 1
                    FROM VisiteMediche vm
                    WHERE vm.PerId = p.PerId
                      AND vm.DataScadenza IS NOT NULL
                      AND vm.DataScadenza <= $visiteEntro
                )
                """);
            command.Parameters.AddWithValue("$visiteEntro", ToDbDate(visiteEntro));
        }

        var whereClause = clauses.Count == 0 ? string.Empty : $"WHERE {string.Join(" AND ", clauses)}";

        command.CommandText =
            $"""
            SELECT p.PerId, p.Cognome, p.Nome, p.CodiceFiscale, p.DataNascita, p.LuogoNascita, p.IndirizzoResidenza, p.Telefono, p.Mail
            FROM Personale p
            {whereClause}
            ORDER BY p.Cognome, p.Nome;
            """;

        using var reader = command.ExecuteReader();
        var items = new List<Personale>();

        while (reader.Read())
        {
            items.Add(MapPersonale(reader));
        }

        return items;
    }

    public Personale? GetPersonaleById(int perId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT PerId, Cognome, Nome, CodiceFiscale, DataNascita, LuogoNascita, IndirizzoResidenza, Telefono, Mail
            FROM Personale
            WHERE PerId = $perId;
            """;
        command.Parameters.AddWithValue("$perId", perId);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        var personale = MapPersonale(reader);
        reader.Close();

        personale.Abilitazioni = GetAbilitazioni(connection, perId);
        personale.VisiteMediche = GetVisite(connection, perId);
        return personale;
    }

    public int SavePersonale(Personale personale, bool isNewRecord)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        int perId;
        if (isNewRecord)
        {
            using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText =
                """
                INSERT INTO Personale (PerId, Cognome, Nome, CodiceFiscale, DataNascita, LuogoNascita, IndirizzoResidenza, Telefono, Mail)
                VALUES ($perId, $cognome, $nome, $codiceFiscale, $dataNascita, $luogoNascita, $indirizzoResidenza, $telefono, $mail);
                """;
            AddPersonaleParameters(insert, personale);
            insert.Parameters.AddWithValue("$perId", personale.PerId);
            insert.ExecuteNonQuery();
            perId = personale.PerId;
        }
        else
        {
            using var update = connection.CreateCommand();
            update.Transaction = transaction;
            update.CommandText =
                """
                UPDATE Personale
                SET Cognome = $cognome,
                    Nome = $nome,
                    CodiceFiscale = $codiceFiscale,
                    DataNascita = $dataNascita,
                    LuogoNascita = $luogoNascita,
                    IndirizzoResidenza = $indirizzoResidenza,
                    Telefono = $telefono,
                    Mail = $mail
                WHERE PerId = $perId;
                """;
            AddPersonaleParameters(update, personale);
            update.Parameters.AddWithValue("$perId", personale.PerId);
            update.ExecuteNonQuery();
            perId = personale.PerId;

            DeleteChildRows(connection, transaction, "PersonaleAbilitazioni", perId);
            DeleteChildRows(connection, transaction, "VisiteMediche", perId);
        }

        InsertAbilitazioni(connection, transaction, perId, personale.Abilitazioni);
        InsertVisite(connection, transaction, perId, personale.VisiteMediche);
        transaction.Commit();

        return perId;
    }

    public void DeletePersonale(int perId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Personale WHERE PerId = $perId;";
        command.Parameters.AddWithValue("$perId", perId);
        command.ExecuteNonQuery();
    }

    private static void AddPersonaleParameters(SqliteCommand command, Personale personale)
    {
        command.Parameters.AddWithValue("$cognome", personale.Cognome.Trim());
        command.Parameters.AddWithValue("$nome", personale.Nome.Trim());
        command.Parameters.AddWithValue("$codiceFiscale", personale.CodiceFiscale.Trim().ToUpperInvariant());
        command.Parameters.AddWithValue("$dataNascita", ToDbValue(personale.DataNascita));
        command.Parameters.AddWithValue("$luogoNascita", DbText(personale.LuogoNascita));
        command.Parameters.AddWithValue("$indirizzoResidenza", DbText(personale.IndirizzoResidenza));
        command.Parameters.AddWithValue("$telefono", DbText(personale.Telefono));
        command.Parameters.AddWithValue("$mail", DbText(personale.Mail));
    }

    private static void DeleteChildRows(SqliteConnection connection, SqliteTransaction transaction, string tableName, int perId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"DELETE FROM {tableName} WHERE PerId = $perId;";
        command.Parameters.AddWithValue("$perId", perId);
        command.ExecuteNonQuery();
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

    private static List<PersonaleAbilitazione> GetAbilitazioni(SqliteConnection connection, int perId)
    {
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

    private static List<VisitaMedica> GetVisite(SqliteConnection connection, int perId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT VisitaMedicaId, PerId, TipoVisita, DataUltimaVisita, DataScadenza, Esito, Note
            FROM VisiteMediche
            WHERE PerId = $perId
            ORDER BY TipoVisita;
            """;
        command.Parameters.AddWithValue("$perId", perId);

        using var reader = command.ExecuteReader();
        var items = new List<VisitaMedica>();

        while (reader.Read())
        {
            items.Add(new VisitaMedica
            {
                VisitaMedicaId = reader.GetInt32(0),
                PerId = reader.GetInt32(1),
                TipoVisita = reader.GetString(2),
                DataUltimaVisita = ParseDbDate(reader, 3),
                DataScadenza = ParseDbDate(reader, 4),
                Esito = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                Note = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
            });
        }

        return items;
    }

    private static Personale MapPersonale(SqliteDataReader reader)
    {
        return new Personale
        {
            PerId = reader.GetInt32(0),
            Cognome = reader.GetString(1),
            Nome = reader.GetString(2),
            CodiceFiscale = reader.GetString(3),
            DataNascita = ParseDbDate(reader, 4),
            LuogoNascita = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
            IndirizzoResidenza = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
            Telefono = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
            Mail = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
        };
    }

    private static SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(DatabasePaths.ConnectionString);
        connection.Open();
        return connection;
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

    private static object DbText(string value) => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
}
