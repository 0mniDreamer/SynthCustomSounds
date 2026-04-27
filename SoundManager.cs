using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MelonLoader;
using UnityEngine;
using UnityEngine.Networking;
using Il2CppInterop.Runtime;

namespace SynthCustomSounds
{
    public class SoundManager
    {
        private readonly Dictionary<SoundType, List<AudioClip>> _sounds = new Dictionary<SoundType, List<AudioClip>>();
        private readonly Dictionary<SoundType, int> _lastPlayed = new Dictionary<SoundType, int>();
        private readonly System.Random _random = new System.Random();

        public string SoundsFolder
        {
            get { return SynthCustomSoundsMod.RootFolder; }
        }

        public SoundManager()
        {
            foreach (SoundType type in Enum.GetValues(typeof(SoundType)))
            {
                _sounds[type] = new List<AudioClip>();
                _lastPlayed[type] = -1;
            }
        }

        public void LoadSoundsForType(SoundType type, Action<List<AudioClip>> onComplete)
        {
            _sounds[type].Clear();
            _lastPlayed[type] = -1;

            var files = FindFilesForType(type);

            if (files.Count == 0)
            {
                if (onComplete != null) onComplete(_sounds[type]);
                return;
            }

            int pending = files.Count;

            foreach (string file in files)
            {
                MelonCoroutines.Start(LoadAudioFile(file, clip => {
                    if (clip != null)
                    {
                        _sounds[type].Add(clip);
                        SynthCustomSoundsMod.Log($"    Loaded: {clip.name}");
                    }

                    pending--;
                    if (pending == 0)
                    {
                        if (onComplete != null) onComplete(_sounds[type]);
                    }
                }));
            }
        }

        public List<string> FindFilesForType(SoundType type)
        {
            var result = new List<string>();

            if (!Directory.Exists(SoundsFolder))
                return result;

            string[] extensions = { ".wav", ".ogg", ".mp3" };

            foreach (string file in Directory.GetFiles(SoundsFolder))
            {
                string ext = Path.GetExtension(file).ToLowerInvariant();
                if (!extensions.Contains(ext))
                    continue;

                if (SoundTypeInfo.MatchesType(file, type))
                {
                    result.Add(file);
                }
            }

            return result;
        }

        private IEnumerator LoadAudioFile(string filePath, Action<AudioClip> onLoaded)
        {
            if (!File.Exists(filePath))
            {
                SynthCustomSoundsMod.LogWarning($"File not found: {filePath}");
                if (onLoaded != null) onLoaded(null);
                yield break;
            }

            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length == 0)
            {
                if (onLoaded != null) onLoaded(null);
                yield break;
            }

            AudioType audioType = GetAudioType(filePath);
            if (audioType == AudioType.UNKNOWN)
            {
                SynthCustomSoundsMod.LogWarning($"Unsupported format: {Path.GetFileName(filePath)}");
                if (onLoaded != null) onLoaded(null);
                yield break;
            }

            string uri = "file:///" + filePath.Replace("\\", "/");

            var www = UnityWebRequestMultimedia.GetAudioClip(uri, audioType);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                SynthCustomSoundsMod.LogWarning($"Failed to load {Path.GetFileName(filePath)}: {www.error}");
                if (onLoaded != null) onLoaded(null);
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
            if (clip != null)
            {
                clip.name = Path.GetFileNameWithoutExtension(filePath);
            }

            if (onLoaded != null) onLoaded(clip);
        }

        private AudioType GetAudioType(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            switch (ext)
            {
                case ".wav": return AudioType.WAV;
                case ".ogg": return AudioType.OGGVORBIS;
                case ".mp3": return AudioType.MPEG;
                default: return AudioType.UNKNOWN;
            }
        }

        public AudioClip GetClip(SoundType type)
        {
            if (!_sounds.ContainsKey(type))
                return null;

            var clips = _sounds[type];
            if (clips.Count == 0)
                return null;

            if (clips.Count == 1)
                return clips[0];

            int index;
            int lastIndex = _lastPlayed.ContainsKey(type) ? _lastPlayed[type] : -1;

            do
            {
                index = _random.Next(clips.Count);
            } while (index == lastIndex && clips.Count > 1);

            _lastPlayed[type] = index;
            return clips[index];
        }

        public bool HasSounds(SoundType type)
        {
            return _sounds.ContainsKey(type) && _sounds[type].Count > 0;
        }

        public void ApplyResultScreenSounds()
        {
            try
            {
                SynthCustomSoundsMod.Log("Applying result screen sounds...");

                GameObject bgAudio = GameObject.Find("[Background Audio]");
                if (bgAudio == null)
                {
                    SynthCustomSoundsMod.Log("  Background Audio not found");
                    return;
                }

                Transform music = bgAudio.transform.Find("[Music]");
                if (music != null)
                {
                    AudioSource musicSource = music.GetComponent<AudioSource>();
                    if (musicSource != null)
                    {
                        var files = FindFilesForType(SoundType.ResultBGM);
                        if (files.Count > 0)
                        {
                            MelonCoroutines.Start(LoadAndApplyToSource(files[0], musicSource));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SynthCustomSoundsMod.LogError($"Error applying result sounds: {ex.Message}");
            }
        }

        private IEnumerator LoadAndApplyToSource(string filePath, AudioSource source)
        {
            AudioClip clip = null;

            yield return LoadAudioFile(filePath, c => clip = c);

            if (clip != null)
            {
                source.clip = clip;
                source.Play();
                SynthCustomSoundsMod.Log($"  Applied result music");
            }
        }
    }
}
