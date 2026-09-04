using AutoTyper.Core.Settings;

namespace AutoTyper.App;

/// <summary>
/// The trigger hotkey, expressed as just another settings group so
/// <see cref="SettingsSchemaBuilder"/> and the generic settings panel handle
/// it the same way as every other setting rather than as a special case.
/// Lives in App (not Core) because <see cref="HotkeyCombo"/> is a WPF type.
/// </summary>
public class HotkeySettings : SettingsGroupBase
{
    private HotkeyCombo? _combo;

    [Setting(Category = "Hotkey", DisplayName = "Trigger Combo")]
    public HotkeyCombo? Combo
    {
        get => _combo;
        set => SetField(ref _combo, value);
    }
}
