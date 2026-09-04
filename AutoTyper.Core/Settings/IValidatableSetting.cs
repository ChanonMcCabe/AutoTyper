namespace AutoTyper.Core.Settings;

/// <summary>
/// Implemented by a settings group that has cross-field rules a per-property
/// Min/Max can't express — e.g. "MinWpm must not exceed MaxWpm". Checked by
/// <see cref="SettingsValidator"/> alongside the generic Min/Max sweep.
/// </summary>
public interface IValidatableSetting
{
    IEnumerable<string> Validate();
}
