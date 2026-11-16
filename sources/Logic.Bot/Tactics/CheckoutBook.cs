using Shared.Models.Bot;

namespace Logic.Bot.Tactics
{
    /// <summary>
    /// Wegebuch für Double-Out: Standard-Checkout-Wege, wie Profis sie spielen.
    /// - Rest 170–41 (3 Darts): Hauptweg und Alternativen aus den üblichen Checkout-Tabellen
    ///   (Uni Münster / „How the Pros Do It“, ergänzt um Dartcoach). Bogey-Zahlen (169, 168, 166, 165, 163, 162, 159) haben keinen Weg.
    /// - Mit 2 Darts: der Hauptweg, wenn er in 2 Darts passt, sonst ein Weg „Ziel + Doppel“ nach Profi-Vorliebe.
    /// - Mit 1 Dart: das Doppel bzw. Bull, wenn der Rest damit checkbar ist.
    /// - Rest 40–2: gerade → direkt das Doppel; ungerade → Single, die das größte passende Zweierpotenz-Doppel stehen lässt
    ///   (32 → D16, 16 → D8, 8 → D4, …), z. B. 33 → S1 D16, 31 → S15 D8, 7 → S3 D2.
    /// Nach jedem Dart wird neu nachgeschlagen: trifft der Bot daneben, gilt der Weg für den neuen Rest.
    /// </summary>
    public static class CheckoutBook
    {
        /// <summary>Bevorzugte Doppel für Wege mit 2 Darts (kleiner = lieber).</summary>
        private static readonly int[] DoublePreference = [20, 16, 18, 12, 10, 8, 14, 6, 4, 2, 19, 17, 15, 13, 11, 9, 7, 5, 3, 1, 25];

        /// <summary>Bevorzugte erste Darts für Wege mit 2 Darts: große Triples, dann Bull, dann Singles.</summary>
        private static readonly DartTarget[] FirstDartPreference = BuildFirstDartPreference();

