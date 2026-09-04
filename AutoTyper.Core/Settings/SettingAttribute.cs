namespace AutoTyper.Core.Settings;

/// <summary>
/// Marks a property as a user-configurable setting discoverable by
/// <see cref="SettingsSchemaBuilder"/>. Applied to a leaf property on a
/// settings group class (e.g. <c>SpeedSettings.Wpm</c>).
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class SettingAttribute : Attribute
{
    /// <summary>Section this setting is grouped under in the UI (e.g. "Speed", "Typos").</summary>
    public string Category { get; init; } = "General";

    /// <summary>
    /// Optional sub-section within a <see cref="Category"/>. Settings that share a
    /// non-null <see cref="Group"/> are rendered together in a collapsible section
    /// (in first-seen order); settings with no group render flat. Used to keep a
    /// long tab like Advanced from becoming one undifferentiated wall of rows.
    /// </summary>
    public string? Group { get; init; }

    /// <summary>Human-readable label. Defaults to the property name if omitted.</summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Inclusive lower bound for numeric settings, or <see cref="double.NaN"/>
    /// for unbounded. Attribute parameters can't be nullable value types, so
    /// NaN is the "unspecified" sentinel — <see cref="SettingsSchemaBuilder"/>
    /// converts it to a null <see cref="SettingDescriptor.Min"/>.
    /// </summary>
    public double Min { get; init; } = double.NaN;

    /// <summary>Inclusive upper bound for numeric settings, or <see cref="double.NaN"/> for unbounded.</summary>
    public double Max { get; init; } = double.NaN;

    /// <summary>When true, hidden behind the settings panel's "show advanced" toggle.</summary>
    public bool Advanced { get; init; }
}
