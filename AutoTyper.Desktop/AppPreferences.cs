using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AutoTyper.Desktop;

/// <summary>
/// Application-level preferences that are not part of the typing engine's
/// settings schema: the visual <see cref="Theme"/> and whether the main
/// window stays <see cref="AlwaysOnTop"/>. Persisted as part of
/// <see cref="AppSettings"/> and edited on the Appearance tab of the Settings
/// window (<see cref="ConfigWindow"/>), which binds to a cloned working copy
/// and only copies the values back on Save. Raises <see cref="PropertyChanged"/>
/// so those bindings stay in sync.
/// </summary>
public class AppPreferences : INotifyPropertyChanged
{
    private AppTheme _theme = AppTheme.System;
    private bool _alwaysOnTop;

    public event PropertyChangedEventHandler? PropertyChanged;

    public AppTheme Theme
    {
        get => _theme;
        set => SetField(ref _theme, value);
    }

    public bool AlwaysOnTop
    {
        get => _alwaysOnTop;
        set => SetField(ref _alwaysOnTop, value);
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
