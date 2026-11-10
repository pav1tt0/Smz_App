using System.Text.Json;
using Microsoft.Data.Sqlite;
using SMZ.Conta.App.Data;
using SMZ.Conta.App.Models;

try
{
// Build-machine-only fixture generator. This tool and its data are never packaged.
if (args.Length != 2) return 1;
if (args[0] == "fingerprint")
{
    using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path.GetFullPath(args[1]), Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
    connection.Open();
    using var command = connection.CreateCommand();
    command.CommandText = "SELECT name FROM sqlite_schema WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name;";
    var tables = new List<string>();
    using (var reader = command.ExecuteReader()) while (reader.Read()) tables.Add(reader.GetString(0));
    var text = new System.Text.StringBuilder();
    foreach (var table in tables)
    {
        text.AppendLine(table);
        command.CommandText = $"SELECT * FROM \"{table.Replace("\"", "\"\"")}\" ORDER BY rowid;";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var values = new object[reader.FieldCount];
            reader.GetValues(values);
            text.AppendLine(JsonSerializer.Serialize(values));
        }
    }
    Console.WriteLine(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text.ToString()))));
    return 0;
}
if (args[0] != "create") return 1;
var root = Path.GetFullPath(args[1]);
if (Directory.Exists(root) && Directory.EnumerateFileSystemEntries(root).Any())
    throw new InvalidOperationException("La cartella di collaudo deve essere vuota.");
Environment.SetEnvironmentVariable(DatabasePaths.AppDataDirectoryEnvironmentVariable, root);
DatabaseInitializer.EnsureDatabase();
var repository = new PersonaleRepository();
repository.SavePersonale(new Personale
{
    PerId = 87654, Cognome = "COLLAUDO FITTIZIO", Nome = "Installazione", CodiceFiscale = "SOLO-COLLAUDO-87654",
    Abilitazioni = [new() { TipoAbilitazioneId = CatalogoAbilitazioni.Tutte.First().TipoAbilitazioneId, Note = "Dato da preservare" }],
}, true);
repository.SaveServizioGiornaliero(new ServizioGiornaliero
{
    DataServizio = new DateOnly(2026, 11, 10), NumeroOrdineServizio = "COLLAUDO-INSTALLAZIONE",
    Partecipanti = [new() { PerId = 87654, Presente = true, GruppoOperativoId = repository.GetCataloghiServizio().GruppiOperativi.First().GruppoOperativoId }],
});
new AccessService().CreateFirstAdministrator(87654, "SoloCollaudo!2026");
Directory.CreateDirectory(DatabasePaths.ExportDirectory);
File.WriteAllText(Path.Combine(DatabasePaths.ExportDirectory, "documento-collaudo.txt"), "Export da preservare");
using (var connection = new SqliteConnection(DatabasePaths.ConnectionString))
{
    connection.Open();
    using var command = connection.CreateCommand();
    command.CommandText = "UPDATE LocalitaOperative SET Descrizione='Catalogo personalizzato di collaudo' WHERE LocalitaOperativaId=(SELECT MIN(LocalitaOperativaId) FROM LocalitaOperative);";
    command.ExecuteNonQuery();
}
var backup = new BackupService().CreateLocalBackup("collaudo-distro-operativa");
SqliteConnection.ClearAllPools();
File.WriteAllText(Path.Combine(root, "fixture.json"), JsonSerializer.Serialize(new { DatabasePath = DatabasePaths.DatabasePath, BackupPath = backup.BackupPath }));
Console.WriteLine("Fixture di collaudo creata fuori dal pacchetto.");
return 0;

}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}
