using System;
using System.IO;
using System.Text.Json;

namespace ProtoFact.Wpf;

/// <summary>
/// The user's theme preference. <see cref="System"/> means "follow the
/// Windows light/dark setting"; <see cref="Light"/>/<see cref="Dark"/> are
/// explicit overrides persisted across app restarts.
/// </summary>
public enum ThemeMode
{
    System,
    Light,
    Dark,
}

/// <summary>
/// Small persisted settings model, stored as JSON under
/// <c>%AppData%\ProtoFact\settings.json</c>. Kept minimal and dependency-free
/// (no App.config/Settings.settings) since it currently only needs to
/// remember the user's theme choice.
/// </summary>
public sealed class ThemeSettings
{
    public ThemeMode Theme { get; set; } = ThemeMode.System;

    private static string SettingsPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ProtoFact", "settings.json");

    public static ThemeSettings Load()
    {
        try
        {
            var path = SettingsPath;
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var settings = JsonSerializer.Deserialize<ThemeSettings>(json);
                if (settings is not null)
                {
                    return settings;
                }
            }
        }
        catch
        {
            // Fall back to defaults if the settings file is missing, unreadable, or corrupt.
        }

        return new ThemeSettings();
    }

    public void Save()
    {
        try
        {
            var path = SettingsPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch
        {
            // Persisting is best-effort; failure to save shouldn't crash the app.
        }
    }
}
