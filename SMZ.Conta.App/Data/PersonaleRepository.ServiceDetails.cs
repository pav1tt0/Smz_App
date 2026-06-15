using Microsoft.Data.Sqlite;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.Data;

public sealed partial class PersonaleRepository
{
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
                   StraordinarioAttivo,
                   StraordinarioInizio,
                   StraordinarioFine,
                   TipoServizio,
                   LocalitaOperativaId,
                   ScopoImmersioneId,
                   UnitaNavaleId,
                   ResponsabileServizioPerId,
                   FuoriSede,
                   IndennitaOrdinePubblico,
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
            StraordinarioAttivo = reader.GetInt32(4) == 1,
            StraordinarioInizio = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
            StraordinarioFine = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
            TipoServizio = reader.GetString(7),
            LocalitaOperativaId = reader.IsDBNull(8) ? null : reader.GetInt32(8),
            ScopoImmersioneId = reader.IsDBNull(9) ? null : reader.GetInt32(9),
            UnitaNavaleId = reader.IsDBNull(10) ? null : reader.GetInt32(10),
            ResponsabileServizioPerId = reader.IsDBNull(11) ? null : reader.GetInt32(11),
            FuoriSede = reader.GetInt32(12) == 1,
            IndennitaOrdinePubblico = reader.GetInt32(13) == 1,
            AttivitaSvolta = reader.IsDBNull(14) ? string.Empty : reader.GetString(14),
            Note = reader.IsDBNull(15) ? string.Empty : reader.GetString(15),
        };
        reader.Close();

        servizio.Partecipanti = GetServizioPartecipanti(connection, servizioGiornalieroId);
        servizio.Immersioni = GetServizioImmersioni(connection, servizioGiornalieroId);
        servizio.OperatoriSubEsterni = GetServizioOperatoriSubEsterni(connection, servizioGiornalieroId);
        servizio.SupportiOccasionali = GetServizioSupportiOccasionali(connection, servizioGiornalieroId);
        var partecipazioniImmersione = GetServizioPartecipantiImmersioni(connection, servizioGiornalieroId);
        var partecipazioniEsterneImmersione = GetServizioOperatoriSubEsterniImmersioni(connection, servizioGiornalieroId);
        var immersioniById = servizio.Immersioni.ToDictionary(item => item.ServizioImmersioneId);
        var partecipantiById = servizio.Partecipanti.ToDictionary(item => item.ServizioPartecipanteId);
        var operatoriEsterniById = servizio.OperatoriSubEsterni.ToDictionary(item => item.ServizioOperatoreSubEsternoId);

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

        foreach (var partecipazione in partecipazioniEsterneImmersione)
        {
            if (immersioniById.TryGetValue(partecipazione.ServizioImmersioneId, out var immersione))
            {
                immersione.PartecipazioniEsterne.Add(partecipazione);
            }

            if (operatoriEsterniById.TryGetValue(partecipazione.ServizioOperatoreSubEsternoId, out var operatoreEsterno))
            {
                operatoreEsterno.Immersioni.Add(partecipazione);
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
                    StraordinarioAttivo,
                    StraordinarioInizio,
                    StraordinarioFine,
                    TipoServizio,
                    LocalitaOperativaId,
                    ScopoImmersioneId,
                    UnitaNavaleId,
                    ResponsabileServizioPerId,
                    FuoriSede,
                    IndennitaOrdinePubblico,
                    AttivitaSvolta,
                    Note
                )
                VALUES (
                    $dataServizio,
                    $numeroOrdineServizio,
                    $orarioServizio,
                    $straordinarioAttivo,
                    $straordinarioInizio,
                    $straordinarioFine,
                    $tipoServizio,
                    $localitaOperativaId,
                    $scopoImmersioneId,
                    $unitaNavaleId,
                    $responsabileServizioPerId,
                    $fuoriSede,
                    $indennitaOrdinePubblico,
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
                    StraordinarioAttivo = $straordinarioAttivo,
                    StraordinarioInizio = $straordinarioInizio,
                    StraordinarioFine = $straordinarioFine,
                    TipoServizio = $tipoServizio,
                    LocalitaOperativaId = $localitaOperativaId,
                    ScopoImmersioneId = $scopoImmersioneId,
                    UnitaNavaleId = $unitaNavaleId,
                    ResponsabileServizioPerId = $responsabileServizioPerId,
                    FuoriSede = $fuoriSede,
                    IndennitaOrdinePubblico = $indennitaOrdinePubblico,
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
        var operatoriEsterniMap = InsertServizioOperatoriSubEsterni(connection, transaction, servizioGiornalieroId, servizio.OperatoriSubEsterni);
        var immersioniMap = InsertServizioImmersioni(
            connection,
            transaction,
            servizioGiornalieroId,
            servizio.Immersioni,
            servizio.LocalitaOperativaId,
            servizio.ScopoImmersioneId);
        InsertServizioPartecipantiImmersioni(connection, transaction, servizio.Immersioni, immersioniMap, partecipantiMap);
        InsertServizioOperatoriSubEsterniImmersioni(connection, transaction, servizio.Immersioni, immersioniMap, operatoriEsterniMap);
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
}
