namespace DesiMerge.Persistence
{
    /// <summary>
    /// Raw local storage for serialized save data, addressed by key.
    /// Keys must be simple identifiers (letters, digits, '-', '_').
    /// Implementations keep the previous version of each key as a backup when it is overwritten.
    /// Read/write methods may throw IO exceptions; <see cref="JsonRepository{T}"/> handles them.
    /// </summary>
    public interface ISaveStore
    {
        bool TryRead(string key, out string contents);

        /// <summary>The version of <paramref name="key"/> that was current before the latest write, if any.</summary>
        bool TryReadBackup(string key, out string contents);

        void Write(string key, string contents);

        void Delete(string key);
    }
}
