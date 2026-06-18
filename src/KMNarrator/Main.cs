using System;
using KMNarrator.Audio;
using KMNarrator.UI;
using UnityEngine;
using UnityModManagerNet;

namespace KMNarrator
{
    public static class Main
    {
        private static string _lastWarningMessage;
        private static DateTime _lastWarningUtc;

        public static UnityModManager.ModEntry ModEntry { get; private set; }

        public static Settings Settings { get; private set; }

        public static bool Enabled { get; private set; }

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModEntry = modEntry;
            Settings = UnityModManager.ModSettings.Load<Settings>(modEntry) ?? new Settings();

            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = OnGui;
            modEntry.OnSaveGUI = OnSaveGui;

            Debug.Log("[KMNarrator] Initializing...");
            AudioPlaybackService.EnsureCreated();
            ModHarmony.Apply(modEntry);
            SettingsGui.OnModLoaded(modEntry);
            Enabled = modEntry.Enabled;
            Debug.Log("[KMNarrator] Initialized.");
            return true;
        }

        public static void LogVerbose(string message)
        {
            if (Settings != null && Settings.VerboseLogging)
            {
                Debug.Log("[KMNarrator] " + message);
            }
        }

        // Avoid flooding Player.log when many lines hit the same API failure (e.g. rate limits).
        public static void LogWarningThrottled(string message, int cooldownSeconds = 30)
        {
            if (string.Equals(message, _lastWarningMessage, StringComparison.Ordinal)
                && (DateTime.UtcNow - _lastWarningUtc).TotalSeconds < cooldownSeconds)
            {
                return;
            }

            _lastWarningMessage = message;
            _lastWarningUtc = DateTime.UtcNow;
            Debug.LogWarning("[KMNarrator] " + message);
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            Enabled = value;
            if (!value)
            {
                AudioPlaybackService.Instance.Stop("ModDisabled");
            }

            Debug.Log("[KMNarrator] Enabled = " + value);
            return true;
        }

        private static void OnGui(UnityModManager.ModEntry modEntry)
        {
            GUILayout.Label("KM Narrator v0.1.0 — ElevenLabs");
            SettingsGui.Draw(modEntry);
        }

        private static void OnSaveGui(UnityModManager.ModEntry modEntry)
        {
            SettingsGui.OnSave(modEntry);
        }
    }
}
