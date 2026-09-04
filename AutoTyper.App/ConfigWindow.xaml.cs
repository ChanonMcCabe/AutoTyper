using System.IO;
using System.Windows;
using AutoTyper.Core.Settings;
using Wpf.Ui.Controls;

namespace AutoTyper.App;

/// <summary>
/// Edits a cloned working copy of <see cref="AppSettings"/> across four tabs:
/// the schema-driven General/Typing/Advanced groups (one <see cref="SettingsPanel"/>
/// per tab, scoped to that category) plus the hand-styled <see cref="AppearancePanel"/>
/// for the app-level <see cref="AppPreferences"/> (theme, always-on-top). Save
/// validates the schema groups (Min/Max range checks plus cross-field rules like
/// MinWpm/MaxWpm) and, only if it passes, copies both the schema values and the
/// preferences into the live settings the caller passed in; Cancel simply closes
/// without touching them. The trigger hotkey and the Main window's own WPM field
/// are deliberately excluded — they're owned by the Main window's own
/// Save/Cancel, not this window's. Applying the committed theme and toggling the
/// Main window's Topmost is the caller's job (see MainWindow.SettingsButton_Click).
/// </summary>
public partial class ConfigWindow : FluentWindow
{
    private static readonly string[] ConfigCategories = ["General", "Typing", "Advanced"];

    private readonly AppSettings _liveSettings;
    private readonly AppSettings _workingCopy;

    public ConfigWindow(AppSettings liveSettings)
    {
        InitializeComponent();

        _liveSettings = liveSettings;
        _workingCopy = AppSettingsCloner.Clone(liveSettings);

        AppearancePanel.DataContext = _workingCopy.Preferences;

        IReadOnlyList<SettingDescriptor> descriptors = SettingsSchemaBuilder.Build(_workingCopy);

        GeneralPanel.SetDescriptors(descriptors
            .Where(d => d.Category == "General" && d.Name != nameof(SpeedSettings.Wpm))
            .ToList());
        TypingPanel.SetDescriptors(descriptors.Where(d => d.Category == "Typing").ToList());
        AdvancedPanel.SetDescriptors(descriptors.Where(d => d.Category == "Advanced").ToList());
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        IReadOnlyList<SettingDescriptor> workingDescriptors = SettingsSchemaBuilder.Build(_workingCopy);
        IReadOnlyList<string> errors = SettingsValidator.Validate(workingDescriptors);

        if (errors.Count > 0)
        {
            ErrorText.Title = "Invalid settings";
            ErrorText.Message = string.Join(" ", errors);
            ErrorText.IsOpen = true;
            return;
        }

        ErrorText.IsOpen = false;

        CommitWorkingCopyToLiveSettings(workingDescriptors);

        // AppPreferences is not part of the schema, so commit it by hand.
        _liveSettings.Preferences.Theme = _workingCopy.Preferences.Theme;
        _liveSettings.Preferences.AlwaysOnTop = _workingCopy.Preferences.AlwaysOnTop;

        try
        {
            SettingsService.Save(_liveSettings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ErrorText.Title = "Couldn't save settings";
            ErrorText.Message = ex.Message;
            ErrorText.IsOpen = true;
            return; // keep the dialog open so the failure is visible
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void CommitWorkingCopyToLiveSettings(IReadOnlyList<SettingDescriptor> workingDescriptors)
    {
        IReadOnlyList<SettingDescriptor> liveDescriptors = SettingsSchemaBuilder.Build(_liveSettings);

        foreach (SettingDescriptor liveDescriptor in liveDescriptors)
        {
            if (!ConfigCategories.Contains(liveDescriptor.Category) || liveDescriptor.Name == nameof(SpeedSettings.Wpm))
            {
                continue;
            }

            SettingDescriptor? match = workingDescriptors.FirstOrDefault(
                d => d.Category == liveDescriptor.Category && d.Name == liveDescriptor.Name);
            if (match is not null)
            {
                liveDescriptor.Setter(match.Getter());
            }
        }
    }
}
