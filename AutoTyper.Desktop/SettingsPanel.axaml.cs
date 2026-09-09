using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using AutoTyper.Core.Input;
using AutoTyper.Core.Settings;

namespace AutoTyper.Desktop;

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
    private readonly List<(SettingDescriptor Descriptor, Control Row)> _rows = [];

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
                RootPanel.Children.Add(new Expander
                {
                    Header = category.Key,
                    IsExpanded = true,
                    Margin = new Thickness(4),
                    Content = body,
                });
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
                    Control row = BuildRow(descriptor);
                    body.Children.Add(row);
                    _rows.Add((descriptor, row));
                }

                continue;
            }

            var section = new StackPanel();
            foreach (SettingDescriptor descriptor in group)
            {
                Control row = BuildRow(descriptor);
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

    private static Control BuildRow(SettingDescriptor descriptor)
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

    private static Control CreateInputControl(SettingDescriptor descriptor)
    {
        if (descriptor.PropertyType == typeof(bool))
        {
            var checkbox = new CheckBox { VerticalAlignment = VerticalAlignment.Center };
            checkbox.Bind(ToggleButton.IsCheckedProperty, Bind(descriptor, BindingMode.TwoWay));
            return checkbox;
        }

        if (descriptor.PropertyType == typeof(HotkeyCombo?))
        {
            var box = new HotkeyCaptureBox { Width = 180, HorizontalAlignment = HorizontalAlignment.Left };
            box.Bind(HotkeyCaptureBox.ComboProperty, Bind(descriptor, BindingMode.TwoWay));
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
            slider.Bind(RangeBase.ValueProperty, Bind(descriptor, BindingMode.TwoWay));

            var valueText = new TextBlock
            {
                Width = 50,
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            valueText.Bind(TextBlock.TextProperty, Bind(descriptor, BindingMode.OneWay));

            panel.Children.Add(slider);
            panel.Children.Add(valueText);
            return panel;
        }

        var textBox = new TextBox { Width = 180 };
        textBox.Bind(TextBox.TextProperty, Bind(descriptor, BindingMode.TwoWay));
        return textBox;
    }

    /// <remarks>
    /// Avalonia has no <c>UpdateSourceTrigger</c>: a two-way binding writes back
    /// as the value changes. That is what the checkbox and slider already asked
    /// for explicitly in the WPF build, and it makes the text box consistent
    /// with them — a <c>DependsOn</c> rule driven by a text field now reacts as
    /// you type rather than waiting for focus to leave.
    /// </remarks>
    private static Binding Bind(SettingDescriptor descriptor, BindingMode mode) =>
        new(descriptor.Name) { Source = descriptor.GroupInstance, Mode = mode };

    private static bool IsNumeric(Type type) =>
        type == typeof(int) || type == typeof(double) || type == typeof(float) || type == typeof(long);

    private void RefreshVisibility()
    {
        foreach ((SettingDescriptor descriptor, Control row) in _rows)
        {
            row.IsVisible = descriptor.DependsOnGetter is null
                || Equals(descriptor.DependsOnGetter(), descriptor.DependsOnValue);
        }
    }
}
