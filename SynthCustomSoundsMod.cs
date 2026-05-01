using System;
using System.IO;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(SynthCustomSounds.SynthCustomSoundsMod), "Synth Custom Sounds", "1.0.0", "0mniDreamer")]
[assembly: MelonGame("Kluge Interactive", "SynthRiders")]

namespace SynthCustomSounds
{
    public class SynthCustomSoundsMod : MelonMod
    {
        public static SynthCustomSoundsMod Instance { get; private set; }
        public static SoundManager Sounds { get; private set; }
        public static Config Settings { get; private set; }

        public static string RootFolder => Path.Combine(Application.dataPath, "..", "SynthCustomSounds");

        public override void OnInitializeMelon()
        {
            Instance = this;

            Settings = new Config();
            Settings.Load();

            Sounds = new SoundManager();

            InitializeFolders();
            PrintBanner();

            SoundPatches.Initialize();
        }

        private void InitializeFolders()
        {
            if (!Directory.Exists(RootFolder))
            {
                Directory.CreateDirectory(RootFolder);
                Log($"Created folder: {RootFolder}");
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

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            base.OnSceneWasInitialized(buildIndex, sceneName);

            if (Settings == null || !Settings.Enabled) return;

            // Result screen - apply result screen sounds
            if (sceneName == "3.GameEnd")
            {
                Sounds.ApplyResultScreenSounds();
            }
        }

        public override void OnApplicationQuit()
        {
            Settings?.Save();
        }

        public static void Log(string message)
        {
            Instance?.LoggerInstance.Msg(message);
        }

        public static void LogWarning(string message)
        {
            Instance?.LoggerInstance.Warning(message);
        }

        public static void LogError(string message)
        {
            Instance?.LoggerInstance.Error(message);
        }
    }
}