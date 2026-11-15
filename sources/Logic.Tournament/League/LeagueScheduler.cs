namespace Logic.Tournament.League
{
    /// <summary>Paarung eines Spieltags; <c>Away</c> = null bedeutet spielfrei für <c>Home</c>.</summary>
    public readonly record struct LeaguePairing(int Home, int? Away);

    /// <summary>
    /// Spielplan einer Liga nach der Kreismethode (Berger-Tabelle): Hinrunde mit n−1 Spieltagen,
    /// Rückrunde mit denselben Paarungen und getauschtem Heimrecht. Bei ungerader Zahl ist pro Spieltag einer spielfrei.
    /// Rein, ohne Datenbank; Teilnehmer per Id.
    /// </summary>
    public static class LeagueScheduler
    {
        public const int MinParticipants = 8;
        public const int MaxParticipants = 16;

        public static IReadOnlyList<IReadOnlyList<LeaguePairing>> CreateDoubleRoundRobin(IReadOnlyList<int> participantIds)
        {
            ArgumentNullException.ThrowIfNull(participantIds);

            if (participantIds.Count is < MinParticipants or > MaxParticipants)
            {
                throw new ArgumentException($"A league needs {MinParticipants} to {MaxParticipants} participants.");
            }

            if (participantIds.Distinct().Count() != participantIds.Count)
            {
                throw new ArgumentException("Participants must be distinct.");
            }

            // Platzhalter null = spielfrei.
            var slots = participantIds.Select(id => (int?)id).ToList();
            if (slots.Count % 2 == 1)
            {
                slots.Add(null);
            }

            var n = slots.Count;
            var firstHalf = new List<IReadOnlyList<LeaguePairing>>();

            for (var round = 0; round < n - 1; round++)
            {
                var day = new List<LeaguePairing>();

                for (var i = 0; i < n / 2; i++)
                {
                    var a = slots.ElementAt(i);
                    var b = slots.ElementAt(n - 1 - i);

                    // Heimrecht abwechseln: der feste Platz wechselt je Spieltag, die übrigen Paare nach Position.
                    var swap = i == 0 ? round % 2 == 1 : i % 2 == 1;
                    var (home, away) = swap ? (b, a) : (a, b);

                    if (home is null && away is null)
                    {
                        continue;
                    }

                    day.Add(home is null ? new LeaguePairing(away!.Value, null)
                        : away is null ? new LeaguePairing(home.Value, null)
                        : new LeaguePairing(home.Value, away.Value));
                }

                firstHalf.Add(day);

                // Kreis drehen: Platz 0 bleibt, der letzte rückt auf Platz 1.
                var last = slots.ElementAt(n - 1);
                slots.RemoveAt(n - 1);
                slots.Insert(1, last);
            }

            var secondHalf = firstHalf
                .Select(day => (IReadOnlyList<LeaguePairing>)day
                    .Select(p => p.Away is null ? p : new LeaguePairing(p.Away.Value, p.Home))
                    .ToList())
                .ToList();

            return [.. firstHalf, .. secondHalf];
        }
    }
}
