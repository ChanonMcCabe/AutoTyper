# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

AutoTyper is a cross-platform (Windows/macOS) desktop utility that types a saved passage into whatever window has focus, simulating human typing: WPM drift, bursts/pauses, typo injection + correction, and periodic "step away." Built with .NET 8 and Avalonia (FluentAvalonia). It began as a port of an AutoHotkey script via an earlier WPF build; comments mentioning "the AHK MVP" or "the WPF build" refer to that history, and the WPF-era settings/hotkey parsing is kept deliberately for backward compatibility.

## Commands

```
./scripts/verify.ps1                    # build + run both test suites; compact output, exit 1 on failure
./scripts/verify.ps1 -Project Core -Filter "FullyQualifiedName~TypingEngineTests"   # fast inner loop
dotnet run --project AutoTyper.Desktop  # run the app
dotnet build AutoTyper.sln              # build everything, including the Harness (rarely needed)
```

`scripts/verify.ps1` is the standard verification step: it runs `dotnet test` on `AutoTyper.Core.Tests` and `AutoTyper.Desktop.Tests` (which builds only what they need — never the Harness) and prints just compiler errors, failed tests with their message and in-repo `file:line`, and pass/fail totals. From Git Bash, invoke it as `powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/verify.ps1`.

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

`TypingOptions` is the root object passed into the engine, composed of the settings-group objects below (the passage itself is a separate `RunAsync` argument). A caller-owned `TypingRunController` handles pause/resume and, when `RunSettings.PauseOnFocusLoss` is on, waits out focus loss on the target (polling `IKeySender.IsTargetFocused`); its state changes and per-token `TypingProgress` snapshots are reported through `IProgress<TypingProgress>`, and a completed run returns a `TypingRunResult` summary.

### Settings schema (AutoTyper.Core/Settings)

Settings groups (`SpeedSettings`, `PauseSettings`, `TypoSettings`, `BurstSettings`, `StepAwaySettings`, `FormattingSettings`, `HotkeySettings`) are plain classes whose configurable properties carry `[Setting(Category, Group, DisplayName, Min, Max, Advanced)]`. `SettingsSchemaBuilder.Build(root)` reflects over an object graph (root → nested settings-group objects → `[Setting]`-attributed leaf properties) and flattens it into `SettingDescriptor` list — **this is what drives the generic settings UI**: adding a new setting to a group class is enough for it to appear, with min/max validation and advanced-toggle visibility, without touching any UI code. `[DependsOn(PropertyName, RequiredValue)]` lets a setting's visibility depend on another setting's current value (see `DependsOnAttribute.cs`). `SettingsValidator` and `IValidatableSetting` handle cross-field validation a single `Min`/`Max` can't express.

Because the UI is generated from this schema, most new user-facing settings should be added as a new `[Setting]`-attributed property on the appropriate group class rather than as bespoke UI + binding code.

### Desktop app (AutoTyper.Desktop)

Light hand-rolled MVVM (no framework): `MainViewModel` implements `INotifyPropertyChanged` and exposes `RelayCommand`s, but deliberately keeps native-handle-requiring concerns (hotkey registration, actual key sending, activation) out of the view model — it only raises request events (`ActivateRequested`, `DeactivateRequested`, etc.) that `MainWindow.axaml.cs` code-behind handles, since those need a real window handle and OS calls. `ConfigWindow` hosts the generic `SettingsPanel` (reflection-driven off `SettingsSchemaBuilder`) plus `AppearancePanel`/`ThemeManager` for theme. `AppSettings`/`SettingsService` handle JSON persistence and the save/cancel staged-edit pattern: the Config window edits a `SettingsService.Clone` of the live settings and only commits back on Save. `SettingsService.DeepClone<T>` (a JSON round-trip with the on-disk options) is the one deep-copy mechanism — `TypingPreset.FromSettings`/`ApplyTo` use it for named presets of the typing groups (presets exclude hotkeys and appearance). Shared styles such as `Border.card` live in `App.axaml`; the AHK Classic theme is applied by `ThemeManager` as resource overrides plus `AhkClassicTheme.axaml`.

### Testing conventions

`AutoTyper.Core.Tests` uses a `FakeKeySender` to unit-test `TypingEngine`/`TypoTyper`/timing logic with a seeded `Random` — no real keystrokes, no OS dependency. `StatisticalParityTests` checks typo-injection/timing distributions statistically rather than exact values, since the engine is randomized. `AutoTyper.Desktop.Tests` includes `CarbonNativeMethodsTests`, which asserts struct layout and four-char-code arithmetic in the macOS P/Invoke declarations *without making any native call* — this lets those tests run and pass on a Windows dev machine even though the code they check only executes on macOS (hence `CA1416` is suppressed there; see the csproj comment).

## Notes

- Avalonia is pinned to the 11.x line and FluentAvaloniaUI to 2.4.0 (not 12.x/2.5.1+/3.x) because those newer lines require net10.0, which would split the Desktop project off the rest of the solution's net8.0 target — see the comment in `AutoTyper.Desktop/AutoTyper.Desktop.csproj`.
- macOS builds are ad-hoc signed, not notarized (no paid Apple Developer account); Gatekeeper blocks first launch until the user right-click → Open.
## Workflow

- **Default: edit directly, verify with one command.** The main agent makes code changes itself and runs `scripts/verify.ps1` once a change is complete (not after every individual edit). While iterating on one area, use `-Project`/`-Filter` to run only the relevant tests, then do one full run before reporting done. Do not spawn a subagent just to build or run tests.
- **On a failure, fix it inline.** The script already reports the failing test's message and `file:line` — read that code and fix it. Reach for `issue-investigator` only when the cause is still unclear after looking at the failing code (e.g. an intermittent failure, or a bug that spans several layers).
- **Specialist agents are opt-in, not the default route.** `.claude/agents/` has `typing-logic` (Core engine/timing), `gui-designer` (Avalonia UI), `issue-investigator` (read-only root-causing), `stability-reviewer` (read-only crash/perf audit) and `git-manager` (git operations). Use one when the user asks for it, or for a large self-contained task that benefits from running in parallel with other work. Every subagent starts with no context, so delegating a small change costs more than making it. The specialists' briefs remain the reference for their area's conventions (min/max clamping and `Random` injection in `typing-logic.md`; Avalonia gotchas and design-approval rules in `gui-designer.md`) — read them when working in that area.
- **Never run two builds at once.** Parallel agents share `bin/`/`obj/`, so if work is split across agents, only one of them runs `verify.ps1` at a time.
