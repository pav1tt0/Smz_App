using System.IO;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace SMZ.Conta.App.Data;

/// <summary>Prepares user-selected operational data without initializing or seeding a database.</summary>
public static class OperationalDatabaseSetup
{
    public const string Command = "--prepare-operational-database";

    public static int RunCommand(string[] args)
    {
        if (args.Length != 4) return 2;
        var resultPath = Path.GetFullPath(args[3]);
        if (!Path.GetExtension(resultPath).Equals(".json", StringComparison.OrdinalIgnoreCase)
            || File.Exists(resultPath)) return 2;

        object result;
        var exitCode = 0;
        try
        {
            result = Prepare(args[1], args[2]);
        }
        catch (Exception ex)
        {
            result = new { Success = false, Error = ex.Message };
            exitCode = 1;
        }
        using var stream = new FileStream(resultPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        JsonSerializer.Serialize(stream, result, new JsonSerializerOptions { WriteIndented = true });
        return exitCode;
    }

    public static DatabasePreparationResult Prepare(string sourcePath, string destinationDirectory)
    {
        sourcePath = Path.GetFullPath(sourcePath);
        destinationDirectory = Path.GetFullPath(destinationDirectory);
        if (!File.Exists(sourcePath)) throw new FileNotFoundException("Database o backup selezionato non trovato.", sourcePath);
        var destinationPath = Path.Combine(destinationDirectory, "smz-conta.db");
        if (sourcePath.Equals(destinationPath, StringComparison.OrdinalIgnoreCase))
        {
            BackupService.ValidateDatabase(sourcePath);
            var backupPath = NewSafetyBackupPath(destinationDirectory);
            CopyConsistentDatabase(sourcePath, backupPath);
            return Describe(sourcePath, backupPath, imported: false);
        }

        if (Directory.Exists(destinationDirectory) && Directory.EnumerateFileSystemEntries(destinationDirectory).Any())
            throw new InvalidOperationException("La cartella dati di destinazione contiene gia file. Il database esistente non e stato sostituito.");

        var parent = Path.GetDirectoryName(destinationDirectory)
            ?? throw new InvalidOperationException("Cartella di destinazione non valida.");
        Directory.CreateDirectory(parent);
        var stage = Path.Combine(parent, $".smz-preparazione-{Guid.NewGuid():N}");
        if (!Path.GetFullPath(stage).StartsWith(parent.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Percorso temporaneo di installazione non valido.");
        var preparedDirectory = Path.Combine(stage, "Dati");
        var preparedDatabase = Path.Combine(preparedDirectory, "smz-conta.db");
        Directory.CreateDirectory(preparedDirectory);
        try
        {
            var databaseSource = sourcePath;
            if (Path.GetExtension(sourcePath).Equals(".smzbak", StringComparison.OrdinalIgnoreCase))
            {
                if (new FileInfo(sourcePath).Length > 512L * 1024 * 1024)
                    throw new InvalidOperationException("Il backup supera la dimensione massima consentita di 512 MB.");
                var extracted = Path.Combine(stage, "Backup estratto");
                Directory.CreateDirectory(extracted);
                var backup = BackupService.ExtractAndValidateBackup(sourcePath, extracted);
                databaseSource = backup.DatabasePath;
                if (backup.ExportDirectory is not null)
                    Directory.Move(backup.ExportDirectory, Path.Combine(preparedDirectory, "Export"));
            }

            BackupService.ValidateDatabase(databaseSource);
            CopyConsistentDatabase(databaseSource, preparedDatabase);
            var safetyPath = NewSafetyBackupPath(preparedDirectory);
            File.Copy(preparedDatabase, safetyPath, overwrite: false);
            var safetyRelativePath = Path.GetRelativePath(preparedDirectory, safetyPath);
            // A previously empty directory can be replaced without removing any user files.
            if (Directory.Exists(destinationDirectory)) Directory.Delete(destinationDirectory, recursive: false);
            Directory.Move(preparedDirectory, destinationDirectory);
            return Describe(destinationPath, Path.Combine(destinationDirectory, safetyRelativePath), imported: true);
        }
        finally
        {
            // Only the unique staging directory created by this invocation is removed.
            if (Directory.Exists(stage)) Directory.Delete(stage, recursive: true);
        }
    }

    private static string NewSafetyBackupPath(string dataDirectory)
    {
        var directory = Path.Combine(dataDirectory, "Backups", "Prima installazione");
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, $"smz-prima-installazione-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.db");
    }

    private static void CopyConsistentDatabase(string sourcePath, string destinationPath)
    {
        // SQLite's backup API includes committed WAL data and leaves the source unchanged.
        using var source = OpenReadOnly(sourcePath);
        using (new FileStream(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { }
        try
        {
            using (var destination = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = destinationPath, Mode = SqliteOpenMode.ReadWrite, Pooling = false,
            }.ToString()))
            {
                destination.Open();
                source.BackupDatabase(destination);
            }
            BackupService.ValidateDatabase(destinationPath);
        }
        catch
        {
            File.Delete(destinationPath);
            throw;
        }
    }

    private static SqliteConnection OpenReadOnly(string path)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path, Mode = SqliteOpenMode.ReadOnly, Pooling = false,
        }.ToString());
        try
        {
            connection.Open();
            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    private static DatabasePreparationResult Describe(string databasePath, string backupPath, bool imported)
    {
        using var connection = OpenReadOnly(databasePath);
        long Count(string table)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_schema WHERE type = 'table' AND name = $name;";
            command.Parameters.AddWithValue("$name", table);
            if (Convert.ToInt32(command.ExecuteScalar()) == 0) return 0;
            command.CommandText = $"SELECT COUNT(*) FROM \"{table}\";";
            return Convert.ToInt64(command.ExecuteScalar());
        }
        return new(true, databasePath, backupPath, imported, Count("Personale"), Count("ServiziGiornalieri"), Count("UtentiAccesso") > 0,
            bool.TryParse(AppContext.GetData("SMZ.RequireInstalledDatabase")?.ToString(), out var required) && required);
    }
}

public sealed record DatabasePreparationResult(bool Success, string DatabasePath, string SafetyBackupPath,
    bool Imported, long PersonaleCount, long ServiziCount, bool HasUsers, bool RequiresInstalledDatabase);
