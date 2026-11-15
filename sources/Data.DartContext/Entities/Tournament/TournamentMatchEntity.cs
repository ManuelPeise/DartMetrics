using Data.DartContext.Entities.Bot;
using Data.DartContext.Entities.User;
using Shared.Enums.Tournament;

namespace Data.DartContext.Entities.Tournament
{
    /// <summary>
    /// Ein Match (ein Satz, best of N Legs) zwischen Heim (Spieler 1) und Gast (Spieler 2).
    /// Im K.-o.-Baum können die Seiten noch leer sein, bis die Vorrunde gespielt ist.
    /// Bei Bots werden Profilversion und Zielwahl-Modell beim Start gespeichert.
    /// </summary>
    public class TournamentMatchEntity : AEntityBase, IConcurrencyStamped
    {
        public int TournamentRoundId { get; set; }

        public TournamentRoundEntity Round { get; set; } = null!;

        public int MatchNumber { get; set; }

        public TournamentMatchStatusEnum Status { get; set; } = TournamentMatchStatusEnum.Scheduled;

        public int? HomeParticipantId { get; set; }

        public TournamentParticipantEntity? HomeParticipant { get; set; }

        public int? AwayParticipantId { get; set; }

        public TournamentParticipantEntity? AwayParticipant { get; set; }

        public int? HomeProfileVersionId { get; set; }

        public DartBotProfileVersionEntity? HomeProfileVersion { get; set; }

        public int? HomeTargetModelId { get; set; }

        public DartBotModelEntity? HomeTargetModel { get; set; }

        public int? AwayProfileVersionId { get; set; }

        public DartBotProfileVersionEntity? AwayProfileVersion { get; set; }

        public int? AwayTargetModelId { get; set; }

        public DartBotModelEntity? AwayTargetModel { get; set; }

        public int HomeLegs { get; set; }

        public int AwayLegs { get; set; }

        public int? WinnerParticipantId { get; set; }

        public TournamentParticipantEntity? WinnerParticipant { get; set; }

        /// <summary>K.-o.: Der Sieger rückt in dieses Match …</summary>
        public int? NextMatchId { get; set; }

        public TournamentMatchEntity? NextMatch { get; set; }

        /// <summary>… auf diese Seite (1 = Heim, 2 = Gast).</summary>
        public int? NextMatchSlot { get; set; }

        /// <summary>Wer die Eingabe gerade leitet (GameMaster oder Spieler bei privaten Turnieren).</summary>
        public int? ConductedByUserId { get; set; }

        public UserEntity? ConductedByUser { get; set; }

        // Wertung (nur offizielle Turniere, genau einmal nach Matchende)
        public int? HomeRatingBefore { get; set; }

        public int? HomeRatingAfter { get; set; }

        public int? AwayRatingBefore { get; set; }

        public int? AwayRatingAfter { get; set; }

        /// <summary>Nur Bots: vergebene XP.</summary>
        public long? HomeXpGained { get; set; }

        public long? AwayXpGained { get; set; }

        public DateTime? LearningProcessedAt { get; set; }

        public DateTime? StartedAt { get; set; }

        public DateTime? FinishedAt { get; set; }

        public Guid ConcurrencyStamp { get; set; }

        public ICollection<TournamentMatchLegEntity> Legs { get; set; } = [];
    }
}
