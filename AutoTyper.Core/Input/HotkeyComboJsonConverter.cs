using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoTyper.Core.Input;

/// <summary>
/// Reads and writes a <see cref="HotkeyCombo"/> as a single string
/// ("Ctrl+Alt+F9").
/// </summary>
/// <remarks>
/// Reading also accepts the legacy object form written by the WPF build —
/// <c>{"Modifiers":"Control, Alt","Key":"F9"}</c>, where the names came from
/// WPF's own <c>ModifierKeys</c>/<c>Key</c> enums serialized by
/// <see cref="JsonStringEnumConverter"/>. Without this, the rename of
/// <c>Windows</c> to <see cref="HotkeyModifiers.Meta"/> (and of keys like
/// <c>Prior</c>) would silently drop the user's saved hotkey on first load.
/// </remarks>
public sealed class HotkeyComboJsonConverter : JsonConverter<HotkeyCombo>
{
    public override HotkeyCombo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.String => ParseString(reader.GetString()),
            JsonTokenType.StartObject => ReadLegacyObject(ref reader),
            _ => throw new JsonException($"Expected a hotkey string or object but found {reader.TokenType}."),
        };

    public override void Write(Utf8JsonWriter writer, HotkeyCombo value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStringValue(value.ToCanonicalString());
    }

    private static HotkeyCombo ParseString(string? text) =>
        HotkeyCombo.TryParse(text, out HotkeyCombo combo)
            ? combo
            : throw new JsonException($"'{text}' is not a valid hotkey combination.");

    private static HotkeyCombo ReadLegacyObject(ref Utf8JsonReader reader)
    {
        var modifiers = HotkeyModifiers.None;
        var key = HotkeyKey.None;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                continue;
            }

            string property = reader.GetString() ?? string.Empty;
            if (!reader.Read())
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.String)
            {
                reader.Skip();
                continue;
            }

            string? value = reader.GetString();
            if (property.Equals(nameof(HotkeyCombo.Modifiers), StringComparison.OrdinalIgnoreCase))
            {
                HotkeyCombo.TryParseModifiers(value, out modifiers);
            }
            else if (property.Equals(nameof(HotkeyCombo.Key), StringComparison.OrdinalIgnoreCase))
            {
                HotkeyCombo.TryParseKey(value, out key);
            }
        }

        return new HotkeyCombo(modifiers, key);
    }
}
