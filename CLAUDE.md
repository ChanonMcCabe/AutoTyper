# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

AutoTyper is a cross-platform (Windows/macOS) desktop utility that types a saved passage into whatever window has focus, simulating human typing: WPM drift, bursts/pauses, typo injection + correction, and periodic "step away." Built with .NET 8 and Avalonia (FluentAvalonia). `AutoTyper_CSharp_Port_Plan.md` documents the original AHK→C# port plan (WPF/single-platform); the app has since moved to Avalonia for cross-platform support, so treat that file as historical context, not current architecture.

## Commands

```
dotnet build AutoTyper.sln              # build everything
dotnet run --project AutoTyper.Desktop  # run the app
dotnet test                             # run all tests (Core.Tests + Desktop.Tests)
dotnet test --filter "FullyQualifiedName~TypingEngineTests"   # run one test class
dotnet test --filter "FullyQualifiedName~TypingEngineTests.MethodName"  # run one test
```

`AutoTyper.Harness` is a Windows-only (`net8.0-windows`, WPF) internal dev tool for driving real typing against Notepad — not part of the shipped app, not built/run as part of normal iteration. It's invoked with flags (`--real`, `--step-away`, `--spacing`, `--hotkey-test`, `--diag-timing`); see `AutoTyper.Harness/Program.cs`.

## Architecture

### Project split and dependency direction

- **AutoTyper.Core** — platform-agnostic typing engine, settings schema, and hotkey/key-sending abstractions. No OS-specific dependencies; fully unit-testable.
- **AutoTyper.Desktop** — the Avalonia app (what ships). References Core. Contains the UI, platform adapters (Windows P/Invoke and macOS Carbon/CoreGraphics), and settings persistence.
- **AutoTyper.Harness** — Windows-only WPF console/dev harness, references both Core and Desktop, for manual verification against real windows.
- **AutoTyper.Core.Tests** / **AutoTyper.Desktop.Tests** — xUnit suites.

### The platform boundary

`AutoTyper.Desktop/PlatformServices.cs` is the single composition root for every OS-specific decision. Everything else in the app talks only to `IKeySender` (`AutoTyper.Core/IKeySender.cs`) and `IHotkeyProvider` (`AutoTyper.Core/Input/IHotkeyProvider.cs`) — never to a concrete Windows or macOS type directly. Windows implementations live under `AutoTyper.Desktop/Windows/`, macOS under `AutoTyper.Desktop/Mac/`. Adding a third platform means implementing these two interfaces and wiring them into `PlatformServices`, not touching the engine or UI.

Note the distinction `PlatformServices` draws between `IsSupported` (an adapter exists for this OS) and `CanTypeNow` (the adapter exists *and* the OS has actually granted permission — relevant on macOS, where `CGEventPost` silently no-ops until Accessibility access is granted).

### Typing engine (AutoTyper.Core)

`TypingEngine.RunAsync` is the orchestrator (`AutoTyper.Core/TypingEngine.cs`): it resolves a base WPM once via `SpeedResolver`, then walks the passage word-by-word, consulting `WpmDriftTracker` for current pace and `BurstController` for whether to run a phrase burst, typing each word through `TypoTyper` (which owns typo injection/correction), with `StepAwayController` periodically triggering a blur/focus cycle via `IKeySender`. `TimingService` computes the actual delay values (letter pause, long pause, burst pauses, step-away duration) that `TypingEngine` awaits between actions. All delays are `Task.Delay(ms, cancellationToken)` — cancellation is checked before every await rather than polled, replacing the original AHK script's `GetKeyState` polling loop.

`TypingOptions` is the root object passed into the engine, composed of the settings-group objects below.

### Settings schema (AutoTyper.Core/Settings)

Settings groups (`SpeedSettings`, `PauseSettings`, `TypoSettings`, `BurstSettings`, `StepAwaySettings`, `FormattingSettings`, `HotkeySettings`) are plain classes whose configurable properties carry `[Setting(Category, Group, DisplayName, Min, Max, Advanced)]`. `SettingsSchemaBuilder.Build(root)` reflects over an object graph (root → nested settings-group objects → `[Setting]`-attributed leaf properties) and flattens it into `SettingDescriptor` list — **this is what drives the generic settings UI**: adding a new setting to a group class is enough for it to appear, with min/max validation and advanced-toggle visibility, without touching any UI code. `[DependsOn(PropertyName, RequiredValue)]` lets a setting's visibility depend on another setting's current value (see `DependsOnAttribute.cs`). `SettingsValidator` and `IValidatableSetting` handle cross-field validation a single `Min`/`Max` can't express.

Because the UI is generated from this schema, most new user-facing settings should be added as a new `[Setting]`-attributed property on the appropriate group class rather than as bespoke UI + binding code.

### Desktop app (AutoTyper.Desktop)

Light hand-rolled MVVM (no framework): `MainViewModel` implements `INotifyPropertyChanged` and exposes `RelayCommand`s, but deliberately keeps native-handle-requiring concerns (hotkey registration, actual key sending, activation) out of the view model — it only raises request events (`ActivateRequested`, `DeactivateRequested`, etc.) that `MainWindow.axaml.cs` code-behind handles, since those need a real window handle and OS calls. `ConfigWindow` hosts the generic `SettingsPanel` (reflection-driven off `SettingsSchemaBuilder`) plus `AppearancePanel`/`ThemeManager` for theme. `AppSettings`/`SettingsService`/`AppSettingsCloner` handle JSON persistence and the save/cancel staged-edit pattern (edits to the Config window are cloned and only committed back on Save).

### Testing conventions

`AutoTyper.Core.Tests` uses a `FakeKeySender` to unit-test `TypingEngine`/`TypoTyper`/timing logic with a seeded `Random` — no real keystrokes, no OS dependency. `StatisticalParityTests` checks typo-injection/timing distributions statistically rather than exact values, since the engine is randomized. `AutoTyper.Desktop.Tests` includes `CarbonNativeMethodsTests`, which asserts struct layout and four-char-code arithmetic in the macOS P/Invoke declarations *without making any native call* — this lets those tests run and pass on a Windows dev machine even though the code they check only executes on macOS (hence `CA1416` is suppressed there; see the csproj comment).

## Notes

- Avalonia is pinned to the 11.x line and FluentAvaloniaUI to 2.4.0 (not 12.x/2.5.1+/3.x) because those newer lines require net10.0, which would split the Desktop project off the rest of the solution's net8.0 target — see the comment in `AutoTyper.Desktop/AutoTyper.Desktop.csproj`.
- macOS builds are ad-hoc signed, not notarized (no paid Apple Developer account); Gatekeeper blocks first launch until the user right-click → Open.
- This repo has a `.claude/agents/` directory with specialist subagents (`typing-logic` for Core engine/timing changes, `gui-designer` for Avalonia UI, `git-manager` for git operations, `test-agent` for build+test verification) — prefer delegating to the matching specialist for in-scope changes rather than editing those areas directly.
- `gui-designer` and `typing-logic` already spawn `test-agent` after every change they make, per their own briefs. When the main agent edits code directly instead of delegating — e.g. a fix that falls outside every specialist's stated scope, such as `AutoTyper.Core/Settings` infrastructure — it must do the same: invoke `test-agent` (via the `Agent` tool, waited on rather than backgrounded) after the change, before reporting the change as done.
