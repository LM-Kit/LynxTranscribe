# CLAUDE.md - AI Assistant Guide for LynxTranscribe

This document provides essential context for AI assistants working on the LynxTranscribe codebase.

## Project Overview

LynxTranscribe is a **cross-platform desktop application** for offline audio transcription using AI speech recognition. It runs entirely locally with no cloud dependencies.

**Key characteristics:**
- Built with **.NET 10 MAUI** (Microsoft's cross-platform UI framework)
- Targets **Windows 10+** and **macOS 15+ (Catalyst)**
- Uses **LM-Kit.NET** for on-device Whisper model inference
- 100% offline - no external API calls for transcription

---

## CRITICAL REQUIREMENTS

### Localization (MANDATORY)

**All user-facing text MUST be localized.** Never hardcode strings in XAML or C# code.

**Required steps for ANY new text:**
1. Add the English string to `Resources/Strings/AppStrings.resx`
2. Add the French translation to `Resources/Strings/AppStrings.fr.resx`
3. Add a constant key to `Localization/StringKeys.cs`
4. Reference the string properly (see below)

**In XAML:**
```xml
<!-- CORRECT - uses localization system -->
<Label Text="{Translate Key=YourNewKey}" />

<!-- WRONG - hardcoded text -->
<Label Text="Some text" />
```

**In C# code:**
```csharp
// CORRECT - uses localization system
var text = LocalizationService.Instance[StringKeys.YourNewKey];

// WRONG - hardcoded text
var text = "Some text";
```

**Key files:**
- `Resources/Strings/AppStrings.resx` - English strings (primary)
- `Resources/Strings/AppStrings.fr.resx` - French translations
- `Localization/StringKeys.cs` - Type-safe string key constants
- `Localization/LocalizationService.cs` - Singleton for accessing strings
- `Localization/TranslateExtension.cs` - XAML markup extension

---

### Theme Compatibility (MANDATORY)

**All UI elements MUST be theme-aware** and correctly update when the user switches between dark and light modes.

#### For Colors in XAML - Use DynamicResource

```xml
<!-- CORRECT - responds to theme changes -->
<Label TextColor="{DynamicResource TextPrimary}" />
<Border BackgroundColor="{DynamicResource BackgroundTertiary}" />

<!-- WRONG - static color won't update on theme switch -->
<Label TextColor="{StaticResource TextPrimary}" />
<Label TextColor="#FFFFFF" />
```

#### For Interactive Controls (Buttons, Tabs, etc.)

Controls with hover states or dynamic styling MUST be registered with the theme system:

```csharp
// In MainPage.Theme.cs InitializeThemedControls() or your initialization code:
RegisterThemedControl(YourButton, () => ControlStyle.ButtonDefault, YourButtonLabel);

// For state-dependent styling:
RegisterThemedControl(YourToggle, () => _isActive ? ControlStyle.ModeSelected : ControlStyle.ButtonDefault, YourToggleLabel);
```

#### For Programmatic Color Changes

**Never hardcode colors.** Always read from current resources:

```csharp
// CORRECT - reads current theme color
var color = (Color)this.Resources["TextPrimary"]!;

// CORRECT - use ApplyStyle for standard control styling
ApplyStyle(myBorder, ControlStyle.ButtonDefault, myLabel);

// WRONG - hardcoded color ignores theme
var color = Colors.White;
myBorder.BackgroundColor = Color.FromArgb("#1F1F23");
```

#### Available ControlStyle Options

| Style | Use Case |
|-------|----------|
| `ButtonDefault` | Standard buttons (tertiary background) |
| `ButtonHover` | Button hover state |
| `ButtonAccent` | Primary action buttons (orange) |
| `ButtonAccentHover` | Primary button hover |
| `ButtonDanger` | Destructive actions (red) |
| `ButtonDangerHover` | Danger button hover |
| `ButtonTransparent` | Icon-only or minimal buttons |
| `ButtonTransparentHover` | Transparent button hover |
| `TabActive` / `TabInactive` / `TabHover` | Tab button states |
| `ModeSelected` / `ModeUnselected` / `ModeHover` | Toggle/mode button states |
| `PlayButton` / `PlayButtonHover` | Playback control buttons |

#### Theme Color Resources (defined in `Resources/Styles/Colors.xaml`)

| Resource Key | Purpose |
|--------------|---------|
| `BackgroundPrimary` | Main window background |
| `BackgroundSecondary` | Panel backgrounds |
| `BackgroundTertiary` | Button/card backgrounds |
| `TextPrimary` | Main text color |
| `TextSecondary` | Muted/secondary text |
| `AccentPrimary` | Orange accent (#F59E0B) |
| `AccentMuted` | Darker accent for hover |
| `AccentSurface` | Light accent background |
| `AccentText` | Text on accent surfaces |
| `SurfaceBorder` | Border color for cards/buttons |
| `DangerColor` | Red for errors/destructive |
| `SuccessColor` | Green for success states |

#### Subscribing to Theme Changes

For custom refresh logic that doesn't fit the control registration system:

```csharp
// Register a callback for theme changes
RegisterThemeRefresh(() =>
{
    // Your custom refresh logic here
    UpdateMyCustomColors();
});

// Or subscribe to the event
ThemeChanged += () =>
{
    // Handle theme change
};
```

---

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
| `MainPage.Theme.cs` | Theme application logic, control registration |
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

## Common Tasks

### Adding a New UI Button

1. Add XAML in `MainPage.xaml` with localized text:
   ```xml
   <Border x:Name="MyNewButton" ...>
       <Label x:Name="MyNewButtonLabel" Text="{Translate Key=MyNewButtonText}" />
   </Border>
   ```
2. Add string to `AppStrings.resx` and `AppStrings.fr.resx`
3. Add key to `StringKeys.cs`: `public const string MyNewButtonText = "MyNewButtonText";`
4. Register for theming in `MainPage.Theme.cs`:
   ```csharp
   RegisterThemedControl(MyNewButton, () => ControlStyle.ButtonDefault, MyNewButtonLabel);
   ```
5. Add hover handlers if needed (use `ApplyStyle` method)
6. Add click handler in appropriate partial class

### Adding a New Setting

1. Add property to `AppSettingsService.cs` with getter/setter that calls `SaveSettings()`
2. Add default value constant
3. Update `SaveSettings()` to include the new property
4. Update `LoadSettings()` to read the new property
5. Bind in XAML or wire up in code

### Adding a New Export Format

1. Add export method to `Helpers/TranscriptExporter.cs`
2. Add UI button in `MainPage.xaml` (export section) with localized text
3. Add handler in `MainPage.Export.cs`
4. Register button for theming

### Adding Keyboard Shortcut

1. Edit `MainPage.KeyboardShortcuts.cs`
2. Handle in `OnKeyDown` or `OnKeyUp` method
3. Document in README.md keyboard shortcuts table

## Testing

No automated test suite exists. Testing is manual:
- Test on both Windows and macOS when possible
- Test with various audio formats (WAV, MP3, MP4, etc.)
- Test both Turbo and Accurate transcription modes
- **Test both light and dark themes** - verify all new UI elements update correctly
- **Test both English and French** - verify all new text is properly localized

## Key Files to Understand First

1. `MainPage.xaml` - Main UI structure (large file, browse sections)
2. `MainPage.xaml.cs` - Core state and initialization
3. `MainPage.Theme.cs` - Theme system and control registration
4. `Services/LMKitService.cs` - AI transcription logic
5. `AppConstants.cs` - Application constants
6. `Localization/StringKeys.cs` - All localization keys

## Debugging Tips

- Check `MainPage.xaml.cs` for state field declarations
- Audio issues: Check platform-specific service (`AudioPlayerService` vs `MacAudioPlayerService`)
- UI issues: Check `MainPage.UI.cs` or platform handlers in `MauiProgram.cs`
- Settings not saving: Check `AppSettingsService.cs` property implementation
- Theme not updating: Ensure control is registered via `RegisterThemedControl` and uses `DynamicResource`
- Missing translation: Check `StringKeys.cs` key matches `.resx` file key exactly

## Performance Considerations

- `MainPage.xaml` is ~258KB - load times may be affected
- Transcription history uses metadata caching to avoid loading full records
- Waveform rendering is optimized with downsampling
- Model loading is async with progress indication
