using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using AutoTyper.Core;
using AutoTyper.Core.Settings;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace AutoTyper.App;

public partial class MainWindow : FluentWindow
{
    private static readonly HotkeyCombo CancelCombo = new(ModifierKeys.None, Key.Escape);

    private readonly MainViewModel _viewModel = new();
    private readonly HotkeyManager _hotkeyManager = new();
    private readonly TypingEngine _typingEngine = new();
    private readonly WinInputKeySender _keySender = new();
    private readonly AppSettings _appSettings;

    private int? _triggerHotkeyId;
    private int? _cancelHotkeyId;
    private CancellationTokenSource? _typingCts;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;

        var app = Application.Current as App;
        if (app is null)
        {
            throw new InvalidOperationException("Application.Current must be initialized as App before MainWindow.");
        }
        _appSettings = app.Settings;

        ThemeManager.Apply(_appSettings.Preferences.Theme);
        if (_appSettings.Preferences.Theme == AppTheme.System)
        {
            SystemThemeWatcher.Watch(this);
        }
        Topmost = _appSettings.Preferences.AlwaysOnTop;

        _viewModel.PassageText = _appSettings.PassageText;
        _viewModel.HasPassage = !string.IsNullOrWhiteSpace(_appSettings.PassageText);
        _viewModel.EditableWpm = _appSettings.Speed.Wpm;
        _viewModel.HasHotkey = _appSettings.Hotkey.Combo.HasValue;

        HotkeyDisplay.SetBinding(
            HotkeyCaptureBox.ComboProperty,
            new System.Windows.Data.Binding(nameof(HotkeySettings.Combo)) { Source = _appSettings.Hotkey, Mode = System.Windows.Data.BindingMode.TwoWay });

        _appSettings.Hotkey.PropertyChanged += (_, _) => _viewModel.HasHotkey = _appSettings.Hotkey.Combo.HasValue;
        _appSettings.Speed.PropertyChanged += (_, _) => UpdateModeIndicator();
        UpdateModeIndicator();

        _viewModel.ActivateRequested += OnActivateRequested;
        _viewModel.DeactivateRequested += OnDeactivateRequested;
        _viewModel.StopTypingRequested += (_, _) => _typingCts?.Cancel();
        _viewModel.SaveSettingsRequested += OnSaveSettingsRequested;
        _viewModel.CancelSettingsRequested += OnCancelSettingsRequested;
        _hotkeyManager.HotkeyPressed += OnHotkeyPressed;
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

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new ConfigWindow(_appSettings) { Owner = this };
        if (settingsWindow.ShowDialog() == true)
        {
            ThemeManager.Apply(_appSettings.Preferences.Theme);
            Topmost = _appSettings.Preferences.AlwaysOnTop;
        }
    }

    private void OnActivateRequested(object? sender, EventArgs e)
    {
        if (_appSettings.Hotkey.Combo is not { } combo)
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

        try
        {
            _triggerHotkeyId = _hotkeyManager.Register(this, combo);
            _viewModel.IsActive = true;
            _viewModel.StatusText = $"Active — press {combo} to type the passage.";
        }
        catch (InvalidOperationException ex)
        {
            _viewModel.StatusText = ex.Message;
        }
    }

    private void OnDeactivateRequested(object? sender, EventArgs e)
    {
        if (_triggerHotkeyId is int id)
        {
            _hotkeyManager.Unregister(id);
            _triggerHotkeyId = null;
        }

        _viewModel.IsActive = false;
        _viewModel.StatusText = "Inactive";
    }

    private void OnHotkeyPressed(object? sender, HotkeyCombo combo)
    {
        if (combo.Equals(CancelCombo))
        {
            _typingCts?.Cancel();
            return;
        }

        if (_appSettings.Hotkey.Combo is { } trigger && combo.Equals(trigger) && !_viewModel.IsTyping)
        {
            StartTyping();
        }
    }

    private void StartTyping()
    {
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

        bool triggerHasAlt = _appSettings.Hotkey.Combo?.Modifiers.HasFlag(ModifierKeys.Alt) ?? false;

        _typingCts = new CancellationTokenSource();
        _viewModel.IsTyping = true;
        _viewModel.StatusText = "Typing... (press Escape or Stop Typing to stop)";

        _ = RunTypingAsync(options, triggerHasAlt, _typingCts.Token);
    }

    private async Task RunTypingAsync(TypingOptions options, bool triggerHasAlt, CancellationToken cancellationToken)
    {
        try
        {
            // Give the trigger hotkey's own key-up events time to finish
            // processing at the OS level before we start sending input.
            await Task.Delay(150, cancellationToken);

            if (triggerHasAlt)
            {
                // Register the cancel hotkey only after this: while our own
                // Escape registration is active it intercepts every Escape
                // system-wide, including this priming one, before it can
                // reach the target window.
                await _keySender.SendEscapeAsync();
                await Task.Delay(100, cancellationToken);
            }

            try
            {
                _cancelHotkeyId = _hotkeyManager.Register(this, CancelCombo);
            }
            catch (InvalidOperationException)
            {
                // Escape is already claimed globally by something else; typing
                // still proceeds, just without the global cancel shortcut.
                _cancelHotkeyId = null;
            }

            // Whatever window is focused now is the one the passage goes into;
            // the Step Away feature needs its handle to leave and return to it.
            // Never target our own window.
            IntPtr target = NativeMethods.GetForegroundWindow();
            IntPtr ownHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            _keySender.SetTypingTarget(target == ownHandle ? IntPtr.Zero : target);

            await Task.Run(
                () => _typingEngine.RunAsync(options.PassageText, options, _keySender, cancellationToken),
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
                _hotkeyManager.Unregister(id);
                _cancelHotkeyId = null;
            }

            _viewModel.IsTyping = false;
            _typingCts?.Dispose();
            _typingCts = null;
        }
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
        }
    }

    private void RestoreMenuItem_Click(object sender, RoutedEventArgs e) => RestoreFromTray();

    private void TrayIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e) => RestoreFromTray();

    private void RestoreFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        try
        {
            SettingsService.Save(_appSettings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Backstop; App.OnExit saves again. Nothing to surface mid-close.
        }

        _typingCts?.Cancel();
        _hotkeyManager.Dispose();
        TrayIcon.Dispose();
    }
}
