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
        var tipiAttivi = CatalogoAbilitazioni.Tutte
            .Select(item => item.TipoAbilitazioneId)
            .ToHashSet();

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
            var tipoAbilitazioneId = reader.GetInt32(0);
            if (!tipiAttivi.Contains(tipoAbilitazioneId))
            {
                continue;
            }

            items.Add(CatalogoAbilitazioni.ApplicaSuggerimenti(new TipoAbilitazione
            {
                TipoAbilitazioneId = tipoAbilitazioneId,
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
}
