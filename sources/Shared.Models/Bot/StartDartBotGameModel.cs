using Shared.Enums.Bot;

namespace Shared.Models.Bot
{
    public class StartDartBotGameModel
    {
        public int DartBotId { get; set; }

        /// <summary>301 oder 501.</summary>
        public int StartingScore { get; set; } = 501;

        /// <summary>1, 3, 5, 7, 9 oder 11.</summary>
        public int BestOfLegs { get; set; } = 1;

        /// <summary>Training, Match oder Tournament (BotVsBot ist hier nicht erlaubt).</summary>
        public DartBotGameModeEnum GameMode { get; set; } = DartBotGameModeEnum.Training;

        /// <summary>Hat der Mensch im ersten Leg den Anwurf? Danach wechselt er je Leg.</summary>
        public bool UserStarts { get; set; } = true;
    }
}
