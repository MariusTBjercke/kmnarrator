using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;
using UnityEngine;

namespace KMNarrator.Audio
{
    public sealed class AudioPlaybackService : MonoBehaviour
    {
        private static AudioPlaybackService _instance;

        private readonly Queue<PendingClip> _pendingFiles = new Queue<PendingClip>();
        private WaveOutEvent _waveOut;
        private WaveFileReader _reader;
        private MemoryStream _playerStream;
        private volatile bool _playbackActive;
        private volatile bool _needsCleanup;
        private float _playbackEndsAt;
        private float _playbackStartedAt;
        private float _playbackExpected;
        private string _playingName = "";
        private int _generation;

        public static AudioPlaybackService Instance
        {
            get
            {
                EnsureCreated();
                return _instance;
            }
        }

        public static void EnsureCreated()
        {
            if (_instance != null)
            {
                return;
            }

            var root = new GameObject("KMNarrator_Audio");
            DontDestroyOnLoad(root);
            _instance = root.AddComponent<AudioPlaybackService>();
        }

        public bool IsPlaying
        {
            get { return _playbackActive; }
        }

        public int CurrentGeneration
        {
            get { return _generation; }
        }

        public void EnqueuePlayFile(string absoluteFilePath)
        {
            EnqueuePlayFile(absoluteFilePath, _generation);
        }

        public void EnqueuePlayFile(string absoluteFilePath, int generation)
        {
            if (string.IsNullOrWhiteSpace(absoluteFilePath) || !File.Exists(absoluteFilePath))
            {
                Debug.LogWarning("[KMNarrator] Cannot play missing audio file: " + absoluteFilePath);
                return;
            }

            if (generation != _generation)
            {
                Main.LogVerbose("Dropped stale narration (gen " + generation + " != " + _generation + "): "
                    + Path.GetFileName(absoluteFilePath));
                return;
            }

            int count;
            lock (_pendingFiles)
            {
                _pendingFiles.Enqueue(new PendingClip(Path.GetFullPath(absoluteFilePath), generation));
                count = _pendingFiles.Count;
            }

            Main.LogVerbose("Enqueued (gen " + generation + ", queue=" + count + "): " + Path.GetFileName(absoluteFilePath));
        }

        public void Stop(string reason)
        {
            bool wasPlaying = _playbackActive;
            float playedFor = wasPlaying ? Time.realtimeSinceStartup - _playbackStartedAt : 0f;

            _generation++;
            int cleared;
            lock (_pendingFiles)
            {
                cleared = _pendingFiles.Count;
                _pendingFiles.Clear();
            }

            _playbackActive = false;
            _needsCleanup = false;
            DisposePlayer();

            Main.LogVerbose("Stop(" + reason + ") frame=" + Time.frameCount + " gen->" + _generation
                + ", wasPlaying=" + wasPlaying + ", playedFor=" + playedFor.ToString("0.0") + "/"
                + _playbackExpected.ToString("0.0") + "s, clearedQueue=" + cleared
                + (wasPlaying ? " [" + _playingName + "]" : ""));
        }

        private void Update()
        {
            if (_needsCleanup && !_playbackActive)
            {
                _needsCleanup = false;
                DisposePlayer();
            }

            PumpQueue();

            if (_playbackActive && Time.realtimeSinceStartup >= _playbackEndsAt)
            {
                Main.LogVerbose("Playback finished (timeout) frame=" + Time.frameCount + " ~"
                    + _playbackExpected.ToString("0.0") + "s [" + _playingName + "]");
                _playbackActive = false;
                _needsCleanup = true;
            }
        }

        public void PumpQueue()
        {
            if (_playbackActive)
            {
                return;
            }

            string nextPath = null;
            lock (_pendingFiles)
            {
                while (_pendingFiles.Count > 0)
                {
                    PendingClip clip = _pendingFiles.Dequeue();
                    if (clip.Generation != _generation)
                    {
                        Main.LogVerbose("Skipped stale queued narration (gen " + clip.Generation + " != " + _generation + ").");
                        continue;
                    }

                    nextPath = clip.Path;
                    break;
                }
            }

            if (nextPath != null)
            {
                StartPlayback(nextPath);
            }
        }

