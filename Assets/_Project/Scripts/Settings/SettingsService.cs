using System;
using DesiMerge.Persistence;
using UnityEngine;

namespace DesiMerge.Settings
{
    public sealed class SettingsService : ISettingsService
    {
        private readonly JsonRepository<SettingsData> _repository;
        private readonly SettingsData _data;
        private bool _dirty;

        /// <param name="defaults">Values used when no settings have been saved yet.</param>
        public SettingsService(ISaveStore store, SettingsData defaults)
        {
            _repository = new JsonRepository<SettingsData>(store, SaveKey.Settings);
            var loaded = _repository.Load(out var outcome);
            // First launch (or unreadable save with no backup): use the designer defaults, not the class defaults.
            _data = outcome == LoadOutcome.Loaded || outcome == LoadOutcome.RecoveredFromBackup ? loaded : Copy(defaults ?? new SettingsData());
            Sanitize(_data);
        }

        public float MusicVolume
        {
            get => _data.musicVolume;
            set => Set(ref _data.musicVolume, Mathf.Clamp01(value));
        }

        public float SfxVolume
        {
            get => _data.sfxVolume;
            set => Set(ref _data.sfxVolume, Mathf.Clamp01(value));
        }

        public bool MusicMuted
        {
            get => _data.musicMuted;
            set => Set(ref _data.musicMuted, value);
        }

        public bool SfxMuted
        {
            get => _data.sfxMuted;
            set => Set(ref _data.sfxMuted, value);
        }

        public bool HapticsEnabled
        {
            get => _data.hapticsEnabled;
            set => Set(ref _data.hapticsEnabled, value);
        }

        public bool TutorialCompleted
        {
            get => _data.tutorialCompleted;
            set => Set(ref _data.tutorialCompleted, value);
        }

        public event Action Changed;

        public void Flush()
        {
            if (!_dirty)
            {
                return;
            }

            if (_repository.Save(_data))
            {
                _dirty = false;
            }
        }

        private void Set(ref float field, float value)
        {
            if (Mathf.Approximately(field, value))
            {
                return;
            }

            field = value;
            MarkChanged();
        }

        private void Set(ref bool field, bool value)
        {
            if (field == value)
            {
                return;
            }

            field = value;
            MarkChanged();
        }

        private void MarkChanged()
        {
            _dirty = true;
            Changed?.Invoke();
        }

        private static SettingsData Copy(SettingsData source)
        {
            return new SettingsData
            {
                musicVolume = source.musicVolume,
                sfxVolume = source.sfxVolume,
                musicMuted = source.musicMuted,
                sfxMuted = source.sfxMuted,
                hapticsEnabled = source.hapticsEnabled,
                tutorialCompleted = source.tutorialCompleted,
            };
        }

        private static void Sanitize(SettingsData data)
        {
            data.schemaVersion = SettingsData.CurrentSchemaVersion;
            data.musicVolume = Mathf.Clamp01(data.musicVolume);
            data.sfxVolume = Mathf.Clamp01(data.sfxVolume);
        }
    }
}
