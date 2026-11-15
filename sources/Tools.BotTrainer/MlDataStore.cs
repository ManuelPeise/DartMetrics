using System.Security.Cryptography;

namespace Tools.BotTrainer
{
    /// <summary>
    /// Ablage unter dem ML-Datenordner (MlData:Path, Standard ml_data). StorageKeys sind relativ und werden nie überschrieben.
    /// </summary>
    public sealed class MlDataStore
    {
        public MlDataStore(string root)
        {
            Root = Path.GetFullPath(root);
        }

        public string Root { get; }

        /// <summary>Absoluter Pfad zu einem neuen StorageKey; der Ordner wird angelegt, eine vorhandene Datei ist ein Fehler.</summary>
        public string PathOf(string storageKey, bool mustBeNew = true)
        {
            var path = Path.GetFullPath(Path.Combine(Root, storageKey));

            if (!path.StartsWith(Root, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"StorageKey außerhalb des ML-Ordners: {storageKey}");
            }

            if (mustBeNew && File.Exists(path))
            {
                throw new InvalidOperationException($"Datei existiert bereits und wird nicht überschrieben: {path}");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            return path;
        }

        public static string Sha256Of(string path)
        {
            using var stream = File.OpenRead(path);
            return Convert.ToHexStringLower(SHA256.HashData(stream));
        }
    }
}
