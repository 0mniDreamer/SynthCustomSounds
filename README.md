# Synth Custom Sounds

A MelonLoader mod for Synth Riders that lets you replace game sounds with your own custom audio files.

## Features

Replace these sounds with your own:

### Gameplay Sounds
- **Hit** - Note hit sounds
- **Miss** - Note miss sounds  
- **Special** - Special note activation
- **Special Pass** - Successfully completing a special
- **Special Fail** - Failing a special
- **Max Multiplier** - Reaching 6x multiplier
- **Wall** - Wall hit sounds

### UI Sounds
- **Button Click** - Menu button clicks
- **Button Hover** - Menu button hover sounds

### Result Screen Sounds
- **Result BGM** - Background music on the score screen
- **Ambient** - Ambient background audio
- **End Message** - The score announcement sound


## Installation

1. Install [MelonLoader](https://melonwiki.xyz/) for Synth Riders
2. Download the latest release
3. Place `SynthCustomSounds.dll` in your `SynthRiders/Mods/` folder
4. Launch the game once to create the config folder
5. Place your sound files in `SynthRiders/SynthCustomSounds/`

## Sound File Setup

Place audio files in `SynthRiders/SynthCustomSounds/` with names matching the sound type:

| Sound Type | File Name Examples |
|------------|-------------------|
| Hit | `hit.wav`, `hit1.wav`, `note.wav` |
| Miss | `miss.wav`, `fail.wav` |
| Special | `special.wav` |
| Special Pass | `specialpass.wav`, `special_pass.wav` |
| Special Fail | `specialfail.wav`, `special_fail.wav` |
| Max Multiplier | `maxmultiplier.wav`, `6x.wav` |
| Wall | `wall.wav` |
| Button Click | `buttonclick.wav`, `click.wav` |
| Button Hover | `buttonhover.wav`, `hover.wav` |
| Result BGM | `resultbgm.wav`, `result.wav` |
| Ambient | `ambient.wav`, `background.wav` |
| End Message | `endmessage.wav`, `scoreend.wav` |

### Supported Formats
- `.wav` (recommended)
- `.ogg`
- `.mp3`

### Multiple Sounds
You can have multiple sounds per type (e.g., `hit1.wav`, `hit2.wav`, `hit3.wav`). The mod will randomly select one each time.

## Configuration

Edit `SynthRiders/SynthCustomSounds/config.ini`:

```ini
[General]
enabled = true
random_selection = true
debug = false

[Audio]
master_volume = 1.00
pitch_variation = 0.05
```

## Requirements

- Synth Riders (PC/VR)
- MelonLoader 0.6.x or 0.7.x
- .NET 6

## Building from Source

1. Clone this repository
2. Update the DLL references in `.csproj` to match your MelonLoader installation
3. Build with `dotnet build -c Release`

## Credits

Created for the Synth Riders modding community.

## License

MIT License - Feel free to use, modify, and distribute.
