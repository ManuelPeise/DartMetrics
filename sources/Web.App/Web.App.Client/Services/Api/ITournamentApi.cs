using global::Shared.Models.Tournament;

namespace Web.App.Client.Services.Api
{
    /// <summary>Turniere, Liga, K.o. und Turnier-Matches (TournamentController).</summary>
    public interface ITournamentApi
    {
        Task<ApiResult<List<TournamentListItem>>> GetTournamentsAsync(CancellationToken cancellationToken = default);
        Task<ApiResult<TournamentCandidates>> GetCandidatesAsync(CancellationToken cancellationToken = default);

        Task<ApiResult<int>> CreateLeagueAsync(CreateLeagueModel model, CancellationToken cancellationToken = default);
        Task<ApiResult<int>> CreateKnockoutAsync(CreateKnockoutModel model, CancellationToken cancellationToken = default);

        /// <summary>Liga oder K.o. je nach <paramref name="isKnockout"/> (gleiche Bedienung auf der Detailseite).</summary>
        Task<ApiResult<TournamentOverview>> GetTournamentAsync(int tournamentId, bool isKnockout, CancellationToken cancellationToken = default);
        Task<ApiResult<List<LeagueTableRow>>> GetLeagueTableAsync(int tournamentId, CancellationToken cancellationToken = default);

        Task<ApiResult<int>> AddParticipantAsync(int tournamentId, bool isKnockout, AddTournamentParticipantModel model, CancellationToken cancellationToken = default);
        Task<ApiResult<bool>> RemoveParticipantAsync(int tournamentId, bool isKnockout, int participantId, CancellationToken cancellationToken = default);

        Task<ApiResult<bool>> StartLeagueAsync(int tournamentId, StartLeagueModel model, CancellationToken cancellationToken = default);
        Task<ApiResult<bool>> StartKnockoutAsync(int tournamentId, StartKnockoutModel model, CancellationToken cancellationToken = default);
        Task<ApiResult<bool>> ScheduleRoundAsync(int tournamentId, bool isKnockout, int roundId, DateTime scheduledAtUtc, CancellationToken cancellationToken = default);

        Task<ApiResult<TournamentMatchState>> GetMatchAsync(int matchId, CancellationToken cancellationToken = default);
        Task<ApiResult<TournamentMatchState>> StartMatchAsync(int matchId, CancellationToken cancellationToken = default);
        Task<ApiResult<TournamentMatchState>> AddDartAsync(int matchId, AddTournamentDartModel model, CancellationToken cancellationToken = default);
        Task<ApiResult<TournamentMatchState>> UndoDartAsync(int matchId, Guid concurrencyStamp, CancellationToken cancellationToken = default);
    }
}
