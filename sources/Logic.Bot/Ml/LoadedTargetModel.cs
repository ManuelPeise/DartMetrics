using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.ML;

namespace Logic.Bot.Ml
{
    /// <summary>
    /// Ein geladenes Zielwahl-Modell. Threadsicher: PredictionEngine ist es nicht, daher ein Pool
    /// (je Vorhersage ausleihen und zurückgeben).
    /// </summary>
    public sealed class LoadedTargetModel
    {
        private readonly MLContext _ml;
        private readonly ITransformer _model;
        private readonly ConcurrentBag<PredictionEngine<TargetCandidateRow, DartsPrediction>> _engines = [];

        private LoadedTargetModel(MLContext ml, ITransformer model)
        {
            _ml = ml;
            _model = model;
        }

        /// <summary>Lädt die Datei und prüft die SHA-256; eine veränderte Datei wird nicht geladen.</summary>
        public static LoadedTargetModel Load(string path, string expectedSha256)
        {
            using (var stream = File.OpenRead(path))
            {
                var actual = Convert.ToHexStringLower(SHA256.HashData(stream));

                if (!string.Equals(actual, expectedSha256, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException($"SHA-256 der Modelldatei stimmt nicht: {path}");
                }
            }

            var ml = new MLContext(seed: 0);
            var model = ml.Model.Load(path, out _);

            return new LoadedTargetModel(ml, model);
        }

        public float Predict(TargetCandidateRow row)
        {
            if (!_engines.TryTake(out var engine))
            {
                engine = _ml.Model.CreatePredictionEngine<TargetCandidateRow, DartsPrediction>(_model);
            }

            try
            {
                return engine.Predict(row).Score;
            }
            finally
            {
                _engines.Add(engine);
            }
        }
    }
}
