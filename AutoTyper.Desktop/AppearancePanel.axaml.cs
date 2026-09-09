using Avalonia.Controls;

namespace AutoTyper.Desktop;

/// <summary>
/// The hand-authored Appearance tab for app-level <see cref="AppPreferences"/>
/// (theme, always-on-top). These are not schema-driven settings, so unlike
/// <see cref="SettingsPanel"/> this control names its controls explicitly.
/// </summary>
public partial class AppearancePanel : UserControl
{
    public AppearancePanel()
    {
        InitializeComponent();
        ThemeCombo.ItemsSource = Enum.GetValues<AppTheme>();
    }
}
