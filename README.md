# JXT KEY LOCK INDICATOR

A lightweight Windows tray app that shows a clean, modern on-screen indicator for **Caps Lock**, **Num Lock** and **Scroll Lock**. It is made for keyboards that have no lock LEDs.

It starts with Windows, sits quietly in the system tray, and never steals focus or blocks your clicks.

**Publisher:** JXT SIDHU

---

## Features

- **Instant lock status** for Caps, Num and Scroll Lock (green when ON, dim when OFF)
- **Crisp, anti-aliased design** with per-pixel transparency, soft glow and shadow, and DPI-aware rendering that stays sharp on high-resolution screens
- **Non-intrusive:** always on top, click-through, no taskbar button, and it never takes keyboard focus
- **Always visible option:** pin any lock (for example Caps Lock) so it is shown permanently
- **Display modes:** always show, show only while ON, or flash briefly when a lock is toggled
- **Position anywhere:** 7 presets, or unlock and drag it to any spot on any monitor
- **Starts with Windows,** hidden in the tray
- **Resizable settings window** with live preview, opened from the tray icon

### Advanced options tab

- Custom colors: ON, OFF, ON text and OFF text
- Rename the labels (CAPS, NUM, SCROLL)
- Horizontal or vertical layout
- Font choice, corner roundness, glow and shadow toggle, ON/OFF text toggle
- Distance from the screen edge, and choice of monitor
- Optional sound when a lock changes
- One-click reset to defaults

---

## Requirements

- Windows 10 or 11
- .NET Framework 4.x (already included with Windows; nothing to install)

## Build and run

1. Download `JXTKeyLock.cs` and `Build.bat` into the same folder.
2. Double-click **`Build.bat`**. It uses the C# compiler that ships with Windows, creates `JXT KEY LOCK INDICATOR.exe`, and starts it.
3. Find the tray icon near the clock (click the **^** arrow if it is hidden). Click it to open settings.

The settings window opens automatically on the first run only. After that the app starts hidden.

## Start with Windows

"Start with Windows" is on by default and can be changed in **Settings > General**. The app re-registers itself every time it launches, so a moved or rebuilt exe still starts correctly. It uses the per-user startup entry (`HKCU\...\Run`) and needs no administrator rights. You can also see or toggle it in **Windows Settings > Apps > Startup**.

## Files and data

| What | Where |
| --- | --- |
| Settings | `%APPDATA%\JXT KEY LOCK INDICATOR\settings.ini` |
| Error log (only if something goes wrong) | `%APPDATA%\JXT KEY LOCK INDICATOR\error.log` |

## Uninstall

1. Untick **Start with Windows** in settings.
2. Right-click the tray icon and choose **Exit**.
3. Delete the exe, and optionally the `%APPDATA%\JXT KEY LOCK INDICATOR` folder.

## Troubleshooting

- **Build fails:** make sure `JXTKeyLock.cs` and `Build.bat` are in the same folder, then open an issue with the error text.
- **Nothing appears after launching:** check the tray (**^** arrow). Launching the exe again opens the settings window of the running copy.
- **A message about a problem appears:** details are saved to `error.log` in the settings folder. Please include it in an issue.
- **Windows SmartScreen warning on a downloaded exe:** the exe is not code-signed. Building it yourself with `Build.bat` avoids this.

## How it works

The app polls the Windows keyboard toggle state (`GetKeyState`) about 10 times per second, which uses almost no CPU, and draws the indicator into a layered, click-through window. It is a single C# file with no dependencies beyond .NET Framework.

## License

MIT (add a `LICENSE` file to the repository).

---

Made by **JXT SIDHU**
