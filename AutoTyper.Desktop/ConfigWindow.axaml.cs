using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using AutoTyper.Core.Settings;

namespace AutoTyper.Desktop;

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
public partial class ConfigWindow : Window
{
    private static readonly string[] ConfigCategories = ["General", "Typing", "Advanced"];

    private readonly AppSettings _liveSettings;
    private readonly AppSettings _workingCopy;
    private bool _presetLoaded;

    public ConfigWindow(AppSettings liveSettings)
    {
        InitializeComponent();

        _liveSettings = liveSettings;
        _workingCopy = SettingsService.Clone(liveSettings);

        AppearanceTab.DataContext = _workingCopy.Preferences;

        RebindAllPanels();
        RefreshPresetList();
    }

    /// <summary>Parameterless constructor for the XAML previewer only.</summary>
    public ConfigWindow()
        : this(new AppSettings())
    {
    }

    private void RefreshPresetList()
    {
        PresetComboBox.Items.Clear();
        PresetComboBox.Items.Add("— Select a preset —");
        foreach (var preset in _liveSettings.Presets)
        {
            PresetComboBox.Items.Add(preset.Name);
        }
        PresetComboBox.SelectedIndex = 0;
    }

    private void PresetComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // Prevent selection of the header item
        if (PresetComboBox.SelectedIndex <= 0)
        {
            PresetComboBox.SelectedIndex = 0;
        }
    }

    /// <summary>
    /// Rebinds all settings panels to reflect current working copy values.
    /// </summary>
    private void RebindAllPanels()
    {
        IReadOnlyList<SettingDescriptor> descriptors = SettingsSchemaBuilder.Build(_workingCopy);
        GeneralPanel.SetDescriptors(descriptors
            .Where(d => d.Category == "General" && d.Name != nameof(SpeedSettings.Wpm))
            .ToList());
        TypingPanel.SetDescriptors(descriptors.Where(d => d.Category == "Typing").ToList());
        AdvancedPanel.SetDescriptors(descriptors.Where(d => d.Category == "Advanced").ToList());
    }

    private void LoadPresetButton_Click(object? sender, RoutedEventArgs e)
    {
        if (PresetComboBox.SelectedIndex <= 0)
        {
            ShowError("No preset selected", "Please select a preset to load.");
            return;
        }

        _liveSettings.Presets[PresetComboBox.SelectedIndex - 1].ApplyTo(_workingCopy);
        RebindAllPanels();
        _presetLoaded = true;
    }

    private async void SavePresetButton_Click(object? sender, RoutedEventArgs e)
    {
        // Validate current settings before saving
        IReadOnlyList<SettingDescriptor> workingDescriptors = SettingsSchemaBuilder.Build(_workingCopy);
        IReadOnlyList<string> errors = SettingsValidator.Validate(workingDescriptors);

        if (errors.Count > 0)
        {
            ShowError("Invalid settings", $"Cannot save preset with invalid settings: {string.Join(" ", errors)}");
            return;
        }

        // Show dialog to get preset name
        var textBox = new TextBox { MinWidth = 300, Margin = new(0, 8, 0, 0) };
        var dialog = new ContentDialog
        {
            Title = "Save Preset",
            PrimaryButtonText = "Save",
            SecondaryButtonText = "Cancel",
            Content = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = "Preset name:" },
                    textBox
                }
            }
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        if (textBox.Text is not { Length: > 0 } name)
        {
            ShowError("Invalid name", "Please enter a preset name.");
            return;
        }

        var preset = TypingPreset.FromSettings(name, _workingCopy);

        // Check if a preset with this name already exists and overwrite it
        var existing = _liveSettings.Presets.FirstOrDefault(p => p.Name == name);
        if (existing is not null)
        {
            _liveSettings.Presets.Remove(existing);
        }

        _liveSettings.Presets.Add(preset);

        try
        {
            SettingsService.Save(_liveSettings);
            RefreshPresetList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowError("Couldn't save preset", ex.Message);
        }
    }

    private void DeletePresetButton_Click(object? sender, RoutedEventArgs e)
    {
        if (PresetComboBox.SelectedIndex <= 0)
        {
            ShowError("No preset selected", "Please select a preset to delete.");
            return;
        }

        var preset = _liveSettings.Presets[PresetComboBox.SelectedIndex - 1];
        _liveSettings.Presets.Remove(preset);

        try
        {
            SettingsService.Save(_liveSettings);
            RefreshPresetList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowError("Couldn't delete preset", ex.Message);
        }
    }

    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        IReadOnlyList<SettingDescriptor> workingDescriptors = SettingsSchemaBuilder.Build(_workingCopy);
        IReadOnlyList<string> errors = SettingsValidator.Validate(workingDescriptors);

        if (errors.Count > 0)
        {
            ShowError("Invalid settings", string.Join(" ", errors));
            return;
        }

        ErrorText.IsOpen = false;

        CommitWorkingCopyToLiveSettings(workingDescriptors, includeWpm: _presetLoaded);

        // AppPreferences is not part of the schema, so commit it by hand.
        _liveSettings.Preferences.Theme = _workingCopy.Preferences.Theme;
        _liveSettings.Preferences.AlwaysOnTop = _workingCopy.Preferences.AlwaysOnTop;

        try
        {
            SettingsService.Save(_liveSettings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowError("Couldn't save settings", ex.Message);
            return; // keep the dialog open so the failure is visible
        }

        Close(true);
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e) => Close(false);

    private void ShowError(string title, string message)
    {
        ErrorText.Title = title;
        ErrorText.Message = message;
        ErrorText.IsOpen = true;
    }

    private void CommitWorkingCopyToLiveSettings(IReadOnlyList<SettingDescriptor> workingDescriptors, bool includeWpm = false)
    {
        IReadOnlyList<SettingDescriptor> liveDescriptors = SettingsSchemaBuilder.Build(_liveSettings);

        foreach (SettingDescriptor liveDescriptor in liveDescriptors)
        {
            if (!ConfigCategories.Contains(liveDescriptor.Category))
            {
                continue;
            }

            // Skip WPM unless a preset was just loaded (in which case commit it)
            if (liveDescriptor.Name == nameof(SpeedSettings.Wpm) && !includeWpm)
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