        // Rest: Hauptweg | Alternativen. Notation: T20, D16, 20 (Single), SB (25), DB (Bull), a/b = zwei Varianten an dieser Stelle.
        private const string Table = """
            170: T20 T20 DB
            167: T20 T19 DB
            164: T20 T18 DB | T19 T19 DB
            161: T20 T17 DB
            160: T20 T20 D20
            158: T20 T20 D19
            157: T20 T19 D20
            156: T20 T20 D18
            155: T20 T19 D19
            154: T20 T18 D20
            153: T20 T19 D18
            152: T20 T20 D16
            151: T20 T17 D20 | T19 T18 D20
            150: T20 T18 D18 | T19 T19 D18
            149: T20 T19 D16
            148: T20 T20 D14 | T19 T17 D20 | T20 T16 D20
            147: T20 T17 D18 | T19 T18 D18
            146: T20 T18 D16 | T19 T19 D16
            145: T20 T19 D14
            144: T20 T20 D12
            143: T20 T17 D16 | T19 T18 D16
            142: T20 T14 D20 | T19 T19 D14 | T18 T20 D14
            141: T20 T19 D12
            140: T20 T20 D10
            139: T20 T13 D20 | T20 T19 D11 | T19 T14 D20
            138: T20 T18 D12 | T19 T19 D12
            137: T20 T19 D10 | T20 T15 D16
            136: T20 T20 D8
            135: T20 T17 D12 | SB T20 DB
            134: T20 T16 D13 | T20 T14 D16
            133: T20 T19 D8 | T20 T11 D20
            132: T20 T16 D12 | SB T19 DB | DB T14 D20
            131: T19 T14 D16 | T20 T13 D16
            130: T20 T20 D5
            129: T19 T16 D12 | T20 T19 D6
            128: T18 T14 D16 | T20 T18 D7
            127: T20 T17 D8
            126: T19 T19 D6
            125: T18 T19 D7 | T20 T15 D10 | DB T17 D12
            124: T20 T14 D11 | T20 T16 D8
            123: T19 T16 D9
            122: T18 T18 D7
            121: T20 T11 D14 | T17 T20 D5
            120: T20 20 D20
            119: T19 T12 D13
            118: T20 18 D20
            117: T19 20 D20 | T20 17 D20
            116: T19 19 D20 | T20 16 D20
            115: T20 15 D20 | T19 18 D20
            114: T19 17 D20 | T20 14 D20
            113: T19 16 D20
            112: T20 T12 D8 | T20 12 D20
            111: T19 14 D20 | T20 11 D20
            110: T20 T10 D10 | T20 10 D20
            109: T20 9 D20
            108: T20 16/8 D16/D20
            107: T19 T10 D10 | T19 18 D16
            106: T20 T10 D8 | T20 14 D16
            105: T20 13 D16
            104: T19 15 D16 | T18 18 D16
            103: T19 6/10 D20/D18 | T19 14 D16
            102: T20 10/6 D16/D18
            101: T20 9 D16
            100: T20 D20
            99: T19 10/6 D16/D18
            98: T20 D19
            97: T19 D20
            96: T20 D18
            95: T19 D19 | SB T20 D5
            94: T18 D20 | SB T19 D6
            93: T19 D18 | SB T18 D7
            92: T20 D16 | SB T17 D8
            91: T17 D20 | SB T16 D9
            90: T20 D15 | T18 D18
            89: T19 D16
            88: T20 D14
            87: T17 D18
            86: T18 D16
            85: T15 D20 | T19 D14
            84: T20 D12
            83: T17 D16
            82: DB D16 | T14 D20
            81: T19 D12 | T15 D18
            80: T20 D10 | D20 D20
            79: T19 D11 | T13 D20
            78: T18 D12
            77: T19 D10
            76: T20 D8 | T16 D14
            75: T17 D12
            74: T14 D16
            73: T19 D8 | T17 D11
            72: T16 D12 | T20 D6
            71: T13 D16
            70: T18 D8 | T20 D5
            69: T19 D6 | T15 D12
            68: T20 D4 | T16 D10 | T18 D7
            67: T9 D20 | T17 D8
            66: T10 D18 | T18 D6 | T16 D9
            65: T11 D16 | T19 D4 | T15 D10
            64: T16 D8 | T14 D11
            63: T13 D12 | T17 D6
            62: T10 D16 | T12 D13
            61: T15 D8 | T7 D20 | T11 D14
            60: 20 D20
            59: 19 D20
            58: 18 D20
            57: 17 D20
            56: T16 D4 | 16 D20
            55: 15 D20
            54: 14 D20
            53: 13 D20
            52: 12 D20 | 20 D16
            51: 11 D20 | 19 D16
            50: 10 D20 | 18 D16
            49: 9 D20 | 17 D16
            48: 16/8 D16/D20
            47: 7 D20 | 15 D16
            46: 6/10 D20/D18 | 14 D16
            45: 13 D16 | 19 D13
            44: 12 D16 | 4 D20
            43: 3 D20 | 11 D16
            42: 10/6 D16/D18
            41: 9 D16
            """;

        private static readonly Dictionary<int, IReadOnlyList<DartTarget[]>> Routes = Parse();

        /// <summary>Alle Wege mit 3 Darts für diesen Rest (Hauptweg zuerst); leer bei Bogey-Zahlen, über 170 und unter 41.</summary>
        public static IReadOnlyList<DartTarget[]> RoutesFor(int remaining) =>
            Routes.TryGetValue(remaining, out var routes) ? routes : [];

        /// <summary>Hat das Buch für diesen Rest und diese Darts einen Weg?</summary>
        public static bool Covers(int remaining, int dartsLeft) => Route(remaining, dartsLeft, variant: 0) is not null;

        /// <summary>Nächster Dart nach dem Buch, null = kein Weg (Bogey, zu hoch, nicht checkbar mit den Darts).</summary>
        public static DartTarget? NextTarget(int remaining, int dartsLeft, int variant = 0) => Route(remaining, dartsLeft, variant)?[0];

        /// <summary>Alle Buch-Wege, die mit diesen Darts zu spielen sind (Hauptweg zuerst); sonst der eine erzeugte Weg.</summary>
        public static IReadOnlyList<DartTarget[]> RoutesFor(int remaining, int dartsLeft)
        {
            if (remaining is >= 41 and <= 170)
            {
                var routes = RoutesFor(remaining).Where(r => r.Length <= dartsLeft).ToList();

                if (routes.Count > 0)
                {
                    return routes;
                }
            }

            return Route(remaining, dartsLeft) is { } single ? [single] : [];
        }

