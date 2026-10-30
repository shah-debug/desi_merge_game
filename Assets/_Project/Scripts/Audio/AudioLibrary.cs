using System;
using UnityEngine;

namespace DesiMerge.Audio
{
    /// <summary>
    /// Maps logical cues to clips and per-cue base volumes. Any clip may be left empty; the cue is then silent.
    /// Phase 3 clips are generated placeholders (see Documentation/PhaseStatus.md).
    /// </summary>
    [CreateAssetMenu(fileName = "AudioLibrary", menuName = "Desi Merge/Audio Library")]
    public sealed class AudioLibrary : ScriptableObject
    {
        [Serializable]
        public struct SfxEntry
        {
            public SfxCue cue;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume;
        }

        [Serializable]
        public struct MusicEntry
        {
            public MusicTrack track;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume;
        }

        [SerializeField] private SfxEntry[] sfx = Array.Empty<SfxEntry>();
        [SerializeField] private MusicEntry[] music = Array.Empty<MusicEntry>();

        [Tooltip("How many sound effects can overlap.")]
        [SerializeField, Range(1, 16)] private int sfxVoices = 6;

        public int SfxVoices => sfxVoices;
        internal System.Collections.Generic.IReadOnlyList<SfxEntry> SfxEntries => sfx;
        internal System.Collections.Generic.IReadOnlyList<MusicEntry> MusicEntries => music;

        public bool TryGetSfx(SfxCue cue, out AudioClip clip, out float volume)
        {
            foreach (var entry in sfx)
            {
                if (entry.cue == cue && entry.clip != null)
                {
                    clip = entry.clip;
                    volume = entry.volume;
                    return true;
                }
            }

            clip = null;
            volume = 0f;
            return false;
        }

        public bool TryGetMusic(MusicTrack track, out AudioClip clip, out float volume)
        {
            foreach (var entry in music)
            {
                if (entry.track == track && entry.clip != null)
                {
                    clip = entry.clip;
                    volume = entry.volume;
                    return true;
                }
            }

            clip = null;
            volume = 0f;
            return false;
        }

        /// <summary>Used by editor tooling and tests to author libraries in code.</summary>
        internal void Set(SfxEntry[] sfxEntries, MusicEntry[] musicEntries, int voices = 6)
        {
            sfx = sfxEntries;
            music = musicEntries;
            sfxVoices = voices;
        }
    }
}
