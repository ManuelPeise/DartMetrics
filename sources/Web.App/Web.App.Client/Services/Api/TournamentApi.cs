using global::Shared.Models.Tournament;

namespace Web.App.Client.Services.Api
{
    public sealed class TournamentApi : ITournamentApi
    {
        private const string Controller = "Tournament/";

        private readonly ApiClient _api;

        public TournamentApi(ApiClient api)
        {
            _api = api;
        }

        private static string Kind(bool isKnockout) => isKnockout ? "Knockout" : "League";

        public Task<ApiResult<List<TournamentListItem>>> GetTournamentsAsync(CancellationToken ct = default) =>
            ApiResponse.ReadAsync<List<TournamentListItem>>(_api.GetAsync(Controller + "GetTournaments", ct), ct);

        public Task<ApiResult<TournamentCandidates>> GetCandidatesAsync(CancellationToken ct = default) =>
            ApiResponse.ReadAsync<TournamentCandidates>(_api.GetAsync(Controller + "GetTournamentCandidates", ct), ct);

        public Task<ApiResult<int>> CreateLeagueAsync(CreateLeagueModel model, CancellationToken ct = default) =>
            ApiResponse.ReadAsync<int>(_api.PostAsJsonAsync(Controller + "CreateLeague", model, ct), ct);

        public Task<ApiResult<int>> CreateKnockoutAsync(CreateKnockoutModel model, CancellationToken ct = default) =>
            ApiResponse.ReadAsync<int>(_api.PostAsJsonAsync(Controller + "CreateKnockout", model, ct), ct);

        public Task<ApiResult<TournamentOverview>> GetTournamentAsync(int tournamentId, bool isKnockout, CancellationToken ct = default) =>
            ApiResponse.ReadAsync<TournamentOverview>(_api.GetAsync($"{Controller}Get{Kind(isKnockout)}?tournamentId={tournamentId}", ct), ct);

        public Task<ApiResult<List<LeagueTableRow>>> GetLeagueTableAsync(int tournamentId, CancellationToken ct = default) =>
            ApiResponse.ReadAsync<List<LeagueTableRow>>(_api.GetAsync($"{Controller}GetLeagueTable?tournamentId={tournamentId}", ct), ct);

        public Task<ApiResult<int>> AddParticipantAsync(int tournamentId, bool isKnockout, AddTournamentParticipantModel model, CancellationToken ct = default) =>
            ApiResponse.ReadAsync<int>(_api.PostAsJsonAsync($"{Controller}Add{Kind(isKnockout)}Participant?tournamentId={tournamentId}", model, ct), ct);

        public Task<ApiResult<bool>> RemoveParticipantAsync(int tournamentId, bool isKnockout, int participantId, CancellationToken ct = default) =>
            ApiResponse.ExecuteAsync(_api.PostAsJsonAsync<object?>($"{Controller}Remove{Kind(isKnockout)}Participant?tournamentId={tournamentId}&participantId={participantId}", null, ct), ct);

        public Task<ApiResult<bool>> StartLeagueAsync(int tournamentId, StartLeagueModel model, CancellationToken ct = default) =>
            ApiResponse.ExecuteAsync(_api.PostAsJsonAsync($"{Controller}StartLeague?tournamentId={tournamentId}", model, ct), ct);

        public Task<ApiResult<bool>> StartKnockoutAsync(int tournamentId, StartKnockoutModel model, CancellationToken ct = default) =>
            ApiResponse.ExecuteAsync(_api.PostAsJsonAsync($"{Controller}StartKnockout?tournamentId={tournamentId}", model, ct), ct);

        public Task<ApiResult<bool>> ScheduleRoundAsync(int tournamentId, bool isKnockout, int roundId, DateTime scheduledAtUtc, CancellationToken ct = default) =>
            ApiResponse.ExecuteAsync(_api.PostAsJsonAsync($"{Controller}Schedule{Kind(isKnockout)}Round?tournamentId={tournamentId}&roundId={roundId}",
                new ScheduleRoundModel { ScheduledAtUtc = scheduledAtUtc }, ct), ct);

        public Task<ApiResult<TournamentMatchState>> GetMatchAsync(int matchId, CancellationToken ct = default) =>
            ApiResponse.ReadAsync<TournamentMatchState>(_api.GetAsync($"{Controller}GetTournamentMatch?matchId={matchId}", ct), ct);

        public Task<ApiResult<TournamentMatchState>> StartMatchAsync(int matchId, CancellationToken ct = default) =>
            ApiResponse.ReadAsync<TournamentMatchState>(_api.PostAsJsonAsync<object?>($"{Controller}StartTournamentMatch?matchId={matchId}", null, ct), ct);

        public Task<ApiResult<TournamentMatchState>> UndoDartAsync(int matchId, Guid concurrencyStamp, CancellationToken ct = default) =>
            ApiResponse.ReadAsync<TournamentMatchState>(_api.PostAsJsonAsync<object?>($"{Controller}UndoTournamentDart?matchId={matchId}&concurrencyStamp={concurrencyStamp}", null, ct), ct);

        public Task<ApiResult<TournamentMatchState>> AddDartAsync(int matchId, AddTournamentDartModel model, CancellationToken ct = default) =>
            ApiResponse.ReadAsync<TournamentMatchState>(_api.PostAsJsonAsync($"{Controller}AddTournamentDart?matchId={matchId}", model, ct), ct);
    }
}
