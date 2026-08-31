using System;
using System.Linq;
using System.Windows;
using Microsoft.Win32;

namespace ProtoFact.Wpf;

/// <summary>
/// Resolves and applies the active light/dark theme: detects the Windows
/// system theme, persists/loads the user's override via
/// <see cref="ThemeSettings"/>, and merges the corresponding resource
/// dictionary into <see cref="Application.Resources"/> so all
/// <c>DynamicResource</c>-bound brushes update immediately when the theme
/// changes.
/// </summary>
public static class ThemeManager
{
    private const string LightThemeUri = "Themes/LightTheme.xaml";
    private const string DarkThemeUri = "Themes/DarkTheme.xaml";

    private static ThemeSettings _settings = new();

    /// <summary>The user's persisted preference (System/Light/Dark).</summary>
    public static ThemeMode CurrentMode => _settings.Theme;

    /// <summary>The theme actually being rendered (resolves "System" to Light/Dark).</summary>
    public static bool IsDark { get; private set; }

    /// <summary>
    /// Raised after the effective theme changes, so open windows/view-models
    /// can refresh any brush values they computed in code (plain C#
    /// properties can't use DynamicResource directly).
    /// </summary>
    public static event EventHandler? ThemeChanged;

    /// <summary>
    /// Loads persisted settings and applies the effective theme. Must be
    /// called once at startup, before any window is shown.
    /// </summary>
    public static void Initialize()
    {
        _settings = ThemeSettings.Load();
        ApplyEffectiveTheme();
    }

    /// <summary>
    /// Changes the user's theme preference, persists it, and re-applies
    /// the effective theme immediately.
    /// </summary>
    public static void ApplyTheme(ThemeMode mode)
    {
        _settings.Theme = mode;
        _settings.Save();
        ApplyEffectiveTheme();
    }

    private static void ApplyEffectiveTheme()
    {
        var useDark = _settings.Theme switch
        {
            ThemeMode.Light => false,
            ThemeMode.Dark => true,
            _ => IsSystemThemeDark(),
        };

        IsDark = useDark;

        var app = Application.Current;
        if (app is null)
        {
            return;
        }

        var uri = useDark ? DarkThemeUri : LightThemeUri;
        var dictionary = new ResourceDictionary { Source = new Uri(uri, UriKind.Relative) };

        // Remove any previously merged theme dictionary (identified by containing
        // one of our well-known keys) before merging the new one.
        var existing = app.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Contains("WindowBackgroundBrush"));
        if (existing is not null)
        {
            app.Resources.MergedDictionaries.Remove(existing);
        }

        app.Resources.MergedDictionaries.Add(dictionary);

        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>
    /// Reads the Windows "Apps use light/dark theme" personalization
    /// setting. Defaults to light if the value is missing or unreadable
    /// (e.g. non-Windows or locked-down environments).
    /// </summary>
    private static bool IsSystemThemeDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            if (value is int intValue)
            {
                return intValue == 0;
            }
        }
        catch
        {
            // Fall through to the light-theme default.
        }

        return false;
    }

    /// <summary>Looks up a brush from the currently merged theme dictionary.</summary>
    public static System.Windows.Media.Brush Brush(string key)
    {
        return Application.Current?.TryFindResource(key) as System.Windows.Media.Brush
               ?? System.Windows.Media.Brushes.Gray;
    }
}
