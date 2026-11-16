using Shared.Enums.Bot;

namespace Shared.Models.Bot
{
    public class CreateDartBotModel
    {
        public string Name { get; set; } = null!;

        public string Alias { get; set; } = null!;

        public DartBotPlayingStyleEnum? PlayingStyle { get; set; }

        /// <summary>Startlevel (0 bis MaxLevel). Fähigkeiten, XP und Wertung werden passend gesetzt.</summary>
        public int StartLevel { get; set; }
    }
}
