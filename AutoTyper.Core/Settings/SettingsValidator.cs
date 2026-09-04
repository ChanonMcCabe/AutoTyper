namespace AutoTyper.Core.Settings;

/// <summary>
/// Validates a flattened settings schema: generic Min/Max range checks from
/// each descriptor, plus cross-field rules from any group implementing
/// <see cref="IValidatableSetting"/>. Returns human-readable error messages;
/// an empty list means the settings are valid.
/// </summary>
public static class SettingsValidator
{
    public static IReadOnlyList<string> Validate(IReadOnlyList<SettingDescriptor> descriptors)
    {
        ArgumentNullException.ThrowIfNull(descriptors);

        var errors = new List<string>();

        foreach (SettingDescriptor descriptor in descriptors)
        {
            if (descriptor.Min is null && descriptor.Max is null)
            {
                continue;
            }

            object? value = descriptor.Getter();
            if (value is null || !IsNumeric(value))
            {
                continue;
            }

            double numeric = Convert.ToDouble(value);
            if (descriptor.Min is double min && numeric < min)
            {
                errors.Add($"{descriptor.DisplayName} must be at least {min}.");
            }

            if (descriptor.Max is double max && numeric > max)
            {
                errors.Add($"{descriptor.DisplayName} must be at most {max}.");
            }
        }

        foreach (object group in descriptors.Select(d => d.GroupInstance).Distinct())
        {
            if (group is IValidatableSetting validatable)
            {
                errors.AddRange(validatable.Validate());
            }
        }

        return errors;
    }

    private static bool IsNumeric(object value) => value is sbyte or byte or short or ushort
        or int or uint or long or ulong or float or double or decimal;
}
