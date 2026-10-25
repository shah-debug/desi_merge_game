namespace DesiMerge.Persistence
{
    /// <summary>
    /// Raw local storage for serialized save data, addressed by key.
    /// Keys must be simple identifiers (letters, digits, '-', '_').
    /// </summary>
    public interface ISaveStore
    {
        bool TryRead(string key, out string contents);

        void Write(string key, string contents);

        void Delete(string key);
    }
}
