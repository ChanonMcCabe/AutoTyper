using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using AutoTyper.Core;
using AutoTyper.Core.Input;
using AutoTyper.Core.Settings;

namespace AutoTyper.Desktop;

public partial class MainWindow : Window
{
    private static readonly HotkeyCombo CancelCombo = new(HotkeyModifiers.None, HotkeyKey.Escape);

    private readonly MainViewModel _viewModel = new();
    private readonly TypingEngine _typingEngine = new();
    private readonly AppSettings _appSettings;
    private readonly IKeySender? _keySender;
    private readonly IHotkeyProvider? _hotkeyProvider;

    private int? _triggerHotkeyId;
    private int? _cancelHotkeyId;
    private int? _pauseHotkeyId;
    private CancellationTokenSource? _typingCts;
    private TypingRunController? _runController;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;

        _appSettings = (Application.Current as App)?.Settings
            ?? throw new InvalidOperationException("Application.Current must be initialized as App before MainWindow.");

        Topmost = _appSettings.Preferences.AlwaysOnTop;

        _viewModel.PassageText = _appSettings.PassageText;
        _viewModel.HasPassage = !string.IsNullOrWhiteSpace(_appSettings.PassageText);
        _viewModel.EditableWpm = _appSettings.Speed.Wpm;
        _viewModel.HasHotkey = _appSettings.Hotkey.Combo.HasValue;

        HotkeyDisplay.Bind(
            HotkeyCaptureBox.ComboProperty,
            new Binding(nameof(HotkeySettings.Combo)) { Source = _appSettings.Hotkey, Mode = BindingMode.TwoWay });

        // Set up drag-and-drop on the passage TextBox
        var passageTextBox = this.FindControl<TextBox>("PassageTextBox");
        if (passageTextBox is not null)
        {
            DragDrop.SetAllowDrop(passageTextBox, true);
            passageTextBox.AddHandler(DragDrop.DropEvent, OnPassageTextBoxDrop);
        }

        _appSettings.Hotkey.PropertyChanged += (_, _) => _viewModel.HasHotkey = _appSettings.Hotkey.Combo.HasValue;
        _appSettings.Speed.PropertyChanged += (_, _) =>
        {
            UpdateModeIndicator();
            UpdateEstimateText();
        };
        _appSettings.Pauses.PropertyChanged += (_, _) => UpdateEstimateText();
        _appSettings.StepAway.PropertyChanged += (_, _) => UpdateEstimateText();
        UpdateModeIndicator();
        UpdateEstimateText();

        // HasPassage drives whether Activate is enabled, so it has to track the
        // text box rather than only being recomputed on Save — otherwise typing
        // a passage and pressing Activate leaves the button greyed out.
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        _viewModel.ActivateRequested += OnActivateRequested;
        _viewModel.DeactivateRequested += OnDeactivateRequested;
        _viewModel.StopTypingRequested += (_, _) => _typingCts?.Cancel();
        _viewModel.SaveSettingsRequested += OnSaveSettingsRequested;
        _viewModel.CancelSettingsRequested += OnCancelSettingsRequested;
        _viewModel.PauseResumeRequested += OnPauseResumeRequested;
        _viewModel.LoadPassageRequested += OnLoadPassageRequested;

        if (!PlatformServices.IsSupported)
        {
            _viewModel.StatusText = PlatformServices.UnsupportedReason ?? "This platform is not supported.";
            return;
        }

