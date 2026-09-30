using Avalonia.Data.Converters;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AutoTyper.Desktop;

/// <summary>
/// Converts enum values to user-friendly display names by inserting spaces
/// between PascalCase/camelCase words. For example, "AhkClassic" becomes
/// "Ahk Classic" and "System" remains "System".
/// </summary>
public class EnumDisplayNameConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        if (value is null)
            return null;

        var enumValue = value.ToString();
        if (enumValue is null)
            return null;

        // Insert spaces before capital letters that follow lowercase letters
        // or before sequences of capitals followed by a lowercase letter
        // e.g., "AhkClassic" -> "Ahk Classic", "ABCDef" -> "AB Cdef"
        var spaced = Regex.Replace(enumValue, @"([a-z])([A-Z])", "$1 $2");
        spaced = Regex.Replace(spaced, @"([A-Z]+)([A-Z][a-z])", "$1 $2");

        return spaced;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        throw new NotSupportedException();
    }
}
