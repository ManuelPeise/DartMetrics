using Shared.Enums.Bot;

namespace Shared.Models.Bot
{
    /// <summary>Stufen mit Ziel-Durchschnitt (3 Darts) und Anzeigename; die Stufe eines Bots ergibt sich aus seinem Basis-Ø.</summary>
    public static class DartBotStages
    {
        public static readonly IReadOnlyList<(DartBotStageEnum Stage, float Average, string Name)> All =
        [
            (DartBotStageEnum.Beginner, 25f, "Anfänger"),
            (DartBotStageEnum.Hobby, 40f, "Hobby"),
            (DartBotStageEnum.Club, 55f, "Vereinsspieler"),
            (DartBotStageEnum.League, 70f, "Liga"),
            (DartBotStageEnum.SemiPro, 85f, "Semi-Profi"),
            (DartBotStageEnum.Pro, 95f, "Profi"),
            (DartBotStageEnum.WorldClass, 100f, "Weltklasse"),
            (DartBotStageEnum.WorldChampion, 105f, "Weltmeister")
        ];

        public const float MinAverage = 15f;
        public const float MaxAverage = 115f;

        public static float AverageOf(DartBotStageEnum stage) => All.First(s => s.Stage == stage).Average;

        public static string NameOf(DartBotStageEnum stage) => All.First(s => s.Stage == stage).Name;

        /// <summary>Höchste Stufe, deren Ziel-Ø (mit halbem Abstand zur nächsten) erreicht ist.</summary>
        public static DartBotStageEnum StageFor(float average)
        {
            var stage = All[0].Stage;

            for (var i = 1; i < All.Count; i++)
            {
                if (average >= (All[i - 1].Average + All[i].Average) / 2)
                {
                    stage = All[i].Stage;
                }
            }

            return stage;
        }
    }
}
