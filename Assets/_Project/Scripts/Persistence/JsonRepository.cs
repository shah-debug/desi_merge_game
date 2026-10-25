using System;
using UnityEngine;

namespace DesiMerge.Persistence
{
    /// <summary>
    /// Loads and saves one serializable data object under a single key using JsonUtility.
    /// Missing or unreadable data falls back to a fresh instance instead of crashing the game;
    /// the corrupt payload is kept under a separate key so it can be inspected later.
    /// </summary>
    public sealed class JsonRepository<T> where T : class, new()
    {
        private readonly ISaveStore _store;
        private readonly string _key;

        public JsonRepository(ISaveStore store, string key)
        {
            SaveKey.Validate(key);
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _key = key;
        }

        public T Load()
        {
            string json;
            try
            {
                if (!_store.TryRead(_key, out json) || string.IsNullOrWhiteSpace(json))
                {
                    return new T();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] Could not read '{_key}', using defaults. {e.Message}");
                return new T();
            }

            try
            {
                return JsonUtility.FromJson<T>(json) ?? new T();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] Corrupt data for '{_key}', using defaults. {e.Message}");
                TryQuarantine(json);
                return new T();
            }
        }

        public void Save(T data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            _store.Write(_key, JsonUtility.ToJson(data));
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
