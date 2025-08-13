# Audio Device Switcher

A lightweight Windows application for quick and easy switching between audio output devices. <br> 
Features global hotkeys, system tray operation, and external program control via Windows Registry. <br>
This utility simplifies the audio output device switching, which for some reason Windows have located in a "strange place" in the audio tray icon.<br>


<div style="display: flex; flex-wrap: wrap; gap: 15px; justify-content: center;">

  <img src="https://github.com/user-attachments/assets/2743a8eb-637b-4b45-829f-6217d89c3624" 
       alt="Image 1" 
       width="293" height="228"
       style="object-fit: contain; border-radius: 8px; box-shadow: 0 2px 6px rgba(0,0,0,0.2);">

  <img src="https://github.com/user-attachments/assets/e30566a4-7759-446d-9cf5-638aafa1869b" 
       alt="Image 2" 
       width="293" height="228"
       style="object-fit: contain; border-radius: 8px; box-shadow: 0 2px 6px rgba(0,0,0,0.2);">

  <img src="https://github.com/user-attachments/assets/33fd0e18-26f8-47d7-ac16-a42b7cc6696d" 
       alt="Image 3" 
       width="150" height="150"
       style="object-fit: contain; border-radius: 8px; box-shadow: 0 2px 6px rgba(0,0,0,0.2);">

  <img src="https://github.com/user-attachments/assets/9dfed9fd-bddb-4c29-a02e-171556a6227b" 
       alt="Image 4" 
       width="250" height="250"
       style="object-fit: contain; border-radius: 8px; box-shadow: 0 2px 6px rgba(0,0,0,0.2);">

</div>



## Features

###  **Device Management**

-   **One-click switching** between audio output devices
-   **Visual device identification** with smart icons (🎧 headphones, 🔊 speakers, 🖥️ monitors)
-   **Device customization** with custom icons, remarks, and hotkeys
-   **Hide unwanted devices** from the main list

###  **Global Hotkeys**

-   **System-wide shortcuts** for instant device switching
-   **Customizable combinations** (Ctrl/Alt/Shift + any key)
-   **Works when minimized** - no need to open the application

###  **System Integration**

-   **System tray operation** with right-click menu and tooltips
-   **Windows startup** integration
-   **External program control** via Windows Registry commands


## Installation

1.  Download the latest release from [GitHub Releases](https://github.com/limbo666/Audio-Device-Switcher/releases)
2.  Extract the ZIP file to your desired location
3.  Run `Audio Device Switcher.exe`
4.  The application will appear in your system tray

## Quick Start

### Basic Usage

1.  **Switch devices**: Click any device in the list to make it default
2.  **System tray**: Double-click tray icon to show/hide window
3.  **Settings**: Click "Settings" button to customize behavior

### Setting Up Hotkeys

1.  Right-click any device → "Set Hot Key..."
2.  Choose modifier keys (Ctrl/Alt/Shift) and main key
3.  Click OK to save
4.  Use the hotkey combination anytime to switch to that device

## External Program Control

Other applications can control Audio Device Switcher through Windows Registry commands:

### Switch to Device Examples

**Command Prompt:**

cmd

```cmd
reg add "HKCU\SOFTWARE\AudioDeviceSwitcher" /v "SwitchToAudioDevice" /t REG_SZ /d "D2" /f
```

**PowerShell:**

powershell

```powershell
Set-ItemProperty -Path "HKCU:\SOFTWARE\AudioDeviceSwitcher" -Name "SwitchToAudioDevice" -Value "D3"
```

### Registry Keys

Key

Purpose

Example Value

`CurrentAudioDevice`

Current device name (read-only)

"Speakers (Realtek Audio)"

`CurrentAudioDeviceID`

Current device ID (read-only)

"D2"

`SwitchToAudioDevice`

Command to switch device

"D5"

### How It Works

1.  Write a device ID (D1, D2, D3, etc.) to `SwitchToAudioDevice`
2.  Audio Device Switcher detects the change within 500ms
3.  Switches to the specified device
4.  Resets the registry value to "XX" to prevent repeated switches

Device IDs are shown in the "ID" column of the main application window.



## System Requirements

-   **OS**: Windows 7 or later
-   **Framework**: .NET Framework 4.8
-   **Permissions**: User-level (no administrator required)
-   **Dependencies**: All included in the release package

## Building from Source

### Prerequisites

-   Visual Studio 2019 or later
-   .NET Framework 4.8 SDK

### Dependencies

-   AudioSwitcher.AudioApi.CoreAudio
-   Newtonsoft.Json
-   System.Windows.Forms (for tray icon)


## Support This Project

Audio Device Switcher is **free and open source**. If you find it useful, please consider supporting its development:


<a href="[https://example.com](https://www.paypal.com/donate/?hosted_button_id=658JPTR7W5LNL)">
  <img src="https://github.com/user-attachments/assets/6410c0f3-d879-4f21-94ec-89c89b8e9e0a" alt="Alt text" height="100">
</a>

Your support helps:

-    Keep the software updated and improved
-    Fix bugs and add new features
-    Maintain documentation and user support
-    Ensure compatibility with new Windows versions

**Supporters** receive a special key to disable donation reminders while keeping all functionality free.

## Contributing

Contributions are welcome! Please feel free to:

-    Report bugs via GitHub Issues
-    Suggest new features
-    Submit pull requests
-    Improve documentation

## License

This project is licensed under the **MIT License** - see below for details:

```
MIT License

Copyright (c) 2025 Nikos Georgousis

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## Author

**Nikos Georgousis**  
🌐 [Hand Water Pump](http://georgousis.info)  
📧 Contact via PayPal donation for support requests

----------

⭐ **Star this repository** if you find it helpful!
