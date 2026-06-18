using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KMNarrator.ElevenLabs
{
    public sealed class ElevenLabsClient
    {
        private static readonly HttpClient Http = new HttpClient();

        private static readonly object VoiceCacheLock = new object();
        private static readonly TimeSpan VoiceCacheTtl = TimeSpan.FromMinutes(15);

        private static string _cachedApiKey;
        private static IReadOnlyList<ElevenLabsVoice> _cachedVoices;
        private static DateTime _cachedAtUtc;

        public static ElevenLabsClient Instance { get; } = new ElevenLabsClient();

        private ElevenLabsClient()
        {
        }

        public void InvalidateVoiceCache()
        {
            lock (VoiceCacheLock)
            {
                _cachedApiKey = null;
                _cachedVoices = null;
            }
        }

        public byte[] Synthesize(string text, string voiceId, string modelId, double speed)
        {
            return SynthesizeAsync(text, voiceId, modelId, speed, CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        }

        public async Task<byte[]> SynthesizeAsync(
            string text,
            string voiceId,
            string modelId,
            double speed,
            CancellationToken cancellationToken)
        {
            string apiKey = GetApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("ElevenLabs API key is not set. Add it in mod settings.");
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Text is empty.", nameof(text));
            }

            string voice = string.IsNullOrWhiteSpace(voiceId) ? ElevenLabsDefaults.VoiceId : voiceId;
            string model = string.IsNullOrWhiteSpace(modelId) ? ElevenLabsDefaults.ModelId : modelId;
            double clampedSpeed = Clamp(speed, ElevenLabsDefaults.MinSpeed, ElevenLabsDefaults.MaxSpeed);

            string url = "https://api.elevenlabs.io/v1/text-to-speech/" + Uri.EscapeDataString(voice)
                + "?output_format=mp3_44100_128";

            var payload = new JObject
            {
                ["text"] = text,
                ["model_id"] = model,
                ["voice_settings"] = new JObject
                {
                    ["speed"] = clampedSpeed
                }
            };

            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Headers.Add("xi-api-key", apiKey);
                request.Content = new StringContent(payload.ToString(Formatting.None), Encoding.UTF8, "application/json");

                using (HttpResponseMessage response = await Http.SendAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        string error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        throw new InvalidOperationException(FormatTtsError(response.StatusCode, error));
                    }

                    return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                }
            }
        }

        public IReadOnlyList<ElevenLabsVoice> GetVoices()
        {
            return GetVoicesAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        public async Task<IReadOnlyList<ElevenLabsVoice>> GetVoicesAsync(CancellationToken cancellationToken)
        {
            string apiKey = GetApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return Array.Empty<ElevenLabsVoice>();
            }

            lock (VoiceCacheLock)
            {
                if (_cachedVoices != null
                    && _cachedApiKey == apiKey
                    && DateTime.UtcNow - _cachedAtUtc < VoiceCacheTtl)
                {
                    return _cachedVoices;
                }
            }

            try
            {
                IReadOnlyList<ElevenLabsVoice> voices = await FetchVoicesFromApiAsync(apiKey, cancellationToken)
                    .ConfigureAwait(false);

                lock (VoiceCacheLock)
                {
                    _cachedApiKey = apiKey;
                    _cachedVoices = voices;
                    _cachedAtUtc = DateTime.UtcNow;
                }

                return voices;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[KMNarrator] Failed to list ElevenLabs voices: " + ex.Message);
                return Array.Empty<ElevenLabsVoice>();
            }
        }

        private static async Task<IReadOnlyList<ElevenLabsVoice>> FetchVoicesFromApiAsync(
            string apiKey,
            CancellationToken cancellationToken)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, "https://api.elevenlabs.io/v1/voices"))
            {
                request.Headers.Add("xi-api-key", apiKey);

                using (HttpResponseMessage response = await Http.SendAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        Debug.LogWarning("[KMNarrator] " + FormatVoiceListError(response.StatusCode));
                        return Array.Empty<ElevenLabsVoice>();
                    }

                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var root = JObject.Parse(json);
                    var voices = new List<ElevenLabsVoice>();

                    if (root["voices"] is JArray array)
                    {
                        foreach (JToken element in array)
                        {
                            string id = element["voice_id"]?.Value<string>();
                            string name = element["name"]?.Value<string>() ?? id;
                            if (!string.IsNullOrEmpty(id))
                            {
                                voices.Add(new ElevenLabsVoice(id, name));
                            }
                        }
                    }

                    return voices;
                }
            }
        }

        private static string GetApiKey()
        {
            Settings settings = Main.Settings;
            return settings == null ? null : settings.ApiKey;
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        private static string FormatTtsError(HttpStatusCode statusCode, string responseBody)
        {
            int code = (int)statusCode;
            switch (code)
            {
                case 401:
                    return "ElevenLabs API key is invalid or expired. Check your key in mod settings.";
                case 402:
                    return "ElevenLabs account has insufficient credits. Top up or change your plan.";
                case 429:
                    return "ElevenLabs rate limit reached. Wait a moment before narrating more lines.";
                default:
                    return "ElevenLabs TTS failed (" + code + "): " + Truncate(responseBody);
            }
        }

        private static string FormatVoiceListError(HttpStatusCode statusCode)
        {
            int code = (int)statusCode;
            if (code == 401)
            {
                return "Could not list ElevenLabs voices: API key is invalid or expired.";
            }

            if (code == 429)
            {
                return "Could not list ElevenLabs voices: rate limit reached. Try again shortly.";
            }

            return "Could not list ElevenLabs voices (" + code + ").";
        }

        private static string Truncate(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= 300)
            {
                return text ?? "";
            }

            return text.Substring(0, 300);
        }
    }
}
