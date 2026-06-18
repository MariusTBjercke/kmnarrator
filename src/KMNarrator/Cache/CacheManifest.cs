using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KMNarrator.Cache
{
    internal sealed class CacheManifest
    {
        public Dictionary<string, CacheEntry> Entries { get; set; } = new Dictionary<string, CacheEntry>();

        public static CacheManifest Load(string path)
        {
            if (!File.Exists(path))
            {
                return new CacheManifest();
            }

            try
            {
                var manifest = new CacheManifest();
                string json = File.ReadAllText(path);
                JObject root = JObject.Parse(json);
                JToken entriesToken = root["entries"];
                if (entriesToken == null)
                {
                    return manifest;
                }

                if (entriesToken is JObject entriesObject)
                {
                    LoadEntriesObject(manifest, entriesObject);
                }
                else if (entriesToken is JArray entriesArray)
                {
                    LoadLegacyEntriesArray(manifest, entriesArray);
                }

                return manifest;
            }
            catch (Exception)
            {
                return new CacheManifest();
            }
        }

        public void Save(string path)
        {
            var entries = new JObject();
            foreach (KeyValuePair<string, CacheEntry> pair in Entries)
            {
                if (pair.Value == null)
                {
                    continue;
                }

                entries[pair.Key] = SerializeEntry(pair.Value);
            }

            var root = new JObject { ["entries"] = entries };
            string json = root.ToString(Formatting.Indented);
            string tempPath = path + ".tmp";
            File.WriteAllText(tempPath, json);
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.Move(tempPath, path);
        }

        private static void LoadEntriesObject(CacheManifest manifest, JObject entriesObject)
        {
            foreach (JProperty property in entriesObject.Properties())
            {
                CacheEntry entry = ParseEntry(property.Value as JObject);
                if (entry != null)
                {
                    manifest.Entries[property.Name] = entry;
                }
            }
        }

        private static void LoadLegacyEntriesArray(CacheManifest manifest, JArray entriesArray)
        {
            foreach (JToken token in entriesArray)
            {
                JObject row = token as JObject;
                if (row == null)
                {
                    continue;
                }

                string key = row.Value<string>("Key");
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                CacheEntry entry = ParseEntry(row["Value"] as JObject);
                if (entry != null)
                {
                    manifest.Entries[key] = entry;
                }
            }
        }

        private static CacheEntry ParseEntry(JObject entryObject)
        {
            if (entryObject == null || !entryObject.HasValues)
            {
                return null;
            }

            return new CacheEntry
            {
                Text = entryObject.Value<string>("text") ?? entryObject.Value<string>("Text"),
                Locale = entryObject.Value<string>("locale") ?? entryObject.Value<string>("Locale"),
                VoiceId = entryObject.Value<string>("voiceId") ?? entryObject.Value<string>("VoiceId"),
                ModelId = entryObject.Value<string>("modelId") ?? entryObject.Value<string>("ModelId"),
                Speed = entryObject.Value<double?>("speed") ?? entryObject.Value<double?>("Speed") ?? 0d,
                SpeakerHint = entryObject.Value<string>("speakerHint") ?? entryObject.Value<string>("SpeakerHint"),
                LocalizationKey = entryObject.Value<string>("localizationKey")
                    ?? entryObject.Value<string>("LocalizationKey"),
                CreatedUtc = entryObject.Value<string>("createdUtc") ?? entryObject.Value<string>("CreatedUtc"),
                File = entryObject.Value<string>("file") ?? entryObject.Value<string>("File"),
            };
        }

        private static JObject SerializeEntry(CacheEntry entry)
        {
            return new JObject
            {
                ["text"] = entry.Text,
                ["locale"] = entry.Locale,
                ["voiceId"] = entry.VoiceId,
                ["modelId"] = entry.ModelId,
                ["speed"] = entry.Speed,
                ["speakerHint"] = entry.SpeakerHint,
                ["localizationKey"] = entry.LocalizationKey,
                ["createdUtc"] = entry.CreatedUtc,
                ["file"] = entry.File,
            };
        }
    }
}
