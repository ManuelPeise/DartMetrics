using Shared.Models.Tournament;

namespace Shared.Interfaces.Tournament
{
    /// <summary>Turnierliste und mögliche Teilnehmer (für GameMaster und Spieler).</summary>
    public interface ITournamentQueryService
    {
        /// <summary>Offizielle Turniere für alle; private nur die eigenen.</summary>
        Task<List<TournamentListItem>> GetTournamentsAsync();

        /// <summary>GameMaster: aktive Spieler und Bots. Spieler (private Turniere): nur Bots und er selbst.</summary>
        Task<TournamentCandidates> GetCandidatesAsync();
    }
}
