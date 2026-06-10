using Microsoft.Data.Sqlite;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.Data;

public sealed partial class PersonaleRepository
{
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

    public LocalitaOperativa AddLocalitaOperativa(string descrizione)
    {
        if (string.IsNullOrWhiteSpace(descrizione))
        {
            throw new InvalidOperationException("Inserisci una localita valida.");
        }

        var descrizionePulita = descrizione.Trim();

        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        using (var existing = connection.CreateCommand())
        {
            existing.Transaction = transaction;
            existing.CommandText =
                """
                SELECT LocalitaOperativaId, Descrizione, Provincia, Attiva, Ordine
                FROM LocalitaOperative
                WHERE UPPER(TRIM(Descrizione)) = UPPER($descrizione)
                LIMIT 1;
                """;
            existing.Parameters.AddWithValue("$descrizione", descrizionePulita);

            using var reader = existing.ExecuteReader();
            if (reader.Read())
            {
                return new LocalitaOperativa
                {
                    LocalitaOperativaId = reader.GetInt32(0),
                    Descrizione = reader.GetString(1),
                    Provincia = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    Attiva = reader.GetInt32(3) == 1,
                    Ordine = reader.GetInt32(4),
                };
            }
        }

        var nextId = GetNextIntegerId(connection, transaction, "LocalitaOperative", "LocalitaOperativaId");
        var nextOrder = GetNextIntegerId(connection, transaction, "LocalitaOperative", "Ordine");

        using (var insert = connection.CreateCommand())
        {
            insert.Transaction = transaction;
            insert.CommandText =
                """
                INSERT INTO LocalitaOperative (LocalitaOperativaId, Descrizione, Provincia, Attiva, Ordine)
                VALUES ($id, $descrizione, NULL, 1, $ordine);
                """;
            insert.Parameters.AddWithValue("$id", nextId);
            insert.Parameters.AddWithValue("$descrizione", descrizionePulita);
            insert.Parameters.AddWithValue("$ordine", nextOrder);
            insert.ExecuteNonQuery();
        }

        transaction.Commit();

        return new LocalitaOperativa
        {
            LocalitaOperativaId = nextId,
            Descrizione = descrizionePulita,
            Provincia = string.Empty,
            Attiva = true,
            Ordine = nextOrder,
        };
    }

    public UnitaNavale AddUnitaNavale(string descrizione)
    {
        if (string.IsNullOrWhiteSpace(descrizione))
        {
            throw new InvalidOperationException("Inserisci una targa o una descrizione valida per il mezzo nautico.");
        }

        var descrizionePulita = descrizione.Trim();

        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        using (var existing = connection.CreateCommand())
        {
            existing.Transaction = transaction;
            existing.CommandText =
                """
                SELECT UnitaNavaleId, Descrizione, Sigla, Attiva, Ordine
                FROM UnitaNavali
                WHERE UPPER(TRIM(Descrizione)) = UPPER($descrizione)
                LIMIT 1;
                """;
            existing.Parameters.AddWithValue("$descrizione", descrizionePulita);

            using var reader = existing.ExecuteReader();
            if (reader.Read())
            {
                return new UnitaNavale
                {
                    UnitaNavaleId = reader.GetInt32(0),
                    Descrizione = reader.GetString(1),
                    Sigla = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    Attiva = reader.GetInt32(3) == 1,
                    Ordine = reader.GetInt32(4),
                };
            }
        }

        var nextId = GetNextIntegerId(connection, transaction, "UnitaNavali", "UnitaNavaleId");
        var nextOrder = GetNextIntegerId(connection, transaction, "UnitaNavali", "Ordine");

        using (var insert = connection.CreateCommand())
        {
            insert.Transaction = transaction;
            insert.CommandText =
                """
                INSERT INTO UnitaNavali (UnitaNavaleId, Descrizione, Sigla, Attiva, Ordine)
                VALUES ($id, $descrizione, NULL, 1, $ordine);
                """;
            insert.Parameters.AddWithValue("$id", nextId);
            insert.Parameters.AddWithValue("$descrizione", descrizionePulita);
            insert.Parameters.AddWithValue("$ordine", nextOrder);
            insert.ExecuteNonQuery();
        }

        transaction.Commit();

        return new UnitaNavale
        {
            UnitaNavaleId = nextId,
            Descrizione = descrizionePulita,
            Sigla = string.Empty,
            Attiva = true,
            Ordine = nextOrder,
        };
    }

