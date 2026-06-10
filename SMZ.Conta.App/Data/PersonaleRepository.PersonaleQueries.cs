using Microsoft.Data.Sqlite;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.Data;

public sealed partial class PersonaleRepository
{
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
                   p.DataDecorrenzaQualifica,
                   p.ProfiloPersonale,
                   p.RuoloSanitario,
                   p.CodiceFiscale,
                   p.MatricolaPersonale,
                   p.NumeroBrevettoSmz,
                   p.StatoServizio,
                   p.DataFineServizio,
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
        personale.Attagliamento = GetAttagliamento(connection, perId);
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

        var personale = new PersonaleArchivio
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
        reader.Close();

        personale.Abilitazioni = GetAbilitazioniArchivio(connection, archiveId);
        personale.VisiteMediche = GetVisiteArchivio(connection, archiveId);
        personale.Attagliamento = GetAttagliamentoArchivio(connection, archiveId);
        return personale;
    }
}
