namespace AutoTyper.Core.Settings;

/// <summary>
/// Marks a setting as only meaningful when a sibling property on the same
/// settings group currently equals <see cref="RequiredValue"/> — e.g.
/// <c>MaxWpm</c> only matters when <c>RangeModeEnabled</c> is true. The
/// settings panel uses this to hide dependent fields until their condition
/// is met, rather than showing controls that do nothing.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class DependsOnAttribute : Attribute
{
    public string PropertyName { get; }

    public object RequiredValue { get; }

    /// <param name="propertyName">Name of the sibling property this setting depends on.</param>
    /// <param name="requiredValue">Value that property must equal for this setting to apply. Defaults to <c>true</c>, the common case of depending on a sibling "Enabled" flag.</param>
    public DependsOnAttribute(string propertyName, object? requiredValue = null)
    {
        PropertyName = propertyName;
        RequiredValue = requiredValue ?? true;
    }
}
