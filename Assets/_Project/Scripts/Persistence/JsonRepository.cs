using System;
using UnityEngine;

namespace DesiMerge.Persistence
{
    public enum LoadOutcome
    {
        /// <summary>No save existed (first launch). Defaults were used.</summary>
        NoSave = 0,
        /// <summary>The current save was read successfully.</summary>
        Loaded = 1,
        /// <summary>The current save was unreadable; the backup (previous version) was used instead.</summary>
        RecoveredFromBackup = 2,
        /// <summary>Neither the save nor its backup could be read. Defaults were used; the bad file was kept aside.</summary>
        ResetToDefaults = 3,
        /// <summary>The storage threw while reading (not "missing"). Defaults are used and this session never overwrites the file.</summary>
        ReadFailed = 4,
    }

    /// <summary>
    /// Loads and saves one serializable data object under a single key using JsonUtility.
    /// Load order: current save → backup → defaults. An unreadable save is copied to "key_corrupt" before anything
    /// else happens, and nothing is written at load time, so a failed load never destroys recoverable data.
    /// Save never throws: failures are logged and reported, and the caller keeps its changes for the next attempt.
    /// </summary>
    public sealed class JsonRepository<T> where T : class, new()
    {
        private readonly ISaveStore _store;
        private readonly string _key;
        private bool _reportedSaveFailure;
        private bool _readThrew;
        private bool _writeBlocked;

        public JsonRepository(ISaveStore store, string key)
        {
            SaveKey.Validate(key);
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _key = key;
        }

        public T Load() => Load(out _);

        public T Load(out LoadOutcome outcome)
        {
            var hasMain = TryReadSafely(false, out var json);
            if (hasMain && TryParse(json, out var data))
            {
                outcome = LoadOutcome.Loaded;
                return data;
            }

            if (hasMain)
            {
                Debug.LogWarning($"[Save] Corrupt data for '{_key}'; trying the backup.");
                TryQuarantine(json);
            }

            if (TryReadSafely(true, out var backupJson) && TryParse(backupJson, out var backup))
            {
                Debug.LogWarning($"[Save] Restored '{_key}' from its backup.");
                outcome = LoadOutcome.RecoveredFromBackup;
                return backup;
            }

            if (_readThrew)
            {
                // The data may be fine but temporarily unreadable: never let this session's defaults replace it.
                Debug.LogWarning($"[Save] '{_key}' could not be read; using defaults for this session without saving over it.");
                _writeBlocked = true;
                outcome = LoadOutcome.ReadFailed;
                return new T();
            }

            if (!hasMain)
            {
                outcome = LoadOutcome.NoSave;
                return new T();
            }

            Debug.LogWarning($"[Save] '{_key}' and its backup are unreadable; using defaults. The bad copy is kept as '{_key}_corrupt'.");
            outcome = LoadOutcome.ResetToDefaults;
            return new T();
        }

        /// <returns>False if the write failed (disk full, permissions, ...). Never throws for IO problems.</returns>
        public bool Save(T data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (_writeBlocked)
            {
                return false;
            }

            try
            {
                _store.Write(_key, JsonUtility.ToJson(data));
                _reportedSaveFailure = false;
                return true;
            }
            catch (Exception e)
            {
                if (!_reportedSaveFailure)
                {
                    Debug.LogWarning($"[Save] Could not write '{_key}': {e.Message}. Will retry on the next save.");
                    _reportedSaveFailure = true;
                }

                return false;
            }
        }

        private bool TryReadSafely(bool backup, out string json)
        {
            try
            {
                var found = backup ? _store.TryReadBackup(_key, out json) : _store.TryRead(_key, out json);
                return found && !string.IsNullOrWhiteSpace(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] Could not read '{_key}'{(backup ? " backup" : string.Empty)}: {e.Message}");
                _readThrew = true;
                json = null;
                return false;
            }
        }

        private static bool TryParse(string json, out T data)
        {
            try
            {
                data = JsonUtility.FromJson<T>(json);
                return data != null;
            }
            catch (Exception)
            {
                data = null;
                return false;
            }
        }

        private void TryQuarantine(string json)
        {
            try
            {
                _store.Write(_key + "_corrupt", json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] Could not quarantine corrupt data for '{_key}'. {e.Message}");
            }
        }
    }
}
