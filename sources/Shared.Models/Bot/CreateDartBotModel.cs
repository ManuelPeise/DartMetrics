using Shared.Enums.Bot;

namespace Shared.Models.Bot
{
    public class CreateDartBotModel
    {
        public string Name { get; set; } = null!;

        public string Alias { get; set; } = null!;

        public DartBotPlayingStyleEnum? PlayingStyle { get; set; }

        /// <summary>Stufe als Vorlage für den Basis-Ø (Standard: Hobby).</summary>
        public DartBotStageEnum? Stage { get; set; }

        /// <summary>Basis-Ø direkt (15–115); hat Vorrang vor der Stufe.</summary>
        public float? BaseAverage { get; set; }
    }
}
