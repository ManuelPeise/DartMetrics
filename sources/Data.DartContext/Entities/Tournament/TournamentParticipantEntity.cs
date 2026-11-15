using Data.DartContext.Entities.Bot;
using Data.DartContext.Entities.User;
using Shared.Enums.Tournament;

namespace Data.DartContext.Entities.Tournament
{
    /// <summary>Teilnehmer: entweder ein Benutzer oder ein Bot. Tabelle und Baum behandeln beide gleich.</summary>
    public class TournamentParticipantEntity : AEntityBase
    {
        public int TournamentId { get; set; }

        public TournamentEntity Tournament { get; set; } = null!;

        public TournamentParticipantTypeEnum ParticipantType { get; set; }

        public int? UserId { get; set; }

        public UserEntity? User { get; set; }

        public int? DartBotId { get; set; }

        public DartBotEntity? DartBot { get; set; }

        /// <summary>Name zum Zeitpunkt der Anmeldung (bleibt bei Umbenennung oder Anonymisierung lesbar).</summary>
        public string DisplayName { get; set; } = null!;

        public TournamentParticipantStatusEnum Status { get; set; } = TournamentParticipantStatusEnum.Confirmed;

        /// <summary>K.-o.: Setzplatz (1 = bester), null = ungesetzt.</summary>
        public int? Seed { get; set; }
    }
}
