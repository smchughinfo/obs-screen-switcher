# Session Memory: ScreenSwitcher Development
**Date**: November 3, 2025, 1:00 AM - 3:00 AM
**Duration**: 2 hours
**Topic**: Building automated OBS multi-monitor source switcher from scratch

## Session Overview

Built ScreenSwitcher - a Windows console application that automatically switches OBS video sources based on which monitor the user's mouse is on. Designed for streamers with large multi-monitor setups (tested with 7 monitors) who want one big "focused" display that follows their cursor while maintaining a persistent multi-view layout.

## Problem Statement

User has a massive 7-monitor display setup for streaming programming sessions. At that scale, individual monitor text becomes unreadable when captured in OBS for streaming. Solution needed: automatically show only the monitor where the mouse is currently located, making text readable while maintaining context via a persistent small multi-view.

## Technical Architecture

### Core Components

1. **ScreenChangeNotifier.cs** - Monitor detection service
2. **OBSService.cs** - OBS WebSocket client wrapper
3. **Program.cs** - Application orchestration

### Key Technical Decisions

#### 1. Monitor Detection Strategy

**Initial Approach - Monitor Handles:**
- Used `GetCursorPos()` → `MonitorFromPoint()` → got `IntPtr` handle
- Problem: Handles are **runtime values that change on reboot/display reconfiguration**
- Not suitable for stable configuration

**Second Approach - Monitor Position:**
- Added `GetMonitorInfo()` with `MONITORINFOEX` struct to get bounds (x, y, width, height)
- Positions are stable across reboots as long as display layout doesn't change
- Issue: Didn't match user's mental model (Windows display numbers)

**Final Approach - Windows Display Numbers:**
- User pointed out Windows Display Settings "Identify" shows numbered displays
- Those numbers don't match device names (`\\.\DISPLAY5` might be Display 4)
- Solution: Use `EnumDisplayDevices()` to enumerate displays in order
- Only count displays with `DISPLAY_DEVICE_ATTACHED_TO_DESKTOP` flag
- Assign logical indices 1, 2, 3... matching Windows' enumeration order
- Map device names to these logical numbers
- **This matches what users see in Windows Display Settings!**

#### 2. Display Number Gaps Problem

Windows display device paths have gaps: `\\.\DISPLAY1`, `\\.\DISPLAY2`, `\\.\DISPLAY5`, `\\.\DISPLAY8`
- Missing numbers (3, 4, 6, 7) can be disconnected monitors Windows remembers
- Virtual displays, or just Windows being Windows
- Initial naive regex extraction (`\\d+` from device name) gave wrong numbers
- `EnumDisplayDevices` automatically handles this by counting only attached displays

#### 3. OBS Integration Strategy

**WebSocket over Plugins:**
- OBS has built-in WebSocket server (v5.x protocol)
- Much simpler than writing native plugins
- Used `obs-websocket-dotnet` NuGet package (v5.0.1)

**Key API Calls:**
- `ConnectAsync()` - Fire-and-forget, used TaskCompletionSource to await properly
- `GetSceneItemList()` - Get all sources in current scene
- `SetSceneItemEnabled(sceneName, itemId, visible)` - Toggle source visibility
- `GetCurrentProgramScene()` - Get active scene name

**Authentication:**
- Password stored in `obs-password.txt` (excluded from git)
- Connection must complete handshake before making requests
- Used event-driven approach: wait for `Connected` event before proceeding

#### 4. Convention Over Configuration

