using System.Collections.Generic;
using System.IO;

namespace DesiMerge.Persistence
{
    /// <summary>Non-persistent store for tests, editor tooling and Editor-only scene fallbacks. Mirrors FileSaveStore's backup behaviour.</summary>
    public sealed class InMemorySaveStore : ISaveStore
    {
        private readonly Dictionary<string, string> _entries = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _backups = new Dictionary<string, string>();

        public int WriteCount { get; private set; }

        /// <summary>Test hook: when true, writes throw like a full or read-only disk.</summary>
        public bool FailWrites { get; set; }

        public bool TryRead(string key, out string contents)
        {
            SaveKey.Validate(key);
            return _entries.TryGetValue(key, out contents);
        }

        public bool TryReadBackup(string key, out string contents)
        {
            SaveKey.Validate(key);
            return _backups.TryGetValue(key, out contents);
        }

        public void Write(string key, string contents)
        {
            SaveKey.Validate(key);
            if (FailWrites)
            {
                throw new IOException("Simulated write failure.");
            }

            if (_entries.TryGetValue(key, out var previous))
            {
                _backups[key] = previous;
            }

            _entries[key] = contents;
            WriteCount++;
        }

        public void Delete(string key)
        {
            SaveKey.Validate(key);
            _entries.Remove(key);
        }

        /// <summary>Test hook: replaces the current contents without touching the backup (simulates disk corruption).</summary>
        public void Corrupt(string key, string contents)
        {
            _entries[key] = contents;
        }
    }
}
