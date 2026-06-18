using System;
using System.Collections.Generic;
using UnityEngine;

namespace KMNarrator.Voice
{
    internal struct VoiceSelection
    {
        public string VoiceId;
        public double Speed;
    }

    internal static class VoiceResolver
    {
        public static VoiceSelection Resolve(string speakerHint, double defaultSpeed)
        {
            Settings settings = Main.Settings;
            var selection = new VoiceSelection
            {
                VoiceId = settings != null ? settings.DefaultVoiceId : null,
                Speed = defaultSpeed,
            };

            if (settings == null || string.IsNullOrWhiteSpace(speakerHint))
            {
                return selection;
            }

            VoiceMapping mapping = LookupMapping(settings.VoiceMappings, speakerHint);
            if (mapping != null)
            {
                selection.VoiceId = mapping.VoiceId.Trim();
                selection.Speed = mapping.Speed > 0f ? mapping.Speed : defaultSpeed;
                Debug.Log("[KMNarrator] Speaker '" + speakerHint + "' -> mapped voice " + selection.VoiceId
                    + " @ speed " + selection.Speed.ToString("0.00") + ".");
                return selection;
            }

            Debug.Log("[KMNarrator] Speaker '" + speakerHint + "' -> default voice " + selection.VoiceId + ".");
            return selection;
        }

        private static VoiceMapping LookupMapping(List<VoiceMapping> mappings, string speakerHint)
        {
            if (mappings == null)
            {
                return null;
            }

            string target = speakerHint.Trim();
            foreach (VoiceMapping mapping in mappings)
            {
                if (mapping == null || string.IsNullOrWhiteSpace(mapping.Speaker) || string.IsNullOrWhiteSpace(mapping.VoiceId))
                {
                    continue;
                }

                if (string.Equals(mapping.Speaker.Trim(), target, StringComparison.OrdinalIgnoreCase))
                {
                    return mapping;
                }
            }

            return null;
        }
    }
}
