using System;
using System.Collections.Generic;
using System.Threading;
using KMNarrator.Audio;
using KMNarrator.Cache;
using KMNarrator.ElevenLabs;
using KMNarrator.Voice;
using UnityEngine;
using UnityModManagerNet;

namespace KMNarrator.UI
{
    internal static class SettingsGui
    {
        private static bool _showApiKey;
        private static bool _showVoiceList;
        private static bool _voicesBusy;
        private static bool _testBusy;
        private static bool _sessionAutoRefreshDone;
        private static string _statusMessage = "";
        private static IReadOnlyList<ElevenLabsVoice> _voices = Array.Empty<ElevenLabsVoice>();
        private static int _selectedVoiceIndex;
        private static int _expandedMappingIndex = -1;
        private static string _voicePickerFilter = "";
        private static Vector2 _voicePickerScroll;

        public static void OnModLoaded(UnityModManager.ModEntry modEntry)
        {
            _sessionAutoRefreshDone = false;
            _showVoiceList = false;
        }

        public static void Draw(UnityModManager.ModEntry modEntry)
        {
            Settings settings = Main.Settings;
            if (settings == null)
            {
                GUILayout.Label("Settings not loaded.");
                return;
            }

            if (!_sessionAutoRefreshDone && HasApiKey(settings) && !_voicesBusy)
            {
                _sessionAutoRefreshDone = true;
                StartVoicesRefresh(expandList: false);
            }

            GUILayout.Label("ElevenLabs", GUILayout.ExpandWidth(false));

            GUILayout.BeginHorizontal();
            GUILayout.Label("API key", GUILayout.Width(120f));
            if (_showApiKey)
            {
                settings.ApiKey = GUILayout.TextField(settings.ApiKey ?? "", GUILayout.ExpandWidth(true));
            }
            else
            {
                GUILayout.Label(MaskKey(settings.ApiKey), GUILayout.ExpandWidth(true));
            }

            if (GUILayout.Button(_showApiKey ? "Hide" : "Show", GUILayout.Width(72f)))
            {
                _showApiKey = !_showApiKey;
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Voice ID", GUILayout.Width(120f));
            settings.DefaultVoiceId = GUILayout.TextField(settings.DefaultVoiceId ?? "", GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Model", GUILayout.Width(120f));
            settings.ModelId = GUILayout.TextField(settings.ModelId ?? "", GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Speed", GUILayout.Width(120f));
            settings.SpeechSpeed = GUILayout.HorizontalSlider(
                settings.SpeechSpeed,
                (float)ElevenLabsDefaults.MinSpeed,
                (float)ElevenLabsDefaults.MaxSpeed,
                GUILayout.ExpandWidth(true));
            GUILayout.Label(settings.SpeechSpeed.ToString("0.00"), GUILayout.Width(40f));
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);
            GUILayout.Label("Playback", GUILayout.ExpandWidth(false));

            GUILayout.BeginHorizontal();
            GUILayout.Label("Volume", GUILayout.Width(120f));
            settings.Volume = GUILayout.HorizontalSlider(settings.Volume, 0f, 1f, GUILayout.ExpandWidth(true));
            GUILayout.Label(Mathf.RoundToInt(settings.Volume * 100f) + "%", GUILayout.Width(40f));
            GUILayout.EndHorizontal();

            settings.OnlyUnvoicedLines = GUILayout.Toggle(settings.OnlyUnvoicedLines, "Only unvoiced lines");
            settings.IncludeStageDirections = GUILayout.Toggle(
                settings.IncludeStageDirections,
                "Speak stage directions (narrator cues / {n}…{/n} text)");
            settings.EnableBookEvents = GUILayout.Toggle(settings.EnableBookEvents, "Book event dialogues");
            settings.EnableBarks = GUILayout.Toggle(settings.EnableBarks, "Exploration barks");
            settings.VerboseLogging = GUILayout.Toggle(settings.VerboseLogging, "Verbose logging (debug playback)");

            DrawVoiceMappings(settings);

            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            GUI.enabled = !_voicesBusy && HasApiKey(settings);
            if (GUILayout.Button(_voicesBusy ? "Loading voices..." : "Refresh voices"))
            {
                StartVoicesRefresh(expandList: true);
            }

            GUI.enabled = !_testBusy && HasApiKey(settings);
            if (GUILayout.Button(_testBusy ? "Testing..." : "Test synthesis"))
            {
                StartSynthesisTest(settings);
            }

            GUI.enabled = AudioPlaybackService.Instance.IsPlaying;
            if (GUILayout.Button("Stop playback"))
            {
                AudioPlaybackService.Instance.Stop("UserStopButton");
                _statusMessage = "Playback stopped.";
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();

            if (_voices.Count > 0)
            {
                _showVoiceList = GUILayout.Toggle(
                    _showVoiceList,
                    "Voice list (" + _voices.Count + ")",
                    GUILayout.ExpandWidth(false));

                if (_showVoiceList)
                {
                    var names = new string[_voices.Count];
                    for (int i = 0; i < _voices.Count; i++)
                    {
                        names[i] = _voices[i].Name + " (" + _voices[i].Id + ")";
                    }

                    _selectedVoiceIndex = Mathf.Clamp(_selectedVoiceIndex, 0, names.Length - 1);
                    int newIndex = GUILayout.SelectionGrid(_selectedVoiceIndex, names, 1);
                    if (newIndex != _selectedVoiceIndex)
                    {
                        _selectedVoiceIndex = newIndex;
                        settings.DefaultVoiceId = _voices[newIndex].Id;
                    }
                }
            }

            if (!string.IsNullOrEmpty(_statusMessage))
            {
                GUILayout.Label(_statusMessage);
            }

            string cacheRoot = DiskCache.Instance.RootPath;
            GUILayout.Label("Cache: " + cacheRoot, GUILayout.ExpandWidth(true));

            GUILayout.Space(4f);
            GUILayout.Label("Save settings from the UMM window (Save / close mod UI).");
        }

        private static void DrawVoiceMappings(Settings settings)
        {
            if (settings.VoiceMappings == null)
            {
                settings.VoiceMappings = new List<VoiceMapping>();
            }

            GUILayout.Space(6f);
            GUILayout.Label("Character voices", GUILayout.ExpandWidth(false));
            GUILayout.Label("Map a dialogue speaker name to a specific voice and speed. Speakers without a mapping use the "
                + "default voice at the global Speed. Names appear in GameLogFull.log as [KMNarrator] Speaker '...' "
                + "or in cache/manifest.json as speakerHint.");

            int removeIndex = -1;
            for (int i = 0; i < settings.VoiceMappings.Count; i++)
            {
                VoiceMapping mapping = settings.VoiceMappings[i];
                if (mapping == null)
                {
                    continue;
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label("Speaker", GUILayout.Width(64f));
                mapping.Speaker = GUILayout.TextField(mapping.Speaker ?? "", GUILayout.MinWidth(220f), GUILayout.ExpandWidth(true));
                bool expanded = _expandedMappingIndex == i;
                if (GUILayout.Button(expanded ? "Hide" : "Pick", GUILayout.Width(56f)))
                {
                    _expandedMappingIndex = expanded ? -1 : i;
                    _voicePickerFilter = "";
                    _voicePickerScroll = Vector2.zero;
                }

                if (GUILayout.Button("X", GUILayout.Width(26f)))
                {
                    removeIndex = i;
                }

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Voice ID", GUILayout.Width(64f));
                mapping.VoiceId = GUILayout.TextField(mapping.VoiceId ?? "", GUILayout.MinWidth(220f), GUILayout.ExpandWidth(true));
                GUILayout.Label(VoiceNameFor(mapping.VoiceId), GUILayout.MinWidth(80f), GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(20f);
                GUILayout.Label("Speed", GUILayout.Width(44f));
                mapping.Speed = GUILayout.HorizontalSlider(
                    mapping.Speed,
                    (float)ElevenLabsDefaults.MinSpeed,
                    (float)ElevenLabsDefaults.MaxSpeed,
                    GUILayout.MinWidth(180f),
                    GUILayout.ExpandWidth(true));
                GUILayout.Label(mapping.Speed.ToString("0.00"), GUILayout.Width(40f));
                GUILayout.EndHorizontal();

                if (_expandedMappingIndex == i)
                {
                    DrawVoicePicker(mapping);
                }
            }

            if (removeIndex >= 0)
            {
                settings.VoiceMappings.RemoveAt(removeIndex);
                _expandedMappingIndex = -1;
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add character", GUILayout.Width(140f)))
            {
                settings.VoiceMappings.Add(new VoiceMapping());
                _expandedMappingIndex = settings.VoiceMappings.Count - 1;
                _voicePickerFilter = "";
                _voicePickerScroll = Vector2.zero;
            }

            if (_voices.Count == 0)
            {
                GUILayout.Label("Tip: use \"Refresh voices\" to pick from the list (or paste a Voice ID).",
                    GUILayout.ExpandWidth(true));
            }

            GUILayout.EndHorizontal();
        }

        private static void DrawVoicePicker(VoiceMapping mapping)
        {
            if (_voices.Count == 0)
            {
                GUILayout.Label("    No voices loaded — click \"Refresh voices\" below, or paste a Voice ID.");
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(20f);
            GUILayout.Label("Filter", GUILayout.Width(40f));
            _voicePickerFilter = GUILayout.TextField(_voicePickerFilter ?? "", GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            _voicePickerScroll = GUILayout.BeginScrollView(_voicePickerScroll, GUILayout.Height(170f));
            string filter = (_voicePickerFilter ?? "").Trim();
            foreach (ElevenLabsVoice voice in _voices)
            {
                if (filter.Length > 0
                    && voice.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0
                    && voice.Id.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                bool isCurrent = string.Equals(voice.Id, mapping.VoiceId, StringComparison.OrdinalIgnoreCase);
                if (GUILayout.Button((isCurrent ? "\u2713 " : "") + voice.Name + "  (" + voice.Id + ")",
                    GUILayout.ExpandWidth(true)))
                {
                    mapping.VoiceId = voice.Id;
                    _expandedMappingIndex = -1;
                }
            }

            GUILayout.EndScrollView();
        }

        private static string VoiceNameFor(string voiceId)
        {
            if (string.IsNullOrWhiteSpace(voiceId))
            {
                return "(no voice)";
            }

            foreach (ElevenLabsVoice voice in _voices)
            {
                if (string.Equals(voice.Id, voiceId, StringComparison.OrdinalIgnoreCase))
                {
                    return voice.Name;
                }
            }

            return "(id set)";
        }

        public static void OnSave(UnityModManager.ModEntry modEntry)
        {
            ElevenLabsClient.Instance.InvalidateVoiceCache();
            Settings settings = Main.Settings;
            if (settings != null)
            {
                settings.Save(modEntry);
            }
        }

        private static bool HasApiKey(Settings settings)
        {
            return !string.IsNullOrWhiteSpace(settings.ApiKey);
        }

        private static string MaskKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return "";
            }

            if (key.Length <= 4)
            {
                return "****";
            }

            return new string('*', key.Length - 4) + key.Substring(key.Length - 4);
        }

        private static void StartVoicesRefresh(bool expandList)
        {
            _voicesBusy = true;
            _statusMessage = "Fetching voices...";
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    IReadOnlyList<ElevenLabsVoice> voices = ElevenLabsClient.Instance.GetVoices();
                    _voices = voices;
                    SyncSelectedVoiceIndex(Main.Settings);
                    if (expandList)
                    {
                        _showVoiceList = true;
                    }

                    _statusMessage = "Loaded " + voices.Count + " voice(s).";
                }
                catch (Exception ex)
                {
                    _statusMessage = "Voice list failed: " + ex.Message;
                }
                finally
                {
                    _voicesBusy = false;
                }
            });
        }

        private static void SyncSelectedVoiceIndex(Settings settings)
        {
            if (settings == null || _voices.Count == 0)
            {
                return;
            }

            string currentId = settings.DefaultVoiceId;
            for (int i = 0; i < _voices.Count; i++)
            {
                if (_voices[i].Id == currentId)
                {
                    _selectedVoiceIndex = i;
                    return;
                }
            }
        }

        private static void StartSynthesisTest(Settings settings)
        {
            _testBusy = true;
            _statusMessage = "Synthesizing test line...";
            const string sampleText = "Greetings, Baron. KM Narrator is online.";

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    CachedSpeechResult result = CachedSpeechService.Instance.GetOrSynthesize(
                        sampleText,
                        settings.DefaultVoiceId,
                        settings.ModelId,
                        settings.SpeechSpeed);

                    string source = result.FromCache ? "cache" : "ElevenLabs API";
                    _statusMessage = "Test OK — " + result.Audio.Length + " bytes (" + source + "). Playing audio...";
                    Debug.Log("[KMNarrator] Test synthesis succeeded (" + result.Audio.Length + " bytes, " + source + ", id=" + result.CacheId + ").");

                    if (!string.IsNullOrEmpty(result.AudioFilePath))
                    {
                        AudioPlaybackService.Instance.EnqueuePlayFile(result.AudioFilePath);
                    }
                }
                catch (Exception ex)
                {
                    _statusMessage = "Test failed: " + ex.Message;
                    Main.LogWarningThrottled("ElevenLabs test failed: " + ex.Message, 0);
                }
                finally
                {
                    _testBusy = false;
                }
            });
        }
    }
}
