using System.IO;
using Microsoft.Data.Sqlite;

namespace SMZ.Conta.App.Data;

public static class DatabasePaths
{
    public static string AppDataDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SMZ",
            "Conta");

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
