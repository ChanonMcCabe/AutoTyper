using System.Windows.Controls;

namespace AutoTyper.App;

/// <summary>
/// The Appearance tab of the Settings window: a styled editor for the app-level
/// <see cref="AppPreferences"/> (theme, always-on-top). Not schema-driven — it
/// binds directly to whatever <see cref="AppPreferences"/> instance the host
/// sets as its <see cref="System.Windows.FrameworkElement.DataContext"/> (the
/// Settings window's cloned working copy), so edits stay on that copy until the
/// window's Save commits them.
/// </summary>
public partial class AppearancePanel : UserControl
{
    public AppearancePanel()
    {
        InitializeComponent();

        ThemeCombo.ItemsSource = Enum.GetValues<AppTheme>();
    }
}
