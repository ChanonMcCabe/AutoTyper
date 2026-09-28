using System.Reflection;

namespace AutoTyper.Core.Settings;

/// <summary>
/// Reflects over a settings object graph — a root object whose properties
/// are settings-group objects (which may themselves nest further groups)
/// with leaf properties marked <see cref="SettingAttribute"/> — and flattens
/// it into a list of <see cref="SettingDescriptor"/>. Adding a new setting
/// to a group class is enough for it to appear in any UI built on this list;
/// nothing needs to be wired by hand.
/// </summary>
public static class SettingsSchemaBuilder
{
    public static IReadOnlyList<SettingDescriptor> Build(object root)
    {
        ArgumentNullException.ThrowIfNull(root);

        var descriptors = new List<SettingDescriptor>();
        Visit(root, descriptors, new HashSet<object>(ReferenceEqualityComparer.Instance));
        return descriptors;
    }

    private static void Visit(object instance, List<SettingDescriptor> descriptors, HashSet<object> visited)
    {
        if (!visited.Add(instance))
        {
            return;
        }

        Type type = instance.GetType();
        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var settingAttr = property.GetCustomAttribute<SettingAttribute>();
            if (settingAttr is not null)
            {
                descriptors.Add(BuildDescriptor(instance, type, property, settingAttr));
                continue;
            }

            if (property.PropertyType.IsValueType
                || property.PropertyType == typeof(string)
                || !property.CanRead
                || property.GetIndexParameters().Length > 0
                || typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType))
            {
                continue;
            }

            object? nested = property.GetValue(instance);
            if (nested is not null)
            {
                Visit(nested, descriptors, visited);
            }
        }
    }

    private static SettingDescriptor BuildDescriptor(object instance, Type type, PropertyInfo property, SettingAttribute settingAttr)
    {
        var dependsOnAttr = property.GetCustomAttribute<DependsOnAttribute>();
        Func<object?>? dependsOnGetter = null;
        if (dependsOnAttr is not null)
        {
            PropertyInfo? dependsOnProperty = type.GetProperty(dependsOnAttr.PropertyName);
            if (dependsOnProperty is not null)
            {
                dependsOnGetter = () => dependsOnProperty.GetValue(instance);
            }
        }

        return new SettingDescriptor
        {
            Name = property.Name,
            Category = settingAttr.Category,
            Group = settingAttr.Group,
            DisplayName = settingAttr.DisplayName ?? property.Name,
            PropertyType = property.PropertyType,
            Min = double.IsNaN(settingAttr.Min) ? null : settingAttr.Min,
            Max = double.IsNaN(settingAttr.Max) ? null : settingAttr.Max,
            Advanced = settingAttr.Advanced,
            DependsOnProperty = dependsOnAttr?.PropertyName,
            DependsOnValue = dependsOnAttr?.RequiredValue,
            DependsOnGetter = dependsOnGetter,
            GroupInstance = instance,
            Getter = () => property.GetValue(instance),
            Setter = value => property.SetValue(instance, value),
        };
    }
}
