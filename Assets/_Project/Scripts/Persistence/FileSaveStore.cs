using System;
using System.IO;
using System.Text;

namespace DesiMerge.Persistence
{
    /// <summary>
    /// Stores each key as a UTF-8 JSON file in a single directory (normally Application.persistentDataPath/saves,
    /// which is app-private internal storage on Android).
    /// Writes go to "key.json.tmp" first and are then swapped in, keeping the replaced file as "key.json.bak",
    /// so an interrupted write (app killed, battery dies) leaves the previous save intact and one older version
    /// is always available for recovery.
    /// </summary>
    public sealed class FileSaveStore : ISaveStore
    {
        private const string Extension = ".json";
        private const string TempExtension = ".tmp";
        private const string BackupExtension = ".bak";

        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

        private readonly string _directory;

        public FileSaveStore(string directory)
        {
            _directory = directory;
        }

        public bool TryRead(string key, out string contents)
        {
            SaveKey.Validate(key);
            return TryReadFile(PathFor(key), out contents);
        }

        public bool TryReadBackup(string key, out string contents)
        {
            SaveKey.Validate(key);
            return TryReadFile(PathFor(key) + BackupExtension, out contents);
        }

        public void Write(string key, string contents)
        {
            SaveKey.Validate(key);
            Directory.CreateDirectory(_directory);

            var path = PathFor(key);
            var tempPath = path + TempExtension;
            var backupPath = path + BackupExtension;
            File.WriteAllText(tempPath, contents, Utf8NoBom);

            if (!File.Exists(path))
            {
                File.Move(tempPath, path);
                return;
            }

            try
            {
                File.Replace(tempPath, path, backupPath);
            }
            catch (PlatformNotSupportedException)
            {
                // Fallback for platforms without an atomic replace: keep a backup copy, then swap.
                File.Copy(path, backupPath, true);
                File.Delete(path);
                File.Move(tempPath, path);
            }
        }

        public void Delete(string key)
        {
            SaveKey.Validate(key);
            var path = PathFor(key);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static bool TryReadFile(string path, out string contents)
        {
            if (!File.Exists(path))
            {
                contents = null;
                return false;
            }

            contents = File.ReadAllText(path, Utf8NoBom);
            return true;
        }

        private string PathFor(string key) => Path.Combine(_directory, key + Extension);
    }
}
