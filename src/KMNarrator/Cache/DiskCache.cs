using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace KMNarrator.Cache
{
    public sealed class DiskCache
    {
        private static readonly object Gate = new object();

        public static DiskCache Instance { get; } = new DiskCache();

        private CacheManifest _manifest;
        private string _manifestPath;
        private string _rootPath;

        private DiskCache()
        {
        }

        public string RootPath
        {
            get
            {
                EnsureLoaded();
                return _rootPath;
            }
        }

        public string GetAudioFilePath(string cacheId)
        {
            EnsureLoaded();

            lock (Gate)
            {
                CacheEntry manifestEntry;
                if (!_manifest.Entries.TryGetValue(cacheId, out manifestEntry))
                {
                    return null;
                }

                return ResolveAudioPath(manifestEntry);
            }
        }

        public bool TryGet(string locale, string cacheId, out byte[] audio, out CacheEntry entry)
        {
            EnsureLoaded();
            audio = null;
            entry = null;

            lock (Gate)
            {
                if (!_manifest.Entries.TryGetValue(cacheId, out CacheEntry manifestEntry))
                {
                    return false;
                }

                string filePath = ResolveAudioPath(manifestEntry);
                if (!File.Exists(filePath))
                {
                    _manifest.Entries.Remove(cacheId);
                    _manifest.Save(_manifestPath);
                    return false;
                }

                entry = manifestEntry;
                audio = File.ReadAllBytes(filePath);
                return audio.Length > 0;
            }
        }

        public void Put(
            string locale,
            string cacheId,
            byte[] audio,
            string text,
            string voiceId,
            string modelId,
            double speed,
            string speakerHint,
            string localizationKey)
        {
            if (audio == null || audio.Length == 0)
            {
                throw new ArgumentException("Audio is empty.", nameof(audio));
            }

            EnsureLoaded();
            string safeLocale = SanitizeLocale(locale);
            string localeDir = Path.Combine(_rootPath, safeLocale);
            Directory.CreateDirectory(localeDir);

            string relativeFile = safeLocale + "/" + cacheId + ".mp3";
            string absoluteFile = Path.Combine(_rootPath, relativeFile.Replace('/', Path.DirectorySeparatorChar));

            lock (Gate)
            {
                File.WriteAllBytes(absoluteFile, audio);

                _manifest.Entries[cacheId] = new CacheEntry
                {
                    Text = text,
                    Locale = safeLocale,
                    VoiceId = voiceId,
                    ModelId = modelId,
                    Speed = speed,
                    SpeakerHint = speakerHint,
                    LocalizationKey = localizationKey,
                    CreatedUtc = DateTime.UtcNow.ToString("o"),
                    File = relativeFile
                };

                _manifest.Save(_manifestPath);
            }

            Debug.Log("[KMNarrator] Cached speech: " + relativeFile);
        }

        private void EnsureLoaded()
        {
            if (_manifest != null)
            {
                return;
            }

            lock (Gate)
            {
                if (_manifest != null)
                {
                    return;
                }

                _rootPath = ResolveRootPath();
                Directory.CreateDirectory(_rootPath);
                _manifestPath = Path.Combine(_rootPath, "manifest.json");
                _manifest = CacheManifest.Load(_manifestPath);
            }
        }

        private static string ResolveRootPath()
        {
            string modDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(modDir))
            {
                modDir = AppDomain.CurrentDomain.BaseDirectory;
            }

            return Path.Combine(modDir, "cache");
        }

        private string ResolveAudioPath(CacheEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.File))
            {
                return null;
            }

            return Path.Combine(_rootPath, entry.File.Replace('/', Path.DirectorySeparatorChar));
        }

        private static string SanitizeLocale(string locale)
        {
            if (string.IsNullOrWhiteSpace(locale))
            {
                return "enGB";
            }

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                locale = locale.Replace(c, '_');
            }

            return locale;
        }
    }
}
