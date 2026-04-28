using System;
using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace SynthCustomSounds
{
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

                SynthCustomSoundsMod.Log("=== FINDING AUDIO CLASSES ===");
                FindGameTypes();

                SynthCustomSoundsMod.Log("=== APPLYING PATCHES ===");
                ApplyPatches();

                SynthCustomSoundsMod.Log("=== DONE ===");
            }
            catch (Exception ex)
            {
                SynthCustomSoundsMod.LogError($"Failed: {ex.Message}");
                SynthCustomSoundsMod.LogError(ex.StackTrace);
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
                            SynthCustomSoundsMod.Log($"  ✓ Found HitSFX: {fullName}");
                        }

                        if (_extraSFXControllerType == null && fullName.Contains("ExtraSFX"))
                        {
                            _extraSFXControllerType = type;
                            SynthCustomSoundsMod.Log($"  ✓ Found ExtraSFX: {fullName}");
                        }
                    }
                }
                catch { }
            }
        }

        private static void ApplyPatches()
        {
            if (_hitSFXSourceType != null)
            {
                try
                {
                    var method = _hitSFXSourceType.GetMethod("Awake",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (method != null)
                    {
                        _harmony.Patch(method, postfix: new HarmonyMethod(typeof(SoundPatches), nameof(HitSFXAwakePostfix)));
                        SynthCustomSoundsMod.Log($"  ✓ Patched HitSFXSource.Awake");
                    }
                }
                catch (Exception ex)
                {
                    SynthCustomSoundsMod.LogError($"  ✗ Failed to patch HitSFX: {ex.Message}");
                }
            }

            if (_extraSFXControllerType != null)
            {
                try
                {
                    var method = _extraSFXControllerType.GetMethod("Awake",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (method != null)
                    {
                        _harmony.Patch(method, postfix: new HarmonyMethod(typeof(SoundPatches), nameof(ExtraSFXAwakePostfix)));
                        SynthCustomSoundsMod.Log($"  ✓ Patched ExtraSFXAudioController.Awake");
                    }
                }
                catch (Exception ex)
                {
                    SynthCustomSoundsMod.LogError($"  ✗ Failed to patch ExtraSFX: {ex.Message}");
                }
            }
        }

        public static void HitSFXAwakePostfix(object __instance)
        {
            SynthCustomSoundsMod.Log(">>> HitSFXSource.Awake called <<<");

            if (SynthCustomSoundsMod.Settings == null || !SynthCustomSoundsMod.Settings.Enabled)
                return;

            var manager = SynthCustomSoundsMod.Sounds;
            if (manager == null) return;

            Type t = __instance.GetType();

            // Arrays (Il2CppReferenceArray) - these are PROPERTIES in IL2CPP
            // Use index 1 for Laser type, index 0 for Impact type
            var arrayProps = new Dictionary<SoundType, string[]>
            {
                { SoundType.Hit, new[] { "m_hitClip", "m_hitBadClip", "m_hitPerfectClip" } },
                { SoundType.RailStart, new[] { "m_lineStartClip" } },
                { SoundType.RailEnd, new[] { "m_lineEndClip" } }
            };

            // Single AudioClip properties
            var singleProps = new Dictionary<SoundType, string[]>
            {
                { SoundType.Miss, new[] { "m_failClip" } },
                { SoundType.Special, new[] { "m_comboClip" } },
                { SoundType.SpecialPass, new[] { "m_comboEndClip" } },
                { SoundType.SpecialFail, new[] { "m_comboFailClip" } },
                { SoundType.MaxMultiplier, new[] { "m_rewardClip" } },
                { SoundType.Wall, new[] { "m_failClipWall" } }
            };

            // Handle array properties (Hit, RailStart, RailEnd)
            foreach (var kvp in arrayProps)
            {
                SoundType soundType = kvp.Key;
                string[] propNames = kvp.Value;

                var files = manager.FindFilesForType(soundType);
                if (files.Count == 0) continue;

                SynthCustomSoundsMod.Log($"  Loading {soundType}: {files.Count} file(s)");

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

            // Handle single clip properties
            foreach (var kvp in singleProps)
            {
                SoundType soundType = kvp.Key;
                string[] propNames = kvp.Value;

                var files = manager.FindFilesForType(soundType);
                if (files.Count == 0) continue;

                SynthCustomSoundsMod.Log($"  Loading {soundType}: {files.Count} file(s)");

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
        /// Sets an element in an IL2CPP array PROPERTY
        /// </summary>
        private static void SetArrayProperty(Type type, object instance, string propName, AudioClip clip)
        {
            try
            {
                // Get the property
                var prop = type.GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (prop == null)
                {
                    SynthCustomSoundsMod.Log($"    Property {propName} not found");
                    return;
                }

                // Get the array value
                object arrayObj = prop.GetValue(instance);
                if (arrayObj == null)
                {
                    SynthCustomSoundsMod.Log($"    {propName} is null");
                    return;
                }

                // Cast to Il2CppReferenceArray<AudioClip>
                if (arrayObj is Il2CppReferenceArray<AudioClip> il2cppArray)
                {
                    // Use index 1 for Laser if available, else 0
                    int idx = (il2cppArray.Length > 1) ? 1 : 0;
                    il2cppArray[idx] = clip;
                    SynthCustomSoundsMod.Log($"    ✓ {propName}[{idx}] = {clip.name}");
                    return;
                }

                // Fallback: try using reflection on the indexer
                Type arrayType = arrayObj.GetType();
                var indexer = arrayType.GetProperty("Item");
                var lengthProp = arrayType.GetProperty("Length") ?? arrayType.GetProperty("Count");

                if (indexer != null && indexer.CanWrite && lengthProp != null)
                {
                    int length = (int)lengthProp.GetValue(arrayObj);
                    int idx = (length > 1) ? 1 : 0;
                    indexer.SetValue(arrayObj, clip, new object[] { idx });
                    SynthCustomSoundsMod.Log($"    ✓ {propName}[{idx}] = {clip.name} (indexer)");
                    return;
                }

                SynthCustomSoundsMod.Log($"    ✗ {propName}: Could not set array element, type={arrayType.Name}");
            }
            catch (Exception ex)
            {
                SynthCustomSoundsMod.Log($"    ✗ {propName} error: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets a single AudioClip property
        /// </summary>
        private static void SetSingleProperty(Type type, object instance, string propName, AudioClip clip)
        {
            try
            {
                var prop = type.GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(instance, clip);
                    SynthCustomSoundsMod.Log($"    ✓ {propName} = {clip.name}");
                    return;
                }
                SynthCustomSoundsMod.Log($"    ✗ {propName} not found or not writable");
            }
            catch (Exception ex)
            {
                SynthCustomSoundsMod.Log($"    ✗ {propName} error: {ex.Message}");
            }
        }

        public static void ExtraSFXAwakePostfix(object __instance)
        {
            SynthCustomSoundsMod.Log(">>> ExtraSFXAudioController.Awake called <<<");

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

                SynthCustomSoundsMod.Log($"  Loading {soundType}: {files.Count} file(s)");

                manager.LoadSoundsForType(soundType, clips => {
                    if (clips.Count > 0)
                    {
                        SetSingleProperty(t, __instance, propName, clips[0]);
                    }
                });
            }
        }
    }
}