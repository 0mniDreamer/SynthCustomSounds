using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace SynthCustomSounds
{
    public static class SoundPatches
    {
        private static HarmonyLib.Harmony _harmony;

        private static Type _hitSFXSourceType;
        private static Type _extraSFXControllerType;
        private static Type _gameControlManagerType;

        public static void Initialize()
        {
            try
            {
                _harmony = new HarmonyLib.Harmony("com.synthcustomsounds.patches");

                SynthCustomSoundsMod.Log("=== SEARCHING FOR GAME TYPES ===");
                FindGameTypes();

                SynthCustomSoundsMod.Log("=== APPLYING PATCHES ===");
                ApplyPatches();

                SynthCustomSoundsMod.Log("=== PATCHES COMPLETE ===");
            }
            catch (Exception ex)
            {
                SynthCustomSoundsMod.LogError($"Failed to initialize patches: {ex.Message}");
                SynthCustomSoundsMod.LogError(ex.StackTrace);
            }
        }

        private static void FindGameTypes()
        {
            // Search for types containing these patterns
            string[] hitPatterns = { "HitSFX", "Util_HitSFX", "HitSfx" };
            string[] extraPatterns = { "ExtraSFX", "ExtraSfx", "MenuSFX", "UiSfx" };
            string[] gamePatterns = { "GameControl", "GameManager" };

            int typesSearched = 0;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string asmName = assembly.GetName().Name;
                
                // Focus on game and IL2CPP assemblies
                if (!asmName.Contains("Assembly") && !asmName.Contains("Il2Cpp"))
                    continue;

                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        typesSearched++;
                        string typeName = type.Name;
                        string fullName = type.FullName ?? "";

                        // Check HitSFX patterns
                        if (_hitSFXSourceType == null)
                        {
                            foreach (string pattern in hitPatterns)
                            {
                                if (typeName.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    fullName.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    // Verify it has audio-related fields
                                    if (HasAudioClipFields(type))
                                    {
                                        _hitSFXSourceType = type;
                                        SynthCustomSoundsMod.Log($"  ✓ Found HitSFX: {fullName}");
                                        LogTypeDetails(type);
                                        break;
                                    }
                                }
                            }
                        }

                        // Check ExtraSFX patterns
                        if (_extraSFXControllerType == null)
                        {
                            foreach (string pattern in extraPatterns)
                            {
                                if (typeName.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    fullName.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    if (HasAudioClipFields(type))
                                    {
                                        _extraSFXControllerType = type;
                                        SynthCustomSoundsMod.Log($"  ✓ Found ExtraSFX: {fullName}");
                                        LogTypeDetails(type);
                                        break;
                                    }
                                }
                            }
                        }

                        // Check GameControl patterns
                        if (_gameControlManagerType == null)
                        {
                            foreach (string pattern in gamePatterns)
                            {
                                if (typeName.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    _gameControlManagerType = type;
                                    SynthCustomSoundsMod.Log($"  ✓ Found GameControl: {fullName}");
                                    break;
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            SynthCustomSoundsMod.Log($"  Searched {typesSearched} types");

            if (_hitSFXSourceType == null)
                SynthCustomSoundsMod.LogWarning("  ✗ HitSFXSource NOT FOUND - check type_dump.txt");
            if (_extraSFXControllerType == null)
                SynthCustomSoundsMod.LogWarning("  ✗ ExtraSFXAudioController NOT FOUND");
            if (_gameControlManagerType == null)
                SynthCustomSoundsMod.LogWarning("  ✗ GameControlManager NOT FOUND");
        }

        private static bool HasAudioClipFields(Type type)
        {
            try
            {
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var field in fields)
                {
                    string fieldTypeName = field.FieldType.Name;
                    if (fieldTypeName.Contains("AudioClip"))
                        return true;
                }
            }
            catch { }
            return false;
        }

        private static void LogTypeDetails(Type type)
        {
            try
            {
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var field in fields)
                {
                    if (field.FieldType.Name.Contains("AudioClip") || field.FieldType.Name.Contains("Audio"))
                    {
                        SynthCustomSoundsMod.Log($"      Field: {field.FieldType.Name} {field.Name}");
                    }
                }
            }
            catch { }
        }

        private static void ApplyPatches()
        {
            if (_hitSFXSourceType != null)
            {
                try
                {
                    // Try to find Awake method
                    var awakeMethod = _hitSFXSourceType.GetMethod("Awake",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (awakeMethod != null)
                    {
                        var postfix = new HarmonyMethod(typeof(SoundPatches), nameof(HitSFXAwakePostfix));
                        _harmony.Patch(awakeMethod, postfix: postfix);
                        SynthCustomSoundsMod.Log($"  ✓ Patched {_hitSFXSourceType.Name}.Awake");
                    }
                    else
                    {
                        // Try Start instead
                        var startMethod = _hitSFXSourceType.GetMethod("Start",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        
                        if (startMethod != null)
                        {
                            var postfix = new HarmonyMethod(typeof(SoundPatches), nameof(HitSFXAwakePostfix));
                            _harmony.Patch(startMethod, postfix: postfix);
                            SynthCustomSoundsMod.Log($"  ✓ Patched {_hitSFXSourceType.Name}.Start");
                        }
                        else
                        {
                            SynthCustomSoundsMod.LogWarning($"  ✗ No Awake/Start method found on {_hitSFXSourceType.Name}");
                            
                            // List available methods
                            var methods = _hitSFXSourceType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            SynthCustomSoundsMod.Log("    Available methods:");
                            foreach (var m in methods.Take(15))
                            {
                                SynthCustomSoundsMod.Log($"      - {m.Name}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    SynthCustomSoundsMod.LogError($"  Failed to patch HitSFX: {ex.Message}");
                }
            }

            if (_extraSFXControllerType != null)
            {
                try
                {
                    var awakeMethod = _extraSFXControllerType.GetMethod("Awake",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (awakeMethod == null)
                    {
                        awakeMethod = _extraSFXControllerType.GetMethod("Start",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    }

                    if (awakeMethod != null)
                    {
                        var postfix = new HarmonyMethod(typeof(SoundPatches), nameof(ExtraSFXAwakePostfix));
                        _harmony.Patch(awakeMethod, postfix: postfix);
                        SynthCustomSoundsMod.Log($"  ✓ Patched {_extraSFXControllerType.Name}.{awakeMethod.Name}");
                    }
                }
                catch (Exception ex)
                {
                    SynthCustomSoundsMod.LogError($"  Failed to patch ExtraSFX: {ex.Message}");
                }
            }

            if (_gameControlManagerType != null)
            {
                try
                {
                    var awakeMethod = _gameControlManagerType.GetMethod("Awake",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (awakeMethod == null)
                    {
                        awakeMethod = _gameControlManagerType.GetMethod("Start",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    }

                    if (awakeMethod != null)
                    {
                        var postfix = new HarmonyMethod(typeof(SoundPatches), nameof(GameControlAwakePostfix));
                        _harmony.Patch(awakeMethod, postfix: postfix);
                        SynthCustomSoundsMod.Log($"  ✓ Patched {_gameControlManagerType.Name}.{awakeMethod.Name}");
                    }
                }
                catch (Exception ex)
                {
                    SynthCustomSoundsMod.LogError($"  Failed to patch GameControl: {ex.Message}");
                }
            }
        }

        public static void HitSFXAwakePostfix(object __instance)
        {
            SynthCustomSoundsMod.Log(">>> HitSFXAwakePostfix CALLED <<<");
            
            if (SynthCustomSoundsMod.Settings == null || !SynthCustomSoundsMod.Settings.Enabled)
            {
                SynthCustomSoundsMod.Log("  Mod disabled, skipping");
                return;
            }

            var manager = SynthCustomSoundsMod.Sounds;
            if (manager == null)
            {
                SynthCustomSoundsMod.LogWarning("  SoundManager is null!");
                return;
            }

            Type instanceType = __instance.GetType();
            SynthCustomSoundsMod.Log($"  Instance type: {instanceType.FullName}");

            // Get all AudioClip fields
            var fields = instanceType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var audioFields = fields.Where(f => f.FieldType.Name.Contains("AudioClip")).ToList();

            SynthCustomSoundsMod.Log($"  Found {audioFields.Count} AudioClip fields:");
            foreach (var field in audioFields)
            {
                SynthCustomSoundsMod.Log($"    - {field.Name} ({field.FieldType.Name})");
            }

            // Try to load and apply hit sounds
            var hitFiles = manager.FindFilesForType(SoundType.Hit);
            SynthCustomSoundsMod.Log($"  Found {hitFiles.Count} hit sound files");

            if (hitFiles.Count > 0)
            {
                manager.LoadSoundsForType(SoundType.Hit, clips => {
                    SynthCustomSoundsMod.Log($"  Loaded {clips.Count} hit clips");
                    
                    if (clips.Count > 0)
                    {
                        AudioClip clip = clips[0];
                        
                        // Try common field names
                        string[] hitFieldNames = { 
                            "m_hitClip", "hitClip", "m_HitClip", "HitClip",
                            "m_hitBadClip", "m_hitPerfectClip",
                            "m_laserHitClip", "laserHitClip"
                        };

                        foreach (string fieldName in hitFieldNames)
                        {
                            SetAudioField(instanceType, __instance, fieldName, clip);
                        }

                        // Also try setting any field with "hit" in the name
                        foreach (var field in audioFields)
                        {
                            if (field.Name.ToLowerInvariant().Contains("hit"))
                            {
                                SetAudioField(instanceType, __instance, field.Name, clip);
                            }
                        }
                    }
                });
            }

            // Load miss sounds
            var missFiles = manager.FindFilesForType(SoundType.Miss);
            if (missFiles.Count > 0)
            {
                manager.LoadSoundsForType(SoundType.Miss, clips => {
                    if (clips.Count > 0)
                    {
                        SetAudioField(instanceType, __instance, "m_failClip", clips[0]);
                        SetAudioField(instanceType, __instance, "failClip", clips[0]);
                    }
                });
            }

            SynthCustomSoundsMod.Log(">>> HitSFXAwakePostfix DONE <<<");
        }

        public static void ExtraSFXAwakePostfix(object __instance)
        {
            SynthCustomSoundsMod.Log(">>> ExtraSFXAwakePostfix CALLED <<<");
            
            if (SynthCustomSoundsMod.Settings == null || !SynthCustomSoundsMod.Settings.Enabled) return;

            var manager = SynthCustomSoundsMod.Sounds;
            if (manager == null) return;

            Type instanceType = __instance.GetType();

            manager.LoadSoundsForType(SoundType.ButtonClick, clips => {
                if (clips.Count > 0)
                {
                    SetAudioField(instanceType, __instance, "buttonClickClip", clips[0]);
                    SetAudioField(instanceType, __instance, "m_buttonClickClip", clips[0]);
                }
            });

            manager.LoadSoundsForType(SoundType.ButtonHover, clips => {
                if (clips.Count > 0)
                {
                    SetAudioField(instanceType, __instance, "buttonHoverClip", clips[0]);
                    SetAudioField(instanceType, __instance, "m_buttonHoverClip", clips[0]);
                }
            });
        }

        public static void GameControlAwakePostfix(object __instance)
        {
            SynthCustomSoundsMod.Log(">>> GameControlAwakePostfix CALLED <<<");
            
            if (SynthCustomSoundsMod.Settings == null || !SynthCustomSoundsMod.Settings.Enabled) return;

            var manager = SynthCustomSoundsMod.Sounds;
            if (manager == null) return;

            Type instanceType = __instance.GetType();

            manager.LoadSoundsForType(SoundType.GameOver, clips => {
                if (clips.Count > 0)
                {
                    SetAudioField(instanceType, __instance, "m_GameOverClip", clips[0]);
                    SetAudioField(instanceType, __instance, "gameOverClip", clips[0]);
                }
            });
        }

        private static void SetAudioField(Type type, object instance, string fieldName, AudioClip clip)
        {
            try
            {
                var field = type.GetField(fieldName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (field == null)
                {
                    return; // Silently skip - field doesn't exist
                }

                if (field.FieldType.IsArray)
                {
                    var array = field.GetValue(instance) as Array;
                    if (array != null && array.Length > 0)
                    {
                        // Set index 1 if it exists (laser type), otherwise index 0
                        int index = array.Length > 1 ? 1 : 0;
                        array.SetValue(clip, index);
                        SynthCustomSoundsMod.Log($"    ✓ Set {fieldName}[{index}] = {clip.name}");
                    }
                }
                else
                {
                    field.SetValue(instance, clip);
                    SynthCustomSoundsMod.Log($"    ✓ Set {fieldName} = {clip.name}");
                }
            }
            catch (Exception ex)
            {
                SynthCustomSoundsMod.LogWarning($"    ✗ Failed to set {fieldName}: {ex.Message}");
            }
        }
    }
}
