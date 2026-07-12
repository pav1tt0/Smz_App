using Microsoft.Data.Sqlite;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.Data;

public sealed partial class PersonaleRepository
{
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
            SELECT PerId,
                   DataServizio,
                   NumeroOrdineServizio,
                   Cognome,
                   Nome,
                   Qualifica,
                   Apparato,
                   FasciaProfondita,
                   MAX(Tariffa) AS Tariffa,
                   SUM(OreOrd) AS OreOrd,
                   SUM(OreAdd) AS OreAdd,
                   SUM(OreSper) AS OreSper,
                   SUM(OreCi) AS OreCi,
                   SUM(Importo) AS Importo
            FROM (
                SELECT p.PerId,
                       s.DataServizio,
                       COALESCE(s.NumeroOrdineServizio, '') AS NumeroOrdineServizio,
                       p.Cognome,
                       p.Nome,
                       COALESCE(p.Qualifica, '') AS Qualifica,
                       tio.Descrizione AS Apparato,
                       fp.Descrizione AS FasciaProfondita,
                       COALESCE(rci.Tariffa, 0) AS Tariffa,
                       CASE WHEN cco.Codice = 'ORD' THEN COALESCE(spi.OreImmersione, 0) ELSE 0 END AS OreOrd,
                       CASE WHEN cco.Codice = 'ADD' THEN COALESCE(spi.OreImmersione, 0) ELSE 0 END AS OreAdd,
                       CASE WHEN cco.Codice = 'SPER' THEN COALESCE(spi.OreImmersione, 0) ELSE 0 END AS OreSper,
                       CASE WHEN cco.Codice = 'CI' THEN COALESCE(spi.OreImmersione, 0) ELSE 0 END AS OreCi,
                       CASE cco.Codice
                           WHEN 'ADD' THEN COALESCE(rci.Tariffa, 0) * COALESCE(spi.OreImmersione, 0) / 2.0
                           WHEN 'SPER' THEN (COALESCE(rci.Tariffa, 0) + COALESCE(rci.Tariffa, 0) * 0.25) * COALESCE(spi.OreImmersione, 0)
                           WHEN 'CI' THEN COALESCE(rci.Tariffa, 0) * COALESCE(spi.OreImmersione, 0) * 0.8
                           ELSE COALESCE(rci.Tariffa, 0) * COALESCE(spi.OreImmersione, 0)
                       END AS Importo,
                       tio.Ordine AS ApparatoOrdine,
                       fp.Ordine AS FasciaOrdine
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
                  AND TRIM(COALESCE(p.ProfiloPersonale, '')) IN ('Operatore Subacqueo', 'SMZ operativo')

                UNION ALL

                SELECT soe.PerId,
                       s.DataServizio,
                       COALESCE(s.NumeroOrdineServizio, '') AS NumeroOrdineServizio,
                       soe.Nominativo AS Cognome,
                       soe.Reparto AS Nome,
                       COALESCE(soe.Qualifica, '') AS Qualifica,
                       tio.Descrizione AS Apparato,
                       fp.Descrizione AS FasciaProfondita,
                       COALESCE(rci.Tariffa, 0) AS Tariffa,
                       CASE WHEN cco.Codice = 'ORD' THEN COALESCE(sei.OreImmersione, 0) ELSE 0 END AS OreOrd,
                       CASE WHEN cco.Codice = 'ADD' THEN COALESCE(sei.OreImmersione, 0) ELSE 0 END AS OreAdd,
                       CASE WHEN cco.Codice = 'SPER' THEN COALESCE(sei.OreImmersione, 0) ELSE 0 END AS OreSper,
                       CASE WHEN cco.Codice = 'CI' THEN COALESCE(sei.OreImmersione, 0) ELSE 0 END AS OreCi,
                       CASE cco.Codice
                           WHEN 'ADD' THEN COALESCE(rci.Tariffa, 0) * COALESCE(sei.OreImmersione, 0) / 2.0
                           WHEN 'SPER' THEN (COALESCE(rci.Tariffa, 0) + COALESCE(rci.Tariffa, 0) * 0.25) * COALESCE(sei.OreImmersione, 0)
                           WHEN 'CI' THEN COALESCE(rci.Tariffa, 0) * COALESCE(sei.OreImmersione, 0) * 0.8
                           ELSE COALESCE(rci.Tariffa, 0) * COALESCE(sei.OreImmersione, 0)
                       END AS Importo,
                       tio.Ordine AS ApparatoOrdine,
                       fp.Ordine AS FasciaOrdine
                FROM ServizioOperatoriSubEsterniImmersioni sei
                INNER JOIN ServizioImmersioni si ON si.ServizioImmersioneId = sei.ServizioImmersioneId
                INNER JOIN ServizioOperatoriSubEsterni soe ON soe.ServizioOperatoreSubEsternoId = sei.ServizioOperatoreSubEsternoId
                INNER JOIN ServiziGiornalieri s ON s.ServizioGiornalieroId = si.ServizioGiornalieroId
                INNER JOIN TipologieImmersioneOperative tio ON tio.TipologiaImmersioneOperativaId = sei.TipologiaImmersioneOperativaId
                INNER JOIN FasceProfondita fp ON fp.FasciaProfonditaId = sei.FasciaProfonditaId
                INNER JOIN CategorieContabiliOre cco ON cco.CategoriaContabileOreId = sei.CategoriaContabileOreId
                LEFT JOIN RegoleContabiliImmersione rci
                       ON rci.TipologiaImmersioneOperativaId = sei.TipologiaImmersioneOperativaId
                      AND rci.FasciaProfonditaId = sei.FasciaProfonditaId
                      AND rci.CategoriaContabileOreId = sei.CategoriaContabileOreId
                      AND rci.Attiva = 1
                WHERE s.DataServizio >= $dataInizio
                  AND s.DataServizio <= $dataFine
            )
            GROUP BY PerId, DataServizio, NumeroOrdineServizio, Cognome, Nome, Qualifica, Apparato, FasciaProfondita
            ORDER BY DataServizio, NumeroOrdineServizio, Cognome, Nome, MIN(ApparatoOrdine), MIN(FasciaOrdine);
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

