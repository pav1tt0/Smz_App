using Microsoft.Data.Sqlite;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.Data;

public sealed partial class PersonaleRepository
{
    public List<ServizioGiornalieroSummary> GetServiziGiornalieriRecenti(
        int maxItems = 10,
        string searchText = "",
        string numeroServizio = "",
        string dataServizio = "",
        string dataInizio = "",
        string dataFine = "")
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
                   s.IndennitaOrdinePubblico,
                   (
                       SELECT COUNT(1)
                       FROM ServizioPartecipanti sp
                       WHERE sp.ServizioGiornalieroId = s.ServizioGiornalieroId
                   ) + (
                       SELECT COUNT(1)
                       FROM ServizioOperatoriSubEsterni soe
                       WHERE soe.ServizioGiornalieroId = s.ServizioGiornalieroId
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
                       FROM ServizioOperatoriSubEsterni soe
                       WHERE soe.ServizioGiornalieroId = s.ServizioGiornalieroId
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
                   COALESCE((
                       SELECT GROUP_CONCAT(Descrizione, ', ')
                       FROM (
                           SELECT DISTINCT tio.Descrizione AS Descrizione
                           FROM ServizioImmersioni si
                           INNER JOIN ServizioPartecipantiImmersioni spi
                               ON spi.ServizioImmersioneId = si.ServizioImmersioneId
                           INNER JOIN TipologieImmersioneOperative tio
                               ON tio.TipologiaImmersioneOperativaId = spi.TipologiaImmersioneOperativaId
                           WHERE si.ServizioGiornalieroId = s.ServizioGiornalieroId
                           UNION
                           SELECT DISTINCT tio.Descrizione AS Descrizione
                           FROM ServizioImmersioni si
                           INNER JOIN ServizioOperatoriSubEsterniImmersioni sei
                               ON sei.ServizioImmersioneId = si.ServizioImmersioneId
                           INNER JOIN TipologieImmersioneOperative tio
                               ON tio.TipologiaImmersioneOperativaId = sei.TipologiaImmersioneOperativaId
                           WHERE si.ServizioGiornalieroId = s.ServizioGiornalieroId
                           ORDER BY Descrizione
                       )
                   ), '') AS ApparatiDescrizione,
                   (
                       SELECT MAX(ProfonditaMetri)
                       FROM (
                           SELECT spi.ProfonditaMetri
                           FROM ServizioImmersioni si
                           INNER JOIN ServizioPartecipantiImmersioni spi
                               ON spi.ServizioImmersioneId = si.ServizioImmersioneId
                           WHERE si.ServizioGiornalieroId = s.ServizioGiornalieroId
                           UNION ALL
                           SELECT sei.ProfonditaMetri
                           FROM ServizioImmersioni si
                           INNER JOIN ServizioOperatoriSubEsterniImmersioni sei
                               ON sei.ServizioImmersioneId = si.ServizioImmersioneId
                           WHERE si.ServizioGiornalieroId = s.ServizioGiornalieroId
                       )
                   ) AS ProfonditaMassimaMetri,
                   COALESCE((
                       SELECT SUM(OreImmersione)
                       FROM (
                           SELECT spi.OreImmersione
                           FROM ServizioImmersioni si
                           INNER JOIN ServizioPartecipantiImmersioni spi
                               ON spi.ServizioImmersioneId = si.ServizioImmersioneId
                           WHERE si.ServizioGiornalieroId = s.ServizioGiornalieroId
                           UNION ALL
                           SELECT sei.OreImmersione
                           FROM ServizioImmersioni si
                           INNER JOIN ServizioOperatoriSubEsterniImmersioni sei
                               ON sei.ServizioImmersioneId = si.ServizioImmersioneId
                           WHERE si.ServizioGiornalieroId = s.ServizioGiornalieroId
                       )
                   ), 0) AS OreImmersioneTotali,
                   COALESCE((
                       SELECT GROUP_CONCAT(Descrizione, ', ')
                       FROM (
                           SELECT DISTINCT cco.Descrizione AS Descrizione
                           FROM ServizioImmersioni si
                           INNER JOIN ServizioPartecipantiImmersioni spi
                               ON spi.ServizioImmersioneId = si.ServizioImmersioneId
                           INNER JOIN CategorieContabiliOre cco
                               ON cco.CategoriaContabileOreId = spi.CategoriaContabileOreId
                           WHERE si.ServizioGiornalieroId = s.ServizioGiornalieroId
                           UNION
                           SELECT DISTINCT cco.Descrizione AS Descrizione
                           FROM ServizioImmersioni si
                           INNER JOIN ServizioOperatoriSubEsterniImmersioni sei
                               ON sei.ServizioImmersioneId = si.ServizioImmersioneId
                           INNER JOIN CategorieContabiliOre cco
                               ON cco.CategoriaContabileOreId = sei.CategoriaContabileOreId
                           WHERE si.ServizioGiornalieroId = s.ServizioGiornalieroId
                           ORDER BY Descrizione
                       )
                   ), '') AS CategorieOreDescrizione,
                   s.AggiornatoIl
            FROM ServiziGiornalieri s
            LEFT JOIN LocalitaOperative lo ON lo.LocalitaOperativaId = s.LocalitaOperativaId
            LEFT JOIN ScopiImmersione sc ON sc.ScopoImmersioneId = s.ScopoImmersioneId
            LEFT JOIN UnitaNavali unv ON unv.UnitaNavaleId = s.UnitaNavaleId
            WHERE ($search = '' OR COALESCE(lo.Descrizione, '') LIKE $searchLike)
              AND ($numeroServizio = '' OR COALESCE(s.NumeroOrdineServizio, '') LIKE $numeroServizioLike)
              AND ($dataServizio = '' OR s.DataServizio = $dataServizio)
              AND ($dataInizio = '' OR s.DataServizio >= $dataInizio)
              AND ($dataFine = '' OR s.DataServizio <= $dataFine)
            ORDER BY s.DataServizio DESC, s.ServizioGiornalieroId DESC
            LIMIT $maxItems;
            """;
        var search = searchText.Trim();
        var numero = numeroServizio.Trim();
        var data = dataServizio.Trim();
        var inizio = dataInizio.Trim();
        var fine = dataFine.Trim();
        command.Parameters.AddWithValue("$maxItems", maxItems);
        command.Parameters.AddWithValue("$search", search);
        command.Parameters.AddWithValue("$searchLike", $"%{search}%");
        command.Parameters.AddWithValue("$numeroServizio", numero);
        command.Parameters.AddWithValue("$numeroServizioLike", $"%{numero}%");
        command.Parameters.AddWithValue("$dataServizio", data);
        command.Parameters.AddWithValue("$dataInizio", inizio);
        command.Parameters.AddWithValue("$dataFine", fine);

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
                IndennitaOrdinePubblico = reader.GetInt32(9) == 1,
                PartecipantiTotali = reader.GetInt32(10),
                PresentiTotali = reader.GetInt32(11),
                ImmersioniTotali = reader.GetInt32(12),
                ApparatiDescrizione = reader.GetString(13),
                ProfonditaMassimaMetri = reader.IsDBNull(14) ? null : reader.GetInt32(14),
                OreImmersioneTotali = reader.GetDecimal(15),
                CategorieOreDescrizione = reader.GetString(16),
                AggiornatoIl = DateTime.Parse(reader.GetString(17)),
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
}