    public void UpdateLocalitaOperative(IEnumerable<LocalitaOperativa> items)
    {
        var localita = items
            .Where(item => item.LocalitaOperativaId > 0)
            .Select(item => new LocalitaOperativa
            {
                LocalitaOperativaId = item.LocalitaOperativaId,
                Descrizione = item.Descrizione.Trim(),
                Provincia = item.Provincia.Trim(),
                Attiva = item.Attiva,
                Ordine = item.Ordine,
            })
            .ToList();

        if (localita.Any(item => string.IsNullOrWhiteSpace(item.Descrizione)))
        {
            throw new InvalidOperationException("Ogni localita deve avere una descrizione.");
        }

        var duplicata = localita
            .GroupBy(item => item.Descrizione, StringComparer.CurrentCultureIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicata is not null)
        {
            throw new InvalidOperationException($"Localita duplicata: {duplicata.Key}.");
        }

        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        foreach (var item in localita)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                """
                UPDATE LocalitaOperative
                SET Descrizione = $descrizione,
                    Provincia = $provincia,
                    Attiva = $attiva,
                    Ordine = $ordine
                WHERE LocalitaOperativaId = $id;
                """;
            command.Parameters.AddWithValue("$id", item.LocalitaOperativaId);
            command.Parameters.AddWithValue("$descrizione", item.Descrizione);
            command.Parameters.AddWithValue("$provincia", string.IsNullOrWhiteSpace(item.Provincia) ? DBNull.Value : item.Provincia);
            command.Parameters.AddWithValue("$attiva", item.Attiva ? 1 : 0);
            command.Parameters.AddWithValue("$ordine", item.Ordine);
            command.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public void UpdateUnitaNavali(IEnumerable<UnitaNavale> items)
    {
        var unitaNavali = items
            .Where(item => item.UnitaNavaleId > 0)
            .Select(item => new UnitaNavale
            {
                UnitaNavaleId = item.UnitaNavaleId,
                Descrizione = item.Descrizione.Trim(),
                Sigla = item.Sigla.Trim(),
                Attiva = item.Attiva,
                Ordine = item.Ordine,
            })
            .ToList();

        if (unitaNavali.Any(item => string.IsNullOrWhiteSpace(item.Descrizione)))
        {
            throw new InvalidOperationException("Ogni mezzo deve avere una descrizione.");
        }

        var duplicata = unitaNavali
            .GroupBy(item => item.Descrizione, StringComparer.CurrentCultureIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicata is not null)
        {
            throw new InvalidOperationException($"Mezzo duplicato: {duplicata.Key}.");
        }

        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        foreach (var item in unitaNavali)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                """
                UPDATE UnitaNavali
                SET Descrizione = $descrizione,
                    Sigla = $sigla,
                    Attiva = $attiva,
                    Ordine = $ordine
                WHERE UnitaNavaleId = $id;
                """;
            command.Parameters.AddWithValue("$id", item.UnitaNavaleId);
            command.Parameters.AddWithValue("$descrizione", item.Descrizione);
            command.Parameters.AddWithValue("$sigla", string.IsNullOrWhiteSpace(item.Sigla) ? DBNull.Value : item.Sigla);
            command.Parameters.AddWithValue("$attiva", item.Attiva ? 1 : 0);
            command.Parameters.AddWithValue("$ordine", item.Ordine);
            command.ExecuteNonQuery();
        }

        transaction.Commit();
    }
}