    private static List<RegistroImmersioneRiga> GetRegistroImmersioniMensile(
        SqliteConnection connection,
        DateOnly dataInizio,
        DateOnly dataFine)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT *
            FROM (
                SELECT s.ServizioGiornalieroId,
                       si.ServizioImmersioneId,
                       s.DataServizio,
                       COALESCE(s.NumeroOrdineServizio, '') AS NumeroOrdineServizio,
                       si.NumeroImmersione,
                       COALESCE(loi.Descrizione, los.Descrizione, '') AS Localita,
                       COALESCE(sci.Descrizione, scs.Descrizione, '') AS ScopoImmersione,
                       COALESCE(cr.Descrizione, 'Altro') AS CategoriaRegistro,
                       p.PerId,
                       p.Cognome,
                       p.Nome,
                       COALESCE(p.Qualifica, '') AS Qualifica,
                       COALESCE(tio.Descrizione, '') AS Apparato,
                       spi.ProfonditaMetri,
                       COALESCE(fp.Descrizione, '') AS FasciaProfondita,
                       COALESCE(spi.OreImmersione, 0) AS OreImmersione,
                       si.OrarioInizio,
                       si.OrarioFine,
                       tio.Ordine AS ApparatoOrdine,
                       fp.Ordine AS FasciaOrdine
                FROM ServizioPartecipantiImmersioni spi
                INNER JOIN ServizioImmersioni si ON si.ServizioImmersioneId = spi.ServizioImmersioneId
                INNER JOIN ServizioPartecipanti sp ON sp.ServizioPartecipanteId = spi.ServizioPartecipanteId
                INNER JOIN ServiziGiornalieri s ON s.ServizioGiornalieroId = si.ServizioGiornalieroId
                INNER JOIN Personale p ON p.PerId = sp.PerId
                LEFT JOIN LocalitaOperative loi ON loi.LocalitaOperativaId = si.LocalitaOperativaId
                LEFT JOIN LocalitaOperative los ON los.LocalitaOperativaId = s.LocalitaOperativaId
                LEFT JOIN ScopiImmersione sci ON sci.ScopoImmersioneId = si.ScopoImmersioneId
                LEFT JOIN ScopiImmersione scs ON scs.ScopoImmersioneId = s.ScopoImmersioneId
                LEFT JOIN CategorieRegistro cr ON cr.CategoriaRegistroId = COALESCE(sci.CategoriaRegistroId, scs.CategoriaRegistroId)
                LEFT JOIN TipologieImmersioneOperative tio ON tio.TipologiaImmersioneOperativaId = spi.TipologiaImmersioneOperativaId
                LEFT JOIN FasceProfondita fp ON fp.FasciaProfonditaId = spi.FasciaProfonditaId
                WHERE s.DataServizio >= $dataInizio
                  AND s.DataServizio <= $dataFine
                  AND sp.Presente = 1
                  AND TRIM(COALESCE(p.ProfiloPersonale, '')) IN ('Operatore Subacqueo', 'SMZ operativo')

                UNION ALL

                SELECT s.ServizioGiornalieroId,
                       si.ServizioImmersioneId,
                       s.DataServizio,
                       COALESCE(s.NumeroOrdineServizio, '') AS NumeroOrdineServizio,
                       si.NumeroImmersione,
                       COALESCE(loi.Descrizione, los.Descrizione, '') AS Localita,
                       COALESCE(sci.Descrizione, scs.Descrizione, '') AS ScopoImmersione,
                       COALESCE(cr.Descrizione, 'Altro') AS CategoriaRegistro,
                       soe.PerId,
                       soe.Nominativo AS Cognome,
                       soe.Reparto AS Nome,
                       COALESCE(soe.Qualifica, '') AS Qualifica,
                       COALESCE(tio.Descrizione, '') AS Apparato,
                       sei.ProfonditaMetri,
                       COALESCE(fp.Descrizione, '') AS FasciaProfondita,
                       COALESCE(sei.OreImmersione, 0) AS OreImmersione,
                       si.OrarioInizio,
                       si.OrarioFine,
                       tio.Ordine AS ApparatoOrdine,
                       fp.Ordine AS FasciaOrdine
                FROM ServizioOperatoriSubEsterniImmersioni sei
                INNER JOIN ServizioImmersioni si ON si.ServizioImmersioneId = sei.ServizioImmersioneId
                INNER JOIN ServizioOperatoriSubEsterni soe ON soe.ServizioOperatoreSubEsternoId = sei.ServizioOperatoreSubEsternoId
                INNER JOIN ServiziGiornalieri s ON s.ServizioGiornalieroId = si.ServizioGiornalieroId
                LEFT JOIN LocalitaOperative loi ON loi.LocalitaOperativaId = si.LocalitaOperativaId
                LEFT JOIN LocalitaOperative los ON los.LocalitaOperativaId = s.LocalitaOperativaId
                LEFT JOIN ScopiImmersione sci ON sci.ScopoImmersioneId = si.ScopoImmersioneId
                LEFT JOIN ScopiImmersione scs ON scs.ScopoImmersioneId = s.ScopoImmersioneId
                LEFT JOIN CategorieRegistro cr ON cr.CategoriaRegistroId = COALESCE(sci.CategoriaRegistroId, scs.CategoriaRegistroId)
                LEFT JOIN TipologieImmersioneOperative tio ON tio.TipologiaImmersioneOperativaId = sei.TipologiaImmersioneOperativaId
                LEFT JOIN FasceProfondita fp ON fp.FasciaProfonditaId = sei.FasciaProfonditaId
                WHERE s.DataServizio >= $dataInizio
                  AND s.DataServizio <= $dataFine
            )
            ORDER BY DataServizio,
                     NumeroOrdineServizio,
                     NumeroImmersione,
                     Cognome,
                     Nome,
                     ApparatoOrdine,
                     FasciaOrdine;
            """;
        command.Parameters.AddWithValue("$dataInizio", dataInizio.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("$dataFine", dataFine.ToString("yyyy-MM-dd"));

        using var reader = command.ExecuteReader();
        var items = new List<RegistroImmersioneRiga>();

        while (reader.Read())
        {
            items.Add(new RegistroImmersioneRiga
            {
                ServizioGiornalieroId = reader.GetInt64(0),
                ServizioImmersioneId = reader.GetInt64(1),
                DataServizio = DateOnly.Parse(reader.GetString(2)),
                NumeroOrdineServizio = reader.GetString(3),
                NumeroImmersione = reader.GetInt32(4),
                Localita = reader.GetString(5),
                ScopoImmersione = reader.GetString(6),
                CategoriaRegistro = reader.GetString(7),
                PerId = reader.GetInt32(8),
                Cognome = reader.GetString(9),
                Nome = reader.GetString(10),
                Qualifica = reader.GetString(11),
                Apparato = reader.GetString(12),
                ProfonditaMetri = reader.IsDBNull(13) ? null : reader.GetInt32(13),
                FasciaProfondita = reader.GetString(14),
                OreImmersione = Convert.ToDecimal(reader.GetValue(15)),
                OrarioInizio = ParseDbTime(reader, 16),
                OrarioFine = ParseDbTime(reader, 17),
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

    private static List<IndennitaFuoriSedeSummary> GetIndennitaFuoriSede(
        SqliteConnection connection,
        DateOnly dataInizio,
        DateOnly dataFine)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT PerId,
                   Cognome,
                   Nome,
                   Qualifica,
                   GROUP_CONCAT(DataServizio, ';') AS DateServizio
            FROM (
                SELECT DISTINCT p.PerId,
                       p.Cognome,
                       p.Nome,
                       COALESCE(p.Qualifica, '') AS Qualifica,
                       s.DataServizio
                FROM ServizioPartecipanti sp
                INNER JOIN ServiziGiornalieri s ON s.ServizioGiornalieroId = sp.ServizioGiornalieroId
                INNER JOIN Personale p ON p.PerId = sp.PerId
                WHERE sp.Presente = 1
                  AND (s.FuoriSede = 1 OR s.TipoServizio = 'FuoriSede')
                  AND s.DataServizio >= $dataInizio
                  AND s.DataServizio <= $dataFine
                ORDER BY p.Cognome, p.Nome, s.DataServizio
            )
            GROUP BY PerId, Cognome, Nome, Qualifica
            ORDER BY Cognome, Nome;
            """;
        command.Parameters.AddWithValue("$dataInizio", dataInizio.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("$dataFine", dataFine.ToString("yyyy-MM-dd"));

        using var reader = command.ExecuteReader();
        var items = new List<IndennitaFuoriSedeSummary>();

        while (reader.Read())
        {
            items.Add(new IndennitaFuoriSedeSummary
            {
                PerId = reader.GetInt32(0),
                Cognome = reader.GetString(1),
                Nome = reader.GetString(2),
                Qualifica = reader.GetString(3),
                DateServizio = reader.IsDBNull(4)
                    ? []
                    : reader.GetString(4)
                        .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(DateOnly.Parse)
                        .ToList(),
            });
        }

        return items;
    }

