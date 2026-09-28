using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using AutoTyper.Core.Settings;
using System.Text.Json;

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
        _workingCopy = AppSettingsCloner.Clone(liveSettings);

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
    /// Clones a settings group via JSON serialization to ensure deep copy.
    /// </summary>
    private T CloneSettingsGroup<T>(T source) where T : new()
    {
        return JsonSerializer.Deserialize<T>(
            JsonSerializer.Serialize(source, SettingsService.JsonOptions),
            SettingsService.JsonOptions) ?? new();
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

        var preset = _liveSettings.Presets[PresetComboBox.SelectedIndex - 1];
        LoadPresetIntoWorkingCopy(preset);
        _presetLoaded = true;
    }

    private void LoadPresetIntoWorkingCopy(TypingPreset preset)
    {
        try
        {
            _workingCopy.Speed = CloneSettingsGroup(preset.Speed);
            _workingCopy.Bursts = CloneSettingsGroup(preset.Bursts);
            _workingCopy.Typos = CloneSettingsGroup(preset.Typos);
            _workingCopy.Pauses = CloneSettingsGroup(preset.Pauses);
            _workingCopy.StepAway = CloneSettingsGroup(preset.StepAway);
            _workingCopy.Formatting = CloneSettingsGroup(preset.Formatting);
            _workingCopy.Run = CloneSettingsGroup(preset.Run);

            RebindAllPanels();
        }
        catch (Exception ex)
        {
            ShowError("Failed to load preset", ex.Message);
        }
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

        // Create preset from current working copy (deep copied via JSON serialization)
        var preset = new TypingPreset
        {
            Name = name,
            Speed = CloneSettingsGroup(_workingCopy.Speed),
            Bursts = CloneSettingsGroup(_workingCopy.Bursts),
            Typos = CloneSettingsGroup(_workingCopy.Typos),
            Pauses = CloneSettingsGroup(_workingCopy.Pauses),
            StepAway = CloneSettingsGroup(_workingCopy.StepAway),
            Formatting = CloneSettingsGroup(_workingCopy.Formatting),
            Run = CloneSettingsGroup(_workingCopy.Run),
        };

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