        _keySender = PlatformServices.CreateKeySender(() => TryGetPlatformHandle()?.Handle ?? IntPtr.Zero);
        _hotkeyProvider = PlatformServices.CreateHotkeyProvider();
        _hotkeyProvider.HotkeyPressed += OnHotkeyPressed;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.PassageText))
        {
            _viewModel.HasPassage = !string.IsNullOrWhiteSpace(_viewModel.PassageText);
            UpdateEstimateText();
        }
        else if (e.PropertyName == nameof(MainViewModel.EditableWpm))
        {
            UpdateEstimateText();
        }
    }

    private void UpdateModeIndicator()
    {
        SpeedSettings speed = _appSettings.Speed;
        _viewModel.IsWpmFieldEnabled = !speed.RangeModeEnabled && !speed.TimeframeModeEnabled;
        _viewModel.ModeText = speed.TimeframeModeEnabled
            ? $"Mode: Type Frame ({speed.FrameMinutes} min)"
            : speed.RangeModeEnabled
                ? $"Mode: WPM Range ({speed.MinWpm}-{speed.MaxWpm})"
                : "Mode: Fixed WPM";
    }

    private void UpdateEstimateText()
    {
        if (string.IsNullOrWhiteSpace(_viewModel.PassageText))
        {
            _viewModel.EstimateText = string.Empty;
            return;
        }

        SpeedSettings saved = _appSettings.Speed;

        // Estimate against the WPM field as currently edited, not the last
        // saved value, so the estimate tracks what the user is typing there.
        var options = new TypingOptions
        {
            Speed = new SpeedSettings
            {
                Wpm = _viewModel.EditableWpm,
                RangeModeEnabled = saved.RangeModeEnabled,
                MinWpm = saved.MinWpm,
                MaxWpm = saved.MaxWpm,
                TimeframeModeEnabled = saved.TimeframeModeEnabled,
                FrameMinutes = saved.FrameMinutes,
            },
            StepAway = _appSettings.StepAway,
        };

        TimeSpan duration = SpeedResolver.EstimateDuration(_viewModel.PassageText, options);
        _viewModel.EstimateText = saved.TimeframeModeEnabled
            ? $"Estimated time: ≈ {FormatDuration(duration)}"
            : $"Estimated time: ≈ {FormatDuration(duration)} at {(saved.RangeModeEnabled ? $"~{(saved.MinWpm + saved.MaxWpm) / 2}" : _viewModel.EditableWpm.ToString())} WPM";
    }

    private void OnSaveSettingsRequested(object? sender, EventArgs e)
    {
        IReadOnlyList<SettingDescriptor> descriptors = SettingsSchemaBuilder.Build(_appSettings);
        SettingDescriptor wpmDescriptor = descriptors.Single(d => d.Category == "General" && d.Name == nameof(SpeedSettings.Wpm));

        bool belowMin = wpmDescriptor.Min is double min && _viewModel.EditableWpm < min;
        bool aboveMax = wpmDescriptor.Max is double max && _viewModel.EditableWpm > max;
        if (belowMin || aboveMax)
        {
            _viewModel.StatusText = $"{wpmDescriptor.DisplayName} must be between {wpmDescriptor.Min} and {wpmDescriptor.Max}.";
            return;
        }

        _appSettings.Speed.Wpm = _viewModel.EditableWpm;
        _appSettings.PassageText = _viewModel.PassageText;
        _viewModel.HasPassage = !string.IsNullOrWhiteSpace(_appSettings.PassageText);

        try
        {
            SettingsService.Save(_appSettings);
            _viewModel.StatusText = "Settings saved.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _viewModel.StatusText = $"Couldn't save settings: {ex.Message}";
        }
    }

    private void OnCancelSettingsRequested(object? sender, EventArgs e)
    {
        _viewModel.PassageText = _appSettings.PassageText;
        _viewModel.EditableWpm = _appSettings.Speed.Wpm;
        _viewModel.StatusText = "Edits discarded.";
    }

    private async void OnPassageTextBoxDrop(object? sender, DragEventArgs e)
    {
        if (e.DataTransfer.TryGetFile() is IStorageFile file)
        {
            e.Handled = true;
            await LoadPassageFromFileAsync(file);
        }
    }

    /// <summary>
    /// Replaces the passage box's text with <paramref name="file"/>'s contents
    /// as a staged edit — same as typing it in, so it is still never persisted.
    /// </summary>
    private async Task LoadPassageFromFileAsync(IStorageFile file)
    {
        const long maxBytes = 1_000_000;

        try
        {
            StorageItemProperties info = await file.GetBasicPropertiesAsync();
            if (info.Size > maxBytes)
            {
                _viewModel.StatusText = $"{file.Name} is too large ({info.Size / 1024:N0} KB) — the limit is 1 MB.";
                return;
            }

            await using Stream stream = await file.OpenReadAsync();
            using var reader = new StreamReader(stream);
            _viewModel.PassageText = await reader.ReadToEndAsync();
            _viewModel.StatusText = $"Loaded {_viewModel.PassageText.Length:N0} characters from {file.Name}.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _viewModel.StatusText = $"Couldn't load {file.Name}: {ex.Message}";
        }
    }

    private void OpenButton_Click(object? sender, RoutedEventArgs e)
    {
        OnLoadPassageRequested(null, EventArgs.Empty);
    }

    private async void SettingsButton_Click(object? sender, RoutedEventArgs e)
    {
        var settingsWindow = new ConfigWindow(_appSettings);
        if (await settingsWindow.ShowDialog<bool>(this))
        {
            ThemeManager.Apply(_appSettings.Preferences.Theme);
            Topmost = _appSettings.Preferences.AlwaysOnTop;
            _viewModel.EditableWpm = _appSettings.Speed.Wpm;
            UpdateEstimateText();
        }
    }

    private void OnActivateRequested(object? sender, EventArgs e)
    {
        if (_hotkeyProvider is null || _appSettings.Hotkey.Combo is not { } combo)
        {
            return;
        }

        IReadOnlyList<SettingDescriptor> descriptors = SettingsSchemaBuilder.Build(_appSettings);
        IReadOnlyList<string> errors = SettingsValidator.Validate(descriptors);
        if (errors.Count > 0)
        {
            _viewModel.StatusText = string.Join(" ", errors);
            return;
        }

        // Checked here rather than at launch: registering the hotkey would
        // succeed regardless (Accessibility trust gates CGEventPost, not
        // RegisterEventHotKey), but letting Activate report success while
        // typing is silently doomed is worse than refusing up front.
        if (!PlatformServices.CanTypeNow)
        {
            _viewModel.StatusText = PlatformServices.PermissionDeniedReason!;
            return;
        }

        try
        {
            _triggerHotkeyId = _hotkeyProvider.Register(combo);
            _viewModel.IsActive = true;
            _viewModel.StatusText = $"Active — press {combo} to type the passage.";
        }
        catch (HotkeyRegistrationException ex)
        {
            _viewModel.StatusText = ex.Message;
        }
    }

    private void OnDeactivateRequested(object? sender, EventArgs e)
    {
        if (_triggerHotkeyId is int id)
        {
            _hotkeyProvider?.Unregister(id);
            _triggerHotkeyId = null;
        }

        _viewModel.IsActive = false;
        _viewModel.StatusText = "Inactive";
    }

    private void OnHotkeyPressed(object? sender, HotkeyCombo combo)
    {
        // The Windows provider raises this on the UI thread already, but the
        // interface does not promise that, so marshal rather than assume.
        Dispatcher.UIThread.Post(() =>
        {
            if (combo.Equals(CancelCombo))
            {
                _typingCts?.Cancel();
                return;
            }

            if (_appSettings.Hotkey.PauseCombo.HasValue && combo.Equals(_appSettings.Hotkey.PauseCombo.Value))
            {
                OnPauseResumeRequested(null, EventArgs.Empty);
                return;
            }

            if (_appSettings.Hotkey.Combo is { } trigger && combo.Equals(trigger))
            {
                if (_viewModel.IsTyping)
                {
                    // Second press of the trigger while a run is in progress stops
                    // it — same path as the Stop Typing button and the Escape hotkey.
                    _typingCts?.Cancel();
                }
                else
                {
                    StartTyping();
                }
            }
        });
    }

    private async void OnPauseResumeRequested(object? sender, EventArgs e)
    {
        if (_runController is not { } controller || _keySender is null)
        {
            return;
        }

        if (controller.IsPaused)
        {
            // Hand focus back to the target first, so resuming from AutoTyper's
            // own button doesn't immediately drop into WaitingForFocus.
            await _keySender.FocusTargetAsync();

            // The run may have been stopped during that await; re-registering
            // Escape now would leave it captured globally with no run to cancel.
            if (!ReferenceEquals(_runController, controller))
            {
                return;
            }

            controller.Resume();
            _viewModel.IsPaused = false;
            PauseResumeButton.Content = "Pause";

            // Re-register the global Escape hotkey on resume
            try
            {
                _cancelHotkeyId = _hotkeyProvider?.Register(CancelCombo);
            }
            catch (HotkeyRegistrationException)
            {
                // If Escape is claimed globally, continue without it
            }
        }
        else
        {
            controller.Pause();
            _viewModel.IsPaused = true;
            PauseResumeButton.Content = "Resume";

            // Unregister the global Escape hotkey while paused
            if (_cancelHotkeyId is int id)
            {
                _hotkeyProvider?.Unregister(id);
                _cancelHotkeyId = null;
            }
        }
    }

    private async void OnLoadPassageRequested(object? sender, EventArgs e)
    {
        if (this.StorageProvider is not { } provider)
        {
            _viewModel.StatusText = "Storage provider not available";
            return;
        }

        var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Load Passage",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Text Files") { Patterns = new[] { "*.txt", "*.md" } },
                FilePickerFileTypes.All
            }
        });

        if (files.Count > 0)
        {
            await LoadPassageFromFileAsync(files[0]);
        }
    }

    private void StartTyping(bool fromTray = false)
    {
        if (_keySender is null)
        {
            return;
        }

        // Commit the current passage text to AppSettings
        _appSettings.PassageText = _viewModel.PassageText;

        var options = new TypingOptions
        {
            PassageText = _appSettings.PassageText,
            Speed = _appSettings.Speed,
            Bursts = _appSettings.Bursts,
            Typos = _appSettings.Typos,
            Pauses = _appSettings.Pauses,
            StepAway = _appSettings.StepAway,
            Formatting = _appSettings.Formatting,
            Run = _appSettings.Run,
        };

        HotkeyCombo trigger = _appSettings.Hotkey.Combo ?? default;

        _runController = new TypingRunController();
        _typingCts = new CancellationTokenSource();
        _viewModel.IsTyping = true;
        _viewModel.IsPaused = false;
        _viewModel.ProgressPercent = 0;
        _viewModel.ProgressText = string.Empty;
        _viewModel.StatusText = "Starting...";
        PauseResumeButton.Content = "Pause";

        _ = RunTypingAsync(options, trigger, fromTray, _typingCts.Token);
    }

    private async Task RunTypingAsync(TypingOptions options, HotkeyCombo trigger, bool fromTray, CancellationToken cancellationToken)
    {
        IKeySender keySender = _keySender!;

        try
        {
            // Tray runs always count down: clicking the tray menu leaves focus
            // somewhere other than the target, so the user needs time to click
            // back into it before CaptureTarget snapshots the foreground window.
            int countdownSeconds = Math.Max(options.Run.StartDelaySeconds, fromTray ? 3 : 0);
            if (countdownSeconds == 0)
            {
                // Give the trigger hotkey's own key-up events time to finish
                // processing at the OS level before we start sending input.
                await Task.Delay(150, cancellationToken);
            }

            for (int i = countdownSeconds; i > 0; i--)
            {
                _viewModel.StatusText = $"Starting in {i}... click into the target window";
                await Task.Delay(1000, cancellationToken);
            }

            _viewModel.StatusText = "Typing... (press Escape or Stop Typing to stop)";

            // Whatever window is focused now is the one the passage goes into;
            // the Step Away feature needs it to leave and return to.
            keySender.CaptureTarget();

            // Must run before the cancel hotkey is registered: this may send a
            // priming Escape, and our own global Escape registration would
            // intercept it before it could reach the target window.
            await keySender.PrepareForTypingAsync(trigger);

            // Register pause hotkey if set
            if (_appSettings.Hotkey.PauseCombo.HasValue)
            {
                try
                {
                    _pauseHotkeyId = _hotkeyProvider?.Register(_appSettings.Hotkey.PauseCombo.Value);
                }
                catch (HotkeyRegistrationException)
                {
                    _pauseHotkeyId = null;
                }
            }

            // Register cancel hotkey (Escape)
            try
            {
                _cancelHotkeyId = _hotkeyProvider?.Register(CancelCombo);
            }
            catch (HotkeyRegistrationException)
            {
                // Escape is already claimed globally by something else; typing
                // still proceeds, just without the global cancel shortcut.
                _cancelHotkeyId = null;
            }

            // Create progress reporter
            var progress = new Progress<TypingProgress>(OnTypingProgress);

            TypingRunResult result = await Task.Run(
                () => _typingEngine.RunAsync(
                    options.PassageText,
                    options,
                    keySender,
                    cancellationToken,
                    _runController,
                    progress),
                cancellationToken);

            // Show completion summary
            string summary = FormatRunSummary(result);
            _viewModel.StatusText = $"Finished — {summary}";
        }
        catch (OperationCanceledException)
        {
            int percentage = _viewModel.ProgressPercent > 0 ? (int)_viewModel.ProgressPercent : 0;
            _viewModel.StatusText = $"Typing cancelled at {percentage}%";
        }
        catch (Exception ex)
        {
            _viewModel.StatusText = $"Typing failed: {ex.Message}";
        }
        finally
        {
            if (_pauseHotkeyId is int pauseId)
            {
                _hotkeyProvider?.Unregister(pauseId);
                _pauseHotkeyId = null;
            }

            if (_cancelHotkeyId is int id)
            {
                _hotkeyProvider?.Unregister(id);
                _cancelHotkeyId = null;
            }

            _viewModel.IsTyping = false;
            _viewModel.IsPaused = false;
            _runController = null;
            _typingCts?.Dispose();
            _typingCts = null;
        }
    }

    private void OnTypingProgress(TypingProgress progress)
    {
        // Update UI on the UI thread
        Dispatcher.UIThread.Post(() =>
        {
            _viewModel.ProgressPercent = (progress.CharsTyped / (double)progress.TotalChars) * 100;

            string stateText = progress.State switch
            {
                RunState.Paused => "Paused",
                RunState.WaitingForFocus => "waiting for target window focus",
                RunState.SteppedAway => "on break",
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(stateText))
            {
                _viewModel.ProgressText = $"{(int)_viewModel.ProgressPercent}% · ~{FormatDuration(progress.EstimatedRemaining)} left · {progress.CurrentWpm:F0} WPM";
            }
            else
            {
                _viewModel.ProgressText = $"{stateText} · {(int)_viewModel.ProgressPercent}%";
            }
        });
    }

    private static string FormatRunSummary(TypingRunResult result)
    {
        var parts = new List<string>();

        parts.Add($"{result.CharsTyped:N0} chars in {FormatDuration(result.Elapsed)}");
        parts.Add($"{result.EffectiveWpm:F0} WPM");

        if (result.TyposMade > 0)
        {
            parts.Add($"{result.TyposMade} typos corrected");
        }

        if (result.StepAways > 0)
        {
            parts.Add($"{result.StepAways} step-aways");
        }

        return string.Join(" · ", parts);
    }

    /// <summary>"42s", "6m 05s", or "1h 12m" — whole units, truncated rather than rounded.</summary>
    private static string FormatDuration(TimeSpan duration) =>
        duration.TotalHours >= 1 ? $"{(int)duration.TotalHours}h {duration.Minutes:D2}m"
        : duration.TotalMinutes >= 1 ? $"{(int)duration.TotalMinutes}m {duration.Seconds:D2}s"
        : $"{duration.Seconds}s";

    internal void StartTypingFromTray()
    {
        if (!_viewModel.HasPassage || _viewModel.IsTyping || !PlatformServices.CanTypeNow)
        {
            return;
        }
        StartTyping(fromTray: true);
    }

    internal void PauseResumeTyping()
    {
        if (_viewModel.IsTyping)
        {
            OnPauseResumeRequested(null, EventArgs.Empty);
        }
    }

    internal void StopTypingFromTray()
    {
        if (_viewModel.IsTyping)
        {
            _typingCts?.Cancel();
        }
    }

    internal void OnActivateMenuItemClick()
    {
        OnActivateRequested(null, EventArgs.Empty);
    }

    internal void OnDeactivateMenuItemClick()
    {
        OnDeactivateRequested(null, EventArgs.Empty);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // Minimise-to-tray. Windows-only idiom: macOS has no tray to minimise
        // into, and hiding the window there would just make it look like the
        // app vanished.
        if (change.Property == WindowStateProperty
            && change.GetNewValue<WindowState>() == WindowState.Minimized
            && OperatingSystem.IsWindows())
        {
            Hide();
        }
    }

    internal void RestoreFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    internal MainViewModel GetViewModel() => _viewModel;

    protected override void OnClosed(EventArgs e)
    {
        try
        {
            SettingsService.Save(_appSettings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Backstop; the application's shutdown handler saves again.
        }

        _typingCts?.Cancel();
        _hotkeyProvider?.Dispose();

        base.OnClosed(e);
    }
}
