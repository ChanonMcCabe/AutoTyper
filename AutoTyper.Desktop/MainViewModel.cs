using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace AutoTyper.Desktop;

/// <summary>
/// Bindable state for the Main window: the passage and WPM fields as staged
/// (unsaved-until-Save) edits, whether a hotkey/passage are currently
/// committed, the cross-cutting mode indicator and WPM-field-enabled state
/// driven by the Config window's Range/Frame toggles, and typing progress —
/// plus Activate/Deactivate/StopTyping/SaveSettings/CancelSettings commands.
/// Everything else the user can tune (typos, bursts, pauses, the hotkey
/// itself, and the Range/Frame mode toggles) lives in the
/// <see cref="AppSettings"/> object graph edited directly by the Config
/// window's generic settings panels, not here. Actual hotkey registration
/// and typing execution are view-layer concerns (they need a native window
/// handle and Win32 key sending), so this view model only raises request
/// events for the code-behind to act on.
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private string _passageText = string.Empty;
    private int _editableWpm = 40;
    private bool _hasHotkey;
    private bool _hasPassage;
    private bool _isWpmFieldEnabled = true;
    private string _modeText = "Mode: Fixed WPM";
    private string _statusText = "Inactive";
    private bool _isActive;
    private bool _isTyping;

    public MainViewModel()
    {
        ActivateCommand = new RelayCommand(
            () => ActivateRequested?.Invoke(this, EventArgs.Empty),
            () => !IsActive && HasHotkey && HasPassage);

        DeactivateCommand = new RelayCommand(
            () => DeactivateRequested?.Invoke(this, EventArgs.Empty),
            () => IsActive);

        StopTypingCommand = new RelayCommand(
            () => StopTypingRequested?.Invoke(this, EventArgs.Empty),
            () => IsTyping);

        SaveSettingsCommand = new RelayCommand(() => SaveSettingsRequested?.Invoke(this, EventArgs.Empty));

        CancelSettingsCommand = new RelayCommand(() => CancelSettingsRequested?.Invoke(this, EventArgs.Empty));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler? ActivateRequested;

    public event EventHandler? DeactivateRequested;

    public event EventHandler? StopTypingRequested;

    public event EventHandler? SaveSettingsRequested;

    public event EventHandler? CancelSettingsRequested;

    public string PassageText
    {
        get => _passageText;
        set => SetField(ref _passageText, value);
    }

    public int EditableWpm
    {
        get => _editableWpm;
        set => SetField(ref _editableWpm, value);
    }

    public bool HasHotkey
    {
        get => _hasHotkey;
        set
        {
            if (SetField(ref _hasHotkey, value))
            {
                ((RelayCommand)ActivateCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public bool HasPassage
    {
        get => _hasPassage;
        set
        {
            if (SetField(ref _hasPassage, value))
            {
                ((RelayCommand)ActivateCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsWpmFieldEnabled
    {
        get => _isWpmFieldEnabled;
        set => SetField(ref _isWpmFieldEnabled, value);
    }

    public string ModeText
    {
        get => _modeText;
        set => SetField(ref _modeText, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetField(ref _statusText, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (SetField(ref _isActive, value))
            {
                ((RelayCommand)ActivateCommand).RaiseCanExecuteChanged();
                ((RelayCommand)DeactivateCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsTyping
    {
        get => _isTyping;
        set
        {
            if (SetField(ref _isTyping, value))
            {
                ((RelayCommand)StopTypingCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand ActivateCommand { get; }

    public ICommand DeactivateCommand { get; }

    public ICommand StopTypingCommand { get; }

    public ICommand SaveSettingsCommand { get; }

    public ICommand CancelSettingsCommand { get; }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
