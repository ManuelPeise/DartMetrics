using Data.DartContext.Entities.User;
using Shared.Enums.Tournament;
using Shared.Enums.X01;

namespace Data.DartContext.Entities.Tournament
{
    /// <summary>
    /// Ein Turnier: Liga (jeder gegen jeden, Hin- und Rückrunde) oder K.-o.-Baum.
    /// Offizielle Turniere legt ein GameMaster an, private ein Spieler (Owner).
    /// </summary>
    public class TournamentEntity : AEntityBase, IConcurrencyStamped
    {
        public string Name { get; set; } = null!;

        public TournamentTypeEnum Type { get; set; }

        public TournamentKindEnum Kind { get; set; }

        public TournamentStatusEnum Status { get; set; } = TournamentStatusEnum.Draft;

        /// <summary>Wer das Turnier angelegt hat und leitet (GameMaster bzw. Spieler bei privaten Turnieren).</summary>
        public int? OwnerUserId { get; set; }

        public UserEntity? OwnerUser { get; set; }

        public int StartingScore { get; set; } = 501;

        public X01InModeEnum InMode { get; set; } = X01InModeEnum.Straight;

        public X01OutModeEnum OutMode { get; set; } = X01OutModeEnum.Double;

        /// <summary>Ende der Anmeldephase (nur offizielle Turniere).</summary>
        public DateTime? RegistrationDeadline { get; set; }

        /// <summary>K.-o.: Setzliste aus dieser abgeschlossenen Liga.</summary>
        public int? SeedSourceTournamentId { get; set; }

        public TournamentEntity? SeedSourceTournament { get; set; }

        /// <summary>K.-o.: Anzahl Gesetzte aus der Liga (Standard 4).</summary>
        public int? SeedCount { get; set; }

        public DateTime? StartedAt { get; set; }

        public DateTime? FinishedAt { get; set; }

        public int? WinnerParticipantId { get; set; }

        public TournamentParticipantEntity? WinnerParticipant { get; set; }

        public Guid ConcurrencyStamp { get; set; }

        public ICollection<TournamentParticipantEntity> Participants { get; set; } = [];

        public ICollection<TournamentRoundEntity> Rounds { get; set; } = [];
    }
}
