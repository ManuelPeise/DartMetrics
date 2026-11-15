using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Enums.User;
using Shared.Interfaces.Tournament;
using Shared.Models.Tournament;

namespace Service.Api.Controllers.Tournament
{
    /// <summary>
    /// Turniere (Liga, K.o.) und ihre Matches. Zugang für GameMaster und Spieler; was jeder darf
    /// (offiziell = GameMaster, privat = Besitzer, Zuschauen = alle) prüfen die Services.
    /// </summary>
    [Authorize(Roles = nameof(UserRoleEnum.GameMaster) + "," + nameof(UserRoleEnum.Player))]
    public class TournamentController : ApiControllerBase
    {
        private readonly ITournamentQueryService _query;
        private readonly ILeagueService _leagues;
        private readonly IKnockoutService _knockouts;
        private readonly ITournamentMatchService _matches;
        private readonly ITournamentRegistrationService _registration;

        public TournamentController(ITournamentQueryService query, ILeagueService leagues, IKnockoutService knockouts, ITournamentMatchService matches,
            ITournamentRegistrationService registration)
        {
            _registration = registration;
            _query = query;
            _leagues = leagues;
            _knockouts = knockouts;
            _matches = matches;
        }

        // ---------- Übersicht ----------

        [HttpGet(Name = "GetTournaments")]
        public async Task<IActionResult> GetTournaments() => Ok(await _query.GetTournamentsAsync());

        [HttpGet(Name = "GetTournamentCandidates")]
        public async Task<IActionResult> GetTournamentCandidates() => Ok(await _query.GetCandidatesAsync());

        // ---------- Anmeldung (offizielle Turniere) ----------

        [HttpPost(Name = "OpenTournamentRegistration")]
        public async Task<IActionResult> OpenTournamentRegistration([FromQuery] int tournamentId, [FromBody] OpenRegistrationModel model)
        {
            await _registration.OpenRegistrationAsync(tournamentId, model.DeadlineUtc);
            return Ok();
        }

        [HttpPost(Name = "RegisterForTournament")]
        public async Task<IActionResult> RegisterForTournament([FromQuery] int tournamentId) => Ok(await _registration.RegisterAsync(tournamentId));

        [HttpPost(Name = "WithdrawFromTournament")]
        public async Task<IActionResult> WithdrawFromTournament([FromQuery] int tournamentId)
        {
            await _registration.WithdrawAsync(tournamentId);
            return Ok();
        }

        [HttpPost(Name = "ConfirmTournamentParticipant")]
        public async Task<IActionResult> ConfirmTournamentParticipant([FromQuery] int tournamentId, [FromQuery] int participantId)
        {
            await _registration.ConfirmParticipantAsync(tournamentId, participantId);
            return Ok();
        }

        // ---------- Liga ----------

        [HttpPost(Name = "CreateLeague")]
        public async Task<IActionResult> CreateLeague([FromBody] CreateLeagueModel model) => Ok(await _leagues.CreateLeagueAsync(model));

        [HttpPost(Name = "AddLeagueParticipant")]
        public async Task<IActionResult> AddLeagueParticipant([FromQuery] int tournamentId, [FromBody] AddTournamentParticipantModel model) =>
            Ok(await _leagues.AddParticipantAsync(tournamentId, model));

        [HttpPost(Name = "RemoveLeagueParticipant")]
        public async Task<IActionResult> RemoveLeagueParticipant([FromQuery] int tournamentId, [FromQuery] int participantId)
        {
            await _leagues.RemoveParticipantAsync(tournamentId, participantId);
            return Ok();
        }

        [HttpPost(Name = "StartLeague")]
        public async Task<IActionResult> StartLeague([FromQuery] int tournamentId, [FromBody] StartLeagueModel model)
        {
            await _leagues.StartLeagueAsync(tournamentId, model);
            return Ok();
        }

        [HttpPost(Name = "ScheduleLeagueRound")]
        public async Task<IActionResult> ScheduleLeagueRound([FromQuery] int tournamentId, [FromQuery] int roundId, [FromBody] ScheduleRoundModel model)
        {
            await _leagues.ScheduleRoundAsync(tournamentId, roundId, model.ScheduledAtUtc);
            return Ok();
        }

        [HttpGet(Name = "GetLeague")]
        public async Task<IActionResult> GetLeague([FromQuery] int tournamentId) => Ok(await _leagues.GetLeagueAsync(tournamentId));

        [HttpGet(Name = "GetLeagueTable")]
        public async Task<IActionResult> GetLeagueTable([FromQuery] int tournamentId) => Ok(await _leagues.GetTableAsync(tournamentId));

        // ---------- K.o. ----------

        [HttpPost(Name = "CreateKnockout")]
        public async Task<IActionResult> CreateKnockout([FromBody] CreateKnockoutModel model) => Ok(await _knockouts.CreateKnockoutAsync(model));

        [HttpPost(Name = "AddKnockoutParticipant")]
        public async Task<IActionResult> AddKnockoutParticipant([FromQuery] int tournamentId, [FromBody] AddTournamentParticipantModel model) =>
            Ok(await _knockouts.AddParticipantAsync(tournamentId, model));

        [HttpPost(Name = "RemoveKnockoutParticipant")]
        public async Task<IActionResult> RemoveKnockoutParticipant([FromQuery] int tournamentId, [FromQuery] int participantId)
        {
            await _knockouts.RemoveParticipantAsync(tournamentId, participantId);
            return Ok();
        }

        [HttpPost(Name = "StartKnockout")]
        public async Task<IActionResult> StartKnockout([FromQuery] int tournamentId, [FromBody] StartKnockoutModel model)
        {
            await _knockouts.StartKnockoutAsync(tournamentId, model);
            return Ok();
        }

        [HttpPost(Name = "ScheduleKnockoutRound")]
        public async Task<IActionResult> ScheduleKnockoutRound([FromQuery] int tournamentId, [FromQuery] int roundId, [FromBody] ScheduleRoundModel model)
        {
            await _knockouts.ScheduleRoundAsync(tournamentId, roundId, model.ScheduledAtUtc);
            return Ok();
        }

        [HttpGet(Name = "GetKnockout")]
        public async Task<IActionResult> GetKnockout([FromQuery] int tournamentId) => Ok(await _knockouts.GetKnockoutAsync(tournamentId));

        // ---------- Matches ----------

        [HttpPost(Name = "StartTournamentMatch")]
        public async Task<IActionResult> StartTournamentMatch([FromQuery] int matchId) => Ok(await _matches.StartMatchAsync(matchId));

        [HttpPost(Name = "AddTournamentDart")]
        public async Task<IActionResult> AddTournamentDart([FromQuery] int matchId, [FromBody] AddTournamentDartModel model) =>
            Ok(await _matches.AddDartAsync(matchId, model));

        [HttpPost(Name = "UndoTournamentDart")]
        public async Task<IActionResult> UndoTournamentDart([FromQuery] int matchId, [FromQuery] Guid? concurrencyStamp) =>
            Ok(await _matches.UndoLastDartAsync(matchId, concurrencyStamp));

        [HttpGet(Name = "GetTournamentMatch")]
        public async Task<IActionResult> GetTournamentMatch([FromQuery] int matchId) => Ok(await _matches.GetMatchStateAsync(matchId));
    }
}
