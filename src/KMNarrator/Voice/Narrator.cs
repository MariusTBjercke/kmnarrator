using System;
using System.Threading;
using Kingmaker;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.Localization;
using KMNarrator.Audio;
using UnityEngine;

namespace KMNarrator.Voice
{
    internal static class Narrator
    {
        public static void Speak(LocalizedString text, string speakerHint, string surface, bool? isNarratorCue = null)
        {
            try
            {
                if (!Main.Enabled)
                {
                    return;
                }

                Settings settings = Main.Settings;
                if (settings == null || text == null)
                {
                    return;
                }

                if (surface == "book" && !settings.EnableBookEvents)
                {
                    Main.LogVerbose("Skip " + surface + ": EnableBookEvents off.");
                    return;
                }

                if (surface == "bark" && !settings.EnableBarks)
                {
                    Main.LogVerbose("Skip " + surface + ": EnableBarks off.");
                    return;
                }

                bool voiced = !VoiceOverHelper.IsUnvoiced(text);
                if (voiced && settings.OnlyUnvoicedLines)
                {
                    Main.LogVerbose("Skip " + surface + ": voiced line (OnlyUnvoicedLines). key=" + text.Key);
                    return;
                }

                bool narratorCue = isNarratorCue ?? TryIsNarratorCue();
                if (!settings.IncludeStageDirections && narratorCue)
                {
                    Main.LogVerbose("Skip " + surface + ": narrator cue (IncludeStageDirections off). key=" + text.Key);
                    return;
                }

                if (string.IsNullOrWhiteSpace(settings.ApiKey) || string.IsNullOrWhiteSpace(settings.DefaultVoiceId))
                {
                    Main.LogVerbose("Skip " + surface + ": no API key or voice id.");
                    return;
                }

                string clean = TextNormalizer.Normalize(text, settings.IncludeStageDirections);
                if (string.IsNullOrWhiteSpace(clean))
                {
                    Main.LogVerbose("Skip " + surface + ": empty text after normalize. key=" + text.Key
                        + ", stageDirs=" + settings.IncludeStageDirections);
                    return;
                }

                if (settings.VerboseLogging)
                {
                    Main.LogVerbose("Normalized (" + surface + ", stageDirs=" + settings.IncludeStageDirections + "): "
                        + Truncate(clean));
                }

                VoiceSelection selection = VoiceResolver.Resolve(speakerHint, settings.SpeechSpeed);
                if (string.IsNullOrWhiteSpace(selection.VoiceId))
                {
                    return;
                }

                string voiceId = selection.VoiceId;
                string modelId = settings.ModelId;
                double speed = selection.Speed;
                string localizationKey = text.Key;
                int generation = AudioPlaybackService.Instance.CurrentGeneration;

                Main.LogVerbose("Narrate " + surface + " (gen " + generation + ", " + clean.Length + " chars): "
                    + Truncate(clean));

                Synthesize(clean, voiceId, modelId, speed, speakerHint, localizationKey, generation);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[KMNarrator] Narrator.Speak(" + surface + ") error: " + ex.Message);
            }
        }

        private static void Synthesize(
            string text,
            string voiceId,
            string modelId,
            double speed,
            string speakerHint,
            string localizationKey,
            int generation)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    CachedSpeechResult result = CachedSpeechService.Instance.GetOrSynthesize(
                        text,
                        voiceId,
                        modelId,
                        speed,
                        speakerHint,
                        localizationKey);

                    Main.LogVerbose("Synth ready (gen " + generation + ", fromCache=" + result.FromCache
                        + ", " + (result.Audio != null ? result.Audio.Length : 0) + " bytes): " + result.CacheId);

                    if (!string.IsNullOrEmpty(result.AudioFilePath))
                    {
                        AudioPlaybackService.Instance.EnqueuePlayFile(result.AudioFilePath, generation);
                    }
                    else
                    {
                        Debug.LogWarning("[KMNarrator] Narration produced no audio file path for " + result.CacheId + ".");
                    }
                }
                catch (Exception ex)
                {
                    Main.LogWarningThrottled("Narration failed: " + ex.Message);
                }
            });
        }

        public static bool TryIsNarratorCue()
        {
            try
            {
                BlueprintCue cue = Game.Instance?.DialogController?.CurrentCue;
                return cue != null && cue.Speaker != null && cue.Speaker.NoSpeaker;
            }
            catch
            {
                return false;
            }
        }

        public static string TryGetDialogSpeakerHint()
        {
            try
            {
                return Game.Instance?.DialogController?.CurrentSpeaker?.CharacterName;
            }
            catch
            {
                return null;
            }
        }

        private static string Truncate(string text)
        {
            return text.Length <= 60 ? text : text.Substring(0, 60) + "...";
        }
    }
}
