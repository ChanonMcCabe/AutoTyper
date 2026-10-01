# AutoTyper

A desktop utility that types text like a human would — with natural pacing, drift, the occasional typo and correction, and configurable pauses — into whatever window has focus.

> **Responsible use**: AutoTyper simulates human typing behavior (typo injection, WPM drift, bursts and pauses, step-away). It's built for practice, accessibility, and testing scenarios. You're responsible for complying with the terms of service of whatever application or site you point it at.

## Features

- **Human-like pacing**: a fixed WPM, a random WPM picked from a range, or "type this passage in N minutes", with per-keystroke jitter and longer pauses after sentences.
- **WPM drift**: typing speed wanders gradually instead of staying perfectly constant.
- **Typo injection**: occasional realistic mistakes (adjacent QWERTY keys), corrected either immediately or after typing a few more characters.
- **Phrase bursts**: short runs of words typed faster, with pauses before and after.
- **Step-away**: every N words, focus leaves the target window for a while and then comes back.
- **Pause/resume and stop**: from the main window, the tray menu, or hotkeys. Typing also pauses automatically if the target window loses focus (Windows only).
- **Progress**: a progress bar with the current WPM and the time remaining, a time estimate before you start, and a summary when the run finishes.
- **Passages**: paste text, open a `.txt`/`.md` file, or drag a file onto the passage box. You can keep or collapse the passage's line breaks and spacing.
- **Presets**: save named sets of typing settings and load them later.
- **Themes**: System, Light, Dark, or AHK Classic (a flat Windows 10 look), plus an always-on-top option.
- **Tray icon**: on Windows, minimizing hides the window to the tray; the tray menu can start, pause and stop typing.
- **Cross-platform**: Windows and macOS.

## Requirements

- Windows 10/11 or macOS 11+
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) to build from source

## Install

Download the latest build for your platform from the [Releases page](https://github.com/ChanonMcCabe/AutoTyper/releases/latest) — no .NET install needed.

- **Windows**: unzip `AutoTyper-win-x64.zip` and run `AutoTyper.Desktop.exe`. The exe is unsigned, so SmartScreen may warn: click **More info → Run anyway**.
- **macOS**: unzip `AutoTyper-osx-arm64.zip` (Apple Silicon) or `AutoTyper-osx-x64.zip` (Intel) and move `AutoTyper.app` to Applications. The app isn't notarized, so on first launch right-click it and choose **Open**, then grant Accessibility access when prompted.

Or build it from source (see below).

## Usage

1. Enter a passage and a WPM on the main window, and set a trigger hotkey by clicking the hotkey box and pressing a combo.
2. Click **Activate**.
3. Click into the window you want to type into, then press the trigger hotkey.

### Hotkeys

| Key | Action |
| --- | --- |
| Trigger hotkey (you set it) | Start typing. Pressing it again during a run stops the run. |
| Escape | Cancel the current run (only registered while typing). |
| Pause/Resume hotkey (optional, set in Settings → General) | Pause or resume the current run. |

The trigger combo is configurable in-app; there's no fixed default.

### Settings

Settings are saved to `%AppData%\AutoTyper\settings.json` on Windows and `~/.config/AutoTyper/settings.json` on macOS. The passage is never saved; it is blank each time you launch the app.

## macOS notes

- AutoTyper needs **Accessibility** access to send keystrokes: System Settings → Privacy & Security → Accessibility. Until you grant it, **Activate** explains what's missing instead of silently typing nothing.
- Step-away and pause-on-focus-loss track the target *app*, not a single window. Stepping away briefly brings Finder to the front. These features haven't been tested on a real Mac yet: if they don't work on your macOS version, typing simply continues through them.
- macOS builds are ad-hoc signed, not notarized (there's no paid Apple Developer account). On first launch, Gatekeeper blocks the app as being "from an unidentified developer". Right-click the app and choose **Open** to run it anyway.

## Build from source

```
dotnet run --project AutoTyper.Desktop    # build and run the app
./scripts/verify.ps1                      # build and run all tests, compact output
```

`scripts/verify.ps1` runs both test suites and prints only compiler errors, failing tests (with the message and `file:line`), and the totals. Options:

- `-Project Core|Desktop` runs a single suite.
- `-Filter "FullyQualifiedName~TypingEngineTests"` runs a subset.

From Git Bash, run it as `powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/verify.ps1`.

### Publishing

```
# Windows
dotnet publish AutoTyper.Desktop -c Release -r win-x64 --self-contained -o ./publish

# macOS (publish from any OS, then finish the .app bundle on a Mac)
dotnet publish AutoTyper.Desktop -c Release -r osx-arm64 --self-contained -o ./publish-osx-arm64
./AutoTyper.Desktop/Packaging/finish-macos-build.sh ./publish-osx-arm64 <path-to-.iconset>
```

`finish-macos-build.sh` builds the `.app` bundle, restores the executable bit, ad-hoc signs it, and zips the result. Use `osx-x64` for Intel Macs.

## Project layout

- **AutoTyper.Core**: the platform-agnostic typing engine, settings schema, and hotkey abstractions.
- **AutoTyper.Desktop**: the Avalonia desktop app (this is what you run), including the Windows and macOS input adapters.
- **AutoTyper.Harness**: an internal dev/test harness for Windows only. It drives real typing against Notepad and is not part of the shipped app.
- **AutoTyper.Core.Tests** / **AutoTyper.Desktop.Tests**: xUnit test suites.

## License

MIT. See [LICENSE](LICENSE).
