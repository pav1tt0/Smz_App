using Microsoft.Data.Sqlite;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.Data;

public sealed partial class PersonaleRepository
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
                   DataArchiviazione,
                   COALESCE(Qualifica, ''), DataDecorrenzaQualifica,
                   ProfiloPersonale, COALESCE(RuoloSanitario, '')
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
                Qualifica = reader.GetString(6),
                DataDecorrenzaQualifica = reader.IsDBNull(7) ? null : DateOnly.Parse(reader.GetString(7)),
                ProfiloPersonale = reader.GetString(8),
                RuoloSanitario = reader.GetString(9),
            });
        }

        return items.OrderBy(item => QualificaFormatter.GetChiaveOrdinamento(item.Qualifica,
                item.DataDecorrenzaQualifica, ProfiliPersonaleCatalogo.IsSanitario(item.ProfiloPersonale), item.RuoloSanitario))
            .ThenBy(item => item.Cognome).ThenBy(item => item.Nome).ToList();
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

    public List<TipoAbilitazione> GetTipiAbilitazione(bool includeInactive = false)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT TipoAbilitazioneId, Codice, Descrizione, Categoria, RichiedeLivello, RichiedeScadenza, RichiedeProfondita, Attiva, Ordine
            FROM TipiAbilitazione
            WHERE $includeInactive = 1 OR Attiva = 1
            ORDER BY Ordine, Descrizione;
            """;
        command.Parameters.AddWithValue("$includeInactive", includeInactive ? 1 : 0);

        using var reader = command.ExecuteReader();
        var items = new List<TipoAbilitazione>();

        while (reader.Read())
        {
            var tipoAbilitazioneId = reader.GetInt32(0);
            items.Add(CatalogoAbilitazioni.ApplicaSuggerimenti(new TipoAbilitazione
            {
                TipoAbilitazioneId = tipoAbilitazioneId,
                Codice = reader.GetString(1),
                Descrizione = reader.GetString(2),
                Categoria = reader.GetString(3),
                RichiedeLivello = reader.GetInt32(4) == 1,
                RichiedeScadenza = reader.GetInt32(5) == 1,
                RichiedeProfondita = reader.GetInt32(6) == 1,
                Attiva = reader.GetInt32(7) == 1,
                Ordine = reader.GetInt32(8),
            }));
        }

        return items;
    }
}
