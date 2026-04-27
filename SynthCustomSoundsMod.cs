using System;
using System.IO;
using System.Linq;
using System.Reflection;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(SynthCustomSounds.SynthCustomSoundsMod), "Synth Custom Sounds", "1.0.0", "OmniDreamer")]
[assembly: MelonGame("Kluge Interactive", "SynthRiders")]

namespace SynthCustomSounds
{
    public class SynthCustomSoundsMod : MelonMod
    {
        public static SynthCustomSoundsMod Instance { get; private set; }
        public static SoundManager Sounds { get; private set; }
        public static Config Settings { get; private set; }

        public static string RootFolder
        {
            get { return Path.Combine(Application.dataPath, "..", "SynthCustomSounds"); }
        }

        public override void OnInitializeMelon()
        {
            Instance = this;

            Settings = new Config();
            Settings.Load();

            Sounds = new SoundManager();

            InitializeFolders();

            PrintBanner();

            // ALWAYS dump types on first run to help debug
            DumpAudioTypes();

            // Apply harmony patches
            SoundPatches.Initialize();
        }

        private void InitializeFolders()
        {
            if (!Directory.Exists(RootFolder))
            {
                Directory.CreateDirectory(RootFolder);
                LoggerInstance.Msg($"Created folder: {RootFolder}");
            }
        }

        private void PrintBanner()
        {
            LoggerInstance.Msg("");
            LoggerInstance.Msg("╔═══════════════════════════════════════════════╗");
            LoggerInstance.Msg("║       SYNTH CUSTOM SOUNDS v1.0.0              ║");
            LoggerInstance.Msg("╚═══════════════════════════════════════════════╝");
            LoggerInstance.Msg($"  Folder: {RootFolder}");
            LoggerInstance.Msg($"  Enabled: {Settings.Enabled}");
            LoggerInstance.Msg("");
        }

        /// <summary>
        /// Dump all audio-related types to console and file for debugging
        /// </summary>
        private void DumpAudioTypes()
        {
            LoggerInstance.Msg("=== DUMPING AUDIO-RELATED TYPES ===");
            
            string dumpPath = Path.Combine(RootFolder, "type_dump.txt");
            using (var writer = new StreamWriter(dumpPath))
            {
                writer.WriteLine("Synth Custom Sounds - Type Dump");
                writer.WriteLine($"Generated: {DateTime.Now}");
                writer.WriteLine("");
                writer.WriteLine("Looking for audio-related classes...");
                writer.WriteLine("");

                string[] keywords = { "sfx", "audio", "sound", "hit", "clip", "music" };

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    string assemblyName = assembly.GetName().Name;
                    
                    // Focus on game assemblies
                    if (!assemblyName.Contains("Assembly") && 
                        !assemblyName.Contains("Il2Cpp") &&
                        !assemblyName.Contains("Unity"))
                        continue;

                    try
                    {
                        foreach (var type in assembly.GetTypes())
                        {
                            string typeName = type.Name.ToLowerInvariant();
                            string fullName = type.FullName ?? type.Name;

                            if (keywords.Any(k => typeName.Contains(k)))
                            {
                                string info = $"[{assemblyName}] {fullName}";
                                LoggerInstance.Msg($"  {info}");
                                writer.WriteLine(info);

                                // List methods
                                try
                                {
                                    var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                                    foreach (var method in methods.Take(20))
                                    {
                                        writer.WriteLine($"    - {method.Name}()");
                                    }
                                }
                                catch { }

                                // List fields with AudioClip
                                try
                                {
                                    var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                    foreach (var field in fields)
                                    {
                                        if (field.FieldType.Name.Contains("AudioClip") || 
                                            field.FieldType.Name.Contains("Audio"))
                                        {
                                            writer.WriteLine($"    FIELD: {field.FieldType.Name} {field.Name}");
                                        }
                                    }
                                }
                                catch { }

                                writer.WriteLine("");
                            }
                        }
                    }
                    catch { }
                }

                writer.WriteLine("");
                writer.WriteLine("=== END DUMP ===");
            }

            LoggerInstance.Msg($"Type dump saved to: {dumpPath}");
            LoggerInstance.Msg("=== END TYPE DUMP ===");
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            base.OnSceneWasInitialized(buildIndex, sceneName);

            LoggerInstance.Msg($"Scene loaded: {sceneName}");

            if (Settings == null || !Settings.Enabled) return;

            if (sceneName == "3.GameEnd")
            {
                Sounds.ApplyResultScreenSounds();
            }
        }

        public override void OnApplicationQuit()
        {
            if (Settings != null)
                Settings.Save();
        }

        public static void Log(string message)
        {
            if (Instance != null)
                Instance.LoggerInstance.Msg(message);
        }

        public static void LogWarning(string message)
        {
            if (Instance != null)
                Instance.LoggerInstance.Warning(message);
        }

        public static void LogError(string message)
        {
            if (Instance != null)
                Instance.LoggerInstance.Error(message);
        }

        public static void LogDebug(string message)
        {
            if (Instance != null)
                Instance.LoggerInstance.Msg($"[DEBUG] {message}");
        }
    }
}
