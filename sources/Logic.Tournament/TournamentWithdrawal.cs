using Data.DartContext.Entities.Tournament;
using Microsoft.EntityFrameworkCore;
using Shared.Enums.Tournament;
using Shared.Interfaces.Tournament;
using Shared.Interfaces.UnitsOfWork;
using Shared.Models.Shared;

namespace Logic.Tournament
{
    public class TournamentWithdrawal : ITournamentWithdrawal
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILeagueService _leagueService;
        private readonly IKnockoutService _knockoutService;

        public TournamentWithdrawal(IUnitOfWork unitOfWork, ILeagueService leagueService, IKnockoutService knockoutService)
        {
            _unitOfWork = unitOfWork;
            _leagueService = leagueService;
            _knockoutService = knockoutService;
        }

        public async Task WithdrawDeletedUserAsync(int userId, string anonymizedName)
        {
            var tournaments = _unitOfWork.TournamentUnitOfWork;
            var participants = await tournaments.TournamentParticipantRepository.GetWhereAsync(
                p => p.UserId == userId,
                include: q => q.Include(p => p.Tournament));

            if (participants.Count == 0)
            {
                return;
            }

            var now = DateTime.UtcNow;
            var running = new List<TournamentEntity>();

            foreach (var participant in participants)
            {
                participant.DisplayName = anonymizedName;

                if (participant.Tournament.Status is TournamentStatusEnum.Draft or TournamentStatusEnum.Registration)
                {
                    participant.Status = TournamentParticipantStatusEnum.Withdrawn;
                    participant.Seed = null;
                }
                else if (participant.Tournament.Status == TournamentStatusEnum.Running)
                {
                    running.Add(participant.Tournament);
                }
            }

            // Offene Matches (geplant oder laufend, Gegner bekannt): Gegner gewinnt kampflos.
            var ids = participants.Select(p => p.Id).ToList();
            var open = await tournaments.TournamentMatchRepository.GetWhereAsync(
                m => (m.Status == TournamentMatchStatusEnum.Scheduled || m.Status == TournamentMatchStatusEnum.Running)
                     && m.HomeParticipantId != null && m.AwayParticipantId != null
                     && (ids.Contains(m.HomeParticipantId.Value) || ids.Contains(m.AwayParticipantId.Value)),
                include: q => q.Include(m => m.Round));

            foreach (var match in open)
            {
                Forfeit(match, loserIsHome: ids.Contains(match.HomeParticipantId!.Value), now);
            }

            await _unitOfWork.SaveChangesAsync();

            foreach (var tournament in running.DistinctBy(t => t.Id))
            {
                if (tournament.Type == TournamentTypeEnum.League)
                {
                    await _leagueService.FinishLeagueIfCompleteAsync(tournament.Id);
                }
                else
                {
                    // Rückt Sieger weiter; trifft der Gelöschte dabei auf einen neuen Gegner, gewinnt dieser kampflos (siehe AdvanceAsync).
                    await _knockoutService.AdvanceAsync(tournament.Id);
                }
            }
        }

        /// <summary>Kampflos: der Gegner bekommt die nötigen Legs, die Legs des Aufgebenden bleiben stehen. Keine Elo-/XP-Wertung.</summary>
        internal static void Forfeit(TournamentMatchEntity match, bool loserIsHome, DateTime now)
        {
            var legsToWin = X01SetRules.LegsToWin(match.Round.BestOfLegs);

            match.Status = TournamentMatchStatusEnum.Finished;
            match.StartedAt ??= now;
            match.FinishedAt = now;

            if (loserIsHome)
            {
                match.AwayLegs = legsToWin;
                match.HomeLegs = Math.Min(match.HomeLegs, legsToWin - 1);
                match.WinnerParticipantId = match.AwayParticipantId;
            }
            else
            {
                match.HomeLegs = legsToWin;
                match.AwayLegs = Math.Min(match.AwayLegs, legsToWin - 1);
                match.WinnerParticipantId = match.HomeParticipantId;
            }

            match.ConcurrencyStamp = Guid.NewGuid();
        }
    }
}
