using System.Globalization;
using Shared.Models.Bot.Ml;

namespace Tools.BotTrainer
{
    /// <summary>Schreibt Kandidaten-Zeilen als CSV (Punkt als Dezimaltrennzeichen, Kopfzeile = Spaltennamen für ML.NET).</summary>
    public sealed class CsvDatasetWriter : IDisposable
    {
        public const string Header =
            "Remaining,DartsAvailable,OpponentRemaining,OutMode,IsOpened," +
            "ThrowPrecision,ThrowConsistency,TacticalIntelligence,PressureResistance,RiskTaking," +
            "CandidateSegment,CandidateMultiplier,CandidateScore,LeftoverIfHit,IsFinishingDart,LeftoverIsCheckable," +
            "ExpectedDartsToFinish,DartsStdDev,Rollouts,SituationId,RelativeDarts";

        private readonly StreamWriter _writer;

        public long RowCount { get; private set; }

        public CsvDatasetWriter(string path)
        {
            _writer = new StreamWriter(path, append: false, new System.Text.UTF8Encoding(false));
            _writer.WriteLine(Header);
        }

        public void Write(IEnumerable<TargetCandidateFeatures> rows)
        {
            foreach (var r in rows)
            {
                _writer.WriteLine(string.Join(',',
                    F(r.Remaining), F(r.DartsAvailable), F(r.OpponentRemaining), F(r.OutMode), B(r.IsOpened),
                    F(r.ThrowPrecision), F(r.ThrowConsistency), F(r.TacticalIntelligence), F(r.PressureResistance), F(r.RiskTaking),
                    r.CandidateSegment.ToString(CultureInfo.InvariantCulture), r.CandidateMultiplier.ToString(CultureInfo.InvariantCulture),
                    F(r.CandidateScore), F(r.LeftoverIfHit), B(r.IsFinishingDart), B(r.LeftoverIsCheckable),
                    F(r.ExpectedDartsToFinish), F(r.DartsStdDev), r.Rollouts.ToString(CultureInfo.InvariantCulture),
                    r.SituationId.ToString(CultureInfo.InvariantCulture), F(r.RelativeDarts)));
                RowCount++;
            }
        }

        public void Dispose() => _writer.Dispose();

        private static string F(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        private static string B(bool value) => value ? "1" : "0";
    }
}
