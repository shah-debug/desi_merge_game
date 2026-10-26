using System;
using System.Collections.Generic;
using DesiMerge.Settings;
using UnityEngine;

namespace DesiMerge.Audio
{
    /// <summary>
    /// AudioSource-backed audio. Owns one music source and a small round-robin pool of SFX sources on a host
    /// GameObject; the bootstrapper's host survives scene loads, so there is only ever one music player.
    /// Volumes follow <see cref="ISettingsService"/> through its Changed event (no polling).
    /// </summary>
    public sealed class AudioService : IAudioService, IDisposable
    {
        private readonly AudioLibrary _library;
        private readonly ISettingsService _settings;
        private readonly AudioSource _music;
        private readonly AudioSource[] _sfxVoices;
        private readonly float[] _sfxBaseVolumes;
        private readonly HashSet<int> _reportedMissing = new HashSet<int>();
        private float _musicBaseVolume;
        private int _nextVoice;
        private bool _disposed;

        public AudioService(GameObject host, AudioLibrary library, ISettingsService settings)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            _library = library ? library : throw new ArgumentNullException(nameof(library));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            _music = CreateSource(host);
            _music.loop = true;

            _sfxVoices = new AudioSource[library.SfxVoices];
            _sfxBaseVolumes = new float[_sfxVoices.Length];
            for (var i = 0; i < _sfxVoices.Length; i++)
            {
                _sfxVoices[i] = CreateSource(host);
            }

            _settings.Changed += ApplyVolumes;
            ApplyVolumes();
        }

        public MusicTrack CurrentTrack { get; private set; }
        public AudioSource MusicSource => _music;
        public int SourceCount => _sfxVoices.Length + 1;

        public void PlaySfx(SfxCue cue, float pitch = 1f)
        {
            if (_disposed || !_library.TryGetSfx(cue, out var clip, out var baseVolume))
            {
                ReportMissingOnce((int)cue + 1000, $"SFX '{cue}'");
                return;
            }

            if (_settings.SfxMuted)
            {
                return;
            }

            var index = _nextVoice;
            _nextVoice = (_nextVoice + 1) % _sfxVoices.Length;
            var voice = _sfxVoices[index];
            _sfxBaseVolumes[index] = baseVolume;
            voice.Stop();
            voice.clip = clip;
            voice.pitch = pitch;
            voice.volume = AudioMix.Effective(baseVolume, _settings.SfxVolume, false);
            voice.Play();
        }

        public void PlayMusic(MusicTrack track)
        {
            if (_disposed)
            {
                return;
            }

            if (track == MusicTrack.None)
            {
                StopMusic();
                return;
            }

            if (CurrentTrack == track && _music.clip != null)
            {
                // Same track (e.g. returning to a scene): keep playing; just undo a pause.
                if (!_music.isPlaying)
                {
                    _music.UnPause();
                }

                return;
            }

            if (!_library.TryGetMusic(track, out var clip, out var baseVolume))
            {
                ReportMissingOnce((int)track, $"music '{track}'");
                StopMusic();
                return;
            }

            CurrentTrack = track;
            _musicBaseVolume = baseVolume;
            _music.clip = clip;
            ApplyVolumes();
            _music.Play();
        }

        public void StopMusic()
        {
            CurrentTrack = MusicTrack.None;
            if (!_disposed)
            {
                _music.Stop();
                _music.clip = null;
            }
        }

        public void SetMusicPaused(bool paused)
        {
            if (_disposed || _music.clip == null)
            {
                return;
            }

            if (paused)
            {
                _music.Pause();
            }
            else
            {
                _music.UnPause();
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _settings.Changed -= ApplyVolumes;
        }

        private void ApplyVolumes()
        {
            _music.volume = AudioMix.Effective(_musicBaseVolume, _settings.MusicVolume, _settings.MusicMuted);
            for (var i = 0; i < _sfxVoices.Length; i++)
            {
                _sfxVoices[i].volume = AudioMix.Effective(_sfxBaseVolumes[i], _settings.SfxVolume, _settings.SfxMuted);
            }
        }

        private void ReportMissingOnce(int key, string description)
        {
            if (!_disposed && _reportedMissing.Add(key))
            {
                Debug.LogWarning($"[Audio] No clip assigned for {description}; it will stay silent.");
            }
        }

        private static AudioSource CreateSource(GameObject host)
        {
            var source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }
    }
}
