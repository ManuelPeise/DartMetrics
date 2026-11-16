using Logic.Statistics.Performance;

namespace Logic.Statistics.Progression
{
    /// <summary>Ergebnis der Prüfung eines Erfolgs: Fortschritt und (falls erreicht) wann.</summary>
    public readonly record struct AchievementState(long Progress, DateTime? AchievedAt);

    public sealed record AchievementDefinition(
        string Key,
        string Name,
        string Description,
        string Category,
        int Tier,
        long Target,
        Func<ProgressionFacts, AchievementState> Evaluate);

    /// <summary>
    /// Alle Erfolge. Geprüft wird über den ganzen Verlauf; das Datum ist der Zeitpunkt des Spiels bzw. der Session,
    /// in der das Ziel erreicht wurde. Neue Erfolge einfach ergänzen (Schlüssel nie ändern, sie stehen in der Datenbank).
    /// </summary>
    public static class AchievementCatalog
    {
        public const string Scoring = "Scoring";
        public const string Checkout = "Checkout";
        public const string Legs = "Legs";
        public const string Bots = "Gegen Bots";
        public const string Training = "Training";
        public const string Tournaments = "Turniere";
        public const string Loyalty = "Treue";

        public static readonly IReadOnlyList<AchievementDefinition> All =
        [
            // Scoring
            Count("first-ton", "Ton!", "Eine Aufnahme mit 100 oder mehr.", Scoring, 0, 1, f => VisitEvents(f, v => v.Points >= 100)),
            Count("first-140", "Ton-Forty", "Eine Aufnahme mit 140 oder mehr.", Scoring, 0, 1, f => VisitEvents(f, v => v.Points >= 140)),
            Count("first-180", "One Hundred and Eighty!", "Die erste 180.", Scoring, 0, 1, f => VisitEvents(f, v => v.Points == 180)),
            Count("oneeighty-10", "180er-Sammler", "10 Mal 180.", Scoring, 1, 10, f => VisitEvents(f, v => v.Points == 180)),
            Count("oneeighty-50", "180er-Maschine", "50 Mal 180.", Scoring, 2, 50, f => VisitEvents(f, v => v.Points == 180)),
            Count("oneeighty-100", "180er-Legende", "100 Mal 180.", Scoring, 3, 100, f => VisitEvents(f, v => v.Points == 180)),

            // Checkout
            Count("first-checkout", "Erster Checkout", "Das erste Leg ausgecheckt.", Checkout, 0, 1, f => VisitEvents(f, v => v.IsCheckout)),
            Count("ton-finish", "Ton-Plus-Finish", "Ein Checkout von 100 oder mehr.", Checkout, 0, 1, f => VisitEvents(f, v => v.IsCheckout && v.ScoreBefore >= 100)),
            Count("big-fish", "Big Fish", "170 ausgecheckt (T20 T20 Bull).", Checkout, 0, 1, f => VisitEvents(f, v => v.IsCheckout && v.ScoreBefore == 170)),
            Count("bull-finish", "Bullseye-Finish", "Ein Leg auf dem Bull beendet.", Checkout, 0, 1,
                f => VisitEvents(f, v => v.IsCheckout && v.Darts.Count > 0 && v.Darts[^1].Segment == 25 && v.Darts[^1].Multiplier == 2)),
            Count("checkouts-50", "Finisher", "50 Legs ausgecheckt.", Checkout, 1, 50, f => VisitEvents(f, v => v.IsCheckout)),
            Count("checkouts-250", "Checkout-König", "250 Legs ausgecheckt.", Checkout, 2, 250, f => VisitEvents(f, v => v.IsCheckout)),

            // Legs (501)
            Count("leg-15", "15-Darter", "Ein Leg 501 mit höchstens 15 Darts.", Legs, 1, 1, f => ShortLegs(f, 15)),
            Count("leg-12", "12-Darter", "Ein Leg 501 mit höchstens 12 Darts.", Legs, 2, 1, f => ShortLegs(f, 12)),
            Count("leg-9", "Neun-Darter", "Das perfekte Leg: 501 mit 9 Darts.", Legs, 3, 1, f => ShortLegs(f, 9)),

            // Gegen Bots
            Count("first-bot-win", "Maschine besiegt", "Das erste Spiel gegen einen Bot gewonnen.", Bots, 0, 1,
                f => f.Matches.Where(m => !m.IsTournament && m.IsWon).Select(m => m.Date)),
            Count("beat-pro", "Profi-Killer", "Einen Bot ab Stufe Profi (Ø 95) besiegt.", Bots, 2, 1,
                f => f.Matches.Where(m => m.IsWon && m.BotOpponentAverage >= 92.5f).Select(m => m.Date)),
            Count("beat-champion", "Weltmeister-Bezwinger", "Einen Weltmeister-Bot besiegt.", Bots, 3, 1,
                f => f.Matches.Where(m => m.IsWon && m.BotOpponentAverage >= 102.5f).Select(m => m.Date)),
            new("win-streak-5", "Lauf", "5 Spiele in Folge gewonnen.", Bots, 1, 5, f => WinStreak(f, 5)),

            // Training
            Count("training-10", "Fleißig", "10 Trainings beendet.", Training, 1, 10, f => f.Trainings.Select(t => t.Date)),
            Count("training-50", "Trainingsweltmeister", "50 Trainings beendet.", Training, 2, 50, f => f.Trainings.Select(t => t.Date)),
            Count("training-100", "Trainingsmonster", "100 Trainings beendet.", Training, 3, 100, f => f.Trainings.Select(t => t.Date)),
            Count("atc-60", "Rund um die Uhr", "Around the Clock mit höchstens 60 Darts.", Training, 0, 1,
                f => f.Trainings.Where(t => t.AroundTheClockDarts is <= 60).Select(t => t.Date)),
            new("training-days-7", "Eine Woche dran", "An 7 Tagen in Folge trainiert.", Training, 1, 7, f => DayStreak(f, 7)),

            // Turniere
            Count("first-tournament-match", "Turnierluft", "Das erste Turnier-Match gespielt.", Tournaments, 0, 1,
                f => f.Matches.Where(m => m.IsTournament).Select(m => m.Date)),
            Count("tournament-wins-10", "Turnierspieler", "10 Turnier-Matches gewonnen.", Tournaments, 1, 10,
                f => f.Matches.Where(m => m.IsTournament && m.IsWon).Select(m => m.Date)),
            Count("tournament-win", "Champion", "Ein Turnier gewonnen.", Tournaments, 3, 1,
                f => f.Matches.Where(m => m.WonTournament).Select(m => m.Date)),

            // Treue
            new("member-month", "Dabei", "Seit einem Monat dabei.", Loyalty, 0, 30, f => Membership(f, 30)),
            new("member-year", "Stammspieler", "Seit einem Jahr dabei.", Loyalty, 2, 365, f => Membership(f, 365))
        ];

