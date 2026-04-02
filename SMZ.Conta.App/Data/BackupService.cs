using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace SMZ.Conta.App.Data;

public sealed class BackupService
{
    private const string BackupExtension = ".smzbak";
    private const string DatabaseEntryName = "smz-conta.db";
    private const string ManifestEntryName = "manifest.json";
    private const int LocalBackupRetentionCount = 20;
    private const int ExternalBackupRetentionCount = 30;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public BackupSettings LoadSettings()
    {
        var path = DatabasePaths.BackupSettingsPath;
        if (!File.Exists(path))
        {
            return new BackupSettings();
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<BackupSettings>(json, JsonOptions) ?? new BackupSettings();
        }
        catch
        {
            return new BackupSettings();
        }
    }

    public void SaveSettings(BackupSettings settings)
    {
        Directory.CreateDirectory(DatabasePaths.AppDataDirectory);
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(DatabasePaths.BackupSettingsPath, json);
    }

    public BackupInfo? GetLatestLocalBackup() => GetLatestBackup(DatabasePaths.LocalBackupDirectory);

    public BackupInfo? GetLatestExternalBackup(string? externalDirectory) => GetLatestBackup(externalDirectory);

    public bool NeedsAutomaticLocalBackup()
    {
        var latest = GetLatestLocalBackup();
        return latest is null || latest.CreatedAtLocal.Date < DateTime.Now.Date;
    }

    public BackupResult CreateLocalBackup(string reason) =>
        CreateBackup(DatabasePaths.LocalBackupDirectory, BackupScope.Local, reason, LocalBackupRetentionCount);

    public BackupResult CreateExternalBackup(string externalDirectory, string reason) =>
        CreateBackup(externalDirectory, BackupScope.External, reason, ExternalBackupRetentionCount);

