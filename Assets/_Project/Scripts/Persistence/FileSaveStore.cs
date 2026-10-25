using System.IO;
using System.Text;

namespace DesiMerge.Persistence
{
    /// <summary>
    /// Stores each key as a UTF-8 JSON file in a single directory (normally under Application.persistentDataPath).
    /// Writes go to a temporary file first and are then swapped in, so an interrupted write
    /// (app killed, battery dies) leaves the previous save intact.
    /// </summary>
    public sealed class FileSaveStore : ISaveStore
    {
        private const string Extension = ".json";
        private const string TempExtension = ".tmp";

        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

        private readonly string _directory;

        public FileSaveStore(string directory)
        {
            _directory = directory;
        }

        public bool TryRead(string key, out string contents)
        {
            SaveKey.Validate(key);
            var path = PathFor(key);
            if (!File.Exists(path))
            {
                contents = null;
                return false;
            }

            contents = File.ReadAllText(path, Utf8NoBom);
            return true;
        }

        public void Write(string key, string contents)
        {
            SaveKey.Validate(key);
            Directory.CreateDirectory(_directory);

            var path = PathFor(key);
            var tempPath = path + TempExtension;
            File.WriteAllText(tempPath, contents, Utf8NoBom);

            if (File.Exists(path))
            {
                File.Replace(tempPath, path, null);
            }
            else
            {
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

        private string PathFor(string key) => Path.Combine(_directory, key + Extension);
    }
}
