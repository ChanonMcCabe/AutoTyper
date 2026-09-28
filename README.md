# AutoTyper

A desktop utility that types text like a human would — with natural pacing, drift, the occasional typo and correction, and configurable pauses.

> **Responsible use**: AutoTyper simulates human typing behavior (typo injection, WPM drift, bursts and pauses, step-away). It's built for practice, accessibility, and testing scenarios. You're responsible for complying with the terms of service of whatever application or site you point it at.

## Features

- **Typo injection** — occasional realistic mistakes, followed by correction
- **WPM drift** — typing speed wanders naturally instead of staying perfectly constant
- **Bursts and pauses** — variable-speed bursts and human-like pauses between words/sentences
- **Step-away** — periodically simulates stepping away mid-task
- **Global hotkey** — start/stop typing from anywhere with a hotkey you configure in-app
- **Cross-platform** — runs on Windows and macOS

## Requirements

- Windows or macOS
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) to build from source

## Install

There isn't a pre-built download yet — build from source below.

## Build from source

```
dotnet build AutoTyper.sln
dotnet run --project AutoTyper.Desktop
```

The solution has five projects:

- **AutoTyper.Core** — the platform-agnostic typing engine, settings schema, and hotkey abstractions
- **AutoTyper.Desktop** — the Avalonia desktop app; this is what you run
- **AutoTyper.Harness** — a Windows-only internal dev/test harness for driving real typing against Notepad; not part of the shipped app
- **AutoTyper.Core.Tests** / **AutoTyper.Desktop.Tests** — xUnit test suites, run with `dotnet test`

## Hotkeys

The trigger combo is configurable in-app — there's no fixed default.

## macOS notes

macOS builds are ad-hoc signed, not notarized (no paid Apple Developer account). On first launch, Gatekeeper will block the app as being "from an unidentified developer" — right-click the app and choose **Open** to run it anyway.

## License

MIT — see [LICENSE](LICENSE).
