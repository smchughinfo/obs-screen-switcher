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
├── ScreenSwitcher/          # Main console application
│   ├── Program.cs           # Entry point (minimal)
│   ├── OBSService.cs        # OBS WebSocket communication
│   └── ScreenSwitcher.csproj
├── SessionMemory/           # Development session logs
├── obs-password.txt         # OBS WebSocket password (NOT in git)
├── .gitignore               # .NET specific gitignore
├── CLAUDE.md               # This file
└── README.md               # Project documentation
```

## Architecture

### Core Functionality

1. **Monitor Detection Loop**
   - Poll mouse position at regular intervals (e.g., 500ms)
   - Use Windows API to determine which monitor the cursor is on
   - Track monitor changes to avoid redundant OBS commands

2. **OBS WebSocket Integration**
   - Connect to local OBS WebSocket server
   - Send scene/source switch commands when monitor changes
   - Handle connection loss and reconnection

3. **Configuration**
   - Map monitor IDs to OBS scene/source names
   - Configurable polling interval
   - OBS WebSocket connection settings (host, port, password)

### Windows API Usage

**Key P/Invoke calls:**
- `User32.GetCursorPos()` - Get current mouse position (x, y)
- `User32.MonitorFromPoint()` - Get monitor handle from point
- `User32.GetMonitorInfo()` - Get monitor details (bounds, primary, etc.)

### OBS WebSocket Protocol

- **Connection**: ws://localhost:4455 (default OBS WebSocket port)
- **Authentication**: Optional password
- **Commands**:
  - Switch scenes: `SetCurrentProgramScene`
  - Switch sources: `SetSceneItemEnabled`
  - Get current state: `GetCurrentProgramScene`

## Configuration Pattern

Expected configuration file (`config.json`):
```json
{
  "obsWebSocket": {
    "host": "localhost",
    "port": 4455,
    "password": ""
  },
  "polling": {
    "intervalMs": 500
  },
  "monitorMappings": [
    {
      "monitorIndex": 0,
      "obsSceneName": "Monitor 1 Scene"
    },
    {
      "monitorIndex": 1,
      "obsSceneName": "Monitor 2 Scene"
    }
  ]
}
```

## Running the Application

### Development
```bash
cd ScreenSwitcher
dotnet run
```

### Production
```bash
dotnet publish -c Release -r win-x64 --self-contained
# Creates standalone executable in bin/Release/net8.0/win-x64/publish/
```

### Auto-Start Options
1. **Task Scheduler**: Create task to run on login
2. **Startup Folder**: Place shortcut in `shell:startup`
3. **Windows Service**: Convert to service (future enhancement)

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

1. **Console Application**: Simple, easy to debug, can run minimized or via Task Scheduler
2. **.NET 8**: Latest LTS, excellent Windows API interop, modern async patterns
3. **Polling vs Events**: Polling chosen for simplicity (Windows mouse events more complex)
4. **Configuration File**: JSON for easy editing without recompilation
5. **Monitor Index**: Using monitor index rather than resolution/name for reliability

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