        private void StartPlayback(string absoluteFilePath)
        {
            string name = Path.GetFileName(absoluteFilePath);
            Main.LogVerbose("StartPlayback gen=" + _generation + " frame=" + Time.frameCount + " file=" + name);

            try
            {
                byte[] wav = BuildPlayableWav(absoluteFilePath);
                if (wav == null)
                {
                    return;
                }

                float seconds;
                if (!WavUtil.TryGetWavDurationSeconds(wav, out seconds) || seconds <= 0f)
                {
                    Debug.LogWarning("[KMNarrator] Could not read WAV duration: " + absoluteFilePath);
                    return;
                }

                Main.LogVerbose("Decoded WAV " + wav.Length + " bytes, " + seconds.ToString("0.0") + "s, file=" + name);

                DisposePlayer();
                float volume = GetVolume();
                _playerStream = new MemoryStream(wav, false);
                _reader = new WaveFileReader(_playerStream);
                _waveOut = new WaveOutEvent();
                _waveOut.PlaybackStopped += OnPlaybackStopped;
                _waveOut.Init(_reader);
                _waveOut.Volume = volume;
                _waveOut.Play();
                _playbackActive = true;
                _needsCleanup = false;
                _playbackStartedAt = Time.realtimeSinceStartup;
                _playbackExpected = seconds;
                _playingName = name;
                _playbackEndsAt = _playbackStartedAt + seconds + 2.0f;
                Debug.Log("[KMNarrator] Playback started (NAudio) — " + seconds.ToString("0.0")
                    + "s, volume=" + Mathf.RoundToInt(volume * 100f) + "%, file=" + name);
            }
            catch (Exception ex)
            {
                _playbackActive = false;
                _needsCleanup = false;
                DisposePlayer();
                Debug.LogWarning("[KMNarrator] NAudio playback failed: " + ex.Message);
            }
        }

        private void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            _playbackActive = false;
            _needsCleanup = true;

            if (e != null && e.Exception != null)
            {
                Debug.LogWarning("[KMNarrator] NAudio playback error: " + e.Exception.Message);
            }
            else
            {
                Main.LogVerbose("Playback finished (NAudio) [" + _playingName + "]");
            }
        }

        private static byte[] BuildPlayableWav(string absoluteFilePath)
        {
            byte[] data = File.ReadAllBytes(absoluteFilePath);

            if (absoluteFilePath.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) || Mp3Decoder.LooksLikeMp3(data))
            {
                return Mp3Decoder.ToWav(data, 1f);
            }

            if (absoluteFilePath.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
            {
                return data;
            }

            Debug.LogWarning("[KMNarrator] Unsupported audio file type: " + absoluteFilePath);
            return null;
        }

        private static float GetVolume()
        {
            Settings settings = Main.Settings;
            float volume = settings != null ? settings.Volume : 1f;
            return Mathf.Clamp(volume, 0f, 1f);
        }

        private void DisposePlayer()
        {
            if (_waveOut != null)
            {
                _waveOut.PlaybackStopped -= OnPlaybackStopped;
                try
                {
                    _waveOut.Stop();
                }
                catch
                {
                }

                _waveOut.Dispose();
                _waveOut = null;
            }

            if (_reader != null)
            {
                _reader.Dispose();
                _reader = null;
            }

            if (_playerStream != null)
            {
                _playerStream.Dispose();
                _playerStream = null;
            }
        }

        private struct PendingClip
        {
            public readonly string Path;
            public readonly int Generation;

            public PendingClip(string path, int generation)
            {
                Path = path;
                Generation = generation;
            }
        }
    }
}
