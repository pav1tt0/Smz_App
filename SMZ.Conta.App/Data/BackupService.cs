using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace SMZ.Conta.App.Data;

public sealed class BackupService
{
    private const string BackupExtension = ".smzbak";
    private const string DatabaseEntryName = "smz-conta.db";
    private const string ManifestEntryName = "manifest.json";
    private const int CurrentBackupFormatVersion = 2;
    private const int LocalBackupRetentionCount = 20;
    private const int ExternalBackupRetentionCount = 30;
    private const int MaxArchiveEntries = 5_000;
    private const long MaxArchiveSizeBytes = 512L * 1024 * 1024;
    private const long MaxEntrySizeBytes = 512L * 1024 * 1024;
    private const long MaxExtractedSizeBytes = 1024L * 1024 * 1024;
    private const long MaxManifestSizeBytes = 1024L * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly IReadOnlyDictionary<string, string[]> RequiredDatabaseSchema =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Personale"] = ["PerId", "Cognome", "Nome", "CodiceFiscale"],
            ["TipiAbilitazione"] = ["TipoAbilitazioneId", "Codice", "Descrizione"],
            ["ServiziGiornalieri"] = ["ServizioGiornalieroId", "DataServizio"],
            ["ServizioPartecipanti"] = ["ServizioPartecipanteId", "ServizioGiornalieroId", "PerId"],
        };

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

        var backupFile = new FileInfo(backupFilePath);
        if (backupFile.Length > MaxArchiveSizeBytes)
        {
            throw new InvalidOperationException(
                $"Il backup supera la dimensione massima consentita di {FormatBytes(MaxArchiveSizeBytes)}.");
        }

        if (!File.Exists(DatabasePaths.DatabasePath))
        {
            throw new InvalidOperationException("Il database corrente non e disponibile: ripristino annullato.");
        }

        var tempDirectory = Path.Combine(Path.GetTempPath(), $"smz-restore-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var validatedBackup = ExtractAndValidateBackup(backupFile.FullName, tempDirectory);
            var safetyBackup = CreateLocalBackup("restore-safety");
            ApplyValidatedBackup(validatedBackup);

            return new RestoreResult
            {
                RestoredBackupPath = backupFile.FullName,
                SafetyBackupPath = safetyBackup.BackupPath,
                RestoredAtLocal = DateTime.Now,
            };
        }
        finally
        {
            TryDeleteDirectory(tempDirectory);
        }
    }

    private static ValidatedBackup ExtractAndValidateBackup(string backupFilePath, string tempDirectory)
    {
        try
        {
            using var archiveStream = new FileStream(
                backupFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);
            using var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read, leaveOpen: false);

            if (archive.Entries.Count == 0 || archive.Entries.Count > MaxArchiveEntries)
            {
                throw new InvalidOperationException(
                    $"Il backup contiene un numero di elementi non consentito (massimo {MaxArchiveEntries}).");
            }

            var extractedRoot = Path.GetFullPath(tempDirectory) + Path.DirectorySeparatorChar;
            var encounteredEntries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            long totalUncompressedSize = 0;
            var includesExport = false;

            foreach (var entry in archive.Entries)
            {
                var normalizedName = NormalizeAndValidateEntryName(entry.FullName);
                var duplicateKey = normalizedName.TrimEnd('/');
                if (!encounteredEntries.Add(duplicateKey))
                {
                    throw new InvalidOperationException($"Il backup contiene elementi duplicati: {normalizedName}");
                }

                var isDirectory = normalizedName.EndsWith("/", StringComparison.Ordinal);
                var isDatabase = normalizedName.Equals(DatabaseEntryName, StringComparison.OrdinalIgnoreCase);
                var isManifest = normalizedName.Equals(ManifestEntryName, StringComparison.OrdinalIgnoreCase);
                var isExport = normalizedName.StartsWith("Export/", StringComparison.OrdinalIgnoreCase);

                if (!isDatabase && !isManifest && !isExport)
                {
                    throw new InvalidOperationException($"Il backup contiene un elemento non previsto: {normalizedName}");
                }

                if (isDirectory)
                {
                    if (!isExport)
                    {
                        throw new InvalidOperationException($"Cartella non prevista nel backup: {normalizedName}");
                    }

                    continue;
                }

                var entryLimit = isManifest ? MaxManifestSizeBytes : MaxEntrySizeBytes;
                if (entry.Length < 0 || (!isExport && entry.Length == 0) || entry.Length > entryLimit)
                {
                    throw new InvalidOperationException(
                        $"Dimensione non consentita per l'elemento {normalizedName}.");
                }

                if (totalUncompressedSize > MaxExtractedSizeBytes - entry.Length)
                {
                    throw new InvalidOperationException(
                        $"Il contenuto estratto supera il limite di {FormatBytes(MaxExtractedSizeBytes)}.");
                }

                totalUncompressedSize += entry.Length;
                includesExport |= isExport;

                var destinationPath = Path.GetFullPath(Path.Combine(
                    tempDirectory,
                    normalizedName.Replace('/', Path.DirectorySeparatorChar)));
                if (!destinationPath.StartsWith(extractedRoot, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Il backup contiene un percorso non sicuro.");
                }

                var destinationDirectory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrWhiteSpace(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                ExtractEntryWithLimit(entry, destinationPath, entryLimit);
            }

            var extractedDatabasePath = Path.Combine(tempDirectory, DatabaseEntryName);
            var manifestPath = Path.Combine(tempDirectory, ManifestEntryName);
            if (!File.Exists(extractedDatabasePath) || !File.Exists(manifestPath))
            {
                throw new InvalidOperationException(
                    "Il file selezionato non e un backup SMZ valido: database o manifest mancanti.");
            }

            ReadAndValidateManifest(manifestPath, extractedDatabasePath, includesExport);
            ValidateDatabase(extractedDatabasePath);

            var extractedExportDirectory = Path.Combine(tempDirectory, "Export");
            return new ValidatedBackup(
                extractedDatabasePath,
                Directory.Exists(extractedExportDirectory) ? extractedExportDirectory : null);
        }
        catch (InvalidDataException ex)
        {
            throw new InvalidOperationException("Il file selezionato non e un archivio SMZ valido.", ex);
        }
    }

    private static string NormalizeAndValidateEntryName(string entryName)
    {
        if (string.IsNullOrWhiteSpace(entryName))
        {
            throw new InvalidOperationException("Il backup contiene un elemento senza nome.");
        }

        var normalizedName = entryName.Replace('\\', '/');
        var pathToCheck = normalizedName.TrimEnd('/');
        var segments = pathToCheck.Split('/', StringSplitOptions.None);
        if (normalizedName.StartsWith("/", StringComparison.Ordinal)
            || pathToCheck.Contains(':')
            || segments.Any(segment => string.IsNullOrWhiteSpace(segment) || segment is "." or ".."))
        {
            throw new InvalidOperationException($"Il backup contiene un percorso non sicuro: {entryName}");
        }

        return normalizedName;
    }

    private static void ExtractEntryWithLimit(ZipArchiveEntry entry, string destinationPath, long byteLimit)
    {
        using var source = entry.Open();
        using var destination = new FileStream(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        var buffer = new byte[81920];
        long written = 0;

        while (true)
        {
            var read = source.Read(buffer, 0, buffer.Length);
            if (read == 0)
            {
                break;
            }

            if (written > byteLimit - read)
            {
                throw new InvalidOperationException($"L'elemento {entry.FullName} supera la dimensione consentita.");
            }

            destination.Write(buffer, 0, read);
            written += read;
        }

        if (written != entry.Length)
        {
            throw new InvalidOperationException($"L'elemento {entry.FullName} risulta incompleto.");
        }
    }

    private static void ReadAndValidateManifest(
        string manifestPath,
        string databasePath,
        bool includesExport)
    {
        BackupManifest manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<BackupManifest>(File.ReadAllText(manifestPath), JsonOptions)
                ?? throw new InvalidOperationException("Il manifest del backup e vuoto.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Il manifest del backup non e leggibile.", ex);
        }

        if (manifest.BackupFormatVersion < 0 || manifest.BackupFormatVersion > CurrentBackupFormatVersion)
        {
            throw new InvalidOperationException(
                $"Versione del backup non supportata: {manifest.BackupFormatVersion}.");
        }

        if (manifest.CreatedAtLocal == default
            || string.IsNullOrWhiteSpace(manifest.AppVersion)
            || !Enum.TryParse<BackupScope>(manifest.Scope, ignoreCase: true, out _))
        {
            throw new InvalidOperationException("Il manifest del backup e incompleto o non valido.");
        }

        var databaseLength = new FileInfo(databasePath).Length;
        if (manifest.DatabaseSizeBytes != databaseLength || manifest.IncludesExport != includesExport)
        {
            throw new InvalidOperationException("Il contenuto del backup non corrisponde al manifest.");
        }

        if (manifest.BackupFormatVersion >= CurrentBackupFormatVersion
            && string.IsNullOrWhiteSpace(manifest.DatabaseSha256))
        {
            throw new InvalidOperationException("Il backup non contiene l'impronta di integrita del database.");
        }

        if (!string.IsNullOrWhiteSpace(manifest.DatabaseSha256))
        {
            var actualHash = ComputeSha256(databasePath);
            if (!actualHash.Equals(manifest.DatabaseSha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Il database del backup non supera il controllo di integrita SHA-256.");
            }
        }

    }

    private static void ValidateDatabase(string databasePath)
    {
        try
        {
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Mode = SqliteOpenMode.ReadOnly,
                ForeignKeys = true,
                Pooling = false,
            };

            using var connection = new SqliteConnection(builder.ToString());
            connection.Open();

            using (var settingsCommand = connection.CreateCommand())
            {
                settingsCommand.CommandText = "PRAGMA query_only = ON; PRAGMA trusted_schema = OFF;";
                settingsCommand.ExecuteNonQuery();
            }

            using (var integrityCommand = connection.CreateCommand())
            {
                integrityCommand.CommandText = "PRAGMA integrity_check;";
                using var reader = integrityCommand.ExecuteReader();
                while (reader.Read())
                {
                    var result = reader.GetString(0);
                    if (!result.Equals("ok", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException($"Database del backup danneggiato: {result}");
                    }
                }
            }

            using (var foreignKeyCommand = connection.CreateCommand())
            {
                foreignKeyCommand.CommandText = "PRAGMA foreign_key_check;";
                using var reader = foreignKeyCommand.ExecuteReader();
                if (reader.Read())
                {
                    throw new InvalidOperationException("Il database del backup contiene relazioni non valide.");
                }
            }

            using (var unsupportedSchemaCommand = connection.CreateCommand())
            {
                unsupportedSchemaCommand.CommandText =
                    "SELECT name FROM sqlite_schema WHERE type IN ('trigger', 'view') AND name NOT LIKE 'sqlite_%' LIMIT 1;";
                var unsupportedObject = Convert.ToString(unsupportedSchemaCommand.ExecuteScalar());
                if (!string.IsNullOrWhiteSpace(unsupportedObject))
                {
                    throw new InvalidOperationException(
                        $"Il database del backup contiene un oggetto non previsto: {unsupportedObject}.");
                }
            }

            foreach (var (tableName, requiredColumns) in RequiredDatabaseSchema)
            {
                EnsureRequiredTableAndColumns(connection, tableName, requiredColumns);
            }
        }
        catch (SqliteException ex)
        {
            throw new InvalidOperationException("Il database contenuto nel backup non e un database SMZ valido.", ex);
        }
    }

    private static void EnsureRequiredTableAndColumns(
        SqliteConnection connection,
        string tableName,
        IReadOnlyCollection<string> requiredColumns)
    {
        using (var tableCommand = connection.CreateCommand())
        {
            tableCommand.CommandText =
                "SELECT COUNT(*) FROM sqlite_schema WHERE type = 'table' AND name = $tableName;";
            tableCommand.Parameters.AddWithValue("$tableName", tableName);
            if (Convert.ToInt32(tableCommand.ExecuteScalar()) != 1)
            {
                throw new InvalidOperationException($"Nel backup manca la tabella SMZ {tableName}.");
            }
        }

        var availableColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var columnsCommand = connection.CreateCommand())
        {
            columnsCommand.CommandText = $"PRAGMA table_info(\"{tableName}\");";
            using var reader = columnsCommand.ExecuteReader();
            while (reader.Read())
            {
                availableColumns.Add(reader.GetString(1));
            }
        }

        var missingColumns = requiredColumns.Where(column => !availableColumns.Contains(column)).ToList();
        if (missingColumns.Count > 0)
        {
            throw new InvalidOperationException(
                $"La tabella {tableName} del backup non e compatibile. Colonne mancanti: {string.Join(", ", missingColumns)}.");
        }
    }

    private static void ApplyValidatedBackup(ValidatedBackup backup)
    {
        Directory.CreateDirectory(DatabasePaths.AppDataDirectory);

        var operationId = Guid.NewGuid().ToString("N");
        var databaseCandidatePath = Path.Combine(
            DatabasePaths.AppDataDirectory,
            $"smz-conta.restore-new-{operationId}.tmp");
        var databaseRollbackPath = Path.Combine(
            DatabasePaths.AppDataDirectory,
            $"smz-conta.restore-old-{operationId}.tmp");
        var exportCandidatePath = Path.Combine(
            DatabasePaths.AppDataDirectory,
            $"Export.restore-new-{operationId}");
        var exportRollbackPath = Path.Combine(
            DatabasePaths.AppDataDirectory,
            $"Export.restore-old-{operationId}");

        var databaseReplaced = false;
        var originalExportMoved = false;
        var replacementExportMoved = false;
        var preserveRollbackArtifacts = false;

        try
        {
            File.Copy(backup.DatabasePath, databaseCandidatePath, overwrite: false);
            ValidateDatabase(databaseCandidatePath);

            if (backup.ExportDirectory is not null)
            {
                CopyDirectoryContents(backup.ExportDirectory, exportCandidatePath);
            }

            SqliteConnection.ClearAllPools();
            File.Replace(
                databaseCandidatePath,
                DatabasePaths.DatabasePath,
                databaseRollbackPath,
                ignoreMetadataErrors: true);
            databaseReplaced = true;

            if (backup.ExportDirectory is not null)
            {
                if (File.Exists(DatabasePaths.ExportDirectory))
                {
                    throw new IOException("Il percorso della cartella Export e occupato da un file.");
                }

                if (Directory.Exists(DatabasePaths.ExportDirectory))
                {
                    Directory.Move(DatabasePaths.ExportDirectory, exportRollbackPath);
                    originalExportMoved = true;
                }

                Directory.Move(exportCandidatePath, DatabasePaths.ExportDirectory);
                replacementExportMoved = true;
            }

            DatabaseInitializer.EnsureDatabase();
            ValidateDatabase(DatabasePaths.DatabasePath);
        }
        catch (Exception restoreException)
        {
            var rollbackErrors = new List<Exception>();
            SqliteConnection.ClearAllPools();

            try
            {
                if (replacementExportMoved && Directory.Exists(DatabasePaths.ExportDirectory))
                {
                    Directory.Delete(DatabasePaths.ExportDirectory, recursive: true);
                }

                if (originalExportMoved && Directory.Exists(exportRollbackPath))
                {
                    Directory.Move(exportRollbackPath, DatabasePaths.ExportDirectory);
                }
            }
            catch (Exception exportRollbackException)
            {
                rollbackErrors.Add(exportRollbackException);
            }

            try
            {
                if (databaseReplaced && File.Exists(databaseRollbackPath))
                {
                    File.Replace(
                        databaseRollbackPath,
                        DatabasePaths.DatabasePath,
                        destinationBackupFileName: null,
                        ignoreMetadataErrors: true);
                    ValidateDatabase(DatabasePaths.DatabasePath);
                }
            }
            catch (Exception databaseRollbackException)
            {
                rollbackErrors.Add(databaseRollbackException);
            }

            if (rollbackErrors.Count > 0)
            {
                preserveRollbackArtifacts = true;
                rollbackErrors.Insert(0, restoreException);
                throw new InvalidOperationException(
                    "Ripristino non riuscito e rollback incompleto. Non usare l'applicazione e conservare i backup di sicurezza.",
                    new AggregateException(rollbackErrors));
            }

            throw new InvalidOperationException(
                "Ripristino non riuscito. Il database e gli export precedenti sono stati ripristinati automaticamente.",
                restoreException);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            TryDeleteFile(databaseCandidatePath);
            TryDeleteDirectory(exportCandidatePath);
            if (!preserveRollbackArtifacts)
            {
                TryDeleteFile(databaseRollbackPath);
                TryDeleteDirectory(exportRollbackPath);
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
                BackupFormatVersion = CurrentBackupFormatVersion,
                CreatedAtLocal = timestamp,
                Scope = scope.ToString(),
                Reason = reason,
                SourceDatabasePath = DatabasePaths.DatabasePath,
                MachineName = Environment.MachineName,
                AppVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "sconosciuta",
                IncludesExport = includesExport,
                DatabaseSizeBytes = new FileInfo(tempDatabasePath).Length,
                DatabaseSha256 = ComputeSha256(tempDatabasePath),
            };

            using var archive = ZipFile.Open(backupPath, ZipArchiveMode.Create);
            archive.CreateEntryFromFile(tempDatabasePath, DatabaseEntryName, CompressionLevel.Optimal);

            if (includesExport)
            {
                foreach (var exportFile in Directory.EnumerateFiles(DatabasePaths.ExportDirectory, "*", SearchOption.AllDirectories))
                {
                    var relativePath = Path.GetRelativePath(DatabasePaths.ExportDirectory, exportFile);
                    var archivePath = $"Export/{relativePath.Replace('\\', '/')}";
                    archive.CreateEntryFromFile(exportFile, archivePath, CompressionLevel.Optimal);
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
            TryDeleteDirectory(tempDirectory);
        }
    }

    private static void CreateConsistentDatabaseCopy(string destinationPath)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = destinationPath,
            ForeignKeys = true,
            Pooling = false,
        };

        using (var source = new SqliteConnection(DatabasePaths.ConnectionString))
        using (var destination = new SqliteConnection(builder.ToString()))
        {
            source.Open();
            destination.Open();
            source.BackupDatabase(destination);
        }

        SqliteConnection.ClearAllPools();
    }

    private static void CopyDirectoryContents(string sourceDirectory, string targetDirectory)
    {
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

    private static string ComputeSha256(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static string FormatBytes(long bytes) => $"{bytes / (1024 * 1024)} MB";

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // La pulizia dei file temporanei non deve compromettere un restore gia concluso o il rollback.
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // La pulizia dei file temporanei non deve mascherare l'esito dell'operazione principale.
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
    public int BackupFormatVersion { get; set; }

    public DateTime CreatedAtLocal { get; set; }

    public string Scope { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public string SourceDatabasePath { get; set; } = string.Empty;

    public string MachineName { get; set; } = string.Empty;

    public string AppVersion { get; set; } = string.Empty;

    public bool IncludesExport { get; set; }

    public long DatabaseSizeBytes { get; set; }

    public string DatabaseSha256 { get; set; } = string.Empty;
}

internal sealed record ValidatedBackup(
    string DatabasePath,
    string? ExportDirectory);

public enum BackupScope
{
    Local,
    External,
}
