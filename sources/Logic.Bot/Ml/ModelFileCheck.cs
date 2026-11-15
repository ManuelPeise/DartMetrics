using System.Security.Cryptography;
using Data.DartContext.Entities.Bot;
using Shared.Enums.Bot;
using Shared.Models.Bot.Ml;

namespace Logic.Bot.Ml
{
    /// <summary>Prüft eine Modelldatei unter dem ML-Ordner: Pfad bleibt im Ordner, Datei vorhanden, Schema und SHA-256 passen.</summary>
    public static class ModelFileCheck
    {
        public static string? ResolvePath(string mlRoot, string storageKey)
        {
            var root = Path.GetFullPath(mlRoot);
            var path = Path.GetFullPath(Path.Combine(root, storageKey));
            var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;

            return path.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase) ? path : null;
        }

        public static DartBotModelFileStatusEnum Check(string mlRoot, DartBotModelEntity model)
        {
            if (model.FeatureSchemaVersion != TargetCandidateFeatures.FeatureSchemaVersion)
            {
                return DartBotModelFileStatusEnum.SchemaMismatch;
            }

            var path = ResolvePath(mlRoot, model.StorageKey);

            if (path is null || !File.Exists(path))
            {
                return DartBotModelFileStatusEnum.Missing;
            }

            using var stream = File.OpenRead(path);
            var actual = Convert.ToHexString(SHA256.HashData(stream));

            return string.Equals(actual, model.Sha256, StringComparison.OrdinalIgnoreCase)
                ? DartBotModelFileStatusEnum.Ok
                : DartBotModelFileStatusEnum.ShaMismatch;
        }
    }
}
