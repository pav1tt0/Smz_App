using System.Globalization;
using SMZ.Conta.App.Models;

namespace SMZ.Conta.App.Data;

public sealed partial class PersonaleRepository
{
    public IReadOnlyList<EventoCalendario> GetEventiCalendario(DateTime dal, DateTime al)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT EventoId, Titolo, Categoria, Data, Ora, Note, Completata
            FROM EventiCalendario WHERE Data >= $dal AND Data <= $al
            ORDER BY Data, CASE WHEN Ora IS NULL THEN 1 ELSE 0 END, Ora, Titolo, EventoId;
            """;
        command.Parameters.AddWithValue("$dal", dal.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$al", al.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        using var reader = command.ExecuteReader();
        var result = new List<EventoCalendario>();
        while (reader.Read())
            result.Add(new EventoCalendario
            {
                EventoId = reader.GetInt64(0), Titolo = reader.GetString(1), Categoria = reader.GetString(2),
                Data = DateTime.ParseExact(reader.GetString(3), "yyyy-MM-dd", CultureInfo.InvariantCulture),
                Ora = reader.IsDBNull(4) ? null : TimeSpan.ParseExact(reader.GetString(4), @"hh\:mm", CultureInfo.InvariantCulture),
                Note = reader.GetString(5), Completata = reader.GetInt32(6) == 1,
            });
        return result;
    }

    public long SaveEventoCalendario(EventoCalendario evento)
    {
        if (string.IsNullOrWhiteSpace(evento.Titolo)) throw new InvalidOperationException("Inserisci il titolo dell'evento.");
        if (evento.Categoria is not ("Appuntamento" or "Scadenza")) throw new InvalidOperationException("Seleziona una categoria valida.");
        if (evento.Ora is { } ora && (ora < TimeSpan.Zero || ora >= TimeSpan.FromDays(1) || ora.Ticks % TimeSpan.TicksPerMinute != 0))
            throw new InvalidOperationException("Inserisci un orario valido nel formato HH:mm.");
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = evento.EventoId == 0
            ? """
              INSERT INTO EventiCalendario (Titolo, Categoria, Data, Ora, Note, Completata)
              VALUES ($titolo, $categoria, $data, $ora, $note, $completata) RETURNING EventoId;
              """
            : """
              UPDATE EventiCalendario SET Titolo=$titolo, Categoria=$categoria, Data=$data,
                  Ora=$ora, Note=$note, Completata=$completata WHERE EventoId=$id RETURNING EventoId;
              """;
        if (evento.EventoId != 0) command.Parameters.AddWithValue("$id", evento.EventoId);
        command.Parameters.AddWithValue("$titolo", evento.Titolo.Trim());
        command.Parameters.AddWithValue("$categoria", evento.Categoria);
        command.Parameters.AddWithValue("$data", evento.Data.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$ora", (object?)evento.Ora?.ToString(@"hh\:mm") ?? DBNull.Value);
        command.Parameters.AddWithValue("$note", evento.Note);
        command.Parameters.AddWithValue("$completata", evento.Categoria == "Scadenza" && evento.Completata ? 1 : 0);
        var id = command.ExecuteScalar();
        return id is null ? throw new InvalidOperationException("L'evento non è più disponibile. Aggiorna il calendario.") : Convert.ToInt64(id);
    }

    public void DeleteEventoCalendario(long id)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM EventiCalendario WHERE EventoId=$id;";
        command.Parameters.AddWithValue("$id", id);
        if (command.ExecuteNonQuery() == 0) throw new InvalidOperationException("L'evento non è più disponibile. Aggiorna il calendario.");
    }
}
