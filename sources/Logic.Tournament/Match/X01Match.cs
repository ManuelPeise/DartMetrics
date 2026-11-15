using Logic.Shared.Rules;
using Shared.Enums.X01;
using Shared.Models.Shared;

namespace Logic.Tournament.Match
{
    /// <summary>
    /// Ablauf eines X01-Matches (ein Satz, best of N Legs) Dart für Dart, ohne Datenbank.
    /// Spieler 1 = Heim, 2 = Gast; der Anwurf wechselt von Leg zu Leg.
    /// Der Stand wird aus den gespeicherten Darts per <see cref="Replay"/> wiederhergestellt,
    /// so bleiben Korrekturen (letzten Dart entfernen) einfach.
    /// </summary>
    public sealed class X01Match
    {
        public const int DartsPerVisit = 3;

        private readonly X01Rules _rules = new();
        private readonly int[] _remaining = new int[2];
        private readonly int[] _legsWon = new int[2];
        private readonly bool[] _opened = new bool[2];
        private readonly List<(int Segment, int Multiplier)> _visit = [];

        private int _visitStartScore;
        private bool _visitStartOpened;

        public X01Match(int startingScore, X01InModeEnum inMode, X01OutModeEnum outMode, int bestOfLegs, int firstLegStarter = 1)
        {
            if (startingScore is not (301 or 501))
            {
                throw new ArgumentOutOfRangeException(nameof(startingScore), "Only 301 and 501 are supported.");
            }

            if (!X01SetRules.AllowedBestOfLegs.Contains(bestOfLegs))
            {
                throw new ArgumentOutOfRangeException(nameof(bestOfLegs), $"Best of {bestOfLegs} legs is not allowed.");
            }

            if (firstLegStarter is not (1 or 2))
            {
                throw new ArgumentOutOfRangeException(nameof(firstLegStarter));
            }

            StartingScore = startingScore;
            InMode = inMode;
            OutMode = outMode;
            BestOfLegs = bestOfLegs;
            FirstLegStarter = firstLegStarter;

            StartLeg(1, firstLegStarter);
        }

        public int StartingScore { get; }
        public X01InModeEnum InMode { get; }
        public X01OutModeEnum OutMode { get; }
        public int BestOfLegs { get; }
        public int FirstLegStarter { get; }

        public int LegNumber { get; private set; }
        public int LegStarter { get; private set; }

        /// <summary>Wer gerade wirft (1 oder 2); nach Matchende der Sieger.</summary>
        public int CurrentPlayer { get; private set; }

        /// <summary>Darts der laufenden Aufnahme (0–2; eine volle oder beendete Aufnahme ist abgeschlossen).</summary>
        public IReadOnlyList<(int Segment, int Multiplier)> CurrentVisit => _visit;

        /// <summary>Rest zu Beginn der laufenden Aufnahme (Stand nach einem Bust).</summary>
        public int VisitStartScore => _visitStartScore;

        public bool IsFinished => Winner is not null;

        public int? Winner { get; private set; }

        public int LegsToWin => X01SetRules.LegsToWin(BestOfLegs);

        public int Remaining(int player) => _remaining[Index(player)];

        public int LegsWon(int player) => _legsWon[Index(player)];

        public bool IsOpened(int player) => _opened[Index(player)];

        /// <summary>Stand aus gespeicherten Darts in Wurfreihenfolge wiederherstellen.</summary>
        public static X01Match Replay(int startingScore, X01InModeEnum inMode, X01OutModeEnum outMode, int bestOfLegs,
            IEnumerable<(int Segment, int Multiplier)> darts, int firstLegStarter = 1)
        {
            var match = new X01Match(startingScore, inMode, outMode, bestOfLegs, firstLegStarter);

            foreach (var (segment, multiplier) in darts)
            {
                match.Throw(segment, multiplier);
            }

            return match;
        }

        public X01DartOutcome Throw(int segment, int multiplier)
        {
            if (IsFinished)
            {
                throw new InvalidOperationException("The match is already finished.");
            }

            if (!_rules.IsValidSegment(segment) || !_rules.IsValidMultiplier(segment, multiplier))
            {
                throw new ArgumentException($"Invalid dart {segment}x{multiplier}.");
            }

            var player = CurrentPlayer;
            var index = Index(player);
            var legNumber = LegNumber;
            var throwNumber = _visit.Count + 1;
            var scoreBeforeThrow = _remaining[index];
            var score = _rules.CalculateScore(segment, multiplier);

            _visit.Add((segment, multiplier));

            if (!_opened[index] && _rules.IsOpeningDart(segment, multiplier, InMode))
            {
                _opened[index] = true;
            }

            var counted = _rules.CountedScore(_visit, _visitStartOpened, InMode);
            var isBust = _rules.IsBust(_visitStartScore, counted, segment, multiplier, OutMode);
            var isCheckout = !isBust && _rules.IsCheckout(_visitStartScore, counted, segment, multiplier, OutMode);

            if (isBust)
            {
                _remaining[index] = _visitStartScore;
                _opened[index] = _visitStartOpened;
            }
            else
            {
                _remaining[index] = _visitStartScore - counted;
            }

            var outcome = new X01DartOutcome
            {
                Player = player,
                LegNumber = legNumber,
                ThrowNumber = throwNumber,
                Score = score,
                ScoreBeforeThrow = scoreBeforeThrow,
                VisitScoreBefore = _visitStartScore,
                RemainingAfter = _remaining[index],
                IsBust = isBust,
                IsCheckout = isCheckout,
                VisitEnded = isBust || isCheckout || throwNumber == DartsPerVisit
            };

            if (isCheckout)
            {
                _legsWon[index]++;
                outcome.LegWinner = player;

                if (_legsWon[index] >= LegsToWin)
                {
                    Winner = player;
                    outcome.MatchWinner = player;
                    _visit.Clear();
                    return outcome;
                }

                StartLeg(LegNumber + 1, Other(LegStarter));
                return outcome;
            }

            if (outcome.VisitEnded)
            {
                StartVisit(Other(player));
            }

            return outcome;
        }

        private void StartLeg(int legNumber, int starter)
        {
            LegNumber = legNumber;
            LegStarter = starter;
            _remaining[0] = _remaining[1] = StartingScore;
            _opened[0] = _opened[1] = InMode == X01InModeEnum.Straight;
            StartVisit(starter);
        }

        private void StartVisit(int player)
        {
            CurrentPlayer = player;
            _visit.Clear();
            _visitStartScore = _remaining[Index(player)];
            _visitStartOpened = _opened[Index(player)];
        }

        private static int Other(int player) => player == 1 ? 2 : 1;

        private static int Index(int player) => player switch
        {
            1 => 0,
            2 => 1,
            _ => throw new ArgumentOutOfRangeException(nameof(player), "Player must be 1 or 2.")
        };
    }

    public sealed class X01DartOutcome
    {
        public int Player { get; init; }
        public int LegNumber { get; init; }
        public int ThrowNumber { get; init; }
        public int Score { get; init; }
        public int ScoreBeforeThrow { get; init; }
        public int VisitScoreBefore { get; init; }
        public int RemainingAfter { get; init; }
        public bool IsBust { get; init; }
        public bool IsCheckout { get; init; }
        public bool VisitEnded { get; init; }
        public int? LegWinner { get; set; }
        public int? MatchWinner { get; set; }
    }
}
