# Monitorian

## TODO
- [X] ~~Add UI for changing key binds~~
- [X] ~~Create release/download link for this fork~~

## Overview
Monitorian is a Windows desktop tool to adjust the brightness of multiple monitors with ease.

Change the brightness of monitors, including external ones, either individually or in unison. Adjust brightness and contrast ranges seamlessly for each monitor.

## Requirements
- Windows 7 or newer
- .NET Framework 4.8
- External monitors must be DDC/CI enabled

## Key Features
- Adjust brightness of multiple monitors individually or together
- Adjust brightness using keyboard (can be changed in the menu)
- Touchpad support (precision touchpad required - swipe horizontally with two fingers)
- Customizable monitor names (press and hold to edit)
- Adjustable brightness and contrast ranges
- Ambient light sensor support

## Add-on Features (Microsoft Store version)
### Hot Keys & Shortcut Keys
- All hot keys for brightness can be switched to contrast
- Dedicated hot keys for switching between brightness and contrast modes

### Command-Line Options
Get or set brightness/contrast programmatically:

```bash
# Get brightness
monitorian /get
monitorian /get all
monitorian /get [Device Instance ID]

# Set brightness
monitorian /set [Brightness]
monitorian /set all [Brightness]
monitorian /set [Device Instance ID] [Brightness]

# Contrast (add 'contrast' after /get or /set)
monitorian /get contrast all
monitorian /set contrast 40
```

Brightness/contrast values: 0-100%, or use +/- for incremental changes (e.g., +10, -10)

## Troubleshooting: Monitor Not Detected?

Common reasons:
1. Monitor doesn't support DDC/CI or the setting is OFF (check OSD menu)
2. Cable, converter, or docking station isn't DDC/CI compatible
3. Monitor's DDC/CI implementation is non-standard
4. PC connector doesn't support DDC/CI
5. Hardware issues (connection problems, especially with older monitors)

### Debugging
Access hidden menu by **clicking the app title 3 times**:
- `Probe into monitors` - generates probe.log with detailed compatibility info
- `Make operation log` - enables operation.log recording
- `Rescan monitors` - manually triggers monitor detection

## Credits
Original project by **emoacht** (emotom[atmark]pobox.com)  
Original repository: https://github.com/emoacht/Monitorian

## Development
### Setup
1. Install Visual Studio with:
   - .NET Framework 4.8 SDK and targeting pack
   - Windows 10 SDK (10.0.19041.0)
2. Load `/Source/Monitorian.sln`
3. Restore NuGet packages
4. For installer: Install WiX Toolset Build Tools and VS Extension

### Adding Languages
Add Resources file to `/Source/Monitorian.Core/Properties/`:
- Format: `Resources.[language-culture].resx`
- Names must match default `Resources.resx`

## License
MIT License

## Libraries
- [XamlBehaviors for WPF](https://github.com/microsoft/XamlBehaviorsWpf)

