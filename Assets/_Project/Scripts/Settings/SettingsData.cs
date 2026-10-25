using System;

namespace DesiMerge.Settings
{
    [Serializable]
    public sealed class SettingsData
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public float musicVolume = 0.8f;
        public float sfxVolume = 1f;
        public bool hapticsEnabled = true;
    }
}