    public RestoreResult RestoreBackup(string backupFilePath)
    {
        if (string.IsNullOrWhiteSpace(backupFilePath) || !File.Exists(backupFilePath))
        {
            throw new FileNotFoundException("File di backup non trovato.", backupFilePath);
        }

        var safetyBackup = CreateLocalBackup("restore-safety");
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"smz-restore-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            ZipFile.ExtractToDirectory(backupFilePath, tempDirectory);

            var extractedDatabasePath = Path.Combine(tempDirectory, DatabaseEntryName);
            if (!File.Exists(extractedDatabasePath))
            {
                throw new InvalidOperationException("Il file di backup non contiene il database principale.");
            }

            Directory.CreateDirectory(DatabasePaths.AppDataDirectory);
            File.Copy(extractedDatabasePath, DatabasePaths.DatabasePath, overwrite: true);

            var extractedExportDirectory = Path.Combine(tempDirectory, "Export");
            if (Directory.Exists(extractedExportDirectory))
            {
                ReplaceDirectoryContents(extractedExportDirectory, DatabasePaths.ExportDirectory);
            }

            DatabaseInitializer.EnsureDatabase();

            return new RestoreResult
            {
                RestoredBackupPath = backupFilePath,
                SafetyBackupPath = safetyBackup.BackupPath,
                RestoredAtLocal = DateTime.Now,
            };
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    private BackupResult CreateBackup(string targetDirectory, BackupScope scope, string reason, int retentionCount)
    {
        if (string.IsNullOrWhiteSpace(targetDirectory))
        {
            throw new InvalidOperationException("Cartella di backup non configurata.");
        }

        Directory.CreateDirectory(targetDirectory);

        var timestamp = DateTime.Now;
        var backupPath = BuildUniqueBackupPath(targetDirectory, timestamp);
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"smz-backup-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        var tempDatabasePath = Path.Combine(tempDirectory, DatabaseEntryName);

        try
        {
            CreateConsistentDatabaseCopy(tempDatabasePath);

            var includesExport = Directory.Exists(DatabasePaths.ExportDirectory)
                && Directory.EnumerateFiles(DatabasePaths.ExportDirectory, "*", SearchOption.AllDirectories).Any();

            var manifest = new BackupManifest
            {
                CreatedAtLocal = timestamp,
                Scope = scope.ToString(),
                Reason = reason,
                SourceDatabasePath = DatabasePaths.DatabasePath,
                MachineName = Environment.MachineName,
                AppVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "sconosciuta",
                IncludesExport = includesExport,
                DatabaseSizeBytes = new FileInfo(tempDatabasePath).Length,
            };

            using var archive = ZipFile.Open(backupPath, ZipArchiveMode.Create);
            archive.CreateEntryFromFile(tempDatabasePath, DatabaseEntryName, CompressionLevel.Optimal);

            if (includesExport)
            {
                foreach (var exportFile in Directory.EnumerateFiles(DatabasePaths.ExportDirectory, "*", SearchOption.AllDirectories))
                {
                    var relativePath = Path.GetRelativePath(DatabasePaths.ExportDirectory, exportFile);
                    archive.CreateEntryFromFile(exportFile, Path.Combine("Export", relativePath), CompressionLevel.Optimal);
                }
            }

            var manifestEntry = archive.CreateEntry(ManifestEntryName, CompressionLevel.Optimal);
            using (var stream = manifestEntry.Open())
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(JsonSerializer.Serialize(manifest, JsonOptions));
            }

            PruneOldBackups(targetDirectory, retentionCount);

            return new BackupResult
            {
                BackupPath = backupPath,
                CreatedAtLocal = timestamp,
                IncludesExport = includesExport,
                Scope = scope,
            };
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    private static void CreateConsistentDatabaseCopy(string destinationPath)
    {
        using var source = new SqliteConnection(DatabasePaths.ConnectionString);
        source.Open();

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = destinationPath,
            ForeignKeys = true,
        };

        using var destination = new SqliteConnection(builder.ToString());
        destination.Open();
        source.BackupDatabase(destination);
    }

    private static void ReplaceDirectoryContents(string sourceDirectory, string targetDirectory)
    {
        if (Directory.Exists(targetDirectory))
        {
            Directory.Delete(targetDirectory, recursive: true);
        }

        Directory.CreateDirectory(targetDirectory);

        foreach (var sourceFile in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
            var destinationFile = Path.Combine(targetDirectory, relativePath);
            var destinationFolder = Path.GetDirectoryName(destinationFile);
            if (!string.IsNullOrWhiteSpace(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }

            File.Copy(sourceFile, destinationFile, overwrite: true);
        }
    }

    private static string BuildUniqueBackupPath(string targetDirectory, DateTime timestamp)
    {
        var baseName = $"smz-backup-{timestamp:yyyy-MM-dd_HHmmss}";
        var candidate = Path.Combine(targetDirectory, $"{baseName}{BackupExtension}");
        var sequence = 1;

        while (File.Exists(candidate))
        {
            candidate = Path.Combine(targetDirectory, $"{baseName}_{sequence:D2}{BackupExtension}");
            sequence++;
        }

        return candidate;
    }

    private static BackupInfo? GetLatestBackup(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return null;
        }

        var latestFile = new DirectoryInfo(directory)
            .EnumerateFiles($"*{BackupExtension}", SearchOption.TopDirectoryOnly)
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .FirstOrDefault();

        return latestFile is null ? null : MapBackupInfo(latestFile.FullName);
    }

    private static BackupInfo MapBackupInfo(string path)
    {
        var file = new FileInfo(path);
        return new BackupInfo
        {
            BackupPath = file.FullName,
            FileName = file.Name,
            CreatedAtLocal = file.LastWriteTime,
            SizeBytes = file.Length,
        };
    }

    private static void PruneOldBackups(string directory, int retentionCount)
    {
        var filesToDelete = new DirectoryInfo(directory)
            .EnumerateFiles($"*{BackupExtension}", SearchOption.TopDirectoryOnly)
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .Skip(retentionCount)
            .ToList();

        foreach (var file in filesToDelete)
        {
            file.Delete();
        }
    }
}

public sealed class BackupSettings
{
    public string ExternalBackupDirectory { get; set; } = string.Empty;
}

public sealed class BackupInfo
{
    public string BackupPath { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public DateTime CreatedAtLocal { get; set; }

    public long SizeBytes { get; set; }
}

public sealed class BackupResult
{
    public string BackupPath { get; set; } = string.Empty;

    public DateTime CreatedAtLocal { get; set; }

    public bool IncludesExport { get; set; }

    public BackupScope Scope { get; set; }
}

public sealed class RestoreResult
{
    public string RestoredBackupPath { get; set; } = string.Empty;

    public string SafetyBackupPath { get; set; } = string.Empty;

    public DateTime RestoredAtLocal { get; set; }
}

public sealed class BackupManifest
{
    public DateTime CreatedAtLocal { get; set; }

    public string Scope { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public string SourceDatabasePath { get; set; } = string.Empty;

    public string MachineName { get; set; } = string.Empty;

    public string AppVersion { get; set; } = string.Empty;

    public bool IncludesExport { get; set; }

    public long DatabaseSizeBytes { get; set; }
}

public enum BackupScope
{
    Local,
    External,
}
