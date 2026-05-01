using System;
using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace SynthCustomSounds
{
    /// <summary>
    /// Harmony patches for replacing game audio clips
    /// </summary>
    public static class SoundPatches
    {
        private static HarmonyLib.Harmony _harmony;
        private static Type _hitSFXSourceType;
        private static Type _extraSFXControllerType;

        public static void Initialize()
        {
            try
            {
                _harmony = new HarmonyLib.Harmony("com.synthcustomsounds.patches");

                FindGameTypes();
                ApplyPatches();

                SynthCustomSoundsMod.Log("Sound patches initialized");
            }
            catch (Exception ex)
            {
                SynthCustomSoundsMod.LogError($"Failed to initialize patches: {ex.Message}");
            }
        }

        private static void FindGameTypes()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        string fullName = type.FullName ?? "";
                        string typeName = type.Name;

                        if (_hitSFXSourceType == null && typeName.Contains("HitSFX"))
                        {
                            _hitSFXSourceType = type;
                        }

                        if (_extraSFXControllerType == null && fullName.Contains("ExtraSFX"))
                        {
                            _extraSFXControllerType = type;
                        }
                    }
                }
                catch { }
            }
        }

        private static void ApplyPatches()
        {
            // Patch HitSFXSource.Awake for gameplay sounds
            if (_hitSFXSourceType != null)
            {
                try
                {
                    var method = _hitSFXSourceType.GetMethod("Awake",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (method != null)
                    {
                        _harmony.Patch(method, postfix: new HarmonyMethod(typeof(SoundPatches), nameof(HitSFXAwakePostfix)));
                        SynthCustomSoundsMod.Log("  ✓ Patched gameplay sounds");
                    }
                }
                catch (Exception ex)
                {
                    SynthCustomSoundsMod.LogError($"  ✗ Failed to patch gameplay sounds: {ex.Message}");
                }
            }

            // Patch ExtraSFXAudioController.Awake for UI sounds
            if (_extraSFXControllerType != null)
            {
                try
                {
                    var method = _extraSFXControllerType.GetMethod("Awake",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (method != null)
                    {
                        _harmony.Patch(method, postfix: new HarmonyMethod(typeof(SoundPatches), nameof(ExtraSFXAwakePostfix)));
                        SynthCustomSoundsMod.Log("  ✓ Patched UI sounds");
                    }
                }
                catch (Exception ex)
                {
                    SynthCustomSoundsMod.LogError($"  ✗ Failed to patch UI sounds: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Called after HitSFXSource.Awake - replaces gameplay sound clips
        /// </summary>
        public static void HitSFXAwakePostfix(object __instance)
        {
            if (SynthCustomSoundsMod.Settings == null || !SynthCustomSoundsMod.Settings.Enabled)
                return;

            var manager = SynthCustomSoundsMod.Sounds;
            if (manager == null) return;

            Type t = __instance.GetType();

            // Array properties (use index 1 for Laser sounds)
            var arrayProps = new Dictionary<SoundType, string[]>
            {
                { SoundType.Hit, new[] { "m_hitClip", "m_hitBadClip", "m_hitPerfectClip" } },
                { SoundType.RailStart, new[] { "m_lineStartClip" } },
                { SoundType.RailEnd, new[] { "m_lineEndClip" } }
            };

            // Single clip properties
            var singleProps = new Dictionary<SoundType, string[]>
            {
                { SoundType.Miss, new[] { "m_failClip" } },
                { SoundType.Special, new[] { "m_comboClip" } },
                { SoundType.SpecialPass, new[] { "m_comboEndClip" } },
                { SoundType.SpecialFail, new[] { "m_comboFailClip" } },
                { SoundType.MaxMultiplier, new[] { "m_rewardClip" } },
                { SoundType.Wall, new[] { "m_failClipWall" } }
            };

            // Apply array properties
            foreach (var kvp in arrayProps)
            {
                SoundType soundType = kvp.Key;
                string[] propNames = kvp.Value;

                var files = manager.FindFilesForType(soundType);
                if (files.Count == 0) continue;

                manager.LoadSoundsForType(soundType, clips => {
                    if (clips.Count > 0)
                    {
                        AudioClip clip = clips[0];
                        foreach (string propName in propNames)
                        {
                            SetArrayProperty(t, __instance, propName, clip);
                        }
                    }
                });
            }

            // Apply single properties
            foreach (var kvp in singleProps)
            {
                SoundType soundType = kvp.Key;
                string[] propNames = kvp.Value;

                var files = manager.FindFilesForType(soundType);
                if (files.Count == 0) continue;

                manager.LoadSoundsForType(soundType, clips => {
                    if (clips.Count > 0)
                    {
                        AudioClip clip = clips[0];
                        foreach (string propName in propNames)
                        {
                            SetSingleProperty(t, __instance, propName, clip);
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Called after ExtraSFXAudioController.Awake - replaces UI sound clips
        /// </summary>
        public static void ExtraSFXAwakePostfix(object __instance)
        {
            if (SynthCustomSoundsMod.Settings == null || !SynthCustomSoundsMod.Settings.Enabled)
                return;

            var manager = SynthCustomSoundsMod.Sounds;
            if (manager == null) return;

            Type t = __instance.GetType();

            var propMap = new Dictionary<SoundType, string>
            {
                { SoundType.ButtonClick, "buttonClickClip" },
                { SoundType.ButtonHover, "buttonHoverClip" }
            };

            foreach (var kvp in propMap)
            {
                SoundType soundType = kvp.Key;
                string propName = kvp.Value;

                var files = manager.FindFilesForType(soundType);
                if (files.Count == 0) continue;

                manager.LoadSoundsForType(soundType, clips => {
                    if (clips.Count > 0)
                    {
                        SetSingleProperty(t, __instance, propName, clips[0]);
                    }
                });
            }
        }

        private static void SetArrayProperty(Type type, object instance, string propName, AudioClip clip)
        {
            try
            {
                var prop = type.GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (prop == null) return;

                object arrayObj = prop.GetValue(instance);
                if (arrayObj == null) return;

                if (arrayObj is Il2CppReferenceArray<AudioClip> il2cppArray)
                {
                    // Index 1 is used for Laser controller sounds
                    int idx = (il2cppArray.Length > 1) ? 1 : 0;
                    il2cppArray[idx] = clip;
                }
            }
            catch { }
        }

        private static void SetSingleProperty(Type type, object instance, string propName, AudioClip clip)
        {
            try
            {
                var prop = type.GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(instance, clip);
                }
            }
            catch { }
        }
    }
}