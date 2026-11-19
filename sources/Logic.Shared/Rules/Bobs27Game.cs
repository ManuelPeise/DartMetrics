namespace Logic.Shared.Rules
{
    /// <summary>
    /// Spielablauf Bob's 27 für 1–N Spieler, berechnet allein aus der Reihenfolge der Runden-Ergebnisse
    /// (Treffer 0–3 je Zug). Runde für Runde wirft jeder noch nicht ausgeschiedene Spieler einmal.
    /// Ende: alle ausgeschieden oder Runde 21 von allen verbliebenen gespielt (allein: Bust oder Runde 21).
    /// Sieger: wer nicht ausgeschieden ist, mit den meisten Punkten; sind alle ausgeschieden, wer am weitesten kam
    /// (spätere Runde), bei Gleichstand mehr Punkte. Gleichstand ergibt mehrere Sieger.
    /// </summary>
    public sealed class Bobs27Game
    {
        private readonly List<Bobs27PlayerState> _players;
        private readonly List<Bobs27Turn> _turns = [];

        public Bobs27Game(int playerCount, bool beginnerMode)
        {
            if (playerCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(playerCount), playerCount, "At least one player is required.");
            }

            BeginnerMode = beginnerMode;
            _players = Enumerable.Range(1, playerCount).Select(n => new Bobs27PlayerState(n)).ToList();
            Round = 1;
            CurrentPlayer = 1;
        }

        /// <summary>Spielt die gespeicherten Züge der Reihe nach nach.</summary>
        public static Bobs27Game Replay(int playerCount, bool beginnerMode, IEnumerable<int> hitsInOrder)
        {
            var game = new Bobs27Game(playerCount, beginnerMode);
            foreach (var hits in hitsInOrder)
            {
                game.Play(hits);
            }

            return game;
        }

        public bool BeginnerMode { get; }

        /// <summary>Laufende Runde 1–21 (nach dem Ende: die letzte gespielte).</summary>
        public int Round { get; private set; }

        /// <summary>Spieler am Wurf (1-basiert), null nach dem Ende.</summary>
        public int? CurrentPlayer { get; private set; }

        public bool IsFinished => CurrentPlayer is null;

        public IReadOnlyList<Bobs27PlayerState> Players => _players;

        public IReadOnlyList<Bobs27Turn> Turns => _turns;

        public int TargetValue => Bobs27Rules.TargetValue(Round);

        public string TargetLabel => Bobs27Rules.TargetLabel(Round);

        /// <summary>Wertet einen Zug des Spielers am Wurf und rückt zum nächsten weiter.</summary>
        public Bobs27Turn Play(int hits)
        {
            if (CurrentPlayer is not { } number)
            {
                throw new InvalidOperationException("The game is already finished.");
            }

            var player = _players[number - 1];
            var before = player.Score;
            var after = Bobs27Rules.ApplyRound(before, Round, hits);

            player.Score = after;
            player.Hits += hits;
            player.RoundsPlayed++;
            if (hits > 0)
            {
                player.RoundsWithHit++;
            }

            var isBust = Bobs27Rules.IsBust(after, BeginnerMode);
            if (isBust)
            {
                player.EliminatedInRound = Round;
            }

            var turn = new Bobs27Turn(_turns.Count + 1, number, Round, hits, before, after, isBust);
            _turns.Add(turn);
            Advance();
            return turn;
        }

        /// <summary>Sieger (1-basierte Nummern) nach dem Ende, sonst leer.</summary>
        public IReadOnlyList<int> Winners
        {
            get
            {
                if (!IsFinished)
                {
                    return [];
                }

                var best = _players.OrderByDescending(RankKey).First();
                return _players.Where(p => RankKey(p) == RankKey(best)).Select(p => p.Number).ToList();
            }
        }

        /// <summary>Wer nicht ausgeschieden ist, steht vor allen Ausgeschiedenen; dann spätere Runde, dann Punkte.</summary>
        private static (int Survived, int Round, int Score) RankKey(Bobs27PlayerState player) =>
            (player.IsEliminated ? 0 : 1, player.EliminatedInRound ?? Bobs27Rules.RoundCount + 1, player.Score);

        private void Advance()
        {
            var number = CurrentPlayer!.Value;

            // Weiter in dieser Runde mit dem nächsten Spieler, der noch dabei ist …
            var next = _players.FirstOrDefault(p => p.Number > number && !p.IsEliminated);
            if (next is not null)
            {
                CurrentPlayer = next.Number;
                return;
            }

            // … sonst nächste Runde mit dem ersten, der noch dabei ist.
            var first = _players.FirstOrDefault(p => !p.IsEliminated);
            if (first is null || Round == Bobs27Rules.RoundCount)
            {
                CurrentPlayer = null;
                return;
            }

            Round++;
            CurrentPlayer = first.Number;
        }
    }

    public sealed class Bobs27PlayerState(int number)
    {
        public int Number { get; } = number;

        public int Score { get; internal set; } = Bobs27Rules.StartScore;

        /// <summary>Runde, in der der Spieler ausgeschieden ist; null = noch dabei.</summary>
        public int? EliminatedInRound { get; internal set; }

        public bool IsEliminated => EliminatedInRound is not null;

        public int Hits { get; internal set; }

        public int RoundsPlayed { get; internal set; }

        public int RoundsWithHit { get; internal set; }
    }

    /// <param name="TurnNumber">Laufende Nummer des Zugs im Spiel (1-basiert).</param>
    public sealed record Bobs27Turn(int TurnNumber, int PlayerNumber, int Round, int Hits, int ScoreBefore, int ScoreAfter, bool IsBust);
}
