using System;
using KMNarrator.Cache;
using KMNarrator.ElevenLabs;

namespace KMNarrator.Voice
{
    public sealed class CachedSpeechResult
    {
        public CachedSpeechResult(byte[] audio, bool fromCache, string cacheId, string locale, string audioFilePath)
        {
            Audio = audio;
            FromCache = fromCache;
            CacheId = cacheId;
            Locale = locale;
            AudioFilePath = audioFilePath;
        }

        public byte[] Audio { get; }

        public bool FromCache { get; }

        public string CacheId { get; }

        public string Locale { get; }

        public string AudioFilePath { get; }
    }

    public sealed class CachedSpeechService
    {
        public static CachedSpeechService Instance { get; } = new CachedSpeechService();

        private CachedSpeechService()
        {
        }

        public CachedSpeechResult GetOrSynthesize(
            string text,
            string voiceId,
            string modelId,
            double speed,
            string speakerHint = null,
            string localizationKey = null)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Text is empty.", nameof(text));
            }

            string locale = GameLocale.GetCurrent();
            string cacheId = CacheKey.Compute(locale, voiceId, modelId, speed, text);

            byte[] cachedAudio;
            CacheEntry entry;
            if (DiskCache.Instance.TryGet(locale, cacheId, out cachedAudio, out entry))
            {
                string cachedPath = DiskCache.Instance.GetAudioFilePath(cacheId);
                return new CachedSpeechResult(cachedAudio, true, cacheId, locale, cachedPath);
            }

            byte[] audio = ElevenLabsClient.Instance.Synthesize(text, voiceId, modelId, speed);
            DiskCache.Instance.Put(
                locale,
                cacheId,
                audio,
                text,
                voiceId,
                modelId,
                speed,
                speakerHint,
                localizationKey);

            string filePath = DiskCache.Instance.GetAudioFilePath(cacheId);
            return new CachedSpeechResult(audio, false, cacheId, locale, filePath);
        }
    }
}
