# ScreenSwitcher Project

## Project Overview

**ScreenSwitcher** - Automated OBS video source switcher that detects which monitor your mouse is on and changes the displayed OBS video source accordingly.

**Core Value Proposition:** Automatic hands-free switching between screens in OBS based on mouse position - no manual scene switching needed during streaming or recording.

## Tech Stack

**Framework:**
- **.NET 8**: Console Application
- **Language**: C#
- **Platform**: Windows (WSL2 environment)

**Key Technologies:**
- **Windows API**: For mouse position tracking (`GetCursorPos`) and monitor detection (`MonitorFromPoint`)
- **OBS WebSocket**: For controlling OBS scene/source switching
- **P/Invoke**: For native Windows API calls

**Dependencies:**
- `obs-websocket-dotnet` - OBS WebSocket library for .NET
- Windows User32.dll (via P/Invoke) - Mouse and monitor APIs

## Project Structure

```
/ScreenSwitcher/
├── ScreenSwitcher/              # Main console application
│   ├── Program.cs               # Entry point - wires everything together
│   ├── ScreenChangeNotifier.cs  # Monitor detection via Windows API
│   ├── OBSService.cs            # OBS WebSocket communication wrapper
│   └── ScreenSwitcher.csproj
├── SessionMemory/               # Development session logs
├── obs-password.txt             # OBS WebSocket password (NOT in git)
├── .gitignore                   # .NET specific gitignore
├── CLAUDE.md                    # This file
└── README.md                    # User documentation
```

## Architecture

### Core Functionality

1. **Monitor Detection Loop** (`ScreenChangeNotifier.cs`)
   - Polls mouse position at 100ms intervals
   - Uses Windows API to determine which monitor the cursor is on
   - Tracks monitor changes and raises `ScreenChanged` event
   - Provides monitor info: position, size, device name, logical display number

2. **Display Number Resolution**
   - Uses `EnumDisplayDevices` to enumerate attached displays
   - Maps device names (`\\.\DISPLAY5`) to logical display numbers (1, 2, 3...)
   - Display numbers match what Windows shows in "Identify" dialog
   - Handles gaps in Windows display numbering automatically

3. **OBS WebSocket Integration** (`OBSService.cs`)
   - Connects to local OBS WebSocket server (ws://localhost:4455)
   - Authenticates using password from `obs-password.txt`
   - Provides simple methods: `SetSourceVisible`, `GetSourceVisible`, `GetSceneItems`
   - No configuration file needed - convention-based naming

4. **Source Switching Logic** (`Program.cs`)
   - Finds all OBS sources starting with `T-Monitor-`
   - When monitor changes, hides all T-Monitor sources
   - Shows only the source matching current display number
   - Example: Mouse on Display 3 → shows `T-Monitor-3`

### Windows API Usage

**Key P/Invoke calls:**
- `GetCursorPos()` - Get current mouse position (x, y)
- `MonitorFromPoint()` - Get monitor handle from point
- `GetMonitorInfo()` - Get monitor details using MONITORINFOEX struct (bounds, device name, primary flag)
- `EnumDisplayDevices()` - Enumerate display adapters to get logical display numbers

### OBS WebSocket Protocol

- **Connection**: ws://localhost:4455 (default OBS WebSocket port)
- **Authentication**: Password stored in `obs-password.txt`
- **Commands Used**:
  - `GetCurrentProgramScene` - Get active scene name
  - `GetSceneItemList` - List all sources in a scene
  - `SetSceneItemEnabled` - Show/hide individual sources
  - `GetSceneItemEnabled` - Check if source is visible

## Naming Convention

**No configuration file needed!** Uses convention-based naming:

- OBS sources must be named `T-Monitor-N` where N is the Windows display number
- Example: `T-Monitor-1`, `T-Monitor-2`, `T-Monitor-3`, etc.
- Display numbers match Windows Display Settings "Identify" dialog
- The "T-" prefix means "Toggle" - these are the sources that switch automatically
- Original display sources (Display-0-0, Display-2-1, etc.) remain always visible for multi-view

## Running the Application

### Development
```bash
cd ScreenSwitcher
dotnet run
```

### Production Build
```bash
dotnet publish -c Release -r win-x64 --self-contained
# Creates standalone executable in bin/Release/net8.0/win-x64/publish/
```

### Running
- Just launch the console app when you want automatic monitor switching
- Keep it running in the background while streaming/recording
- Press Ctrl+C to stop

## Session Memory

### Purpose
Session memory provides a chronological record of development conversations and progress, capturing key decisions, technical insights, and project evolution.

### Implementation
When requested to save session memory:

1. **Summarize conversation** focusing on technical progress and decisions
2. **Save to** `/SessionMemory/MM-DD-YY-HH-MM-topic.md`
3. **Include**: Development progress, technical insights, decisions made, and next steps

### File Structure
```
/SessionMemory/
├── 11-03-25-01-00-initial-setup.md
└── [future sessions]
```

## Development Guidelines

### Code Style
- **Minimal code, assume success**: Remove unnecessary logging, try/catch blocks, and error messages. Let things fail naturally.
- **Separation of concerns**: Keep Program.cs minimal. Move logic into dedicated service classes.
- **Use async/await** for OBS WebSocket communication
- **Clean shutdown** on Ctrl+C (CancellationToken)
- **Avoid busy-waiting** - use Task.Delay for polling


### Performance Considerations
- Minimize Windows API calls
- Cache monitor information
- Only send OBS commands on actual monitor changes
- Configurable polling interval to balance responsiveness vs CPU usage

## Key Design Decisions

1. **Console Application**: Simple, easy to debug, runs manually when needed
2. **.NET 8**: Latest LTS, excellent Windows API interop, modern async patterns
3. **Polling vs Events**: Polling (100ms) chosen for simplicity over complex Windows hook APIs
4. **Convention over Configuration**: No config file - uses `T-Monitor-N` naming convention
5. **Display Number Resolution**: Uses `EnumDisplayDevices` to get Windows logical display numbers
6. **Monitor Handle Instability**: Handles change on reboot, so we map device names to stable display numbers
7. **Event-Driven Architecture**: `ScreenChangeNotifier` raises events, `Program.cs` orchestrates

## Future Enhancements

- System tray icon with context menu
- GUI for configuration
- Support for source-level switching (not just scenes)
- Multi-application support (beyond OBS)
- Hotkey support for manual overrides
- Activity-based switching (keyboard activity, window focus)
- Convert to Windows Service for true background operation

## Known Limitations

- Windows-only (requires Windows API)
- Requires OBS WebSocket plugin enabled
- Mouse position polling (not event-driven)
- Requires user session (can't run as system service without modifications)

## Important Notes

- **OBS WebSocket Version**: Ensure OBS has WebSocket 5.x+ enabled (Tools > WebSocket Server Settings)
- **Monitor Indexing**: Windows may reorder monitors on reboot - test configuration after restart
- **Performance**: Default 500ms polling is balance of responsiveness vs CPU - adjust as needed
- **Security**: If OBS WebSocket password is set, must be in config (consider encryption for production)
