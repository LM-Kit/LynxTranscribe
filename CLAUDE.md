# CLAUDE.md - AI Assistant Guide for LynxTranscribe

This document provides essential context for AI assistants working on the LynxTranscribe codebase.

## Project Overview

LynxTranscribe is a **cross-platform desktop application** for offline audio transcription using AI speech recognition. It runs entirely locally with no cloud dependencies.

**Key characteristics:**
- Built with **.NET 10 MAUI** (Microsoft's cross-platform UI framework)
- Targets **Windows 10+** and **macOS 15+ (Catalyst)**
- Uses **LM-Kit.NET** for on-device Whisper model inference
- 100% offline - no external API calls for transcription

## Technology Stack

| Component | Technology |
|-----------|------------|
| UI Framework | .NET MAUI (10.0) |
| AI/ML Engine | LM-Kit.NET 2026.1.5 (Whisper models) |
| Audio (Windows) | NAudio 2.2.1 |
| Audio (macOS) | AVFoundation (native) |
| Document Export | DocumentFormat.OpenXml 3.4.1 |
| UI Toolkit | CommunityToolkit.Maui 14.0.0 |

## Build Commands

```bash
# Restore dependencies
dotnet restore

# Build (Windows)
dotnet build -f net10.0-windows10.0.19041.0

# Build (macOS)
dotnet build -f net10.0-maccatalyst

# Run
dotnet run --project LynxTranscribe.csproj

# Publish (Windows x64)
dotnet publish -c Release -f net10.0-windows10.0.19041.0 -r win-x64

# Publish (macOS x64)
dotnet publish -c Release -f net10.0-maccatalyst -r maccatalyst-x64
```

## Project Structure

```
LynxTranscribe/
├── Controls/              # Custom reusable UI controls
│   └── CustomToggle.xaml  # Animated toggle switch component
├── Helpers/               # Utility classes
│   ├── NativeFolderPicker.cs      # Platform folder dialogs
│   ├── SystemEx.cs                # System extensions
│   ├── TranscriptExporter.cs      # DOCX/TXT/SRT export
│   ├── VarispeedSampleProvider.cs # NAudio playback speed
│   ├── WaveformDrawable.cs        # Audio waveform rendering
│   └── WhisperLanguages.cs        # Language code mappings
├── Localization/          # Multi-language support (EN, FR)
│   ├── LocalizationService.cs     # Language switching singleton
│   ├── StringKeys.cs              # Type-safe string keys
│   └── TranslateExtension.cs      # XAML {Translate} markup
├── Models/
│   └── TranscriptionRecord.cs     # Core data model for transcriptions
├── Services/              # Business logic layer
│   ├── AppSettingsService.cs      # JSON-based settings persistence
│   ├── AudioPlayerService.cs      # Windows audio playback
│   ├── AudioRecorderService.cs    # Windows audio recording
│   ├── LMKitService.cs            # AI model management & transcription
│   ├── MacAudioPlayerService.cs   # macOS audio playback
│   ├── MacAudioRecorderService.cs # macOS audio recording
│   └── TranscriptionHistoryService.cs # History persistence layer
├── Platforms/
│   ├── Windows/           # Windows-specific code & assets
│   └── MacCatalyst/       # macOS-specific code & assets
├── Resources/
│   ├── AppIcon/           # Application icons
│   ├── Fonts/             # OpenSans fonts
│   ├── Images/            # Logos, screenshots
│   ├── Strings/           # Localized strings (.resx)
│   └── Styles/            # Colors.xaml, Styles.xaml
├── MainPage.xaml          # Main UI layout (large, ~258KB)
├── MainPage.*.cs          # Partial classes (see below)
├── AppConstants.cs        # Application-wide constants
├── MauiProgram.cs         # DI configuration & platform handlers
└── App.xaml.cs            # Application entry point
```

## MainPage Partial Classes Architecture

The MainPage is split into **12 partial class files** by concern. This is a key architectural pattern in this codebase:

| File | Responsibility |
|------|----------------|
| `MainPage.xaml.cs` | Core initialization, fields, language picker |
| `MainPage.UI.cs` | Drag/drop, toasts, fonts, panel resizing, lifecycle |
| `MainPage.History.cs` | Tab navigation, history list, record loading |
| `MainPage.AudioPlayback.cs` | Playback controls, position tracking, seek |
| `MainPage.Recording.cs` | Microphone recording, device selection |
| `MainPage.Transcription.cs` | File selection, model loading, transcription |
| `MainPage.Search.cs` | In-transcript search with highlighting |
| `MainPage.Export.cs` | Export to DOCX/TXT/SRT, clipboard |
| `MainPage.Settings.cs` | Settings panel, model mode toggle |
| `MainPage.Settings.Theme.cs` | Dark/light theme switching |
| `MainPage.Settings.Transcription.cs` | VAD, dictation, language settings |
| `MainPage.Settings.Storage.cs` | Folder paths configuration |
| `MainPage.Settings.UI.cs` | Font size, UI preferences |
| `MainPage.Theme.cs` | Theme application logic |
| `MainPage.KeyboardShortcuts.cs` | Keyboard event handling |

**When modifying UI behavior**, identify the correct partial class file based on the feature area.

## Key Services

### LMKitService (`Services/LMKitService.cs`)
- Manages Whisper model loading (`whisper-large-turbo3` for Turbo, `whisper-large3` for Accurate)
- Handles audio file loading and format conversion
- Executes transcription with progress callbacks
- **Important**: Non-WAV files are converted to PCM WAV before transcription

### AppSettingsService (`Services/AppSettingsService.cs`)
- Persists settings to `%LOCALAPPDATA%/LynxTranscribe/settings.json`
- Properties trigger automatic save on change
- Default values are defined as constants within the class

### TranscriptionHistoryService (`Services/TranscriptionHistoryService.cs`)
- One JSON file per transcription record
- Implements metadata caching (5-second expiry) for fast list loading
- Records stored in user-configurable history directory

### AudioPlayerService / MacAudioPlayerService
- Platform-specific implementations for audio playback
- Support playback speed control (0.5x to 2.0x)
- 100ms timer interval for position updates

## Important Constants (`AppConstants.cs`)

```csharp
SeekSeconds = 5                // Audio seek step
TimerIntervalMs = 100          // Playback position update interval
ToastDurationMs = 3000         // Notification display time
MinPanelWidth = 260            // Left panel minimum width
MaxPanelWidth = 500            // Left panel maximum width
CountdownSeconds = 3           // Recording countdown
RecordingDotBlinkMs = 500      // Recording indicator blink rate

// Supported formats
SupportedMediaExtensions = [".wav", ".mp3", ".flac", ".ogg", ".m4a",
                           ".wma", ".mp4", ".aac", ".avi", ".mov"]
```

## Platform-Specific Patterns

### Conditional Compilation
```csharp
#if WINDOWS
    // Windows-specific code (NAudio)
#endif

#if MACCATALYST
    // macOS-specific code (AVFoundation)
#endif
```

### Platform Service Selection
Audio services are instantiated conditionally in `MainPage.xaml.cs`:
```csharp
#if WINDOWS
    _audioPlayer = new AudioPlayerService();
    _audioRecorder = new AudioRecorderService();
#elif MACCATALYST
    _audioPlayer = new MacAudioPlayerService();
    _audioRecorder = new MacAudioRecorderService();
#endif
```

## Code Conventions

### Naming
- Private fields: `_camelCase` prefix with underscore
- Methods: `PascalCase`
- Async methods: Suffix with `Async`
- Event handlers: `On<Element><Event>` (e.g., `OnPlayButtonClicked`)

### Async Patterns
- All I/O and transcription operations are async
- Use `CancellationToken` for long-running operations
- UI updates via `MainThread.BeginInvokeOnMainThread()`

### XAML
- Use `{Translate Key=StringKey}` for localized text
- Theme colors defined in `Resources/Styles/Colors.xaml`
- Styles defined in `Resources/Styles/Styles.xaml`

### Localization
- Add new strings to `Resources/Strings/AppStrings.resx` (English)
- Add French translations to `Resources/Strings/AppStrings.fr.resx`
- Add string key constant to `Localization/StringKeys.cs`

## Common Tasks

### Adding a New Setting
1. Add property to `AppSettingsService.cs` with getter/setter that calls `SaveSettings()`
2. Add default value constant
3. Update `SaveSettings()` to include the new property
4. Update `LoadSettings()` to read the new property
5. Bind in XAML or wire up in code

### Adding a New Export Format
1. Add export method to `Helpers/TranscriptExporter.cs`
2. Add UI button in `MainPage.xaml` (export section)
3. Add handler in `MainPage.Export.cs`

### Adding Keyboard Shortcut
1. Edit `MainPage.KeyboardShortcuts.cs`
2. Handle in `OnKeyDown` or `OnKeyUp` method
3. Document in README.md keyboard shortcuts table

### Adding New Localized String
1. Add to `Resources/Strings/AppStrings.resx`
2. Add French translation to `AppStrings.fr.resx`
3. Add key constant to `Localization/StringKeys.cs`
4. Use in XAML: `{Translate Key=YourNewKey}`
5. Use in code: `LocalizationService.Instance[StringKeys.YourNewKey]`

## Testing

No automated test suite exists. Testing is manual:
- Test on both Windows and macOS when possible
- Test with various audio formats (WAV, MP3, MP4, etc.)
- Test both Turbo and Accurate transcription modes
- Test light and dark themes

## Key Files to Understand First

1. `MainPage.xaml` - Main UI structure (large file, browse sections)
2. `MainPage.xaml.cs` - Core state and initialization
3. `Services/LMKitService.cs` - AI transcription logic
4. `AppConstants.cs` - Application constants
5. `MauiProgram.cs` - App configuration and DI

## Debugging Tips

- Check `MainPage.xaml.cs` for state field declarations
- Audio issues: Check platform-specific service (`AudioPlayerService` vs `MacAudioPlayerService`)
- UI issues: Check `MainPage.UI.cs` or platform handlers in `MauiProgram.cs`
- Settings not saving: Check `AppSettingsService.cs` property implementation

## Performance Considerations

- `MainPage.xaml` is ~258KB - load times may be affected
- Transcription history uses metadata caching to avoid loading full records
- Waveform rendering is optimized with downsampling
- Model loading is async with progress indication
