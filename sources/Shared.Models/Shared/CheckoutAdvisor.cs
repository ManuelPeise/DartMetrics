using System.Collections.Concurrent;
using Shared.Enums.X01;

namespace Shared.Models.Shared
{
    /// <summary>
    /// Checkout-Empfehlung für menschliche Spieler: die besten Wege für einen Rest und die verbleibenden Darts,
    /// bester zuerst. Läuft auf dem Server (Training, Turnier) und im Browser (Bot-Partie, nach jedem Dart).
    /// Regeln wie im Turnierdarts:
    /// 1. so wenige Darts wie möglich (40 = D20, nicht S20 · D10),
    /// 2. beliebte Finish-Doubles zuerst (D20, D16, D8, …),
    /// 3. einfache Ziele vor Triples und Bull auf dem Weg dorthin, bei Gleichstand höhere Felder zuerst.
    /// Ergebnis wird je (Rest, Darts, Out-Modus) zwischengespeichert.
    /// </summary>
    public static class CheckoutAdvisor
    {
        /// <summary>Wie viele Wege zurückgegeben werden (der erste ist die Empfehlung, die anderen Alternativen).</summary>
        public const int MaxRoutes = 3;

        private static readonly ConcurrentDictionary<(int Remaining, int Darts, X01OutModeEnum OutMode), CheckoutDart[][]> Cache = new();

        // Alle 62 Felder: S/D/T 1–20, Single-Bull (25), Bull (50).
        private static readonly CheckoutDart[] AllDarts = BuildAllDarts();

        // Niedriger = beliebter. Reihenfolge der Profis: D20, D16, D8 (lassen sich bei Fehlwurf weiter halbieren).
        // Bull (25) nur, wenn kein übliches Double passt (98 = T20 · D19, nicht T16 · Bull).
        private static readonly Dictionary<int, int> DoublePreference = new()
        {
            [20] = 0, [16] = 1, [8] = 2, [18] = 3, [12] = 4, [10] = 5, [4] = 6, [2] = 7,
            [14] = 8, [6] = 9, [25] = 12, [1] = 15
        };

        private const int OtherDoublePreference = 11;

        /// <summary>Größter Rest, der mit drei Darts noch gecheckt werden kann.</summary>
        public static int MaxCheckout(X01OutModeEnum outMode) => outMode == X01OutModeEnum.Straight ? 180 : 170;

        /// <summary>Kleinster Rest, der noch gecheckt werden kann: 2 bei Double-Out, 1 bei Straight-Out.</summary>
        public static int MinRemaining(X01OutModeEnum outMode) => outMode == X01OutModeEnum.Straight ? 1 : 2;

        /// <summary>
        /// Beste Checkout-Wege für <paramref name="remaining"/> mit höchstens <paramref name="darts"/> Darts.
        /// Leer, wenn der Rest so nicht checkbar ist (z. B. 169 oder über 170 bei Double-Out).
        /// </summary>
        public static List<CheckoutSuggestion> GetBestRoutes(int remaining, int darts, X01OutModeEnum outMode = X01OutModeEnum.Double)
        {
            darts = Math.Min(darts, 3);

            if (darts < 1 || remaining < MinRemaining(outMode) || remaining > MaxCheckout(outMode))
            {
                return [];
            }

            var routes = Cache.GetOrAdd((remaining, darts, outMode), key => FindBestRoutes(key.Remaining, key.Darts, key.OutMode));

            // Kopie: Aufrufer dürfen die Wege im Cache nicht verändern.
            return routes
                .Select(route => new CheckoutSuggestion { Darts = route.Select(Copy).ToList() })
                .ToList();
        }

        private static CheckoutDart[][] FindBestRoutes(int remaining, int maxDarts, X01OutModeEnum outMode)
        {
            // Mit einem Dart beginnen und nur bei Bedarf tiefer suchen -> automatisch die kleinste Dartanzahl.
            for (var darts = 1; darts <= maxDarts; darts++)
            {
                var found = new List<CheckoutDart[]>();
                Search(remaining, darts, new List<CheckoutDart>(), found, outMode);

                if (found.Count > 0)
                {
                    return found
                        .OrderBy(RouteCost)
                        .Take(MaxRoutes)
                        .ToArray();
                }
            }

            return [];
        }

        private static void Search(int remaining, int dartsLeft, List<CheckoutDart> way, List<CheckoutDart[]> found, X01OutModeEnum outMode)
        {
            if (dartsLeft == 1)
            {
                // Letzter Dart: muss genau auf 0 gehen und ein gültiges Finish sein.
                foreach (var dart in AllDarts)
                {
                    if (dart.Score == remaining && IsFinishingDart(dart, outMode))
                    {
                        found.Add([.. way, dart]);
                    }
                }

                return;
            }

            foreach (var dart in AllDarts)
            {
                var rest = remaining - dart.Score;

                // Vor dem letzten Dart muss ein checkbarer Rest bleiben (bei Double-Out mindestens 2).
                if (rest < MinRemaining(outMode))
                {
                    continue;
                }

                way.Add(dart);
                Search(rest, dartsLeft - 1, way, found, outMode);
                way.RemoveAt(way.Count - 1);
            }
        }

        private static bool IsFinishingDart(CheckoutDart dart, X01OutModeEnum outMode) =>
            outMode == X01OutModeEnum.Straight || dart.Multiplier == 2;

        /// <summary>Kosten eines Weges: Schwierigkeit der Darts davor + Beliebtheit des Finish-Feldes.</summary>
        private static double RouteCost(CheckoutDart[] route)
        {
            double cost = 0;

            for (var i = 0; i < route.Length - 1; i++)
            {
                // Schwierigkeit, abzüglich eines kleinen Bonus für hohe Felder (T20 · T20 · Bull vor T20 · Bull · T20).
                cost += SetupDifficulty(route[i]) - route[i].Score / 1000.0 / (i + 1);
            }

            var finish = route[^1];
            cost += finish.Multiplier == 2
                ? DoublePreference.GetValueOrDefault(finish.Segment, OtherDoublePreference)
                : SetupDifficulty(finish);

            return cost;
        }

        private static double SetupDifficulty(CheckoutDart dart) => (dart.Segment, dart.Multiplier) switch
        {
            (25, 2) => 5,
            (25, 1) => 4,
            // Doubles als Vorbereitung sind unüblich (schmales Feld, kein Vorteil) -> teurer als Triples.
            (_, 3) => 2,
            (_, 2) => 4,
            _ => 0
        };

        private static CheckoutDart[] BuildAllDarts()
        {
            var darts = new List<CheckoutDart>();

            for (var segment = 1; segment <= 20; segment++)
            {
                for (var multiplier = 1; multiplier <= 3; multiplier++)
                {
                    darts.Add(new CheckoutDart { Segment = segment, Multiplier = multiplier, Score = segment * multiplier });
                }
            }

            darts.Add(new CheckoutDart { Segment = 25, Multiplier = 1, Score = 25 });
            darts.Add(new CheckoutDart { Segment = 25, Multiplier = 2, Score = 50 });

            return darts.ToArray();
        }

        private static CheckoutDart Copy(CheckoutDart dart) =>
            new() { Segment = dart.Segment, Multiplier = dart.Multiplier, Score = dart.Score };
    }
}
