using Data.DartContext.Entities.User;
using Shared.Enums.Bot;

namespace Data.DartContext.Entities.Bot
{
    /// <summary>
    /// Teilnehmer eines Spiels: entweder ein User oder ein Bot.
    /// Bei Bots werden Profilversion vom Spielstart gespeichert.
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

        /// <summary>Name zum Zeitpunkt des Spiels.</summary>
        public string DisplayName { get; set; } = null!;

        /// <summary>Nur Bots in Spielen gegen Menschen.</summary>
        public long? XpGained { get; set; }

        /// <summary>V2, nur Bots: Tagesform dieses Spiels (Faktor auf die Streuung, 1 = normal, größer = schlechter).</summary>
        public float? DayForm { get; set; }

        /// <summary>Gespielter 3-Dart-Durchschnitt in diesem Spiel (für den Verlauf Basis-Ø gegenüber Tages-Ø).</summary>
        public float? AverageInGame { get; set; }

        public int? RatingBefore { get; set; }

        public int? RatingAfter { get; set; }

        /// <summary>Gesetzt, sobald das Lernen für dieses Spiel verarbeitet wurde (genau einmal).</summary>
        public DateTime? LearningProcessedAt { get; set; }

        public ICollection<DartBotGameVisitEntity> Visits { get; set; } = [];
    }
}
