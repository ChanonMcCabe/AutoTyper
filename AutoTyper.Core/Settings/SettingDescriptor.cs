namespace AutoTyper.Core.Settings;

/// <summary>
/// A single discovered setting: everything a generic UI needs to render and
/// edit it without knowing the concrete settings class it came from.
/// </summary>
public sealed class SettingDescriptor
{
    public required string Name { get; init; }

    public required string Category { get; init; }

    /// <summary>Optional collapsible sub-section within <see cref="Category"/>; null means render flat.</summary>
    public string? Group { get; init; }

    public required string DisplayName { get; init; }

    public required Type PropertyType { get; init; }

    public double? Min { get; init; }

    public double? Max { get; init; }

    public bool Advanced { get; init; }

    public string? DependsOnProperty { get; init; }

    public object? DependsOnValue { get; init; }

    /// <summary>Reads the sibling property named by <see cref="DependsOnProperty"/>, or null if this setting has no dependency.</summary>
    public Func<object?>? DependsOnGetter { get; init; }

    /// <summary>The settings group instance this property belongs to — usable as a WPF Binding source.</summary>
    public required object GroupInstance { get; init; }

    public required Func<object?> Getter { get; init; }

    public required Action<object?> Setter { get; init; }
}
