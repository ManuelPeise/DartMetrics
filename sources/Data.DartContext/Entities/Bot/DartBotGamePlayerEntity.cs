using Data.DartContext.Entities.User;
using Shared.Enums.Bot;

namespace Data.DartContext.Entities.Bot
{
    /// <summary>
    /// Teilnehmer eines Spiels: entweder ein User oder ein Bot.
    /// Bei Bots werden Profilversion und Modelle vom Spielstart gespeichert.
    /// </summary>
    public class DartBotGamePlayerEntity : AEntityBase
    {
        public int DartBotGameId { get; set; }

        public DartBotGameEntity Game { get; set; } = null!;

        /// <summary>1 oder 2.</summary>
        public int PlayerNumber { get; set; }

        public DartBotPlayerTypeEnum PlayerType { get; set; }

        /// <summary>NULL bei Bots oder bei anonymisierten (gelöschten) Usern.</summary>
        public int? UserId { get; set; }

        public UserEntity? User { get; set; }

        public int? DartBotId { get; set; }

        public DartBotEntity? DartBot { get; set; }

        public int? ProfileVersionId { get; set; }

        public DartBotProfileVersionEntity? ProfileVersion { get; set; }

        public int? TargetModelId { get; set; }

        public DartBotModelEntity? TargetModel { get; set; }

        public int? ThrowModelId { get; set; }

        public DartBotModelEntity? ThrowModel { get; set; }

        /// <summary>Name zum Zeitpunkt des Spiels.</summary>
        public string DisplayName { get; set; } = null!;

        /// <summary>Nur Bots in Spielen gegen Menschen.</summary>
        public long? XpGained { get; set; }

        public int? RatingBefore { get; set; }

        public int? RatingAfter { get; set; }

        /// <summary>Gesetzt, sobald das Lernen für dieses Spiel verarbeitet wurde (genau einmal).</summary>
        public DateTime? LearningProcessedAt { get; set; }

        public ICollection<DartBotGameVisitEntity> Visits { get; set; } = [];
    }
}
