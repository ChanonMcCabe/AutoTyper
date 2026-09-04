using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AutoTyper.Core.Settings;

/// <summary>
/// Base for settings group classes (<c>SpeedSettings</c>, <c>TypoSettings</c>,
/// etc.). Raises <see cref="PropertyChanged"/> on every setter so a bound UI
/// — or a <c>DependsOn</c> visibility check — stays in sync automatically.
/// </summary>
public abstract class SettingsGroupBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
