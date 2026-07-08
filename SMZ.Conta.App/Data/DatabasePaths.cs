using System.IO;
using Microsoft.Data.Sqlite;

namespace SMZ.Conta.App.Data;

public static class DatabasePaths
{
    public const string AppDataDirectoryEnvironmentVariable = "SMZ_CONTA_APPDATA";

    public static string AppDataDirectory =>
        Environment.GetEnvironmentVariable(AppDataDirectoryEnvironmentVariable) is { } overridePath
            && !string.IsNullOrWhiteSpace(overridePath)
            ? overridePath
            : Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SMZ",
                "Conta");

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
