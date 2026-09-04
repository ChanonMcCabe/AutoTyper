using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using AutoTyper.Core.Settings;

namespace AutoTyper.App;

/// <summary>
/// Renders any <see cref="SettingDescriptor"/> list as an editable form: one
/// input control per setting chosen by its type (checkbox for bool, slider
/// for a ranged number, the hotkey capture box for <see cref="HotkeyCombo"/>,
/// a plain text box otherwise). If the descriptors span more than one
/// <see cref="SettingDescriptor.Category"/> — e.g. when not pre-scoped to a
/// single Config tab — each category gets its own expandable section;
/// otherwise rows are listed flat. Adding a new [Setting] property to a
/// settings group is enough for it to appear here — nothing in this control
/// needs to change. Bindings go straight to the descriptor's own group
/// instance, so edits apply immediately and <c>DependsOn</c> visibility
/// reacts to live changes via INotifyPropertyChanged on that group.
/// </summary>
public partial class SettingsPanel : UserControl
{
    private readonly List<(SettingDescriptor Descriptor, FrameworkElement Row)> _rows = [];

    public SettingsPanel()
    {
        InitializeComponent();
    }

    public void SetDescriptors(IReadOnlyList<SettingDescriptor> descriptors)
    {
        ArgumentNullException.ThrowIfNull(descriptors);

        foreach (object group in descriptors.Select(d => d.GroupInstance).Distinct())
        {
            if (group is INotifyPropertyChanged notifier)
            {
                notifier.PropertyChanged += (_, _) => RefreshVisibility();
            }
        }

        RootPanel.Children.Clear();
        _rows.Clear();

        var categories = descriptors.GroupBy(d => d.Category).ToList();
        bool singleCategory = categories.Count <= 1;

        foreach (IGrouping<string, SettingDescriptor> category in categories)
        {
            var body = new StackPanel();

            AddRows(body, category);

            if (singleCategory)
            {
                RootPanel.Children.Add(body);
            }
            else
            {
                var expander = new Expander { Header = category.Key, IsExpanded = true, Margin = new Thickness(4), Content = body };
                RootPanel.Children.Add(expander);
            }
        }

        RefreshVisibility();
    }

    /// <summary>
    /// Appends a category's rows to <paramref name="body"/>. Descriptors with no
    /// <see cref="SettingDescriptor.Group"/> are added as flat rows; each distinct
    /// group (in first-seen order) becomes its own collapsed <see cref="Expander"/>
    /// so a long category isn't one undifferentiated wall of controls.
    /// </summary>
    private void AddRows(Panel body, IEnumerable<SettingDescriptor> descriptors)
    {
        foreach (IGrouping<string?, SettingDescriptor> group in descriptors.GroupBy(d => d.Group))
        {
            if (group.Key is null)
            {
                foreach (SettingDescriptor descriptor in group)
                {
                    FrameworkElement row = BuildRow(descriptor);
                    body.Children.Add(row);
                    _rows.Add((descriptor, row));
                }

                continue;
            }

            var section = new StackPanel();
            foreach (SettingDescriptor descriptor in group)
            {
                FrameworkElement row = BuildRow(descriptor);
                section.Children.Add(row);
                _rows.Add((descriptor, row));
            }

            body.Children.Add(new Expander
            {
                Header = group.Key,
                IsExpanded = false,
                Margin = new Thickness(0, 4, 0, 4),
                Content = section,
            });
        }
    }

    private static FrameworkElement BuildRow(SettingDescriptor descriptor)
    {
        var row = new DockPanel { Margin = new Thickness(4, 2, 4, 2) };

        var label = new TextBlock
        {
            Text = descriptor.DisplayName,
            Width = 220,
            VerticalAlignment = VerticalAlignment.Center,
        };
        DockPanel.SetDock(label, Dock.Left);
        row.Children.Add(label);

        row.Children.Add(CreateInputControl(descriptor));
        return row;
    }

    private static FrameworkElement CreateInputControl(SettingDescriptor descriptor)
    {
        if (descriptor.PropertyType == typeof(bool))
        {
            var checkbox = new CheckBox { VerticalAlignment = VerticalAlignment.Center };
            checkbox.SetBinding(
                ToggleButton.IsCheckedProperty,
                new Binding(descriptor.Name) { Source = descriptor.GroupInstance, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            return checkbox;
        }

        if (descriptor.PropertyType == typeof(HotkeyCombo?))
        {
            var box = new HotkeyCaptureBox { Width = 180, HorizontalAlignment = HorizontalAlignment.Left };
            box.SetBinding(
                HotkeyCaptureBox.ComboProperty,
                new Binding(descriptor.Name) { Source = descriptor.GroupInstance, Mode = BindingMode.TwoWay });
            return box;
        }

        if (IsNumeric(descriptor.PropertyType) && descriptor.Min.HasValue && descriptor.Max.HasValue)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            bool isInteger = descriptor.PropertyType == typeof(int);

            var slider = new Slider
            {
                Minimum = descriptor.Min.Value,
                Maximum = descriptor.Max.Value,
                Width = 160,
                VerticalAlignment = VerticalAlignment.Center,
                IsSnapToTickEnabled = isInteger,
                TickFrequency = isInteger ? 1 : 0,
            };
            slider.SetBinding(
                RangeBase.ValueProperty,
                new Binding(descriptor.Name) { Source = descriptor.GroupInstance, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });

            var valueText = new TextBlock { Width = 50, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            valueText.SetBinding(TextBlock.TextProperty, new Binding(descriptor.Name) { Source = descriptor.GroupInstance, Mode = BindingMode.OneWay });

            panel.Children.Add(slider);
            panel.Children.Add(valueText);
            return panel;
        }

        var textBox = new TextBox { Width = 180 };
        textBox.SetBinding(
            TextBox.TextProperty,
            new Binding(descriptor.Name) { Source = descriptor.GroupInstance, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.LostFocus });
        return textBox;
    }

    private static bool IsNumeric(Type type) =>
        type == typeof(int) || type == typeof(double) || type == typeof(float) || type == typeof(long);

    private void RefreshVisibility()
    {
        foreach ((SettingDescriptor descriptor, FrameworkElement row) in _rows)
        {
            bool visible = descriptor.DependsOnGetter is null || Equals(descriptor.DependsOnGetter(), descriptor.DependsOnValue);
            row.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