        public static AchievementDefinition? Find(string key) => All.FirstOrDefault(a => a.Key == key);

        /// <summary>Zähl-Erfolg: Ereignisse (Datum je Ereignis) bis zum Ziel.</summary>
        private static AchievementDefinition Count(string key, string name, string description, string category, int tier, long target,
            Func<ProgressionFacts, IEnumerable<DateTime>> events) =>
            new(key, name, description, category, tier, target, f =>
            {
                var ordered = events(f).OrderBy(d => d).ToList();
                return new AchievementState(Math.Min(ordered.Count, target), ordered.Count >= target ? ordered[(int)target - 1] : null);
            });

        private static IEnumerable<DateTime> VisitEvents(ProgressionFacts facts, Func<StatVisit, bool> predicate) =>
            facts.Legs.SelectMany(l => l.Visits.Where(predicate).Select(_ => l.Date));

        /// <summary>Ausgecheckte Legs ab 501 mit höchstens so vielen Darts.</summary>
        private static IEnumerable<DateTime> ShortLegs(ProgressionFacts facts, int maxDarts) =>
            facts.Legs
                .Where(l => l.Visits.Count > 0 && l.Visits[0].ScoreBefore == 501 && l.Visits[^1].IsCheckout)
                .Where(l => l.Visits.Sum(v => v.Darts.Count) <= maxDarts)
                .Select(l => l.Date);

        private static AchievementState WinStreak(ProgressionFacts facts, int target)
        {
            var current = 0;
            var best = 0;

            foreach (var match in facts.Matches.OrderBy(m => m.Date))
            {
                current = match.IsWon ? current + 1 : 0;
                best = Math.Max(best, current);

                if (current == target)
                {
                    return new AchievementState(target, match.Date);
                }
            }

            return new AchievementState(Math.Min(best, target), null);
        }

        private static AchievementState DayStreak(ProgressionFacts facts, int target)
        {
            var days = facts.Trainings.Select(t => t.Date.Date).Distinct().OrderBy(d => d).ToList();
            var streak = 0;
            var best = 0;
            DateTime? previous = null;

            foreach (var day in days)
            {
                streak = previous is { } p && (day - p).TotalDays == 1 ? streak + 1 : 1;
                best = Math.Max(best, streak);
                previous = day;

                if (streak == target)
                {
                    return new AchievementState(target, day);
                }
            }

            return new AchievementState(Math.Min(best, target), null);
        }

        private static AchievementState Membership(ProgressionFacts facts, int days)
        {
            var since = (long)Math.Floor((facts.Now - facts.RegisteredAt).TotalDays);
            return new AchievementState(Math.Clamp(since, 0, days), since >= days ? facts.RegisteredAt.AddDays(days) : null);
        }
    }
}