        /// <summary>Kompletter Weg für Rest und Darts (Variante 0 = Hauptweg).</summary>
        public static DartTarget[]? Route(int remaining, int dartsLeft, int variant = 0)
        {
            if (dartsLeft is < 1 or > 3 || remaining < 2 || remaining > 170)
            {
                return null;
            }

            if (remaining <= 40)
            {
                return Low(remaining, dartsLeft);
            }

            var routes = RoutesFor(remaining).Where(r => r.Length <= dartsLeft).ToList();

            if (routes.Count > 0)
            {
                return routes[Math.Clamp(variant, 0, routes.Count - 1)];
            }

            return dartsLeft switch
            {
                1 => remaining == 50 ? [DartTarget.Double(25)] : null,
                2 => TwoDarts(remaining),
                _ => null
            };
        }

        /// <summary>Rest 2–40: Doppel direkt oder Single auf eine Zweierpotenz (32, 16, 8, 4, 2).</summary>
        private static DartTarget[]? Low(int remaining, int dartsLeft)
        {
            if (remaining % 2 == 0)
            {
                return [DartTarget.Double(remaining / 2)];
            }

            foreach (var leave in (int[])[32, 16, 8, 4, 2])
            {
                var single = remaining - leave;

                if (single is >= 1 and <= 20)
                {
                    DartTarget[] route = [DartTarget.Single(single), DartTarget.Double(leave / 2)];
                    // Mit nur einem Dart ist der Weg nicht zu Ende zu spielen, der Setup-Dart bleibt trotzdem richtig.
                    return dartsLeft >= 2 ? route : [route[0]];
                }
            }

            // 3 und 1 (Rest 1 ist bei Double-Out schon Bust): 3 → S1, dann D1.
            return remaining == 3 ? (dartsLeft >= 2 ? [DartTarget.Single(1), DartTarget.Double(1)] : [DartTarget.Single(1)]) : null;
        }

        /// <summary>Zwei Darts, Rest 41–110: erster Dart nach Vorliebe, zweiter das liebste Doppel (Bull zuletzt).</summary>
        private static DartTarget[]? TwoDarts(int remaining)
        {
            foreach (var doubleSegment in DoublePreference)
            {
                var finish = DartTarget.Double(doubleSegment);
                var first = remaining - finish.Score;

                foreach (var candidate in FirstDartPreference)
                {
                    if (candidate.Score == first)
                    {
                        return [candidate, finish];
                    }
                }
            }

            return null;
        }

        private static DartTarget[] BuildFirstDartPreference()
        {
            var list = new List<DartTarget>();

            for (var segment = 20; segment >= 1; segment--)
            {
                list.Add(DartTarget.Triple(segment));
            }

            list.Add(DartTarget.Double(25));
            list.Add(DartTarget.Single(25));

            for (var segment = 20; segment >= 1; segment--)
            {
                list.Add(DartTarget.Single(segment));
            }

            for (var segment = 20; segment >= 1; segment--)
            {
                list.Add(DartTarget.Double(segment));
            }

            return [.. list];
        }

        private static Dictionary<int, IReadOnlyList<DartTarget[]>> Parse()
        {
            var result = new Dictionary<int, IReadOnlyList<DartTarget[]>>();

            foreach (var rawLine in Table.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var colon = rawLine.IndexOf(':');
                var remaining = int.Parse(rawLine[..colon]);
                var routes = new List<DartTarget[]>();

                foreach (var variant in rawLine[(colon + 1)..].Split('|', StringSplitOptions.TrimEntries))
                {
                    routes.AddRange(Expand(variant));
                }

                result[remaining] = routes;
            }

            return result;
        }

        /// <summary>„T20 16/8 D16/D20“ → [T20 S16 D16] und [T20 S8 D20].</summary>
        private static IEnumerable<DartTarget[]> Expand(string variant)
        {
            var darts = variant.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var alternatives = darts.Max(d => d.Split('/').Length);

            for (var a = 0; a < alternatives; a++)
            {
                yield return darts.Select(d =>
                {
                    var parts = d.Split('/');
                    return ParseDart(parts[Math.Min(a, parts.Length - 1)]);
                }).ToArray();
            }
        }

        private static DartTarget ParseDart(string text) => text switch
        {
            "DB" => DartTarget.Double(25),
            "SB" => DartTarget.Single(25),
            _ when text[0] == 'T' => DartTarget.Triple(int.Parse(text[1..])),
            _ when text[0] == 'D' => DartTarget.Double(int.Parse(text[1..])),
            _ => DartTarget.Single(int.Parse(text))
        };
    }
}
