using System;
using System.Collections.Generic;
using UnityModManagerNet;

namespace KMNarrator
{
    public class Settings : UnityModManager.ModSettings
    {
        public string ApiKey = "";

        public string DefaultVoiceId = ElevenLabsDefaults.VoiceId;

        public string ModelId = ElevenLabsDefaults.ModelId;

        public float SpeechSpeed = (float)ElevenLabsDefaults.DefaultSpeed;

        public float Volume = 1f;

        public bool OnlyUnvoicedLines = true;

        public bool IncludeStageDirections = true;

        public bool EnableBookEvents = true;

        public bool EnableBarks;

        public bool VerboseLogging;

        public List<VoiceMapping> VoiceMappings = new List<VoiceMapping>();

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            UnityModManager.ModSettings.Save(this, modEntry);
        }
    }

    public class VoiceMapping
    {
        public string Speaker = "";

        public string VoiceId = "";

        public float Speed = (float)ElevenLabsDefaults.DefaultSpeed;
    }
}
