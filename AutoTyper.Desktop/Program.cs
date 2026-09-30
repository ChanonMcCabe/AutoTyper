using Avalonia;

namespace AutoTyper.Desktop;

internal static class Program
{
    // STAThread is required by the Windows backend (OLE/clipboard); it is
    // simply ignored on macOS.
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    // Referenced by name from Avalonia's XAML previewer tooling.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
