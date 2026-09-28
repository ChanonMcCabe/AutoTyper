using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoTyper.Desktop;

/// <summary>
/// Loads and saves <see cref="AppSettings"/> as JSON in
/// <c>%AppData%\AutoTyper\settings.json</c>.
/// </summary>
public static class SettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AutoTyper",
        "settings.json");

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,

        // The default encoder escapes '+' (and other ASCII punctuation) as
        // +, which would render a hotkey as "Ctrl+Alt+F9" in a
        // file users can open and edit. Relaxed escaping is safe here: this is
        // a local settings file, never embedded in HTML or a script.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() },
    };

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return new AppSettings();
            }

            string json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();

            // Ensure all settings groups are never null even if JSON contains explicit null or omits the property
            settings.Preferences ??= new();
            settings.Speed ??= new();
            settings.Bursts ??= new();
            settings.Typos ??= new();
            settings.Pauses ??= new();
            settings.StepAway ??= new();
            settings.Formatting ??= new();
            settings.Run ??= new();
            settings.Hotkey ??= new();
            settings.Presets ??= new();

            return settings;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        string? directory = Path.GetDirectoryName(SettingsPath);
        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(settings, JsonOptions);

        // Write-then-rename so a crash mid-write can't leave a truncated file.
        string tempPath = SettingsPath + ".tmp";
        File.WriteAllText(tempPath, json);
        try
        {
            File.Move(tempPath, SettingsPath, overwrite: true);
        }
        catch
        {
            // If move fails, clean up the temp file to avoid orphaning it.
            try
            {
                File.Delete(tempPath);
            }
            catch
            {
                // Ignore cleanup failures; let the original exception propagate.
            }

            throw;
        }
    }
}
