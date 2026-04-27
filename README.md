# SynthCustomSounds

> **Custom Sound Replacer for Synth Riders**  
> IL2CPP / .NET 6 / MelonLoader 0.6.x

Replace any in-game sound effect with your own audio files. Supports multiple sounds per type with random selection, per-sound volume control, and pitch variation.

## Features

- 🎵 **16 Replaceable Sounds** - Hit, miss, rail, special, menu, and more
- 🎲 **Multi-Sound Support** - Multiple files per type = random selection
- 🔊 **Volume Control** - Master volume + per-sound-type volumes
- 🎛️ **Pitch Variation** - Slight randomness for natural feel
- 📁 **Multiple Formats** - WAV, OGG, MP3
- ⚙️ **Full Configuration** - INI file for all settings

## Installation

### Requirements
- Synth Riders (PCVR)
- [MelonLoader 0.6.x](https://melonwiki.xyz/)

### Steps
1. Install MelonLoader if you haven't already
2. Download `SynthCustomSounds.dll` from Releases
3. Place it in `Synth Riders/Mods/`
4. Run the game once to create the config folder
5. Add your sounds to `Synth Riders/SynthCustomSounds/`

## Supported Sounds

| File Name | Sound |
|-----------|-------|
| `hit.wav` | Note hit / Laser hit |
| `miss.wav` | Missed note |
| `railstart.wav` | Rail/slider start |
| `railend.wav` | Rail/slider end |
| `special.wav` | Special note start |
| `specialpass.wav` | Special complete |
| `specialfail.wav` | Special fail |
| `maxmultiplier.wav` | 6x multiplier achieved |
| `wall.wav` | Wall hit |
| `buttonclick.wav` | Menu button click |
| `buttonhover.wav` | Menu button hover |
| `gameover.wav` | Game over |
| `endmessage.wav` | End message (Good/Awesome/Perfect) |
| `resultbgm.wav` | Result screen music |
| `ambient.wav` | Result screen ambient |
| `applause.wav` | Result screen applause |

## Multiple Sounds

Add multiple files for random selection:

```
SynthCustomSounds/
├── hit.wav           # Randomly selected
├── hit2.wav          # Randomly selected
├── hit_kick.ogg      # Randomly selected
├── hit_snare.mp3     # Randomly selected
└── miss.wav
```

The mod picks one at random each time!

## Configuration

Edit `SynthCustomSounds/config.ini`:

```ini
[General]
enabled = true
random_selection = true
debug = false

[Audio]
master_volume = 1.00
pitch_variation = 0.05

[Volumes]
hit = 1.00
miss = 1.00
railstart = 0.80
railend = 0.80
special = 1.00
specialpass = 1.20
; ... etc

[Enabled_Sounds]
hit = true
miss = true
; ... set to false to use original game sound
```

### Volume Settings
- `0.00` = Silent
- `1.00` = Normal (100%)
- `2.00` = Double volume (200%)

### Pitch Variation
- `0.00` = No variation
- `0.05` = ±5% (subtle, recommended)
- `0.10` = ±10% (noticeable)

## Tips

- **Keep hit sounds SHORT** (under 0.5 seconds)
- **OGG format** recommended (small file, good quality)
- **Empty files** (0 bytes) = mute that sound
- **Delete a file** = use original game sound
- **Check the console** for "✓ Loaded" messages

## Building from Source

### Requirements
- .NET 6.0 SDK
- Synth Riders with MelonLoader (run once to generate assemblies)

### Steps

1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/SynthCustomSounds.git
   ```

2. Update the path in `SynthCustomSounds.csproj`:
   ```xml
   <SynthRidersPath>C:\Your\Path\To\Synth Riders</SynthRidersPath>
   ```

3. Build:
   ```bash
   dotnet build --configuration Release
   ```

4. The DLL is automatically copied to your Mods folder!

## Troubleshooting

### Mod not loading?
- Check that MelonLoader 0.6.x is installed
- Look for errors in `MelonLoader/Latest.log`
- Verify the DLL is in the `Mods` folder

### Sounds not playing?
- Files must be in `SynthCustomSounds/` (in game root, not in Mods)
- Check the console for loading messages
- Verify format is WAV, OGG, or MP3
- Make sure the sound type is enabled in config

### Build errors?
- Run the game once with MelonLoader first
- Check that `MelonLoader/Il2CppAssemblies/` exists and contains DLLs
- Verify the path in `.csproj` is correct

## License

MIT License - do whatever you want with it!

## Credits

- Built for Synth Riders by Kluge Interactive
- Uses MelonLoader by LavaGang
- Inspired by the Synth Riders modding community
