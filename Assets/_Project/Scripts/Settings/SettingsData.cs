using System;

namespace DesiMerge.Settings
{
    /// <summary>
    /// Version 2 added the mute flags. Version 1 saves load fine: JsonUtility keeps the field defaults below
    /// for fields missing from the file, so old players start unmuted.
    /// </summary>
    [Serializable]
    public sealed class SettingsData
    {
        public const int CurrentSchemaVersion = 2;

        public int schemaVersion = CurrentSchemaVersion;
        public float musicVolume = 0.8f;
        public float sfxVolume = 1f;
        public bool musicMuted;
        public bool sfxMuted;
        public bool hapticsEnabled = true;
    }
}