**No Config File Needed:**
- OBS sources named `T-Monitor-N` where N is Windows display number
- "T-" prefix stands for "Toggle" (user's pun with monitor/moniker noted!)
- Program filters sources starting with `T-Monitor-` at runtime
- Display numbers automatically detected via `EnumDisplayDevices`

**Benefits:**
- Zero configuration for users
- Self-documenting naming scheme
- Stable across reboots (display numbers are stable)
- Easy to add/remove monitors (just add/remove sources)

#### 5. Polling Strategy

**100ms polling interval:**
- Fast enough to feel instant (10 FPS effective detection rate)
- Light enough to not impact system performance
- Simple polling loop with `Task.Delay(100)`
- Considered Windows hooks but too complex for this use case

**Change Detection:**
- Track current monitor handle (`IntPtr`)
- Only raise event when handle changes
- Prevents redundant OBS commands on same monitor

### Windows API Deep Dive

#### P/Invoke Declarations

```csharp
[DllImport("user32.dll")]
private static extern bool GetCursorPos(out POINT lpPoint);

[DllImport("user32.dll")]
private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

[DllImport("user32.dll", CharSet = CharSet.Auto)]
private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

[DllImport("user32.dll", CharSet = CharSet.Auto)]
private static extern bool EnumDisplayDevices(string? lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);
```

#### Key Structs

**MONITORINFOEX:**
- Must set `Size` field before calling `GetMonitorInfo`
- `CharSet = CharSet.Auto` critical for string marshaling
- Contains device name like `\\.\DISPLAY5`
- Flags field indicates primary monitor (`MONITORINFOF_PRIMARY`)

**DISPLAY_DEVICE:**
- Used with `EnumDisplayDevices` to enumerate adapters
- `StateFlags & DISPLAY_DEVICE_ATTACHED_TO_DESKTOP` = actively used display
- `cb` field must be set to struct size
- Device names in `DeviceName` field match `MONITORINFOEX.DeviceName`

#### Critical Implementation Details

1. **Size Field Initialization:**
   ```csharp
   var mi = new MONITORINFOEX();
   mi.Size = (uint)Marshal.SizeOf(typeof(MONITORINFOEX));
   ```
   Initial bug: Used `Marshal.SizeOf(mi)` which returned 0. Fixed by using `typeof()`.

2. **CharSet Attribute:**
   Added `CharSet = CharSet.Auto` to both `GetMonitorInfo` DllImport and `MONITORINFOEX` struct.
   Without this, device name string was empty.

3. **Display Enumeration:**
   ```csharp
   uint displayNum = 0;
   int logicalIndex = 1;
   while (true)
   {
       var device = new DISPLAY_DEVICE();
       device.cb = (uint)Marshal.SizeOf(typeof(DISPLAY_DEVICE));

       if (!EnumDisplayDevices(null, displayNum, ref device, 0))
           break;

       if ((device.StateFlags & DISPLAY_DEVICE_ATTACHED_TO_DESKTOP) != 0)
       {
           _deviceToDisplayNumber[device.DeviceName] = logicalIndex;
           logicalIndex++;
       }

       displayNum++;
   }
   ```
   This maps `\\.\DISPLAYN` → logical number (1, 2, 3...) matching Windows UI.

## Development Timeline

### Phase 1: Initial Setup (15 min)
- Created .NET 8 console application
- Added `.gitignore` for .NET projects
- Created `obs-password.txt` for WebSocket auth
- Added `obs-websocket-dotnet` NuGet package
- Set up `CLAUDE.md` and project structure

### Phase 2: OBS Integration Test (30 min)
- Created `OBSService.cs` wrapper class
- Tested basic WebSocket connection
- **Bug**: `ConnectAsync` is void, can't await it
  - Solution: Used `TaskCompletionSource` with `Connected` event
- **Bug**: Authentication failing
  - Wrong password in file, updated to correct one
- **Bug**: API method names wrong (`GetSceneItemProperties`, `SetSceneItemRender` don't exist)
  - Research showed correct methods: `GetSceneItemEnabled`, `SetSceneItemEnabled`
  - Take `sceneItemId` (int), not source name (string)
- Successfully toggled first source in scene

### Phase 3: Monitor Detection (45 min)
- Created `ScreenChangeNotifier.cs`
- Implemented basic mouse tracking with handles
- Added `GetMonitorInfo()` to get position/size
- **Bug**: All values returning 0
  - Fixed struct size calculation
  - Added CharSet attribute
- Successfully detecting monitor changes with position info
- Printing monitor details to console

### Phase 4: Display Number Resolution (30 min)
- User reported: display numbers one too high, monitor 7 showing as monitor 8
- Root cause: Device path numbers (`\\.\DISPLAY5`) != Windows display numbers
- Researched `EnumDisplayDevices` API
- Implemented display enumeration and mapping
- Added `DisplayNumber` property to `MonitorInfo`
- Successfully matching Windows "Identify" dialog numbers

### Phase 5: Integration & Polish (20 min)
- Wired together `ScreenChangeNotifier` + `OBSService` in `Program.cs`
- Implemented convention-based `T-Monitor-N` naming
- Filter sources by prefix, build lookup dictionary
- On monitor change: hide all T-Monitor sources, show only active one
- Removed unused `MonitorConfig.cs` class
- Testing confirmed proper switching across all 7 monitors

### Phase 6: Documentation (20 min)
- Updated `CLAUDE.md` with final architecture
- Wrote comprehensive `README.md` with setup instructions
- Created this session memory document
- Added "vibe coded" timestamp to README

## Code Style Patterns

**Minimal Error Handling:**
- User preference: assume success, let it fail naturally
- No try/catch blocks unless absolutely necessary
- No verbose logging or validation messages

**Separation of Concerns:**
- `Program.cs` kept minimal - just wiring components
- Logic moved to dedicated service classes
- Clean single-responsibility classes

**Event-Driven Architecture:**
- `ScreenChangeNotifier` raises `ScreenChanged` event with `MonitorInfo`
- `Program.cs` subscribes and orchestrates OBS calls
- Loose coupling between components

**No Configuration Files:**
- Convention-based naming eliminates config
- Password in simple text file (gitignored)
- Self-discovering via OBS scene introspection

## Technical Insights

### Windows Display Quirks

1. **Handle Instability**: Monitor handles (`IntPtr`) change on reboot/reconfiguration
2. **Device Name Gaps**: `\\.\DISPLAYN` paths have gaps from disconnected displays
3. **Display Number Order**: Windows assigns display numbers in enumeration order, not spatial layout
4. **Scaling Irrelevant**: Position values unaffected by DPI scaling (work in virtual desktop coordinates)

### OBS WebSocket Gotchas

1. **Async Connection**: `ConnectAsync()` is fire-and-forget, must wait for `Connected` event
2. **Item IDs**: Sources identified by integer `ItemId`, not just name
3. **Scene Context**: Sources exist per-scene, same source can have different IDs in different scenes
4. **Visibility vs Enabled**: Uses `SetSceneItemEnabled` (not "visibility" or "render")

### Performance Considerations

- 100ms polling = 600 API calls per minute
- Each monitor change triggers N+1 OBS commands (hide all + show one)
- For 7 monitors: 8 WebSocket commands per switch
- Negligible performance impact even with aggressive polling

## Lessons Learned

### Windows API Marshaling
- **Always** set struct size fields before P/Invoke calls
- Use `typeof(StructName)` not instance for `Marshal.SizeOf`
- `CharSet = CharSet.Auto` required for string fields in structs
- Pay attention to `LayoutKind.Sequential` and field ordering

### OBS WebSocket Protocol
- Read actual library source when docs unclear
- Event-driven connection model requires `TaskCompletionSource` pattern
- Protocol version matters (obs-websocket 5.x very different from 4.x)

### User Experience Design
- Convention over configuration reduces friction
- Match system UI conventions (Windows display numbers)
- Self-discovery eliminates manual mapping
- Stable identifiers critical for good UX (display numbers > handles)

## Future Enhancement Ideas

**Not Implemented (Out of Scope):**
- System tray icon with context menu
- Hotkeys to manually override active monitor
- Window focus tracking (switch to monitor with active window)
- Configuration GUI
- Transition animations in OBS
- Multi-scene support
- Task Scheduler auto-start
- Windows Service conversion
- Logging/diagnostics
- Per-monitor sensitivity zones

**Why Console App:**
- User runs manually when needed, not as a service
- Mouse tracking requires user session (Session 0 limitation)
- OBS runs in user session, not system service
- Simple Ctrl+C to stop

## Project Stats

- **Time**: 2 hours (1:02 AM - 3:02 AM)
- **Language**: C# / .NET 8
- **Lines of Code**: ~300 (excluding generated files)
- **Files Created**: 3 core classes + documentation
- **NuGet Packages**: 1 (obs-websocket-dotnet v5.0.1)
- **Windows API Calls**: 4 P/Invoke functions
- **Git Commits**: 7

## Key Takeaways

1. **Windows display numbering is not obvious** - device paths, handles, and display numbers are all different things
2. **Convention-based design eliminates configuration** - `T-Monitor-N` naming is self-documenting
3. **Event-driven architecture scales well** - clean separation between detection and action
4. **P/Invoke requires precision** - struct sizes, CharSet, and marshaling matter
5. **OBS WebSocket is powerful** - no plugins needed for basic automation
6. **Polling is fine for non-critical tasks** - 100ms is fast enough and much simpler than hooks

## Technical Debt / Known Issues

**None identified.** Project is feature-complete for stated use case.

**Assumptions:**
- Single OBS instance on localhost:4455
- Display layout doesn't change frequently
- User manually starts/stops program
- Console window acceptable (no GUI needed)
- Windows 10/11 (relies on user32.dll APIs)

## References

- **obs-websocket-dotnet**: https://github.com/BarRaider/obs-websocket-dotnet
- **OBS WebSocket Protocol**: https://github.com/obsproject/obs-websocket/blob/master/docs/generated/protocol.md
- **Windows API - GetMonitorInfo**: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getmonitorinfo
- **Windows API - EnumDisplayDevices**: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumdisplaydevicesa

---

**Project Status**: ✅ Complete and working as designed
**Tested On**: Windows with 7 monitors, OBS Studio 30.0.2, WebSocket 5.3.4
**User Satisfaction**: High (user's "yes! that's it claude, thank you so much!")
