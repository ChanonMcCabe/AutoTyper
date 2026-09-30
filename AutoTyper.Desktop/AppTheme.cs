namespace AutoTyper.Desktop;

/// <summary>
/// The visual theme the app paints itself in. <see cref="System"/> follows the
/// OS light/dark setting (Windows or macOS); <see cref="Light"/> and
/// <see cref="Dark"/> force one regardless of the OS. <see cref="AhkClassic"/>
/// mimics the flat, square-cornered look of the original AutoHotkey GUI.
/// </summary>
public enum AppTheme
{
    System,
    Light,
    Dark,
    AhkClassic,
}
