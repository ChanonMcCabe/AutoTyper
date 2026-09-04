namespace AutoTyper.Core.Settings;

/// <summary>
/// How the passage's own whitespace is treated when typing. With
/// <see cref="PreserveSpacing"/> on (the default) line breaks, tabs and runs of
/// spaces are typed exactly as pasted — newlines and tabs as real Enter/Tab key
/// presses. With it off, every run of whitespace collapses to a single space and
/// leading/trailing whitespace is trimmed, so text pasted from a wrapped or
/// indented document types as one clean flowing block.
/// </summary>
public class FormattingSettings : SettingsGroupBase
{
    private bool _preserveSpacing = true;

    [Setting(Category = "Typing", DisplayName = "Preserve Spacing & Line Breaks")]
    public bool PreserveSpacing
    {
        get => _preserveSpacing;
        set => SetField(ref _preserveSpacing, value);
    }
}
