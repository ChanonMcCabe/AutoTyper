# AutoTyper — C# Port Plan

**From:** AutoHotkey v2 MVP (`AutoTyper_MVP.ahk`)
**To:** C# / .NET, WPF desktop application

## 1. Goals

- Preserve the core behavior of the AHK MVP: type a saved passage into whatever window has focus, at a target WPM, with human-like timing jitter and occasional typo-and-correction, triggered by a global hotkey.
- Improve on the MVP's architectural weaknesses:
  - Blocking `Sleep()` calls that would freeze a WPF UI thread
  - Typo/cancel logic polling `GetKeyState` instead of using real cancellation
  - No separation between typing logic and GUI code
  - No persistence of settings between runs
  - Fixed hotkey list instead of arbitrary capture
- Produce a codebase that is testable, extensible, and packaged as a distributable Windows executable.

## 2. Architecture Overview

### 2.1 Solution structure

Two projects in one solution:

- **`AutoTyper.Core`** — class library, no Windows-specific dependencies. Contains the typing engine, timing/typo logic, and option models. Fully unit-testable in isolation.
- **`AutoTyper.App`** — WPF executable. References `Core`. Contains the UI, Win32 key-sending implementation, global hotkey registration, tray icon, and settings persistence.

This split is the main structural upgrade over the AHK version, where GUI and typing logic were interleaved in the same file.

### 2.2 Key design decisions

| Concern | AHK MVP approach | C# approach |
|---|---|---|
| UI framework | AHK `Gui` object | WPF with light MVVM bindings |
| Sending keystrokes | `SendText()` | P/Invoke `SendInput` with `KEYEVENTF_UNICODE` |
| Global hotkey | `Hotkey()` with fixed dropdown list | P/Invoke `RegisterHotKey` + a "press your combo" capture control |
| Timing | Blocking `Sleep()` | `await Task.Delay(ms, cancellationToken)` |
| Cancel mid-type | Poll `GetKeyState("Escape")` | `CancellationTokenSource`, bound to a Cancel button and/or Escape hotkey |
| Settings | Not persisted | Serialized to JSON in `%AppData%\AutoTyper` |
| Testability | None (GUI and logic combined) | `IKeySender` abstraction lets `TypingEngine` be unit-tested with a fake sender |

### 2.3 Core abstractions

- `TypingEngine` — async, takes a passage string, `TypingOptions`, an `IKeySender`, and a `CancellationToken`; produces the human-like typing sequence.
- `IKeySender` — interface with `Task SendCharAsync(char c)` and `Task SendBackspaceAsync()`. Lets `Core` stay Win32-free.
- `TypingOptions` — WPM, typo toggle, hotkey combo, passage text. Serializable.
- `WinInputKeySender : IKeySender` — lives in `App`; wraps `SendInput`.
- `HotkeyManager` — wraps `RegisterHotKey`/`UnregisterHotKey`, hooked via `HwndSource.AddHook` to catch `WM_HOTKEY`.

## 3. Phased Implementation Plan

### Phase 1 — Scaffold the solution
- Create `.sln` with `AutoTyper.Core` (class library) and `AutoTyper.App` (WPF).
- Set up project references: `App` → `Core`.
- Deliverable: solution builds with empty `Core` and a blank WPF window.

### Phase 2 — Port the typing engine to Core
- Translate `GetLetterPause`, `TypeWordWithTypos`, and `HumanType` into an async `TypingEngine` class.
- Replace direct key-sending calls with the `IKeySender` interface.
- Keep typo-injection rate, notice-delay, and backspace-correction logic equivalent to the AHK version.
- Deliverable: `TypingEngine.RunAsync(...)` compiles and runs against a fake `IKeySender` in a console test harness.

### Phase 3 — Implement Win32 key sending
- Build `WinInputKeySender` using P/Invoke `SendInput` with `KEYEVENTF_UNICODE` for characters and a synthetic `VK_BACK` press for backspace.
- Deliverable: a console app can type a hardcoded string into Notepad via the engine + real sender.

### Phase 4 — Implement global hotkey capture
- Build `HotkeyManager` around `RegisterHotKey`/`UnregisterHotKey`.
- Build a "press a key combo" capture control (replacing the AHK dropdown) that records modifiers + key.
- Deliverable: pressing a user-chosen combo fires an event, even when the app window isn't focused.

### Phase 5 — Build the WPF UI
- Recreate: passage textbox, WPM field, hotkey capture control, typo checkbox, Activate/Deactivate buttons, status text.
- Add a tray icon (`Hardcodet.NotifyIcon.Wpf`) for minimize/restore, mirroring the AHK tray menu.
- Deliverable: full UI parity with the AHK MVP's window.

### Phase 6 — Wire up async typing with cancellation
- On hotkey trigger, run `TypingEngine.RunAsync(...)` on a background `Task` so the UI thread stays responsive.
- Bind Cancel (button + optional Escape hotkey) to `CancellationTokenSource.Cancel()`.
- Deliverable: typing runs without freezing the UI; cancellation stops mid-word cleanly.

### Phase 7 — Add settings persistence
- Serialize `TypingOptions` to JSON in `%AppData%\AutoTyper` on save; load on startup.
- Deliverable: closing and reopening the app restores the last-used passage, WPM, hotkey, and typo setting.

### Phase 8 — Write unit tests and package the app
- Unit-test `TypingEngine` with a seeded `Random` and a fake `IKeySender`: verify typo injection rate, pause timing bounds, and cancellation behavior — no real input involved.
- `dotnet publish` as a self-contained single-file `win-x64` executable.
- Deliverable: test suite passes in CI; distributable `.exe` produced.

## 4. Dependencies

- **`Hardcodet.NotifyIcon.Wpf`** — tray icon support (NuGet).
- P/Invoke for `SendInput` and `RegisterHotKey` — no package required, just `DllImport` declarations against `user32.dll`.
- `System.Text.Json` — settings serialization (built into .NET).

## 5. Testing Strategy

- **Unit tests** (`Core` only): timing bounds, typo-injection probability, cancellation propagation — all via a fake `IKeySender`, no real keystrokes sent.
- **Manual/integration testing** (`App`): verify real key sending into Notepad/browser fields, hotkey capture across different apps, tray behavior, and settings round-tripping.

## 6. Suggested Starting Point

Begin with Phases 1–2 using a throwaway console harness that calls `TypingEngine` directly with a fake sender. This validates the timing and typo logic before any UI or Win32 code is written, keeping early iteration fast.

## 7. Stretch Goals (Post-MVP)

Features cut from the original AHK script that could be reintroduced once the core port is stable:

- Phrase bursts and burst cooldowns
- Gradual WPM drift over the course of typing
- "Type within X minutes" timeframe mode
- WPM-range mode (random WPM within a min/max band)
