using System.Text.Json;
using System.Text.Json.Serialization;
using AutoTyper.Core.Settings;

namespace AutoTyper.Core.Tests;

public class SettingsSchemaTests
{
    [Fact]
    public void Build_ReflectsAllAttributedPropertiesAcrossNestedGroups()
    {
        var options = new TypingOptions();

        IReadOnlyList<SettingDescriptor> descriptors = SettingsSchemaBuilder.Build(options);

        Assert.Contains(descriptors, d => d.Name == nameof(SpeedSettings.Wpm) && d.Category == "General");
        Assert.Contains(descriptors, d => d.Name == nameof(TypoSettings.ProbabilityPercent) && d.Category == "Typing");
        Assert.Contains(descriptors, d => d.Name == nameof(BurstSettings.BurstWordCount) && d.Category == "Advanced");
        Assert.Contains(descriptors, d => d.Name == nameof(PauseSettings.JitterPercent) && d.Category == "Advanced");
        Assert.Contains(descriptors, d => d.Name == nameof(FormattingSettings.PreserveSpacing) && d.Category == "Typing");
    }

    [Fact]
    public void Build_DescriptorDelegatesReadAndWriteTheUnderlyingProperty()
    {
        var options = new TypingOptions();
        IReadOnlyList<SettingDescriptor> descriptors = SettingsSchemaBuilder.Build(options);
        SettingDescriptor wpmDescriptor = descriptors.Single(d => d.Name == nameof(SpeedSettings.Wpm));

        wpmDescriptor.Setter(75);

        Assert.Equal(75, options.Speed.Wpm);
        Assert.Equal(75, wpmDescriptor.Getter());
    }

    [Fact]
    public void Build_ResolvesDependsOnGetterToSiblingPropertyValue()
    {
        var options = new TypingOptions();
        options.Speed.RangeModeEnabled = true;
        IReadOnlyList<SettingDescriptor> descriptors = SettingsSchemaBuilder.Build(options);
        SettingDescriptor minWpm = descriptors.Single(d => d.Name == nameof(SpeedSettings.MinWpm));

        Assert.Equal(nameof(SpeedSettings.RangeModeEnabled), minWpm.DependsOnProperty);
        Assert.Equal(true, minWpm.DependsOnGetter?.Invoke());
    }

    [Fact]
    public void Validate_RejectsRangeModeAndTimeframeModeBothEnabled()
    {
        var options = new TypingOptions();
        options.Speed.RangeModeEnabled = true;
        options.Speed.TimeframeModeEnabled = true;
        IReadOnlyList<SettingDescriptor> descriptors = SettingsSchemaBuilder.Build(options);

        IReadOnlyList<string> errors = SettingsValidator.Validate(descriptors);

        Assert.Contains(errors, e => e.Contains("Type Frame"));
    }

    [Fact]
    public void Validate_RejectsMinWpmGreaterThanMaxWpm()
    {
        var options = new TypingOptions();
        options.Speed.RangeModeEnabled = true;
        options.Speed.MinWpm = 80;
        options.Speed.MaxWpm = 40;
        IReadOnlyList<SettingDescriptor> descriptors = SettingsSchemaBuilder.Build(options);

        IReadOnlyList<string> errors = SettingsValidator.Validate(descriptors);

        Assert.Contains(errors, e => e.Contains("Minimum WPM"));
    }

    [Fact]
    public void Validate_RejectsOutOfRangeNumericValue()
    {
        var options = new TypingOptions();
        options.Speed.Wpm = 10_000; // Wpm's [Setting] Max is 300.
        IReadOnlyList<SettingDescriptor> descriptors = SettingsSchemaBuilder.Build(options);

        IReadOnlyList<string> errors = SettingsValidator.Validate(descriptors);

        Assert.Contains(errors, e => e.Contains("Words Per Minute"));
    }

    [Fact]
    public void Validate_RejectsLongPauseMinGreaterThanMax()
    {
        var options = new TypingOptions();
        options.Pauses.LongPauseMinMs = 2000;
        options.Pauses.LongPauseMaxMs = 500;

        IReadOnlyList<string> errors = SettingsValidator.Validate(SettingsSchemaBuilder.Build(options));

        Assert.Contains(errors, e => e.Contains("Long Pause"));
    }

    [Fact]
    public void Validate_RejectsInvertedBurstRangesOnlyWhileBurstsEnabled()
    {
        var options = new TypingOptions();
        options.Bursts.PreBurstPauseMinMs = 500;
        options.Bursts.PreBurstPauseMaxMs = 100;
        options.Bursts.PostBurstPauseMinMs = 900;
        options.Bursts.PostBurstPauseMaxMs = 100;
        options.Bursts.CooldownMinMs = 9000;
        options.Bursts.CooldownMaxMs = 1000;

        Assert.Empty(SettingsValidator.Validate(SettingsSchemaBuilder.Build(options)));

        options.Bursts.Enabled = true;
        IReadOnlyList<string> errors = SettingsValidator.Validate(SettingsSchemaBuilder.Build(options));

        Assert.Contains(errors, e => e.Contains("Pre-Burst"));
        Assert.Contains(errors, e => e.Contains("Post-Burst"));
        Assert.Contains(errors, e => e.Contains("Cooldown"));
    }

    [Fact]
    public void Validate_RejectsInvertedNoticeDelayOnlyWhileTyposEnabled()
    {
        var options = new TypingOptions();
        options.Typos.NoticeDelayMinMs = 900;
        options.Typos.NoticeDelayMaxMs = 100;

        Assert.Contains(SettingsValidator.Validate(SettingsSchemaBuilder.Build(options)), e => e.Contains("Notice Delay"));

        options.Typos.Enabled = false;
        Assert.Empty(SettingsValidator.Validate(SettingsSchemaBuilder.Build(options)));
    }

    [Fact]
    public void Validate_AcceptsDefaultSettings()
    {
        var options = new TypingOptions();
        IReadOnlyList<SettingDescriptor> descriptors = SettingsSchemaBuilder.Build(options);

        IReadOnlyList<string> errors = SettingsValidator.Validate(descriptors);

        Assert.Empty(errors);
    }

    [Fact]
    public void Deserialize_OldJsonMissingNewerGroupsAndFields_FillsInDefaults()
    {
        // Simulates a settings file saved before BurstSettings (and
        // TypoSettings.FullWordTypoRatioPercent) existed: the JSON only has a
        // partial "Speed" object. This is the same
        // deserialize-onto-a-defaulted-instance pattern AutoTyper.Desktop's
        // SettingsService relies on for graceful migration.
        const string oldJson = """
            {
              "Speed": { "Wpm": 55 }
            }
            """;

        var jsonOptions = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } };
        TypingOptions? loaded = JsonSerializer.Deserialize<TypingOptions>(oldJson, jsonOptions);

        Assert.NotNull(loaded);
        Assert.Equal(55, loaded.Speed.Wpm);

        // Fields and whole groups absent from the old JSON fall back to defaults.
        Assert.False(loaded.Speed.RangeModeEnabled);
        Assert.NotNull(loaded.Bursts);
        Assert.False(loaded.Bursts.Enabled);
        Assert.Equal(4.0, loaded.Typos.ProbabilityPercent);
        Assert.Equal(35.0, loaded.Typos.FullWordTypoRatioPercent);
        Assert.NotNull(loaded.Formatting);
        Assert.True(loaded.Formatting.PreserveSpacing);
    }
}
