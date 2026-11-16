using Logic.Shared.Rules;
using Shared.Enums.X01;
using Shared.Models.Bot;

namespace Logic.Bot.Policy
{
    /// <summary>
    /// Exakte Zielwahl für ein Leg gegen die Uhr (dynamische Programmierung): erwartete Darts bis zum Checkout je Zustand
    /// (Stand zu Beginn der Aufnahme u, aktueller Rest r, Darts in der Aufnahme d) für eine feste Trefferverteilung.
    /// Bust: der Dart zählt, die Aufnahme endet, der Stand fällt auf u zurück.
    /// Rechnung von kleinen Resten aufwärts: W(u) hängt nur von kleineren Resten und von sich selbst ab (Bust, drei Misses);
    /// dieser Fixpunkt wird je u mit dem Sekantenverfahren gelöst.
    /// </summary>
    public sealed class PolicyTable
    {
        public const int MaxScore = 501;

        private const int MaxDartScore = 60;

        private readonly ThrowOutcomeModel _model;
        private readonly int _minRemaining;
        private readonly int _maxScore;

        // W[u] = V(u, u, 3); V2[u][k] = V(u, u-k, 2), k ≤ 60; V1[u][k] = V(u, u-k, 1), k ≤ 120.
        private readonly float[] _w;
        private readonly float[][] _v2;
        private readonly float[][] _v1;

        private PolicyTable(ThrowOutcomeModel model, X01OutModeEnum outMode, bool opponentPressure, int maxScore)
        {
            _model = model;
            OutMode = outMode;
            OpponentPressure = opponentPressure;
            _minRemaining = X01Rules.MinRemaining(outMode);
            _maxScore = maxScore;
            _w = new float[maxScore + 1];
            _v2 = new float[maxScore + 1][];
            _v1 = new float[maxScore + 1][];
        }

        public X01OutModeEnum OutMode { get; }

        /// <summary>Gegner auf dem Finish: jeder Dart wird mit Druck geworfen (wie BotVisitPlayer).</summary>
        public bool OpponentPressure { get; }

        public ThrowOutcomeModel Model => _model;

        /// <summary>Rechenzeit der Tabelle (für Auswertungen).</summary>
        public TimeSpan SolveTime { get; private set; }

        /// <param name="maxScore">Höchster Rest (501; in Tests kleiner für kurze Laufzeit).</param>
        public static PolicyTable Solve(ThrowOutcomeModel model, X01OutModeEnum outMode, bool opponentPressure, int maxScore = MaxScore)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var table = new PolicyTable(model, outMode, opponentPressure, maxScore);
            table.SolveAll();
            table.SolveTime = stopwatch.Elapsed;
            return table;
        }

        /// <summary>Erwartete Darts bis zum Checkout ab Beginn einer Aufnahme bei Rest u.</summary>
        public float ExpectedDarts(int remaining) => _w[remaining];

        /// <summary>Erwartete Darts, wenn in diesem Zustand auf das Ziel geworfen wird (inklusive dieses Darts).</summary>
        public float Q(int visitStart, int remaining, int dartsLeft, int targetIndex) =>
            Q(visitStart, remaining, dartsLeft, targetIndex, _w[visitStart]);

        /// <summary>Gültiger Zustand der Tabelle? Sonst (z. B. Rest über maxScore) kann sie nicht entscheiden.</summary>
        public bool Covers(int visitStart, int remaining, int dartsLeft) =>
            dartsLeft is >= 1 and <= 3 &&
            remaining >= _minRemaining && visitStart <= _maxScore && remaining <= visitStart &&
            visitStart - remaining <= (3 - dartsLeft) * MaxDartScore &&
            (dartsLeft != 3 || remaining == visitStart);

        private void SolveAll()
        {
            for (var u = 0; u < _minRemaining && u <= _maxScore; u++)
            {
                _w[u] = 0f;
            }

            for (var u = _minRemaining; u <= _maxScore; u++)
            {
                _v2[u] = new float[MaxDartScore + 1];
                _v1[u] = new float[2 * MaxDartScore + 1];

                // Sekantenverfahren für den Fixpunkt x = g(x), x = W(u). g ist stückweise linear mit Steigung < 1.
                var x0 = u > _minRemaining ? _w[u - 1] : 3f;
                var g0 = Evaluate(u, x0);
                var x1 = g0;

                for (var iteration = 0; iteration < 60; iteration++)
                {
                    var g1 = Evaluate(u, x1);

                    if (MathF.Abs(g1 - x1) < 1e-4f)
                    {
                        x0 = x1;
                        break;
                    }

                    var denominator = (g1 - x1) - (g0 - x0);
                    var next = MathF.Abs(denominator) < 1e-9f ? g1 : x1 - (g1 - x1) * (x1 - x0) / denominator;

                    // Absicherung: der Fixpunkt liegt nie unter 1 Dart.
                    next = MathF.Max(1f, next);

                    x0 = x1;
                    g0 = g1;
                    x1 = next;
                }

                // Tabellen mit dem endgültigen Wert füllen.
                _w[u] = Evaluate(u, x0);
            }
        }

        /// <summary>Füllt V1/V2 für u unter der Annahme W(u) = x und liefert den neuen Wert V(u, u, 3).</summary>
        private float Evaluate(int u, float x)
        {
            var v1 = _v1[u];
            var v2 = _v2[u];

            for (var k = 0; k <= 2 * MaxDartScore; k++)
            {
                var r = u - k;
                v1[k] = r >= _minRemaining ? Best(u, r, 1, x) : x;
            }

            for (var k = 0; k <= MaxDartScore; k++)
            {
                var r = u - k;
                v2[k] = r >= _minRemaining ? Best(u, r, 2, x) : x;
            }

            return Best(u, u, 3, x);
        }

        private float Best(int u, int r, int d, float x)
        {
            var best = float.MaxValue;

            for (var t = 0; t < ThrowOutcomeModel.Targets.Length; t++)
            {
                var q = Q(u, r, d, t, x);

                if (q < best)
                {
                    best = q;
                }
            }

            return best;
        }

        private float Q(int u, int r, int d, int targetIndex, float bustValue)
        {
            var target = ThrowOutcomeModel.Targets[targetIndex];
            var isFinishingAim = target.Score == r && IsFinishing(target.Multiplier == 2);
            var outcomes = _model.Outcomes(targetIndex, OpponentPressure || isFinishingAim);
            var expected = 1f;

            foreach (var outcome in outcomes)
            {
                var newRemaining = r - outcome.Score;
                float next;

                if (newRemaining == 0)
                {
                    next = IsFinishing(outcome.IsDouble) ? 0f : bustValue;
                }
                else if (newRemaining < _minRemaining)
                {
                    next = bustValue;
                }
                else if (d == 1)
                {
                    next = newRemaining == u ? bustValue : _w[newRemaining];
                }
                else if (d == 2)
                {
                    next = _v1[u][u - newRemaining];
                }
                else
                {
                    next = _v2[u][u - newRemaining];
                }

                expected += outcome.Probability * next;
            }

            return expected;
        }

        private bool IsFinishing(bool isDouble) => OutMode == X01OutModeEnum.Straight || isDouble;
    }
}
