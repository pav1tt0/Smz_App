using Microsoft.Data.Sqlite;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.Data;

public sealed partial class PersonaleRepository
{
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
            SELECT TipologiaImmersioneOperativaId,
                   Codice,
                   Descrizione,
                   ProfonditaMinimaMetri,
                   ProfonditaMassimaMetri,
                   Attiva,
                   Ordine
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
                ProfonditaMinimaMetri = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                ProfonditaMassimaMetri = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                Attiva = reader.GetInt32(5) == 1,
                Ordine = reader.GetInt32(6),
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
}
