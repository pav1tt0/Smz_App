using System.IO;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace SMZ.Conta.App.Data;

public static class DatabasePaths
{
    public const string AppDataDirectoryEnvironmentVariable = "SMZ_CONTA_APPDATA";
    public const string DataProfileAppContextKey = "SMZ.DataProfile";

    public static string AppDataDirectory =>
        Environment.GetEnvironmentVariable(AppDataDirectoryEnvironmentVariable) is { } overridePath
            && !string.IsNullOrWhiteSpace(overridePath)
            ? ValidateOverride(overridePath)
            : ResolveInstalledDataDirectory(AppContext.BaseDirectory) ?? GetFallbackDataDirectory();

    private static bool RequiresInstalledDatabase =>
        bool.TryParse(AppContext.GetData("SMZ.RequireInstalledDatabase")?.ToString(), out var required) && required;

    private static string ValidateOverride(string directory)
    {
        if (RequiresInstalledDatabase && !File.Exists(Path.Combine(directory, "smz-conta.db")))
            throw new InvalidOperationException("Il database reale selezionato non e disponibile. Ripetere l'installazione di SMZ.");
        return directory;
    }

    private static string GetFallbackDataDirectory()
    {
        if (RequiresInstalledDatabase)
            throw new InvalidOperationException("Installare SMZ con INSTALLA-E-AVVIA-SMZ.cmd e selezionare il database reale prima dell'avvio.");
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SMZ", GetDataProfile());
    }

    public static string? ResolveInstalledDataDirectory(string applicationDirectory)
    {
        var settingsPath = Path.Combine(applicationDirectory, "installazione.json");
        if (!File.Exists(settingsPath)) return null;
        try
        {
            using var settings = JsonDocument.Parse(File.ReadAllText(settingsPath));
            if (settings.RootElement.GetProperty("PackageKind").GetString() != "Operational")
                throw new InvalidOperationException("Tipo di installazione SMZ non riconosciuto.");
            var directory = settings.RootElement.GetProperty("DataDirectory").GetString();
            if (string.IsNullOrWhiteSpace(directory) || !Path.IsPathFullyQualified(directory))
                throw new InvalidOperationException("Percorso del database installato non valido.");
            directory = Path.GetFullPath(directory);
            if (!File.Exists(Path.Combine(directory, "smz-conta.db")))
                throw new InvalidOperationException("Il database reale configurato non e disponibile. Ripetere l'installazione e selezionare il database corretto.");
            return directory;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or KeyNotFoundException)
        {
            throw new InvalidOperationException("Configurazione dell'installazione non leggibile. Ripetere l'installazione di SMZ.", ex);
        }
    }

    private static string GetDataProfile()
    {
        var profile = AppContext.GetData(DataProfileAppContextKey) as string;
        if (string.IsNullOrWhiteSpace(profile))
        {
            return "Conta";
        }

        if (profile is "." or ".." || profile.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new InvalidOperationException("Profilo dati del pacchetto SMZ non valido.");
        }

        return profile;
    }

    public static string ExportDirectory => Path.Combine(AppDataDirectory, "Export");

    public static string BackupsDirectory => Path.Combine(AppDataDirectory, "Backups");

    public static string LocalBackupDirectory => Path.Combine(BackupsDirectory, "Local");

    public static string BackupSettingsPath => Path.Combine(AppDataDirectory, "backup-settings.json");

    public static string DatabasePath => Path.Combine(AppDataDirectory, "smz-conta.db");

    public static string ConnectionString
    {
        get
        {
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = DatabasePath,
                ForeignKeys = true,
            };

            return builder.ToString();
        }
    }
}
