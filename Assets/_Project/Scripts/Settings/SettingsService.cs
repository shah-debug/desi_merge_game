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
            _data = store.TryRead(SaveKey.Settings, out _) ? _repository.Load() : Copy(defaults ?? new SettingsData());
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

        public bool HapticsEnabled
        {
            get => _data.hapticsEnabled;
            set
            {
                if (_data.hapticsEnabled == value)
                {
                    return;
                }

                _data.hapticsEnabled = value;
                MarkChanged();
            }
        }

        public event Action Changed;

        public void Flush()
        {
            if (!_dirty)
            {
                return;
            }

            _repository.Save(_data);
            _dirty = false;
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
                hapticsEnabled = source.hapticsEnabled,
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
