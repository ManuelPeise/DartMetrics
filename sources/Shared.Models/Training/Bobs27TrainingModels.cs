namespace Shared.Models.Training
{
    /// <summary>Neue Bob's-27-Partie: Spieler 1 ist der angemeldete Benutzer, weitere sind Gäste am selben Gerät.</summary>
    public class StartBobs27TrainingModel
    {
        /// <summary>1–8 Namen in Wurfreihenfolge, je höchstens 40 Zeichen.</summary>
        public List<string> PlayerNames { get; set; } = [];

        /// <summary>Kein Ausscheiden bei 0 oder weniger Punkten, Weiterspielen im Minus.</summary>
        public bool IsBeginnerMode { get; set; }
    }

    /// <summary>Treffer (0–3) des Spielers am Wurf auf das aktuelle Ziel.</summary>
    public class Bobs27TrainingTurnModel
    {
        public int TrainingId { get; set; }

        /// <summary>Erwartete Nummer dieses Zugs (Bobs27TrainingState.NextTurnNumber), schützt vor doppeltem Senden.</summary>
        public int TurnNumber { get; set; }

        public int Hits { get; set; }
    }

    /// <summary>Letzten Zug zurücknehmen.</summary>
    public class Bobs27TrainingUndoModel
    {
        public int TrainingId { get; set; }

        /// <summary>Nummer des zurückzunehmenden (letzten) Zugs, schützt vor doppeltem Zurücknehmen.</summary>
        public int TurnNumber { get; set; }
    }

    /// <summary>Vollständiger Stand einer Partie, nach jedem Zug neu berechnet.</summary>
    public class Bobs27TrainingState
    {
        public int TrainingId { get; set; }

        public DateTime StartedAt { get; set; }

        public bool IsBeginnerMode { get; set; }

        public bool IsFinished { get; set; }

        /// <summary>Laufende Runde 1–21 (nach dem Ende die letzte gespielte).</summary>
        public int Round { get; set; }

        /// <summary>D1 … D20, Bull.</summary>
        public string TargetLabel { get; set; } = "";

        /// <summary>Punktwert des Ziels (2 … 40, Bull 50); so viel gibt es je Treffer, so viel kostet ein Fehlversuch.</summary>
        public int TargetValue { get; set; }

        /// <summary>Spieler am Wurf (1-basiert), null nach dem Ende.</summary>
        public int? CurrentPlayerNumber { get; set; }

        /// <summary>Nummer des nächsten Zugs (für Bobs27TrainingTurnModel.TurnNumber).</summary>
        public int NextTurnNumber { get; set; }

        /// <summary>Letzter Zug kann zurückgenommen werden (auch direkt nach dem Spielende, nicht nach „Beenden“).</summary>
        public bool CanUndo { get; set; }

        public int MaxScore { get; set; }

        public List<Bobs27PlayerResult> Players { get; set; } = [];

        /// <summary>Sieger (Spielernummern) nach dem Ende; bei Gleichstand mehrere.</summary>
        public List<int> Winners { get; set; } = [];

        /// <summary>Letzte Züge, neueste zuerst (für die Anzeige).</summary>
        public List<Bobs27TurnItem> RecentTurns { get; set; } = [];
    }

    public class Bobs27PlayerResult
    {
        public int Number { get; set; }

        public string Name { get; set; } = "";

        public int Score { get; set; }

        public bool IsEliminated { get; set; }

        public int? EliminatedInRound { get; set; }

        /// <summary>Getroffene Doubles insgesamt.</summary>
        public int Hits { get; set; }

        public int RoundsPlayed { get; set; }

        /// <summary>Runden mit mindestens einem Treffer.</summary>
        public int RoundsWithHit { get; set; }

        /// <summary>Trefferquote je Dart in Prozent.</summary>
        public decimal HitRate { get; set; }
    }

    public class Bobs27TurnItem
    {
        public int TurnNumber { get; set; }

        public int PlayerNumber { get; set; }

        public int Round { get; set; }

        public string TargetLabel { get; set; } = "";

        public int Hits { get; set; }

        public int ScoreBefore { get; set; }

        public int ScoreAfter { get; set; }

        public bool IsBust { get; set; }
    }
}