    private static List<ReportPersonaleMensileRiga> GetReportPersonaleServizi(
        SqliteConnection connection,
        DateOnly dataInizio,
        DateOnly dataFine)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT s.ServizioGiornalieroId,
                   s.DataServizio,
                   COALESCE(s.NumeroOrdineServizio, '') AS NumeroOrdineServizio,
                   p.PerId,
                   COALESCE(p.Qualifica, '') AS Qualifica,
                   TRIM(p.Cognome || ' ' || p.Nome) AS Nominativo,
                   COALESCE(go.Descrizione, '') AS GruppoOperativo,
                   COALESCE(ro.Descrizione, '') AS RuoloOperativo,
                   COALESCE(lo.Descrizione, '') AS Localita,
                   COALESCE(sc.Descrizione, '') AS ScopoImmersione
            FROM ServizioPartecipanti sp
            INNER JOIN ServiziGiornalieri s ON s.ServizioGiornalieroId = sp.ServizioGiornalieroId
            INNER JOIN Personale p ON p.PerId = sp.PerId
            LEFT JOIN GruppiOperativi go ON go.GruppoOperativoId = sp.GruppoOperativoId
            LEFT JOIN RuoliOperativi ro ON ro.RuoloOperativoId = sp.RuoloOperativoId
            LEFT JOIN LocalitaOperative lo ON lo.LocalitaOperativaId = s.LocalitaOperativaId
            LEFT JOIN ScopiImmersione sc ON sc.ScopoImmersioneId = s.ScopoImmersioneId
            WHERE sp.Presente = 1
              AND s.DataServizio >= $dataInizio
              AND s.DataServizio <= $dataFine
              AND NOT EXISTS (
                  SELECT 1
                  FROM ServizioPartecipantiImmersioni spi
                  WHERE spi.ServizioPartecipanteId = sp.ServizioPartecipanteId
              )
            ORDER BY s.DataServizio, COALESCE(s.NumeroOrdineServizio, ''), p.Cognome, p.Nome;
            """;
        command.Parameters.AddWithValue("$dataInizio", dataInizio.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("$dataFine", dataFine.ToString("yyyy-MM-dd"));

        using var reader = command.ExecuteReader();
        var items = new List<ReportPersonaleMensileRiga>();

        while (reader.Read())
        {
            var gruppo = reader.GetString(6);
            var ruolo = reader.GetString(7);
            items.Add(new ReportPersonaleMensileRiga
            {
                ServizioGiornalieroId = reader.GetInt64(0),
                DataServizio = DateOnly.Parse(reader.GetString(1)),
                NumeroOrdineServizio = reader.GetString(2),
                PerId = reader.GetInt32(3),
                Qualifica = reader.GetString(4),
                Nominativo = reader.GetString(5),
                TipoRiga = "Servizio",
                GruppoRuolo = string.IsNullOrWhiteSpace(ruolo)
                    ? gruppo
                    : string.IsNullOrWhiteSpace(gruppo)
                        ? ruolo
                        : $"{gruppo} | {ruolo}",
                Localita = reader.GetString(8),
                ScopoImmersione = reader.GetString(9),
            });
        }

        return items;
    }

    private static List<ReportPersonaleMensileRiga> GetReportPersonaleSupporti(
        SqliteConnection connection,
        DateOnly dataInizio,
        DateOnly dataFine)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT s.ServizioGiornalieroId,
                   s.DataServizio,
                   COALESCE(s.NumeroOrdineServizio, '') AS NumeroOrdineServizio,
                   COALESCE(so.Qualifica, '') AS Qualifica,
                   TRIM(so.Nominativo) AS Nominativo,
                   COALESCE(so.Ruolo, '') AS Ruolo,
                   COALESCE(lo.Descrizione, '') AS Localita,
                   COALESCE(sc.Descrizione, '') AS ScopoImmersione
            FROM ServizioSupportiOccasionali so
            INNER JOIN ServiziGiornalieri s ON s.ServizioGiornalieroId = so.ServizioGiornalieroId
            LEFT JOIN LocalitaOperative lo ON lo.LocalitaOperativaId = s.LocalitaOperativaId
            LEFT JOIN ScopiImmersione sc ON sc.ScopoImmersioneId = s.ScopoImmersioneId
            WHERE so.Presente = 1
              AND TRIM(COALESCE(so.Nominativo, '')) <> ''
              AND s.DataServizio >= $dataInizio
              AND s.DataServizio <= $dataFine
            ORDER BY s.DataServizio, COALESCE(s.NumeroOrdineServizio, ''), UPPER(TRIM(so.Nominativo));
            """;
        command.Parameters.AddWithValue("$dataInizio", dataInizio.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("$dataFine", dataFine.ToString("yyyy-MM-dd"));

        using var reader = command.ExecuteReader();
        var items = new List<ReportPersonaleMensileRiga>();

        while (reader.Read())
        {
            items.Add(new ReportPersonaleMensileRiga
            {
                ServizioGiornalieroId = reader.GetInt64(0),
                DataServizio = DateOnly.Parse(reader.GetString(1)),
                NumeroOrdineServizio = reader.GetString(2),
                Qualifica = reader.GetString(3),
                Nominativo = reader.GetString(4),
                TipoRiga = "Supporto",
                GruppoRuolo = reader.GetString(5),
                Localita = reader.GetString(6),
                ScopoImmersione = reader.GetString(7),
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
}
