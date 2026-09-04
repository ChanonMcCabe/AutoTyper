namespace AutoTyper.App;

/// <summary>
/// The visual theme the app paints itself in. <see cref="System"/> follows the
/// Windows "app mode" (light/dark) setting; <see cref="Light"/> and
/// <see cref="Dark"/> force one regardless of the OS.
/// </summary>
public enum AppTheme
{
    System,
    Light,
    Dark,
}
