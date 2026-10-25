using System.Collections.Generic;

namespace DesiMerge.Persistence
{
    /// <summary>Non-persistent store for tests and editor tooling.</summary>
    public sealed class InMemorySaveStore : ISaveStore
    {
        private readonly Dictionary<string, string> _entries = new Dictionary<string, string>();

        public int WriteCount { get; private set; }

        public bool TryRead(string key, out string contents)
        {
            SaveKey.Validate(key);
            return _entries.TryGetValue(key, out contents);
        }

        public void Write(string key, string contents)
        {
            SaveKey.Validate(key);
            _entries[key] = contents;
            WriteCount++;
        }

        public void Delete(string key)
        {
            SaveKey.Validate(key);
            _entries.Remove(key);
        }
    }
}
