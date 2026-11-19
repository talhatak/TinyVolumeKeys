# Tiny Volume Keys

A small Windows desktop app for assigning volume controls to function keys, even on keyboards without an Fn key. No installation or third-party packages required. Uses the Windows .NET Framework and native global hotkeys, and runs without administrator access.

## Requirements

- Windows with .NET Framework 4.x (normally included with Windows 10/11).
- A keyboard with function keys.

## Quick start

1. Open `TinyVolumeKeys.exe`. If you only have the source code, follow **Build from source** below first.
2. Use the default shortcuts, or choose your own keys and click **Apply**.
3. Close the window to keep shortcuts running in the system tray.

The initial shortcuts are:

| Key | Action |
| --- | --- |
| F9 | Mute / unmute |
| F10 | Volume down |
| F11 | Volume up |

## Customize shortcuts

Choose F1 through F11, optionally with Ctrl, Alt, Shift, or combinations of these. Click **Apply** to save and activate your choices. Choose **Disabled** to leave an action unassigned. Hold a volume key to repeat; mute toggles once per press. F12 is reserved for the Windows debugger and is intentionally excluded.

Mapped shortcuts replace their usual action while the app runs. **Pause** restores normal keys; **Resume** reactivates the saved mappings. Applying changes also resumes shortcuts. If another app already owns a shortcut, the app displays a conflict message; choose another key or modifier.

## System tray controls

Closing the window keeps the app in the notification area. Double-click its information icon to reopen settings; right-click to pause, resume, or exit. **Exit** releases all shortcuts. Windows may put the icon under the tray's hidden icons arrow.

## Saved settings

Settings are saved to `%LOCALAPPDATA%\TinyVolumeKeys\settings.xml` and loaded the next time you open the app.

## Run at startup

The app does not add itself to Windows startup. To start it when you sign in:

1. Keep `TinyVolumeKeys.exe` in a permanent folder and create a shortcut to it.
2. Press **Win+R**, type `shell:startup`, and press **Enter**.
3. Move the shortcut into the folder that opens.

Remove that shortcut to stop automatic startup. If you move the executable later, update the shortcut.

## Limitations

- Shortcuts work while the app is running in your signed-in Windows session.
- Protected desktops, some games, and elevated apps can restrict synthetic input.
- The executable is unsigned.

## Build from source

Open PowerShell in the project folder containing `build.ps1` and `TinyVolumeKeys.cs`, then run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

The build creates `TinyVolumeKeys.exe` in that same folder. Exit any running copy of the app before rebuilding.

This uses the C# compiler supplied with Windows .NET Framework. No .NET SDK or NuGet restore is needed.

Native hotkey behavior: [Microsoft RegisterHotKey documentation](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerhotkey).
