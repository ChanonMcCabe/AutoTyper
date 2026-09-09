using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace AutoTyper.Desktop.Mac;

/// <summary>
/// Checks whether AutoTyper has been granted Accessibility access, which
/// macOS requires before <c>CGEventPost</c> (see <see cref="MacKeySender"/>)
/// will actually deliver a synthesized keystroke. Without the grant,
/// <c>CGEventPost</c> silently does nothing — no exception, no return code —
/// which is why <see cref="PlatformServices.CanTypeNow"/> checks this
/// up front rather than letting a run start and quietly type nothing.
/// </summary>
/// <remarks>
/// Deliberately does not build the <c>kAXTrustedCheckOptionPrompt</c>
/// CFDictionary needed to make <c>AXIsProcessTrustedWithOptions</c> pop
/// macOS's own "AutoTyper wants to control this computer" system dialog.
/// <see cref="PlatformServices.PermissionDeniedReason"/> already tells the
/// user where to go (System Settings → Privacy &amp; Security →
/// Accessibility), and building that dictionary needs more untestable
/// P/Invoke surface (<c>CFStringCreateWithCString</c>,
/// <c>CFDictionaryCreate</c>) for a marginal UX gain — a scope-narrowing
/// call made explicitly rather than silently.
/// <para>
/// Known accepted gap: if the user revokes Accessibility mid-run, after
/// Activate already checked <see cref="IsTrusted"/> and succeeded,
/// <c>CGEventPost</c> gives no failure signal and the run goes silently
/// inert. Detecting that would mean polling <see cref="IsTrusted"/> before
/// every character, which is disproportionate to a rare edge case, so it is
/// documented rather than solved.
/// </para>
/// </remarks>
[SupportedOSPlatform("macos")]
internal static class MacAccessibility
{
    private const string ApplicationServices =
        "/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices";

    [DllImport(ApplicationServices)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool AXIsProcessTrusted();

    /// <summary>Read-only; never prompts. Safe to call as often as needed.</summary>
    public static bool IsTrusted() => AXIsProcessTrusted();
}
