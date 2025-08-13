# Audio Device Switcher - User Manual

## Overview

Audio Device Switcher allows you to quickly switch between audio output devices using clicks, hotkeys, or external commands.

## Basic Usage

### Switching Audio Devices

-   **Single/Double-click** any device in the list to make it the default
-   **Active device** is highlighted in green with a green dot
-   Click mode can be changed in Settings

### System Tray

-   **Double-click** tray icon to show/hide main window
-   **Right-click** tray icon for menu: Show / About / Exit
-   **Tooltip** shows current active device and ID

## Device Management

### Right-click Device Options

-   **Custom Icon...** - Choose from 18 available icons
-   **Set Hot Key...** - Assign global keyboard shortcut
-   **Edit Remarks...** - Add personal notes
-   **Hide from list** - Remove device from main view

### Global Hotkeys

-   Work system-wide (even when app is minimized)
-   Require at least one modifier key (Ctrl/Alt/Shift)
-   Set via right-click → "Set Hot Key..."

## Settings Window

### Settings Tab

-   **Start with Windows** - Auto-launch on boot
-   **Start in system tray** - Launch hidden
-   **Single-click switching** - Change click behavior
-   **Show hidden device count** - Display hidden devices info

### Hidden Devices Tab

-   **View hidden devices** - See all devices you've hidden
-   **Restore devices** - Click "Show" to unhide individual devices
-   **Restore all** - Unhide all devices at once

### Supporter Tab

-   **Register donation** - Enter email and supporter key to disable reminders
-   Only visible if you haven't registered yet

## External Control

### Registry Commands

Switch devices from other programs using Windows Registry:

cmd

```cmd
reg add "HKCU\SOFTWARE\AudioDeviceSwitcher" /v "SwitchToAudioDevice" /t REG_SZ /d "D2" /f
```

Replace "D2" with any device ID shown in the main window.

## Device IDs

-   Each device gets a unique ID (D1, D2, D3, etc.)
-   IDs are shown in the "ID" column
-   IDs persist across sessions for consistency

## Tips

-   **Minimize to tray** instead of closing for quick access
-   **Use hotkeys** for instant switching without opening the app
-   **Hide unused devices** to keep the list clean
-   **Check Settings** to customize behavior to your preferences

----------

**Support:** If you find this software useful, please consider making a donation via the PayPal button.
