using System.IO;
using System.Text.Json;
using SMZ.Conta.App.Data;

namespace SMZ.Conta.App.Infrastructure;

public static class DisplayPreferences
{
    public const int MinimumZoom = 70;
    public const int MaximumZoom = 150;
    private static string SettingsPath => Path.Combine(DatabasePaths.AppDataDirectory, "display-settings.json");

    public static int LoadZoom()
    {
        try
        {
            return Math.Clamp(JsonSerializer.Deserialize<Settings>(File.ReadAllText(SettingsPath))?.ZoomPercent ?? 100, MinimumZoom, MaximumZoom);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return 100;
        }
    }

    public static void SaveZoom(int percent)
    {
        try
        {
            Directory.CreateDirectory(DatabasePaths.AppDataDirectory);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(new Settings(Math.Clamp(percent, MinimumZoom, MaximumZoom))));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A display preference must not interrupt the user's work.
        }
    }

    private sealed record Settings(int ZoomPercent = 100);
}
