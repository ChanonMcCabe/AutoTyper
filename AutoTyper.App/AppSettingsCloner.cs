using System.Text.Json;

namespace AutoTyper.App;

/// <summary>
/// Deep-copies an <see cref="AppSettings"/> graph via a JSON round-trip so
/// the Config window can edit a working copy and only apply it to the live
/// settings when Save is clicked — Cancel just discards the copy.
/// </summary>
internal static class AppSettingsCloner
{
    public static AppSettings Clone(AppSettings source)
    {
        string json = JsonSerializer.Serialize(source, SettingsService.JsonOptions);
        var result = JsonSerializer.Deserialize<AppSettings>(json, SettingsService.JsonOptions)
            ?? throw new InvalidOperationException("Failed to clone settings.");

        // Ensure all settings groups are not null even if the JSON omitted them or contained explicit null
        result.Preferences ??= new();
        result.Speed ??= new();
        result.Bursts ??= new();
        result.Typos ??= new();
        result.Pauses ??= new();
        result.StepAway ??= new();
        result.Formatting ??= new();
        result.Hotkey ??= new();

        return result;
    }
}
