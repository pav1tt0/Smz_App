using Microsoft.Data.Sqlite;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.Data;

public sealed class PersonaleRepository
{
    public List<PersonaleArchivioSummary> GetArchivio()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT PersonaleArchivioId,
                   PerIdOriginale,
                   Cognome,
                   Nome,
                   CodiceFiscale,
                   DataArchiviazione
            FROM PersonaleArchivio
            ORDER BY DataArchiviazione DESC, Cognome, Nome;
            """;

        using var reader = command.ExecuteReader();
        var items = new List<PersonaleArchivioSummary>();

        while (reader.Read())
        {
            items.Add(new PersonaleArchivioSummary
            {
                PersonaleArchivioId = reader.GetInt64(0),
                PerIdOriginale = reader.GetInt32(1),
                Cognome = reader.GetString(2),
                Nome = reader.GetString(3),
                CodiceFiscale = reader.GetString(4),
                DataArchiviazione = DateTime.Parse(reader.GetString(5)),
            });
        }

        return items;
    }

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
            ORDER BY TipoAbilitazioneId;
            """;

        using var reader = command.ExecuteReader();
        var items = new List<TipoAbilitazione>();

        while (reader.Read())
        {
            items.Add(CatalogoAbilitazioni.ApplicaSuggerimenti(new TipoAbilitazione
            {
                TipoAbilitazioneId = reader.GetInt32(0),
                Codice = reader.GetString(1),
                Descrizione = reader.GetString(2),
                Categoria = reader.GetString(3),
                RichiedeLivello = reader.GetInt32(4) == 1,
                RichiedeScadenza = reader.GetInt32(5) == 1,
                RichiedeProfondita = reader.GetInt32(6) == 1,
            }));
        }

