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

    /// <summary>Liga oder K.-o.: Stammdaten, Teilnehmer, Runden mit Matches.</summary>
    public class TournamentOverview
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public TournamentTypeEnum Type { get; set; }
        public TournamentKindEnum Kind { get; set; }
        public TournamentStatusEnum Status { get; set; }
        public int StartingScore { get; set; }
        public X01InModeEnum InMode { get; set; }
        public X01OutModeEnum OutMode { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public int? WinnerParticipantId { get; set; }
        public int? SeedSourceTournamentId { get; set; }
        public int? SeedCount { get; set; }

        /// <summary>Darf der angemeldete Benutzer verwalten und Matches leiten?</summary>
        public bool CanManage { get; set; }
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

        /// <summary>K.-o.: Match, in das der Sieger rückt, und die Seite (1 = Heim, 2 = Gast).</summary>
        public int? NextMatchId { get; set; }
        public int? NextMatchSlot { get; set; }
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

namespace Shared.Models.Tournament
{
    public class CreateKnockoutModel
    {
        public string Name { get; set; } = "";

        public TournamentKindEnum Kind { get; set; } = TournamentKindEnum.Official;

        public int StartingScore { get; set; } = 501;

        public X01InModeEnum InMode { get; set; } = X01InModeEnum.Straight;

        public X01OutModeEnum OutMode { get; set; } = X01OutModeEnum.Double;

        /// <summary>Abgeschlossene Liga, deren beste <see cref="SeedCount"/> gesetzt werden (optional).</summary>
        public int? SeedSourceTournamentId { get; set; }

        /// <summary>Anzahl Gesetzte aus der Liga (2, 4 oder 8; Standard 4).</summary>
        public int SeedCount { get; set; } = 4;
    }

    public class StartKnockoutModel
    {
        /// <summary>Satzlänge je Runde, erste Runde zuerst bis zum Finale (je 5, 7, 9 oder 13). Leer = überall 5.</summary>
        public List<int> BestOfLegsPerRound { get; set; } = [];
    }
}

namespace Shared.Models.Tournament
{
    public class TournamentListItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public TournamentTypeEnum Type { get; set; }
        public TournamentKindEnum Kind { get; set; }
        public TournamentStatusEnum Status { get; set; }
        public int ParticipantCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public string? WinnerName { get; set; }

        /// <summary>Darf der angemeldete Benutzer das Turnier verwalten und Matches leiten?</summary>
        public bool CanManage { get; set; }
    }

    /// <summary>Wer sich als Teilnehmer eintragen lässt: Spieler (nur für offizielle Turniere) und aktive Bots.</summary>
    public class TournamentCandidates
    {
        public List<TournamentCandidate> Players { get; set; } = [];
        public List<TournamentCandidate> Bots { get; set; } = [];
    }

    public class TournamentCandidate
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = "";

        /// <summary>Elo (Spieler) bzw. Elo des Bots.</summary>
        public int Rating { get; set; }

        /// <summary>Nur Bots: Entwicklungsstufe.</summary>
        public int? Level { get; set; }
    }
}

namespace Shared.Models.Tournament
{
    public class ScheduleRoundModel
    {
        /// <summary>Termin in UTC (der Client rechnet aus der Ortszeit um).</summary>
        public DateTime ScheduledAtUtc { get; set; }
    }
}
