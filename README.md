# Discord Vocal Overlay

A Windows-only always-on-top overlay application for recording voice messages and sending them to Discord.

## Table of Contents

- [Features](#features)
- [How It Works](#how-it-works)
- [User Interface](#user-interface)
- [Technical Stack](#technical-stack)
- [Building and Running](#building-and-running)
- [Distribution](#distribution)
- [Project Structure](#project-structure)
- [Functionality](#functionality)

## Features

- **Always-on-top overlay**: The overlay window stays above all other windows
- **Audio recording**: Record voice messages up to 60 seconds
- **Audio playback**: Play back recorded audio
- **Recording gain**: 1.5x volume boost for clearer recordings
- **Device selection**: Choose from available microphones
- **Settings**: Configure recordings folder location
- **Copy to clipboard**: Copy recorded file to clipboard for pasting in Discord
- **Delete recordings**: Remove unwanted recordings
- **Open recordings folder**: Quick access to saved files
- **State machine**: Explicit state management (Idle, Recording, RecordedReady, Playing)
- **Audio level meter**: Visual feedback during recording

## How It Works

### Recording Workflow

1. **Idle State**
   - Status: "Ready to Record"
   - State icon: 🎤
   - Timer: 00:00
   - Available actions: Record, Settings, Device Selection

2. **Recording State**
   - Status: "Recording..."
   - State icon: 🔴
   - Timer: Counts up from 00:00 to 01:00
   - Audio level meter: Visible (shows input level)
   - Available actions: Stop (recording)
   - Recording stops automatically at 60 seconds

3. **RecordedReady State**
   - Status: "Recording Ready"
   - State icon: ✅
   - Timer: Shows final duration
   - Available actions: Play, Delete, Back, Copy, Open Folder
   - **Copy button appears**: Click to copy file to clipboard
   - User can paste in Discord (Ctrl+V) to send as attachment

4. **Playing State**
   - Status: "Playing..."
   - State icon: 🔊
   - Timer: Shows duration
   - Available actions: Stop (playback)
   - Play button changes to "Stop" with danger styling

### Sending to Discord

**Method 1: Copy to Clipboard**
1. Click "Copy" button (📋)
2. File path is copied to clipboard
3. Switch to Discord chat
4. Press Ctrl+V to paste
5. File is sent as an attachment

**Method 2: Drag from File Explorer**
1. Click "Open Recordings Folder" (📂)
2. File Explorer opens with file selected
3. Drag the file to Discord chat
4. File is sent as an attachment

### Settings Workflow

1. Click "Settings" button (⚙)
2. Settings dialog opens
3. Click "Browse..." to select recordings folder
4. Click "Close" to save
5. Settings are persisted to `%LocalAppData%\DiscordVocalOverlay\settings.json`
6. Recordings are saved to the selected folder

## User Interface

### Main Overlay

The overlay is a horizontal rectangular window with a modern Discord-themed design:

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│ Status Bar                                                              │
│ [Status Text]                                              [Timer]    │
│ Ready to Record                                               00:00       │
├─────────────────────────────────────────────────────────────────────────────────┤
│ [Icon] [Audio Level Meter]  [Buttons]                        │
│ 🎤     [███████████████████]    [Record] [Play] [Delete] [Back] [Copy] │
│                                [Settings] [Device] [Folder] [Close]           │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### UI Elements

**Status Bar** (Top)
- Status text: Shows current state (Ready to Record, Recording..., Recording Ready, Playing...)
- Timer: Displays recording duration in MM:SS format
- Color-coded by state:
  - Blue (#5865F2): Idle, Playing
  - Red (#ED4245): Recording
  - Green (#57F287): Recording Ready

**Main Content** (Center)
- **State Icon**: Visual indicator of current state
  - 🎤 (Idle)
  - 🔴 (Recording)
  - ✅ (Recording Ready)
  - 🔊 (Playing)

- **Audio Level Meter**: Visual feedback during recording
  - Vertical bar that responds to microphone input
  - Height: 4px to 40px (based on audio level)
  - Color:
    - Blue (#5865F2): Low levels
    - Orange (#FAA61A): Medium levels
    - Red (#ED4245): High levels

- **Action Buttons**:
  - **Record**: Primary button (blue) - starts/stops recording
  - **Play**: Primary button (blue) - plays/stops audio
  - **Delete**: Base button - deletes recording
  - **Back**: Base button - returns to Idle without deleting
  - **Copy**: Base button (📋) - copies file to clipboard (visible only when recording is ready)
  - **Settings**: Base button (⚙) - opens settings dialog
  - **Device**: Base button (🎙) - opens device selection
  - **Folder**: Base button (📂) - opens recordings folder
  - **Close**: Base button (×) - closes application

### Settings Dialog

```
┌─────────────────────────────────────────────────────┐
│ Settings                                      │
├─────────────────────────────────────────────────────┤
│ Recordings Folder:                          │
│ C:\Users\...\Vocals                         │
│                                                │
│ [Browse...]                                  │
│                                                │
│ [Close]                                      │
└─────────────────────────────────────────────────────┘
```

### Device Selection Dialog

```
┌─────────────────────────────────────────────────────┐
│ Select Microphone                              │
├─────────────────────────────────────────────────────┤
│ [Dropdown with available devices]              │
│                                                │
│ [OK]                                         │
└─────────────────────────────────────────────────────┘
```

## Technical Stack

### Framework and Language
- **.NET 8 (LTS)**: Latest stable .NET runtime
- **C# 12**: Primary programming language
- **WPF (Windows Presentation Foundation)**: UI framework
- **XAML**: Markup language for UI definition

### Libraries and Packages
- **ModernWpfUI 0.9.6**: Modern Fluent Design styling
  - Provides modern, Fluent Design-inspired controls
  - Consistent with Windows 11 design language
  - Custom button styles, animations, and effects

- **NAudio 2.2.1**: Audio capture and playback
  - WASAPI (Windows Audio Session API): Low-latency audio capture
  - WaveOutEvent: Audio playback
  - Supports multiple audio formats and devices
  - Cross-platform audio I/O

### Architecture Pattern
- **MVVM (Model-View-ViewModel)**: Separation of concerns
  - **Views (XAML)**: UI layout and bindings only
  - **ViewModels**: Business logic, state management, commands
  - **Services**: Audio, file system, drag-drop, settings
  - **Code-behind**: Window interop only (dragging, events)

### State Machine

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│ State Machine                                                    │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────┐    ┌──────────────┐    ┌─────────────┐    │
│  │   Idle    │───▶│ Recording    │───▶│ RecordedReady │───▶│  Playing   │    │
│  └─────────┘    └──────────────┘    └─────────────┘    │
│                                                                  │
│  Transitions:                                                  │
│  • Idle → Recording (Record button)                             │
│  • Recording → RecordedReady (Stop button or 60s timeout)           │
│  • RecordedReady → Playing (Play button)                            │
│  • Playing → RecordedReady (Stop button)                             │
│  • RecordedReady → Idle (Back button)                               │
│  • Any state → Idle (Close button)                                  │
│                                                                  │
│  Constraints:                                                   │
│  • Recording and Playing are mutually exclusive                           │
│  • Copy/Drag/Delete only when recording is ready                    │
│  • Playback only when recording is ready                            │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

### Threading Model
- **UI Thread**: Main WPF UI thread
- **Audio Recording**: Background thread (Task.Run)
- **Audio Playback**: Background thread (WaveOut)
- **UI Updates**: Dispatcher.Invoke() for thread-safe updates
- **Cancellation**: CancellationToken for safe async operation cancellation

### File System
- **Default Location**: `%LocalAppData%\DiscordVocalOverlay\Vocals`
- **Configurable**: Users can change via Settings dialog
- **Filename Format**: `vocal_yyyy-mm-dd_hh-mm-ss.wav`
- **Format**: WAV PCM, 48kHz, mono, 16-bit
- **Settings Storage**: `%LocalAppData%\DiscordVocalOverlay\settings.json` (JSON)

### Audio Processing
- **Recording Gain**: 1.5x volume boost applied to audio samples
- **Sample Rate**: 48kHz (CD quality)
- **Channels**: Mono (single channel)
- **Bit Depth**: 16-bit (standard for voice)
- **Max Duration**: 60 seconds (hard limit)

### Drag & Drop
- **Implementation**: OS-level FileDrop via Clipboard.SetFileDropList()
- **Compatibility**: Works with Discord and other Windows applications
- **Fallback**: Users can also drag from File Explorer

## Building and Running

### Prerequisites
- **OS**: Windows 10 or later
- **.NET SDK**: .NET 8.0 SDK or later
- **IDE**: Visual Studio 2022 or later (recommended) or VS Code with C# extension

### Build from Command Line
```bash
# Navigate to project directory
cd "path/to/discord-recorder"

# Restore dependencies
dotnet restore

# Build in Debug mode
dotnet build

# Build in Release mode
dotnet build -c Release

# Run the application
dotnet run

# Run in Release mode
dotnet run -c Release
```

### Build in Visual Studio
1. Open `DiscordVocalOverlay.sln` or `DiscordVocalOverlay.csproj`
2. Select "Release" configuration
3. Click "Build" → "Build Solution"
4. Output: `bin\Release\net8.0-windows\DiscordVocalOverlay.exe`

### Publish as Single Executable
```bash
# Publish for Windows x64
dotnet publish -c Release -r win-x64 --self-contained true

# Output: bin\Release\net8.0-windows\win-x64\publish\DiscordVocalOverlay.exe
```

## Distribution

### GitHub Release (Self-Contained Single File)
```powershell
# Build a self-contained single-file exe for Windows x64
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true

# Zip the publish folder for distribution
cd bin\Release\net8.0-windows\win-x64
Compress-Archive -Path publish\* -DestinationPath DiscordVocalOverlay-win-x64.zip
```

**Release steps**
1. Create a new GitHub Release (tag e.g. `v1.0.0`).
2. Upload `DiscordVocalOverlay-win-x64.zip` as a release asset.
3. Add short release notes (features, fixes, known issues).

### Automated GitHub Release (Recommended)
This repo includes a GitHub Actions workflow that builds and publishes the zip automatically when you push a version tag.

```powershell
# Create and push a version tag
git tag v1.0.0
git push origin v1.0.0
```

After the workflow finishes, the GitHub Release page will contain the zip file for users to download.

### Install / Run (End Users)
1. Download `DiscordVocalOverlay-win-x64.zip` from the latest GitHub Release.
2. Extract the zip anywhere (for example: `C:\Apps\DiscordVocalOverlay`).
3. Run `DiscordVocalOverlay.exe`.
4. If Windows SmartScreen warns, click "More info" -> "Run anyway" (unsigned app).

### Troubleshooting

**Build Errors**
- If you see build errors, ensure .NET 8 SDK is installed
- Verify ModernWpfUI package is restored: `dotnet restore`

**Runtime Errors**
- If the app crashes on startup, check for missing dependencies
- Verify NAudio is properly installed and compatible with your system

**Audio Issues**
- If recording is too quiet, check microphone input device
- If playback has no sound, verify output device settings
- Audio level meter not working: Check WASAPI permissions

**Drag & Drop Issues**
- If Discord doesn't accept pasted file: Use drag from File Explorer instead
- Copy button not working: Check Windows clipboard permissions

## Project Structure

```
discord-recorder/
├── .kilocode/
│   └── README_AGENT.md          # Project constraints and requirements
├── ViewModels/
│   ├── MainViewModel.cs          # Main application logic and state machine
│   ├── SettingsViewModel.cs       # Settings dialog logic
│   └── RelayCommand.cs          # Shared ICommand implementation
├── Views/
│   ├── MainWindow.xaml              # Main overlay UI layout
│   ├── MainWindow.xaml.cs          # Window interop (dragging, events)
│   ├── SettingsWindow.xaml         # Settings dialog UI
│   └── SettingsWindow.xaml.cs     # Settings dialog code-behind
├── Services/
│   ├── AudioRecordingService.cs   # Audio capture with WASAPI
│   ├── AudioPlaybackService.cs    # Audio playback with WaveOut
│   ├── RecordingStorageService.cs  # File system management
│   ├── DragDropService.cs         # OS-level drag-drop operations
│   └── SettingsService.cs         # Settings persistence (JSON)
├── App.xaml                      # Application entry point
├── App.xaml.cs                   # Application initialization
├── DiscordVocalOverlay.csproj    # Project file
├── global.json                    # .NET version pinning
├── app.manifest                  # Windows application manifest
└── README.md                     # This file
```

## Functionality

### Audio Recording
- **Input Device Selection**: Choose from available microphones
- **Start/Stop Recording**: Manual control or 60-second auto-stop
- **Real-time Timer**: Updates every second during recording
- **Audio Level Meter**: Visual feedback of microphone input
- **Recording Gain**: 1.5x volume boost for clearer recordings
- **Format**: WAV PCM, 48kHz, mono, 16-bit
- **Max Duration**: 60 seconds (enforced limit)

### Audio Playback
- **Play/Stop**: Toggle playback with single button
- **Button State Change**: "Play" → "Stop" with danger styling
- **Same Window**: Playback within overlay window (no new windows)
- **Auto-Transition**: Returns to RecordedReady state when playback ends

### File Management
- **Automatic Naming**: `vocal_yyyy-mm-dd_hh-mm-ss.wav`
- **Custom Folder**: Users can configure via Settings
- **Persistence**: Settings saved to JSON file
- **Delete**: Removes file from disk and resets UI
- **Open Folder**: Quick access to saved files

### Copy to Clipboard
- **Copy Button**: Appears only when recording is ready
- **Clipboard Format**: FileDropList (OS-level file drop)
- **Paste in Discord**: Works with Ctrl+V
- **Notification**: "File copied to clipboard!" confirmation

### Settings
- **Recordings Folder**: Configurable via Browse dialog
- **Persistence**: JSON file in `%LocalAppData%`
- **Default Location**: `%LocalAppData%\DiscordVocalOverlay\Vocals`
- **Auto-Create**: Folder is created if it doesn't exist

### Window Behavior
- **Always-on-Top**: Stays above all other windows
- **Draggable**: Click and drag anywhere on background
- **Borderless**: No standard window chrome
- **Rounded Corners**: 12px radius
- **Shadow**: Subtle drop shadow
- **Transparent**: Allows custom borderless design

### State Machine
- **Four States**: Idle, Recording, RecordedReady, Playing
- **Transitions**: Explicit and controlled
- **Mutual Exclusion**: Recording and Playing cannot be active together
- **Command States**: All commands update CanExecute based on state
- **UI Synchronization**: UI updates on state changes

## Discord Integration

### Important Notes

**No Discord API or Bot Integration**
- This application does NOT integrate with Discord
- No Discord API usage
- No bot functionality
- No user tokens or self-bots
- No UI automation of Discord

**How It Works**
- Generates local audio files
- Uses OS-level drag-drop or clipboard for file transfer
- Discord treats files as normal attachments
- No special Discord features required

**Limitations**
- Discord does not support pasting certain file types from clipboard
- If paste doesn't work, use drag from File Explorer
- Maximum recording duration: 60 seconds
- Only WAV format supported (no MP3 encoding)

## Development Notes

### MVVM Pattern
- **ViewModel**: Contains all business logic, state management, and commands
- **View (XAML)**: Contains only UI layout and data bindings
- **Code-behind**: Contains only window interop (dragging, event handlers)
- **No business logic in code-behind**: All logic is in ViewModels

### Async/Await Pattern
- **Audio Recording**: Uses Task.Run for background thread
- **UI Updates**: Uses Dispatcher.Invoke for thread-safe UI updates
- **Cancellation**: CancellationToken for safe async operation cancellation

### Resource Management
- **NAudio Resources**: Properly disposed in Stop() methods
- **Window Resources**: Cleaned up in OnClosing() event
- **Event Subscriptions**: Unsubscribed in OnClosing() to prevent memory leaks

## License

This project is provided as-is for educational and personal use.

## Contributing

This is a personal project for the user's specific needs. If you wish to contribute:

1. Follow the MVVM pattern strictly
2. Keep business logic in ViewModels
3. Keep UI in Views (XAML)
4. Use proper async/await patterns
5. Ensure proper resource disposal
6. Test all state transitions

## Version History

- **v1.0**: Initial MVP with WPF + ModernWpfUI
  - Basic recording and playback
  - File management
  - Settings dialog
  - Device selection
  - Copy to clipboard
