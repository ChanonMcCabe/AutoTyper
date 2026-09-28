using AutoTyper.Core.Input;

namespace AutoTyper.Core.Settings;

/// <summary>
/// The trigger hotkey, expressed as just another settings group so
/// <see cref="SettingsSchemaBuilder"/> and the generic settings panel handle
/// it the same way as every other setting rather than as a special case.
/// <see cref="PauseCombo"/> is optional and only registered while a run is in
/// progress, the same way the Escape cancel shortcut is.
/// </summary>
public class HotkeySettings : SettingsGroupBase, IValidatableSetting
{
    private HotkeyCombo? _combo;
    private HotkeyCombo? _pauseCombo;

    [Setting(Category = "Hotkey", DisplayName = "Trigger Combo")]
    public HotkeyCombo? Combo
    {
        get => _combo;
        set => SetField(ref _combo, value);
    }

    [Setting(Category = "General", DisplayName = "Pause/Resume Hotkey")]
    public HotkeyCombo? PauseCombo
    {
        get => _pauseCombo;
        set => SetField(ref _pauseCombo, value);
    }

    public IEnumerable<string> Validate()
    {
        if (PauseCombo is not { } pause)
        {
            yield break;
        }

        if (pause.Equals(Combo))
        {
            yield return "Pause/Resume Hotkey must differ from the trigger hotkey.";
        }

        if (pause.Equals(new HotkeyCombo(HotkeyModifiers.None, HotkeyKey.Escape)))
        {
            yield return "Pause/Resume Hotkey can't be Escape — Escape already cancels typing.";
        }
    }
}
