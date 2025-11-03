# ScreenSwitcher

Automatic OBS video source switcher for multi-monitor setups. ScreenSwitcher tracks which monitor your mouse is on and automatically switches which display capture source is visible in OBS - perfect for streamers with massive multi-monitor arrays who want one big focused view that follows their cursor.

## What It Does

- Monitors your mouse cursor position across all displays
- Detects which monitor you're currently on (Windows display numbers 1, 2, 3...)
- Automatically shows/hides OBS display capture sources based on your cursor location
- Keeps non-toggle sources visible (for multi-view layouts)

## Prerequisites

1. **Windows** (uses Windows API for monitor detection)
2. **.NET 8** runtime or SDK
3. **OBS Studio** with WebSocket server enabled
4. **Multiple monitors** (obviously!)

## Setup Instructions

### Step 1: Enable OBS WebSocket Server

1. Open OBS Studio
2. Go to **Tools > WebSocket Server Settings**
3. Check **"Enable WebSocket server"**
4. Note the **Server Password** (you'll need this)
5. Leave the port as default (4455) unless you have a reason to change it
6. Click **OK**

### Step 2: Create Your Toggle Sources in OBS

You need to create display capture sources with a specific naming pattern:

1. In your OBS scene, add Display Capture sources for each monitor
2. Name them using the pattern: **`T-Monitor-N`** where N is the Windows display number
   - Example: `T-Monitor-1`, `T-Monitor-2`, `T-Monitor-3`, etc.
   - The "T-" prefix stands for "Toggle" (no pun intended with monitor/moniker!)

**How to find your display numbers:**
- Go to Windows Settings > System > Display
- Click **"Identify"** - Windows will show a number on each monitor
- Use those numbers in your source names

**Pro tip:** You can keep your original display sources visible for a multi-view layout, and only the T-Monitor sources will toggle. This gives you a persistent small multi-view plus one big focused display that follows your mouse!

### Step 3: Configure ScreenSwitcher Password

1. In the ScreenSwitcher project root, find or create the file: `obs-password.txt`
2. Paste your OBS WebSocket password into this file (just the password, nothing else)
3. Save the file

**Note:** This file is excluded from git for security.

### Step 4: Build and Run

#### Option A: Run from Visual Studio
1. Open `ScreenSwitcher.sln` in Visual Studio
2. Press F5 or click the Run button
3. The console will show you which monitor you're on as you move your mouse

#### Option B: Run from Command Line
```bash
cd ScreenSwitcher
dotnet run
```

#### Option C: Build a Standalone Executable
```bash
dotnet publish -c Release -r win-x64 --self-contained
# Executable will be in: bin/Release/net8.0/win-x64/publish/ScreenSwitcher.exe
```

### Step 5: Test It Out!

1. Make sure OBS is running
2. Launch ScreenSwitcher
3. You should see output like:
   ```
   Found 7 T-Monitor sources
   ScreenSwitcher running... Move your mouse between monitors!

   Display 4 [PRIMARY] (\\.\DISPLAY5) - Position: (0, 0), Size: 3440x1440 -> T-Monitor-4
   Display 2 (\\.\DISPLAY3) - Position: (3440, 0), Size: 2560x1440 -> T-Monitor-2
   ```
4. Move your mouse between monitors and watch the OBS sources toggle!

## Usage

- Run ScreenSwitcher whenever you want automatic monitor switching during streaming/recording
- Keep the console window open (minimize it if you want)
- Press **Ctrl+C** to stop the program
- The program polls mouse position every 100ms - fast enough to feel instant, light enough to not impact performance

## Troubleshooting

**"Failed to connect to OBS"**
- Make sure OBS is running
- Check that WebSocket server is enabled in OBS
- Verify the password in `obs-password.txt` matches OBS settings

**"Found 0 T-Monitor sources"**
- Make sure your OBS sources are named exactly `T-Monitor-1`, `T-Monitor-2`, etc.
- Check that you're looking at the correct scene
- Source names are case-sensitive

**Wrong monitors switching**
- Use Windows Settings > Display > "Identify" to see your display numbers
- Make sure your OBS source names match those numbers
- Example: If Windows shows "4" on your main monitor, name the source `T-Monitor-4`

**Sources not toggling**
- Check the console output to see which source it's trying to switch to
- Verify those sources exist in your current OBS scene
- Make sure OBS WebSocket password is correct

## How It Works (Technical)

1. **Monitor Detection**: Uses Windows API (`GetCursorPos`, `MonitorFromPoint`, `GetMonitorInfo`) to track cursor position and determine which monitor it's on
2. **Display Number Mapping**: Uses `EnumDisplayDevices` to enumerate displays and map Windows device names to logical display numbers (the ones you see in "Identify")
3. **OBS Control**: Connects to OBS WebSocket server and uses `SetSceneItemEnabled` to toggle source visibility
4. **Convention-Based**: No config files needed - just name your sources `T-Monitor-N` and everything works automatically

## Project Structure

- **ScreenChangeNotifier.cs** - Monitors mouse position and detects monitor changes
- **OBSService.cs** - Wrapper for OBS WebSocket communication
- **Program.cs** - Ties everything together

## Notes

- Display numbers are based on Windows' enumeration order, not physical layout
- The program uses monitor handles internally, but these change on reboot - display numbers are stable
- Only sources starting with "T-Monitor-" are toggled - everything else stays visible
- Perfect for huge multi-monitor setups (tested with 7 monitors!)

---

**Vibe coded in 2 hours with [Claude Code](https://claude.com/claude-code)** 
