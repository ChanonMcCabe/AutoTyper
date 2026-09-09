using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;

namespace AutoTyper.Desktop;

internal static class Program
{
    /// <summary>
    /// Theme resource keys the UI relies on. A missing key does not throw —
    /// the brush silently falls back and the affected element renders flat —
    /// so <c>--diag-theme</c> exists to assert they really resolve, in both
    /// theme variants, rather than trusting that they look right.
    /// </summary>
    private static readonly string[] RequiredThemeResources =
    [
        "ControlStrokeColorDefaultBrush",
        "CardBackgroundFillColorDefaultBrush",
    ];

    // STAThread is required by the Windows backend (OLE/clipboard); it is
    // simply ignored on macOS.
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Contains("--diag-theme"))
        {
            RunThemeDiagnostics();
            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static void RunThemeDiagnostics()
    {
        BuildAvaloniaApp().SetupWithoutStarting();

        Application app = Application.Current!;
        bool allResolved = true;

        foreach (ThemeVariant variant in new[] { ThemeVariant.Light, ThemeVariant.Dark })
        {
            Console.WriteLine($"--- {variant} ---");
            foreach (string key in RequiredThemeResources)
            {
                bool found = app.TryGetResource(key, variant, out object? value);
                string rendered = value is ISolidColorBrush brush ? brush.Color.ToString() : value?.ToString() ?? "<null>";
                Console.WriteLine($"  {(found ? "OK  " : "MISS")} {key} = {rendered}");
                allResolved &= found && value is not null;
            }
        }

        Console.WriteLine(allResolved
            ? "PASS: every required theme resource resolved in both variants."
            : "FAIL: at least one theme resource did not resolve — it will silently fall back.");
    }

    // Referenced by name from Avalonia's XAML previewer tooling.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
