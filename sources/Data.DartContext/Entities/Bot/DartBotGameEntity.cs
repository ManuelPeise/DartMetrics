using Shared.Enums.Bot;
using Shared.Enums.X01;

namespace Data.DartContext.Entities.Bot
{
    /// <summary>Ein Match (Best of X Legs) mit genau zwei Teilnehmern.</summary>
    public class DartBotGameEntity : AEntityBase
    {
        public DartBotGameModeEnum GameMode { get; set; }

        public DartBotGameStatusEnum Status { get; set; } = DartBotGameStatusEnum.Running;

        public int StartingScore { get; set; }

        public X01InModeEnum InMode { get; set; } = X01InModeEnum.Straight;

        public X01OutModeEnum OutMode { get; set; } = X01OutModeEnum.Double;

        public int BestOfLegs { get; set; }

        /// <summary>Teilnehmer dieses Spiels (Zugehörigkeit prüft die Anwendung).</summary>
        public int? WinnerPlayerId { get; set; }

        public DartBotGamePlayerEntity? WinnerPlayer { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime? FinishedAt { get; set; }

        public ICollection<DartBotGamePlayerEntity> Players { get; set; } = [];

        public ICollection<DartBotGameLegEntity> Legs { get; set; } = [];
    }
}
