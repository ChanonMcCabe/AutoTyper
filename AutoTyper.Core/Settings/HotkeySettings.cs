using AutoTyper.Core.Input;

namespace AutoTyper.Core.Settings;

/// <summary>
/// The trigger hotkey, expressed as just another settings group so
/// <see cref="SettingsSchemaBuilder"/> and the generic settings panel handle
/// it the same way as every other setting rather than as a special case.
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
