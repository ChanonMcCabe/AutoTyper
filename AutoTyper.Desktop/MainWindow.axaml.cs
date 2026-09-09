using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data;
using Avalonia.Interactivity;
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
    private CancellationTokenSource? _typingCts;

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

        _appSettings.Hotkey.PropertyChanged += (_, _) => _viewModel.HasHotkey = _appSettings.Hotkey.Combo.HasValue;
        _appSettings.Speed.PropertyChanged += (_, _) => UpdateModeIndicator();
        UpdateModeIndicator();

        // HasPassage drives whether Activate is enabled, so it has to track the
        // text box rather than only being recomputed on Save — otherwise typing
        // a passage and pressing Activate leaves the button greyed out.
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        _viewModel.ActivateRequested += OnActivateRequested;
        _viewModel.DeactivateRequested += OnDeactivateRequested;
        _viewModel.StopTypingRequested += (_, _) => _typingCts?.Cancel();
        _viewModel.SaveSettingsRequested += OnSaveSettingsRequested;
        _viewModel.CancelSettingsRequested += OnCancelSettingsRequested;

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

    private async void SettingsButton_Click(object? sender, RoutedEventArgs e)
    {
        var settingsWindow = new ConfigWindow(_appSettings);
        if (await settingsWindow.ShowDialog<bool>(this))
        {
            ThemeManager.Apply(_appSettings.Preferences.Theme);
            Topmost = _appSettings.Preferences.AlwaysOnTop;
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

    private void StartTyping()
    {
        if (_keySender is null)
        {
            return;
        }

        var options = new TypingOptions
        {
            PassageText = _appSettings.PassageText,
            Speed = _appSettings.Speed,
            Bursts = _appSettings.Bursts,
            Typos = _appSettings.Typos,
            Pauses = _appSettings.Pauses,
            StepAway = _appSettings.StepAway,
            Formatting = _appSettings.Formatting,
        };

        HotkeyCombo trigger = _appSettings.Hotkey.Combo ?? default;

        _typingCts = new CancellationTokenSource();
        _viewModel.IsTyping = true;
        _viewModel.StatusText = "Typing... (press Escape or Stop Typing to stop)";

        _ = RunTypingAsync(options, trigger, _typingCts.Token);
    }

    private async Task RunTypingAsync(TypingOptions options, HotkeyCombo trigger, CancellationToken cancellationToken)
    {
        IKeySender keySender = _keySender!;

        try
        {
            // Give the trigger hotkey's own key-up events time to finish
            // processing at the OS level before we start sending input.
            await Task.Delay(150, cancellationToken);

            // Whatever window is focused now is the one the passage goes into;
            // the Step Away feature needs it to leave and return to.
            keySender.CaptureTarget();

            // Must run before the cancel hotkey is registered: this may send a
            // priming Escape, and our own global Escape registration would
            // intercept it before it could reach the target window.
            await keySender.PrepareForTypingAsync(trigger);

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

            await Task.Run(
                () => _typingEngine.RunAsync(options.PassageText, options, keySender, cancellationToken),
                cancellationToken);
            _viewModel.StatusText = "Finished typing.";
        }
        catch (OperationCanceledException)
        {
            _viewModel.StatusText = "Typing cancelled.";
        }
        catch (Exception ex)
        {
            _viewModel.StatusText = $"Typing failed: {ex.Message}";
        }
        finally
        {
            if (_cancelHotkeyId is int id)
            {
                _hotkeyProvider?.Unregister(id);
                _cancelHotkeyId = null;
            }

            _viewModel.IsTyping = false;
            _typingCts?.Dispose();
            _typingCts = null;
        }
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