        return items
            .OrderBy(CatalogoAbilitazioni.GetOrdineVisualizzazione)
            .ThenBy(item => item.Descrizione)
            .ToList();
    }

    public CataloghiServizioSnapshot GetCataloghiServizio()
    {
        using var connection = OpenConnection();

        return new CataloghiServizioSnapshot
        {
            CategorieRegistro = GetCategorieRegistro(connection),
            LocalitaOperative = GetLocalitaOperative(connection),
            ScopiImmersione = GetScopiImmersione(connection),
            UnitaNavali = GetUnitaNavali(connection),
            TipologieImmersione = GetTipologieImmersioneOperative(connection),
            FasceProfondita = GetFasceProfondita(connection),
            CategorieContabiliOre = GetCategorieContabiliOre(connection),
            GruppiOperativi = GetGruppiOperativi(connection),
            RegoleContabiliImmersione = GetRegoleContabiliImmersione(connection),
            RuoliOperativi = GetRuoliOperativi(connection),
        };
    }

    public List<ServizioGiornalieroSummary> GetServiziGiornalieriRecenti(int maxItems = 24)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT s.ServizioGiornalieroId,
                   s.DataServizio,
                   COALESCE(s.NumeroOrdineServizio, '') AS NumeroOrdineServizio,
                   COALESCE(s.OrarioServizio, '') AS OrarioServizio,
                   s.TipoServizio,
                   COALESCE(lo.Descrizione, '') AS LocalitaDescrizione,
                   COALESCE(sc.Descrizione, '') AS ScopoDescrizione,
                   COALESCE(unv.Descrizione, '') AS UnitaNavaleDescrizione,
                   s.FuoriSede,
                   (
                       SELECT COUNT(1)
                       FROM ServizioPartecipanti sp
                       WHERE sp.ServizioGiornalieroId = s.ServizioGiornalieroId
                   ) + (
                       SELECT COUNT(1)
                       FROM ServizioSupportiOccasionali so
                       WHERE so.ServizioGiornalieroId = s.ServizioGiornalieroId
                   ) AS PartecipantiTotali,
                   (
                       SELECT COUNT(1)
                       FROM ServizioPartecipanti sp
                       WHERE sp.ServizioGiornalieroId = s.ServizioGiornalieroId
                         AND sp.Presente = 1
                   ) + (
                       SELECT COUNT(1)
                       FROM ServizioSupportiOccasionali so
                       WHERE so.ServizioGiornalieroId = s.ServizioGiornalieroId
                         AND so.Presente = 1
                   ) AS PresentiTotali,
                   (
                       SELECT COUNT(1)
                       FROM ServizioImmersioni si
                       WHERE si.ServizioGiornalieroId = s.ServizioGiornalieroId
                   ) AS ImmersioniTotali,
                   s.AggiornatoIl
            FROM ServiziGiornalieri s
            LEFT JOIN LocalitaOperative lo ON lo.LocalitaOperativaId = s.LocalitaOperativaId
            LEFT JOIN ScopiImmersione sc ON sc.ScopoImmersioneId = s.ScopoImmersioneId
            LEFT JOIN UnitaNavali unv ON unv.UnitaNavaleId = s.UnitaNavaleId
            ORDER BY s.DataServizio DESC, s.ServizioGiornalieroId DESC
            LIMIT $maxItems;
            """;
        command.Parameters.AddWithValue("$maxItems", maxItems);

        using var reader = command.ExecuteReader();
        var items = new List<ServizioGiornalieroSummary>();

        while (reader.Read())
        {
            items.Add(new ServizioGiornalieroSummary
            {
                ServizioGiornalieroId = reader.GetInt64(0),
                DataServizio = DateOnly.Parse(reader.GetString(1)),
                NumeroOrdineServizio = reader.GetString(2),
                OrarioServizio = reader.GetString(3),
                TipoServizio = reader.GetString(4),
                LocalitaDescrizione = reader.GetString(5),
                ScopoDescrizione = reader.GetString(6),
                UnitaNavaleDescrizione = reader.GetString(7),
                FuoriSede = reader.GetInt32(8) == 1,
                PartecipantiTotali = reader.GetInt32(9),
                PresentiTotali = reader.GetInt32(10),
                ImmersioniTotali = reader.GetInt32(11),
                AggiornatoIl = DateTime.Parse(reader.GetString(12)),
            });
        }

        return items;
    }

    public List<int> GetAnniServiziDisponibili()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT DISTINCT CAST(substr(DataServizio, 1, 4) AS INTEGER) AS Anno
            FROM ServiziGiornalieri
            WHERE DataServizio IS NOT NULL
              AND length(DataServizio) >= 4
            ORDER BY Anno DESC;
            """;

        using var reader = command.ExecuteReader();
        var items = new List<int>();

        while (reader.Read())
        {
            items.Add(reader.GetInt32(0));
        }

        return items;
    }

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

    public ServizioGiornaliero? GetServizioGiornalieroById(long servizioGiornalieroId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT ServizioGiornalieroId,
                   DataServizio,
                   NumeroOrdineServizio,
                   OrarioServizio,
                   TipoServizio,
                   LocalitaOperativaId,
                   ScopoImmersioneId,
                   UnitaNavaleId,
                   FuoriSede,
                   AttivitaSvolta,
                   Note
            FROM ServiziGiornalieri
            WHERE ServizioGiornalieroId = $servizioGiornalieroId;
            """;
        command.Parameters.AddWithValue("$servizioGiornalieroId", servizioGiornalieroId);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        var servizio = new ServizioGiornaliero
        {
            ServizioGiornalieroId = reader.GetInt64(0),
            DataServizio = DateOnly.Parse(reader.GetString(1)),
            NumeroOrdineServizio = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
            OrarioServizio = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
            TipoServizio = reader.GetString(4),
            LocalitaOperativaId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
            ScopoImmersioneId = reader.IsDBNull(6) ? null : reader.GetInt32(6),
            UnitaNavaleId = reader.IsDBNull(7) ? null : reader.GetInt32(7),
            FuoriSede = reader.GetInt32(8) == 1,
            AttivitaSvolta = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
            Note = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
        };
        reader.Close();

        servizio.Partecipanti = GetServizioPartecipanti(connection, servizioGiornalieroId);
        servizio.Immersioni = GetServizioImmersioni(connection, servizioGiornalieroId);
        servizio.SupportiOccasionali = GetServizioSupportiOccasionali(connection, servizioGiornalieroId);
        var partecipazioniImmersione = GetServizioPartecipantiImmersioni(connection, servizioGiornalieroId);
        var immersioniById = servizio.Immersioni.ToDictionary(item => item.ServizioImmersioneId);
        var partecipantiById = servizio.Partecipanti.ToDictionary(item => item.ServizioPartecipanteId);

        foreach (var partecipazione in partecipazioniImmersione)
        {
            if (immersioniById.TryGetValue(partecipazione.ServizioImmersioneId, out var immersione))
            {
                immersione.Partecipazioni.Add(partecipazione);
            }

            if (partecipantiById.TryGetValue(partecipazione.ServizioPartecipanteId, out var partecipante))
            {
                partecipante.Immersioni.Add(partecipazione);
            }
        }

        return servizio;
    }

    public long SaveServizioGiornaliero(ServizioGiornaliero servizio)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        long servizioGiornalieroId;
        if (servizio.ServizioGiornalieroId == 0)
        {
            using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText =
                """
                INSERT INTO ServiziGiornalieri (
                    DataServizio,
                    NumeroOrdineServizio,
                    OrarioServizio,
                    TipoServizio,
                    LocalitaOperativaId,
                    ScopoImmersioneId,
                    UnitaNavaleId,
                    FuoriSede,
                    AttivitaSvolta,
                    Note
                )
                VALUES (
                    $dataServizio,
                    $numeroOrdineServizio,
                    $orarioServizio,
                    $tipoServizio,
                    $localitaOperativaId,
                    $scopoImmersioneId,
                    $unitaNavaleId,
                    $fuoriSede,
                    $attivitaSvolta,
                    $note
                );
                SELECT last_insert_rowid();
                """;
            AddServizioParameters(insert, servizio);
            servizioGiornalieroId = Convert.ToInt64(insert.ExecuteScalar());
        }
        else
        {
            using var update = connection.CreateCommand();
            update.Transaction = transaction;
            update.CommandText =
                """
                UPDATE ServiziGiornalieri
                SET DataServizio = $dataServizio,
                    NumeroOrdineServizio = $numeroOrdineServizio,
                    OrarioServizio = $orarioServizio,
                    TipoServizio = $tipoServizio,
                    LocalitaOperativaId = $localitaOperativaId,
                    ScopoImmersioneId = $scopoImmersioneId,
                    UnitaNavaleId = $unitaNavaleId,
                    FuoriSede = $fuoriSede,
                    AttivitaSvolta = $attivitaSvolta,
                    Note = $note,
                    AggiornatoIl = CURRENT_TIMESTAMP
                WHERE ServizioGiornalieroId = $servizioGiornalieroId;
                """;
            AddServizioParameters(update, servizio);
            update.Parameters.AddWithValue("$servizioGiornalieroId", servizio.ServizioGiornalieroId);

            if (update.ExecuteNonQuery() == 0)
            {
                throw new InvalidOperationException("Servizio giornaliero non trovato.");
            }

            servizioGiornalieroId = servizio.ServizioGiornalieroId;
            DeleteServizioChildRows(connection, transaction, servizioGiornalieroId);
        }

        var partecipantiMap = InsertServizioPartecipanti(connection, transaction, servizioGiornalieroId, servizio.Partecipanti);
        var immersioniMap = InsertServizioImmersioni(
            connection,
            transaction,
            servizioGiornalieroId,
            servizio.Immersioni,
            servizio.LocalitaOperativaId,
            servizio.ScopoImmersioneId);
        InsertServizioPartecipantiImmersioni(connection, transaction, servizio.Immersioni, immersioniMap, partecipantiMap);
        InsertServizioSupportiOccasionali(connection, transaction, servizioGiornalieroId, servizio.SupportiOccasionali);

        transaction.Commit();
        return servizioGiornalieroId;
    }

    public void DeleteServizioGiornaliero(long servizioGiornalieroId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM ServiziGiornalieri WHERE ServizioGiornalieroId = $servizioGiornalieroId;";
        command.Parameters.AddWithValue("$servizioGiornalieroId", servizioGiornalieroId);

        if (command.ExecuteNonQuery() == 0)
        {
            throw new InvalidOperationException("Servizio giornaliero non trovato.");
        }
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
            clauses.Add(
                """
                (
                    p.Cognome LIKE $cognome
                    OR p.Nome LIKE $cognome
                    OR TRIM(p.Cognome || ' ' || p.Nome) LIKE $cognome
                    OR TRIM(p.Nome || ' ' || p.Cognome) LIKE $cognome
                )
                """);
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
            SELECT p.PerId,
                   p.Cognome,
                   p.Nome,
                   p.Qualifica,
                   p.ProfiloPersonale,
                   p.RuoloSanitario,
                   p.CodiceFiscale,
                   p.MatricolaPersonale,
                   p.NumeroBrevettoSmz,
                   p.DataNascita,
                   p.LuogoNascita,
                   p.ViaResidenza,
                   p.CapResidenza,
                   p.CittaResidenza,
                   p.Telefono1,
                   p.Telefono2,
                   p.Mail1Utente,
                   p.Mail2Utente
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
            SELECT PerId,
                   Cognome,
                   Nome,
                   Qualifica,
                   ProfiloPersonale,
                   RuoloSanitario,
                   CodiceFiscale,
                   MatricolaPersonale,
                   NumeroBrevettoSmz,
                   DataNascita,
                   LuogoNascita,
                   ViaResidenza,
                   CapResidenza,
                   CittaResidenza,
                   Telefono1,
                   Telefono2,
                   Mail1Utente,
                   Mail2Utente
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

    public PersonaleArchivio? GetArchivioById(long archiveId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT PersonaleArchivioId,
                   PerIdOriginale,
                   Cognome,
                   Nome,
                   Qualifica,
                   ProfiloPersonale,
                   RuoloSanitario,
                   CodiceFiscale,
                   MatricolaPersonale,
                   NumeroBrevettoSmz,
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

        var personale = new PersonaleArchivio
        {
            PersonaleArchivioId = reader.GetInt64(0),
            PerIdOriginale = reader.GetInt32(1),
            Cognome = reader.GetString(2),
            Nome = reader.GetString(3),
            Qualifica = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
            ProfiloPersonale = reader.IsDBNull(5) ? "SMZ operativo" : reader.GetString(5),
            RuoloSanitario = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
            CodiceFiscale = reader.GetString(7),
            MatricolaPersonale = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
            NumeroBrevettoSmz = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
            DataNascita = ParseDbDate(reader, 10),
            LuogoNascita = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
            ViaResidenza = reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
            CapResidenza = reader.IsDBNull(13) ? string.Empty : reader.GetString(13),
            CittaResidenza = reader.IsDBNull(14) ? string.Empty : reader.GetString(14),
            Telefono1 = reader.IsDBNull(15) ? string.Empty : reader.GetString(15),
            Telefono2 = reader.IsDBNull(16) ? string.Empty : reader.GetString(16),
            Mail1Utente = reader.IsDBNull(17) ? string.Empty : reader.GetString(17),
            Mail2Utente = reader.IsDBNull(18) ? string.Empty : reader.GetString(18),
            DataArchiviazione = DateTime.Parse(reader.GetString(19)),
        };
        reader.Close();

        personale.Abilitazioni = GetAbilitazioniArchivio(connection, archiveId);
        personale.VisiteMediche = GetVisiteArchivio(connection, archiveId);
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
                INSERT INTO Personale (
                    PerId,
                    Cognome,
                    Nome,
                    Qualifica,
                    ProfiloPersonale,
                    RuoloSanitario,
                    CodiceFiscale,
                    MatricolaPersonale,
                    NumeroBrevettoSmz,
                    DataNascita,
                    LuogoNascita,
                    ViaResidenza,
                    CapResidenza,
                    CittaResidenza,
                    Telefono1,
                    Telefono2,
                    Mail1Utente,
                    Mail2Utente)
                VALUES (
                    $perId,
                    $cognome,
                    $nome,
                    $qualifica,
                    $profiloPersonale,
                    $ruoloSanitario,
                    $codiceFiscale,
                    $matricolaPersonale,
                    $numeroBrevettoSmz,
                    $dataNascita,
                    $luogoNascita,
                    $viaResidenza,
                    $capResidenza,
                    $cittaResidenza,
                    $telefono1,
                    $telefono2,
                    $mail1Utente,
                    $mail2Utente);
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
                    Qualifica = $qualifica,
                    ProfiloPersonale = $profiloPersonale,
                    RuoloSanitario = $ruoloSanitario,
                    CodiceFiscale = $codiceFiscale,
                    MatricolaPersonale = $matricolaPersonale,
                    NumeroBrevettoSmz = $numeroBrevettoSmz,
                    DataNascita = $dataNascita,
                    LuogoNascita = $luogoNascita,
                    ViaResidenza = $viaResidenza,
                    CapResidenza = $capResidenza,
                    CittaResidenza = $cittaResidenza,
                    Telefono1 = $telefono1,
                    Telefono2 = $telefono2,
                    Mail1Utente = $mail1Utente,
                    Mail2Utente = $mail2Utente
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

    public long DeletePersonale(int perId)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        var archivedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        var archiveId = ArchivePersonale(connection, transaction, perId, archivedAt);
        ArchiveAbilitazioni(connection, transaction, archiveId, perId);
        ArchiveVisite(connection, transaction, archiveId, perId);

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "DELETE FROM Personale WHERE PerId = $perId;";
        command.Parameters.AddWithValue("$perId", perId);

        if (command.ExecuteNonQuery() == 0)
        {
            throw new InvalidOperationException($"Scheda con PerID {perId} non trovata.");
        }

        transaction.Commit();
        return archiveId;
    }

    public int RestorePersonaleArchivio(long archiveId)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        var archived = GetArchivioById(connection, transaction, archiveId);
        if (archived is null)
        {
            throw new InvalidOperationException("Scheda archiviata non trovata.");
        }

        if (ExistsActiveCodiceFiscale(connection, transaction, archived.CodiceFiscale))
        {
            throw new InvalidOperationException(
                $"Esiste gia una scheda attiva con codice fiscale {archived.CodiceFiscale}. Ripristino bloccato.");
        }

        var perIdDaRipristinare = ExistsActivePerId(connection, transaction, archived.PerIdOriginale)
            ? GetNextAvailablePerId(connection, transaction)
            : archived.PerIdOriginale;

        using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText =
            """
            INSERT INTO Personale (
                PerId,
                Cognome,
                Nome,
                Qualifica,
                ProfiloPersonale,
                RuoloSanitario,
                CodiceFiscale,
                MatricolaPersonale,
                NumeroBrevettoSmz,
                DataNascita,
                LuogoNascita,
                ViaResidenza,
                CapResidenza,
                CittaResidenza,
                Telefono1,
                Telefono2,
                Mail1Utente,
                Mail2Utente
            )
            VALUES (
                $perId,
                $cognome,
                $nome,
                $qualifica,
                $profiloPersonale,
                $ruoloSanitario,
                $codiceFiscale,
                $matricolaPersonale,
                $numeroBrevettoSmz,
                $dataNascita,
                $luogoNascita,
                $viaResidenza,
                $capResidenza,
                $cittaResidenza,
                $telefono1,
                $telefono2,
                $mail1Utente,
                $mail2Utente
            );
            """;
        insert.Parameters.AddWithValue("$perId", perIdDaRipristinare);
        insert.Parameters.AddWithValue("$cognome", archived.Cognome);
        insert.Parameters.AddWithValue("$nome", archived.Nome);
        insert.Parameters.AddWithValue("$qualifica", DbText(archived.Qualifica));
        insert.Parameters.AddWithValue("$profiloPersonale", archived.ProfiloPersonale.Trim());
        insert.Parameters.AddWithValue("$ruoloSanitario", DbText(archived.RuoloSanitario));
        insert.Parameters.AddWithValue("$codiceFiscale", archived.CodiceFiscale);
        insert.Parameters.AddWithValue("$matricolaPersonale", DbText(archived.MatricolaPersonale));
        insert.Parameters.AddWithValue("$numeroBrevettoSmz", DbText(archived.NumeroBrevettoSmz));
        insert.Parameters.AddWithValue("$dataNascita", ToDbValue(archived.DataNascita));
        insert.Parameters.AddWithValue("$luogoNascita", DbText(archived.LuogoNascita));
        insert.Parameters.AddWithValue("$viaResidenza", DbText(archived.ViaResidenza));
        insert.Parameters.AddWithValue("$capResidenza", DbText(archived.CapResidenza));
        insert.Parameters.AddWithValue("$cittaResidenza", DbText(archived.CittaResidenza));
        insert.Parameters.AddWithValue("$telefono1", DbText(archived.Telefono1));
        insert.Parameters.AddWithValue("$telefono2", DbText(archived.Telefono2));
        insert.Parameters.AddWithValue("$mail1Utente", DbText(archived.Mail1Utente));
        insert.Parameters.AddWithValue("$mail2Utente", DbText(archived.Mail2Utente));
        insert.ExecuteNonQuery();

        RestoreAbilitazioniArchivio(connection, transaction, archiveId, perIdDaRipristinare);
        RestoreVisiteArchivio(connection, transaction, archiveId, perIdDaRipristinare);

        DeleteArchivio(connection, transaction, archiveId);
        transaction.Commit();

        return perIdDaRipristinare;
    }

    public void DeletePersonaleArchivio(long archiveId)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        DeleteArchivio(connection, transaction, archiveId);
        transaction.Commit();
    }

    private static void AddPersonaleParameters(SqliteCommand command, Personale personale)
    {
        command.Parameters.AddWithValue("$cognome", personale.Cognome.Trim());
        command.Parameters.AddWithValue("$nome", personale.Nome.Trim());
        command.Parameters.AddWithValue("$qualifica", DbText(personale.Qualifica));
        command.Parameters.AddWithValue("$profiloPersonale", personale.ProfiloPersonale.Trim());
        command.Parameters.AddWithValue("$ruoloSanitario", DbText(personale.RuoloSanitario));
        command.Parameters.AddWithValue("$codiceFiscale", personale.CodiceFiscale.Trim().ToUpperInvariant());
        command.Parameters.AddWithValue("$matricolaPersonale", DbText(personale.MatricolaPersonale));
        command.Parameters.AddWithValue("$numeroBrevettoSmz", DbText(personale.NumeroBrevettoSmz));
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
        command.Parameters.AddWithValue("$tipoServizio", servizio.TipoServizio.Trim());
        command.Parameters.AddWithValue("$localitaOperativaId", servizio.LocalitaOperativaId is null ? DBNull.Value : servizio.LocalitaOperativaId.Value);
        command.Parameters.AddWithValue("$scopoImmersioneId", servizio.ScopoImmersioneId is null ? DBNull.Value : servizio.ScopoImmersioneId.Value);
        command.Parameters.AddWithValue("$unitaNavaleId", servizio.UnitaNavaleId is null ? DBNull.Value : servizio.UnitaNavaleId.Value);
        command.Parameters.AddWithValue("$fuoriSede", servizio.FuoriSede ? 1 : 0);
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
                ProfiloPersonale,
                RuoloSanitario,
                CodiceFiscale,
                MatricolaPersonale,
                NumeroBrevettoSmz,
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
                   ProfiloPersonale,
                   RuoloSanitario,
                   CodiceFiscale,
                   MatricolaPersonale,
                   NumeroBrevettoSmz,
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

    private static List<PersonaleAbilitazione> GetAbilitazioniArchivio(SqliteConnection connection, long archiveId)
    {
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

    private static List<CategoriaRegistroItem> GetCategorieRegistro(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT CategoriaRegistroId, Descrizione, Attiva, Ordine
            FROM CategorieRegistro
            ORDER BY Ordine, Descrizione;
            """;

        using var reader = command.ExecuteReader();
        var items = new List<CategoriaRegistroItem>();

        while (reader.Read())
        {
            items.Add(new CategoriaRegistroItem
            {
                CategoriaRegistroId = reader.GetInt32(0),
                Descrizione = reader.GetString(1),
                Attiva = reader.GetInt32(2) == 1,
                Ordine = reader.GetInt32(3),
            });
        }

        return items;
    }

    private static List<LocalitaOperativa> GetLocalitaOperative(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT LocalitaOperativaId, Descrizione, Provincia, Attiva, Ordine
            FROM LocalitaOperative
            ORDER BY Ordine, Descrizione;
            """;

        using var reader = command.ExecuteReader();
        var items = new List<LocalitaOperativa>();

        while (reader.Read())
        {
            items.Add(new LocalitaOperativa
            {
                LocalitaOperativaId = reader.GetInt32(0),
                Descrizione = reader.GetString(1),
                Provincia = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Attiva = reader.GetInt32(3) == 1,
                Ordine = reader.GetInt32(4),
            });
        }

        return items;
    }

    private static List<ScopoImmersioneItem> GetScopiImmersione(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT ScopoImmersioneId, Descrizione, CategoriaRegistroId, Attiva, Ordine
            FROM ScopiImmersione
            ORDER BY Ordine, Descrizione;
            """;

        using var reader = command.ExecuteReader();
        var items = new List<ScopoImmersioneItem>();

        while (reader.Read())
        {
            items.Add(new ScopoImmersioneItem
            {
                ScopoImmersioneId = reader.GetInt32(0),
                Descrizione = reader.GetString(1),
                CategoriaRegistroId = reader.GetInt32(2),
                Attiva = reader.GetInt32(3) == 1,
                Ordine = reader.GetInt32(4),
            });
        }

        return items;
    }

    private static List<UnitaNavale> GetUnitaNavali(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT UnitaNavaleId, Descrizione, Sigla, Attiva, Ordine
            FROM UnitaNavali
            ORDER BY Ordine, Descrizione;
            """;

        using var reader = command.ExecuteReader();
        var items = new List<UnitaNavale>();

        while (reader.Read())
        {
            items.Add(new UnitaNavale
            {
                UnitaNavaleId = reader.GetInt32(0),
                Descrizione = reader.GetString(1),
                Sigla = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Attiva = reader.GetInt32(3) == 1,
                Ordine = reader.GetInt32(4),
            });
        }

        return items;
    }

    private static List<TipologiaImmersioneOperativa> GetTipologieImmersioneOperative(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT TipologiaImmersioneOperativaId, Codice, Descrizione, Attiva, Ordine
            FROM TipologieImmersioneOperative
            ORDER BY Ordine, Descrizione;
            """;

        using var reader = command.ExecuteReader();
        var items = new List<TipologiaImmersioneOperativa>();

        while (reader.Read())
        {
            items.Add(new TipologiaImmersioneOperativa
            {
                TipologiaImmersioneOperativaId = reader.GetInt32(0),
                Codice = reader.GetString(1),
                Descrizione = reader.GetString(2),
                Attiva = reader.GetInt32(3) == 1,
                Ordine = reader.GetInt32(4),
            });
        }

        return items;
    }

    private static List<FasciaProfondita> GetFasceProfondita(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT FasciaProfonditaId, Descrizione, MetriDa, MetriA, Attiva, Ordine
            FROM FasceProfondita
            ORDER BY Ordine, Descrizione;
            """;

        using var reader = command.ExecuteReader();
        var items = new List<FasciaProfondita>();

        while (reader.Read())
        {
            items.Add(new FasciaProfondita
            {
                FasciaProfonditaId = reader.GetInt32(0),
                Descrizione = reader.GetString(1),
                MetriDa = reader.GetInt32(2),
                MetriA = reader.GetInt32(3),
                Attiva = reader.GetInt32(4) == 1,
                Ordine = reader.GetInt32(5),
            });
        }

        return items;
    }

    private static List<CategoriaContabileOre> GetCategorieContabiliOre(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT CategoriaContabileOreId, Codice, Descrizione, Attiva, Ordine
            FROM CategorieContabiliOre
            ORDER BY Ordine, Descrizione;
            """;

        using var reader = command.ExecuteReader();
        var items = new List<CategoriaContabileOre>();

        while (reader.Read())
        {
            items.Add(new CategoriaContabileOre
            {
                CategoriaContabileOreId = reader.GetInt32(0),
                Codice = reader.GetString(1),
                Descrizione = reader.GetString(2),
                Attiva = reader.GetInt32(3) == 1,
                Ordine = reader.GetInt32(4),
            });
        }

        return items;
    }

    private static List<GruppoOperativo> GetGruppiOperativi(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT GruppoOperativoId, Codice, Descrizione, Attiva, Ordine
            FROM GruppiOperativi
            ORDER BY Ordine, Descrizione;
            """;

        using var reader = command.ExecuteReader();
        var items = new List<GruppoOperativo>();

        while (reader.Read())
        {
            items.Add(new GruppoOperativo
            {
                GruppoOperativoId = reader.GetInt32(0),
                Codice = reader.GetString(1),
                Descrizione = reader.GetString(2),
                Attiva = reader.GetInt32(3) == 1,
                Ordine = reader.GetInt32(4),
            });
        }

        return items;
    }

    private static List<RegolaContabileImmersione> GetRegoleContabiliImmersione(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT RegolaContabileImmersioneId,
                   TipologiaImmersioneOperativaId,
                   FasciaProfonditaId,
                   CategoriaContabileOreId,
                   Tariffa,
                   DataInizioValidita,
                   DataFineValidita,
                   Attiva
            FROM RegoleContabiliImmersione
            ORDER BY RegolaContabileImmersioneId;
            """;

        using var reader = command.ExecuteReader();
        var items = new List<RegolaContabileImmersione>();

        while (reader.Read())
        {
            items.Add(new RegolaContabileImmersione
            {
                RegolaContabileImmersioneId = reader.GetInt32(0),
                TipologiaImmersioneOperativaId = reader.GetInt32(1),
                FasciaProfonditaId = reader.GetInt32(2),
                CategoriaContabileOreId = reader.GetInt32(3),
                Tariffa = reader.GetDecimal(4),
                DataInizioValidita = ParseDbDate(reader, 5),
                DataFineValidita = ParseDbDate(reader, 6),
                Attiva = reader.GetInt32(7) == 1,
            });
        }

        return items;
    }

    private static List<RuoloOperativo> GetRuoliOperativi(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT RuoloOperativoId, Codice, Descrizione, Attiva, Ordine
            FROM RuoliOperativi
            ORDER BY Ordine, Descrizione;
            """;

        using var reader = command.ExecuteReader();
        var items = new List<RuoloOperativo>();

        while (reader.Read())
        {
            items.Add(new RuoloOperativo
            {
                RuoloOperativoId = reader.GetInt32(0),
                Codice = reader.GetString(1),
                Descrizione = reader.GetString(2),
                Attiva = reader.GetInt32(3) == 1,
                Ordine = reader.GetInt32(4),
            });
        }

        return items;
    }

    private static List<ContabilitaSanitarioSummary> GetContabilitaSanitari(
        SqliteConnection connection,
        DateOnly dataInizio,
        DateOnly dataFine)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT p.PerId,
                   p.Cognome,
                   p.Nome,
                   COALESCE(p.Qualifica, '') AS Qualifica,
                   COALESCE(p.RuoloSanitario, '') AS RuoloSanitario,
                   COUNT(DISTINCT s.DataServizio) AS GiornateImpiego,
                   MAX(s.DataServizio) AS UltimaDataServizio
            FROM ServizioPartecipanti sp
            INNER JOIN ServiziGiornalieri s ON s.ServizioGiornalieroId = sp.ServizioGiornalieroId
            INNER JOIN Personale p ON p.PerId = sp.PerId
            WHERE sp.Presente = 1
              AND p.ProfiloPersonale = 'Sanitario'
              AND s.DataServizio >= $dataInizio
              AND s.DataServizio <= $dataFine
            GROUP BY p.PerId, p.Cognome, p.Nome, p.Qualifica, p.RuoloSanitario
            ORDER BY p.Cognome, p.Nome;
            """;
        command.Parameters.AddWithValue("$dataInizio", dataInizio.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("$dataFine", dataFine.ToString("yyyy-MM-dd"));

        using var reader = command.ExecuteReader();
        var items = new List<ContabilitaSanitarioSummary>();

        while (reader.Read())
        {
            items.Add(new ContabilitaSanitarioSummary
            {
                PerId = reader.GetInt32(0),
                Cognome = reader.GetString(1),
                Nome = reader.GetString(2),
                Qualifica = reader.GetString(3),
                RuoloSanitario = reader.GetString(4),
                GiornateImpiego = reader.GetInt32(5),
                UltimaDataServizio = ParseDbDate(reader, 6),
            });
        }

        return items;
    }

    private static List<ContabilitaSmzSummary> GetContabilitaSmzImmersioni(
        SqliteConnection connection,
        DateOnly dataInizio,
        DateOnly dataFine)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT p.PerId,
                   s.DataServizio,
                   COALESCE(s.NumeroOrdineServizio, '') AS NumeroOrdineServizio,
                   p.Cognome,
                   p.Nome,
                   COALESCE(p.Qualifica, '') AS Qualifica,
                   tio.Descrizione AS Apparato,
                   fp.Descrizione AS FasciaProfondita,
                   MAX(COALESCE(rci.Tariffa, 0)) AS Tariffa,
                   SUM(CASE WHEN cco.Codice = 'ORD' THEN COALESCE(spi.OreImmersione, 0) ELSE 0 END) AS OreOrd,
                   SUM(CASE WHEN cco.Codice = 'ADD' THEN COALESCE(spi.OreImmersione, 0) ELSE 0 END) AS OreAdd,
                   SUM(CASE WHEN cco.Codice = 'SPER' THEN COALESCE(spi.OreImmersione, 0) ELSE 0 END) AS OreSper,
                   SUM(CASE WHEN cco.Codice = 'CI' THEN COALESCE(spi.OreImmersione, 0) ELSE 0 END) AS OreCi,
                   SUM(
                       CASE cco.Codice
                           WHEN 'ADD' THEN COALESCE(rci.Tariffa, 0) * COALESCE(spi.OreImmersione, 0) / 2.0
                           WHEN 'SPER' THEN (COALESCE(rci.Tariffa, 0) + COALESCE(rci.Tariffa, 0) * 0.25) * COALESCE(spi.OreImmersione, 0)
                           WHEN 'CI' THEN COALESCE(rci.Tariffa, 0) * COALESCE(spi.OreImmersione, 0) * 0.8
                           ELSE COALESCE(rci.Tariffa, 0) * COALESCE(spi.OreImmersione, 0)
                       END
                   ) AS Importo
            FROM ServizioPartecipantiImmersioni spi
            INNER JOIN ServizioPartecipanti sp ON sp.ServizioPartecipanteId = spi.ServizioPartecipanteId
            INNER JOIN ServiziGiornalieri s ON s.ServizioGiornalieroId = sp.ServizioGiornalieroId
            INNER JOIN Personale p ON p.PerId = sp.PerId
            INNER JOIN TipologieImmersioneOperative tio ON tio.TipologiaImmersioneOperativaId = spi.TipologiaImmersioneOperativaId
            INNER JOIN FasceProfondita fp ON fp.FasciaProfonditaId = spi.FasciaProfonditaId
            INNER JOIN CategorieContabiliOre cco ON cco.CategoriaContabileOreId = spi.CategoriaContabileOreId
            LEFT JOIN RegoleContabiliImmersione rci
                   ON rci.TipologiaImmersioneOperativaId = spi.TipologiaImmersioneOperativaId
                  AND rci.FasciaProfonditaId = spi.FasciaProfonditaId
                  AND rci.CategoriaContabileOreId = spi.CategoriaContabileOreId
                  AND rci.Attiva = 1
            WHERE s.DataServizio >= $dataInizio
              AND s.DataServizio <= $dataFine
              AND p.ProfiloPersonale = 'SMZ operativo'
            GROUP BY p.PerId, s.DataServizio, s.NumeroOrdineServizio, p.Cognome, p.Nome, p.Qualifica, tio.Descrizione, fp.Descrizione
            ORDER BY s.DataServizio, COALESCE(s.NumeroOrdineServizio, ''), p.Cognome, p.Nome, tio.Ordine, fp.Ordine;
            """;
        command.Parameters.AddWithValue("$dataInizio", dataInizio.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("$dataFine", dataFine.ToString("yyyy-MM-dd"));

        using var reader = command.ExecuteReader();
        var items = new List<ContabilitaSmzSummary>();

        while (reader.Read())
        {
            items.Add(new ContabilitaSmzSummary
            {
                PerId = reader.GetInt32(0),
                DataServizio = DateOnly.Parse(reader.GetString(1)),
                NumeroOrdineServizio = reader.GetString(2),
                Cognome = reader.GetString(3),
                Nome = reader.GetString(4),
                Qualifica = reader.GetString(5),
                Apparato = reader.GetString(6),
                FasciaProfondita = reader.GetString(7),
                Tariffa = Convert.ToDecimal(reader.GetValue(8)),
                OreOrd = Convert.ToDecimal(reader.GetValue(9)),
                OreAdd = Convert.ToDecimal(reader.GetValue(10)),
                OreSper = Convert.ToDecimal(reader.GetValue(11)),
                OreCi = Convert.ToDecimal(reader.GetValue(12)),
                Importo = Convert.ToDecimal(reader.GetValue(13)),
            });
        }

        return items;
    }

    private static List<ContabilitaSupportoSummary> GetContabilitaSupportiOccasionali(
        SqliteConnection connection,
        DateOnly dataInizio,
        DateOnly dataFine)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT TRIM(so.Nominativo) AS Nominativo,
                   MAX(COALESCE(TRIM(so.Qualifica), '')) AS Qualifica,
                   MAX(COALESCE(TRIM(so.Ruolo), '')) AS Ruolo,
                   COUNT(DISTINCT s.DataServizio) AS GiornateImpiego,
                   MAX(s.DataServizio) AS UltimaDataServizio
            FROM ServizioSupportiOccasionali so
            INNER JOIN ServiziGiornalieri s ON s.ServizioGiornalieroId = so.ServizioGiornalieroId
            WHERE so.Presente = 1
              AND TRIM(COALESCE(so.Nominativo, '')) <> ''
              AND s.DataServizio >= $dataInizio
              AND s.DataServizio <= $dataFine
            GROUP BY UPPER(TRIM(so.Nominativo))
            ORDER BY UPPER(TRIM(so.Nominativo));
            """;
        command.Parameters.AddWithValue("$dataInizio", dataInizio.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("$dataFine", dataFine.ToString("yyyy-MM-dd"));

        using var reader = command.ExecuteReader();
        var items = new List<ContabilitaSupportoSummary>();

        while (reader.Read())
        {
            items.Add(new ContabilitaSupportoSummary
            {
                Nominativo = reader.GetString(0),
                Qualifica = reader.GetString(1),
                Ruolo = reader.GetString(2),
                GiornateImpiego = reader.GetInt32(3),
                UltimaDataServizio = ParseDbDate(reader, 4),
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
            Qualifica = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
            ProfiloPersonale = reader.IsDBNull(4) ? "SMZ operativo" : reader.GetString(4),
            RuoloSanitario = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
            CodiceFiscale = reader.GetString(6),
            MatricolaPersonale = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
            NumeroBrevettoSmz = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
            DataNascita = ParseDbDate(reader, 9),
            LuogoNascita = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
            ViaResidenza = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
            CapResidenza = reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
            CittaResidenza = reader.IsDBNull(13) ? string.Empty : reader.GetString(13),
            Telefono1 = reader.IsDBNull(14) ? string.Empty : reader.GetString(14),
            Telefono2 = reader.IsDBNull(15) ? string.Empty : reader.GetString(15),
            Mail1Utente = reader.IsDBNull(16) ? string.Empty : reader.GetString(16),
            Mail2Utente = reader.IsDBNull(17) ? string.Empty : reader.GetString(17),
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
                   ProfiloPersonale,
                   RuoloSanitario,
                   CodiceFiscale,
                   MatricolaPersonale,
                   NumeroBrevettoSmz,
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
            ProfiloPersonale = reader.IsDBNull(5) ? "SMZ operativo" : reader.GetString(5),
            RuoloSanitario = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
            CodiceFiscale = reader.GetString(7),
            MatricolaPersonale = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
            NumeroBrevettoSmz = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
            DataNascita = ParseDbDate(reader, 10),
            LuogoNascita = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
            ViaResidenza = reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
            CapResidenza = reader.IsDBNull(13) ? string.Empty : reader.GetString(13),
            CittaResidenza = reader.IsDBNull(14) ? string.Empty : reader.GetString(14),
            Telefono1 = reader.IsDBNull(15) ? string.Empty : reader.GetString(15),
            Telefono2 = reader.IsDBNull(16) ? string.Empty : reader.GetString(16),
            Mail1Utente = reader.IsDBNull(17) ? string.Empty : reader.GetString(17),
            Mail2Utente = reader.IsDBNull(18) ? string.Empty : reader.GetString(18),
            DataArchiviazione = DateTime.Parse(reader.GetString(19)),
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

    private static void DeleteServizioChildRows(SqliteConnection connection, SqliteTransaction transaction, long servizioGiornalieroId)
    {
        using var deleteSupporti = connection.CreateCommand();
        deleteSupporti.Transaction = transaction;
        deleteSupporti.CommandText = "DELETE FROM ServizioSupportiOccasionali WHERE ServizioGiornalieroId = $servizioGiornalieroId;";
        deleteSupporti.Parameters.AddWithValue("$servizioGiornalieroId", servizioGiornalieroId);
        deleteSupporti.ExecuteNonQuery();

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
