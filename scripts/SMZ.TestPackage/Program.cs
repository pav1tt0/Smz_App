using Microsoft.Data.Sqlite;
using SMZ.Conta.App.Data;
using SMZ.Conta.App.Models;

if (args.Length != 2 || args[0] is not ("create" or "verify"))
{
    Console.Error.WriteLine("Uso: SMZ.TestPackage create|verify <cartella dati>");
    return 1;
}

var dataDirectory = Path.GetFullPath(args[1]);
Environment.SetEnvironmentVariable(DatabasePaths.AppDataDirectoryEnvironmentVariable, dataDirectory);
try
{
    if (args[0] == "create")
    {
        if (Directory.Exists(dataDirectory) && Directory.EnumerateFileSystemEntries(dataDirectory).Any())
        {
            throw new InvalidOperationException("La cartella dati iniziali deve essere vuota. Nessun database esistente viene riutilizzato.");
        }

        DatabaseInitializer.EnsureDatabase();
        new PersonaleRepository().SavePersonale(new Personale
        {
            PerId = 12345,
            Nome = "Mario",
            Cognome = "Rossi",
            CodiceFiscale = "TEST12345",
            ProfiloPersonale = ProfiliPersonaleCatalogo.OperatoreSubacqueo,
            StatoServizio = StatoServizioPersonaleCatalogo.Attivo,
        }, isNewRecord: true);
    }

    using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
    {
        DataSource = DatabasePaths.DatabasePath,
        Mode = SqliteOpenMode.ReadOnly,
        Pooling = false,
    }.ToString());
    connection.Open();
    using var command = connection.CreateCommand();
    command.CommandText = "PRAGMA integrity_check;";
    if (!string.Equals(command.ExecuteScalar()?.ToString(), "ok", StringComparison.Ordinal))
        throw new InvalidOperationException("Integrita SQLite non valida.");
    command.CommandText = "PRAGMA foreign_key_check;";
    using (var reader = command.ExecuteReader())
    {
        if (reader.Read()) throw new InvalidOperationException("Riferimenti SQLite non validi.");
    }

    var catalogTables = new HashSet<string>(StringComparer.Ordinal)
    {
        "TipiAbilitazione", "CategorieRegistro", "LocalitaOperative", "ScopiImmersione", "UnitaNavali",
        "TipologieImmersioneOperative", "FasceProfondita", "CategorieContabiliOre", "GruppiOperativi",
        "RuoliOperativi", "RegoleContabiliImmersione",
    };
    command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name;";
    var tables = new List<string>();
    using (var reader = command.ExecuteReader())
    {
        while (reader.Read()) tables.Add(reader.GetString(0));
    }

    foreach (var table in tables)
    {
        command.CommandText = $"SELECT COUNT(*) FROM \"{table.Replace("\"", "\"\"")}\";";
        var count = Convert.ToInt32(command.ExecuteScalar());
        if (table == "Personale" ? count != 1 : catalogTables.Contains(table) ? count == 0 : count != 0)
            throw new InvalidOperationException($"Contenuto inatteso nella tabella {table}: {count} righe.");
        Console.WriteLine($"{table}: {count}");
    }

    command.CommandText = "SELECT COUNT(*) FROM Personale WHERE PerId = 12345 AND Nome = 'Mario' AND Cognome = 'Rossi' AND CodiceFiscale = 'TEST12345';";
    if (Convert.ToInt32(command.ExecuteScalar()) != 1)
        throw new InvalidOperationException("Il solo dipendente previsto e Mario Rossi, PerID 12345.");

    Console.WriteLine("OK: solo Mario Rossi 12345, cataloghi standard e nessun altro dato operativo o account.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}
finally
{
    SqliteConnection.ClearAllPools();
    Environment.SetEnvironmentVariable(DatabasePaths.AppDataDirectoryEnvironmentVariable, null);
}
