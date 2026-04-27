using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace SynthCustomSounds
{
    public class Config
    {
        private static string ConfigPath
        {
            get { return Path.Combine(Application.dataPath, "..", "SynthCustomSounds", "config.ini"); }
        }

        public bool Enabled { get; set; }
        public bool RandomSelection { get; set; }
        public bool DebugMode { get; set; }
        public float MasterVolume { get; set; }
        public float PitchVariation { get; set; }

        private readonly Dictionary<SoundType, float> _typeVolumes = new Dictionary<SoundType, float>();
        private readonly Dictionary<SoundType, bool> _typeEnabled = new Dictionary<SoundType, bool>();

        public Config()
        {
            Enabled = true;
            RandomSelection = true;
            DebugMode = false;
            MasterVolume = 1.0f;
            PitchVariation = 0.05f;

            foreach (SoundType type in Enum.GetValues(typeof(SoundType)))
            {
                _typeVolumes[type] = 1.0f;
                _typeEnabled[type] = true;
            }
        }

        public float GetTypeVolume(SoundType type)
        {
            if (_typeVolumes.ContainsKey(type))
                return _typeVolumes[type];
            return 1.0f;
        }

        public bool IsSoundEnabled(SoundType type)
        {
            if (_typeEnabled.ContainsKey(type))
                return _typeEnabled[type];
            return true;
        }

        public void Load()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    SynthCustomSoundsMod.Log("Creating default config...");
                    Save();
                    return;
                }

                string currentSection = null;

                foreach (string rawLine in File.ReadAllLines(ConfigPath))
                {
                    string line = rawLine.Trim();

                    if (string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("#"))
                        continue;

                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        currentSection = line.Substring(1, line.Length - 2).ToLowerInvariant();
                        continue;
                    }

                    int equalsIndex = line.IndexOf('=');
                    if (equalsIndex < 0) continue;

                    string key = line.Substring(0, equalsIndex).Trim().ToLowerInvariant();
                    string value = line.Substring(equalsIndex + 1).Trim();

                    if (currentSection == "general")
                    {
                        if (key == "enabled") Enabled = ParseBool(value, true);
                        else if (key == "random_selection") RandomSelection = ParseBool(value, true);
                        else if (key == "debug") DebugMode = ParseBool(value, false);
                    }
                    else if (currentSection == "audio")
                    {
                        if (key == "master_volume") MasterVolume = ParseFloat(value, 1f);
                        else if (key == "pitch_variation") PitchVariation = ParseFloat(value, 0.05f);
                    }
                }

                SynthCustomSoundsMod.Log("Config loaded");
            }
            catch (Exception ex)
            {
                SynthCustomSoundsMod.LogError($"Failed to load config: {ex.Message}");
            }
        }

        public void Save()
        {
            try
            {
                string directory = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                using (var writer = new StreamWriter(ConfigPath))
                {
                    writer.WriteLine("; Synth Custom Sounds Configuration");
                    writer.WriteLine("");
                    writer.WriteLine("[General]");
                    writer.WriteLine("enabled = " + (Enabled ? "true" : "false"));
                    writer.WriteLine("random_selection = " + (RandomSelection ? "true" : "false"));
                    writer.WriteLine("debug = " + (DebugMode ? "true" : "false"));
                    writer.WriteLine("");
                    writer.WriteLine("[Audio]");
                    writer.WriteLine("master_volume = " + MasterVolume.ToString("F2"));
                    writer.WriteLine("pitch_variation = " + PitchVariation.ToString("F2"));
                }

                SynthCustomSoundsMod.Log("Config saved");
            }
            catch (Exception ex)
            {
                SynthCustomSoundsMod.LogError($"Failed to save config: {ex.Message}");
            }
        }

        private static bool ParseBool(string value, bool defaultValue)
        {
            string lower = value.ToLowerInvariant();
            if (lower == "true" || lower == "1" || lower == "yes") return true;
            if (lower == "false" || lower == "0" || lower == "no") return false;
            return defaultValue;
        }

        private static float ParseFloat(string value, float defaultValue)
        {
            float result;
            if (float.TryParse(value, out result)) return result;
            return defaultValue;
        }
    }
}
