using Shared.Enums.Tournament;
using Shared.Enums.X01;

namespace Shared.Models.Tournament
{
    public class CreateLeagueModel
    {
        public string Name { get; set; } = "";

        /// <summary>Official: nur GameMaster. Private: Spieler für sich und Bots.</summary>
        public TournamentKindEnum Kind { get; set; } = TournamentKindEnum.Official;

        public int StartingScore { get; set; } = 501;

        public X01InModeEnum InMode { get; set; } = X01InModeEnum.Straight;

        public X01OutModeEnum OutMode { get; set; } = X01OutModeEnum.Double;
    }

    /// <summary>Genau eins von beiden: ein Benutzer (Rolle Player) oder ein aktiver Bot.</summary>
    public class AddTournamentParticipantModel
    {
        public int? UserId { get; set; }

        public int? DartBotId { get; set; }
    }

    public class StartLeagueModel
    {
        /// <summary>Satzlänge aller Liga-Matches (5, 7, 9 oder 13).</summary>
        public int BestOfLegs { get; set; } = 5;
    }

    public class LeagueOverview
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public TournamentKindEnum Kind { get; set; }
        public TournamentStatusEnum Status { get; set; }
        public int StartingScore { get; set; }
        public X01InModeEnum InMode { get; set; }
        public X01OutModeEnum OutMode { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public int? WinnerParticipantId { get; set; }
        public List<TournamentParticipantItem> Participants { get; set; } = [];
        public List<TournamentRoundItem> Rounds { get; set; } = [];
    }

    public class TournamentParticipantItem
    {
        public int Id { get; set; }
        public TournamentParticipantTypeEnum ParticipantType { get; set; }
        public int? UserId { get; set; }
        public int? DartBotId { get; set; }
        public string DisplayName { get; set; } = "";
        public TournamentParticipantStatusEnum Status { get; set; }
        public int? Seed { get; set; }
    }

    public class TournamentRoundItem
    {
        public int Id { get; set; }
        public int RoundNumber { get; set; }
        public string Name { get; set; } = "";
        public int BestOfLegs { get; set; }
        public DateTime? ScheduledAt { get; set; }
        public List<TournamentMatchItem> Matches { get; set; } = [];
    }

    public class TournamentMatchItem
    {
        public int Id { get; set; }
        public int MatchNumber { get; set; }
        public TournamentMatchStatusEnum Status { get; set; }
        public int? HomeParticipantId { get; set; }
        public int? AwayParticipantId { get; set; }
        public int HomeLegs { get; set; }
        public int AwayLegs { get; set; }
        public int? WinnerParticipantId { get; set; }
    }

    public class LeagueTableRow
    {
        public int Position { get; set; }
        public int ParticipantId { get; set; }
        public string DisplayName { get; set; } = "";
        public TournamentParticipantTypeEnum ParticipantType { get; set; }
        public int Played { get; set; }
        public int Won { get; set; }
        public int Lost { get; set; }
        public int Points { get; set; }
        public int LegsFor { get; set; }
        public int LegsAgainst { get; set; }
        public int LegDifference { get; set; }
        public double Average { get; set; }
    }
}
