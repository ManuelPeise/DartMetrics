using Logic.Bot.Models;
using Shared.Models.Bot;

namespace Logic.Bot.Simulation
{
    /// <summary>
    /// Standard-Dartscheibe (Steeltip) in mm. Ursprung Bull-Mitte, x nach rechts, y nach oben, 20 oben.
    /// </summary>
    public static class DartBoardGeometry
    {
        public const double InnerBullRadius = 6.35;
        public const double OuterBullRadius = 15.9;
        public const double TripleInnerRadius = 99.0;
        public const double TripleOuterRadius = 107.0;
        public const double DoubleInnerRadius = 162.0;
        public const double DoubleOuterRadius = 170.0;

        private const double SegmentAngle = 18.0;

        /// <summary>Segmente im Uhrzeigersinn, beginnend oben.</summary>
        public static readonly IReadOnlyList<int> SegmentOrder =
            [20, 1, 18, 4, 13, 6, 10, 15, 2, 17, 3, 19, 7, 16, 8, 11, 14, 9, 12, 5];

        // Zielradien jeweils in der Mitte des Rings.
        private const double TripleAimRadius = (TripleInnerRadius + TripleOuterRadius) / 2;
        private const double DoubleAimRadius = (DoubleInnerRadius + DoubleOuterRadius) / 2;

        // Inneres Single-Feld (größer als das äußere).
        private const double SingleAimRadius = (OuterBullRadius + TripleInnerRadius) / 2;

        private const double OuterBullAimRadius = (InnerBullRadius + OuterBullRadius) / 2;

        /// <summary>Zielpunkt in der Mitte des Feldes.</summary>
        public static BoardPoint GetAimPoint(DartTarget target)
        {
            if (target.Segment == 25)
            {
                // Bull: Mitte; einfacher 25er: Mitte des äußeren Bull-Rings oberhalb.
                return target.Multiplier == 2
                    ? new BoardPoint(0, 0)
                    : new BoardPoint(0, OuterBullAimRadius);
            }

            if (target.Segment is < 1 or > 20)
            {
                throw new ArgumentException($"Kein gültiges Ziel: {target}.", nameof(target));
            }

            var radius = target.Multiplier switch
            {
                3 => TripleAimRadius,
                2 => DoubleAimRadius,
                1 => SingleAimRadius,
                _ => throw new ArgumentException($"Kein gültiges Ziel: {target}.", nameof(target))
            };

            var index = IndexOf(target.Segment);
            var angleRadians = (90.0 - index * SegmentAngle) * Math.PI / 180.0;

            return new BoardPoint(radius * Math.Cos(angleRadians), radius * Math.Sin(angleRadians));
        }

        /// <summary>Welches Feld liegt an diesem Punkt?</summary>
        public static DartTarget Resolve(BoardPoint point)
        {
            var radius = point.Radius;

            if (radius <= InnerBullRadius)
            {
                return new DartTarget(25, 2);
            }

            if (radius <= OuterBullRadius)
            {
                return new DartTarget(25, 1);
            }

            if (radius > DoubleOuterRadius)
            {
                return DartTarget.Miss;
            }

            var segment = SegmentAt(point);

            var multiplier = radius switch
            {
                >= TripleInnerRadius and <= TripleOuterRadius => 3,
                >= DoubleInnerRadius => 2,
                _ => 1
            };

            return new DartTarget(segment, multiplier);
        }

        private static int SegmentAt(BoardPoint point)
        {
            var mathDegrees = Math.Atan2(point.Y, point.X) * 180.0 / Math.PI;

            // Winkel im Uhrzeigersinn ab oben, 0..360.
            var clockwiseFromTop = ((90.0 - mathDegrees) % 360.0 + 360.0) % 360.0;

            var index = (int)Math.Floor((clockwiseFromTop + SegmentAngle / 2) / SegmentAngle) % SegmentOrder.Count;

            return SegmentOrder[index];
        }

        private static int IndexOf(int segment)
        {
            for (var i = 0; i < SegmentOrder.Count; i++)
            {
                if (SegmentOrder[i] == segment)
                {
                    return i;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(segment));
        }
    }
}
