using System;
using Newtonsoft.Json;

namespace KMNarrator.Cache
{
    public sealed class CacheEntry
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("locale")]
        public string Locale { get; set; }

        [JsonProperty("voiceId")]
        public string VoiceId { get; set; }

        [JsonProperty("modelId")]
        public string ModelId { get; set; }

        [JsonProperty("speed")]
        public double Speed { get; set; }

        [JsonProperty("speakerHint")]
        public string SpeakerHint { get; set; }

        [JsonProperty("localizationKey")]
        public string LocalizationKey { get; set; }

        [JsonProperty("createdUtc")]
        public string CreatedUtc { get; set; }

        [JsonProperty("file")]
        public string File { get; set; }
    }
}
