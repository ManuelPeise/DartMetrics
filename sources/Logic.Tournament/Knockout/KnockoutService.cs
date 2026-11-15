using Data.DartContext.Entities.Tournament;
using Microsoft.EntityFrameworkCore;
using Shared.Enums.Tournament;
using Shared.Interfaces.Authentication;
using Shared.Interfaces.Logger;
using Shared.Interfaces.Tournament;
using Shared.Interfaces.UnitsOfWork;
using Shared.Models.Shared;
using Shared.Models.Tournament;

namespace Logic.Tournament.Knockout
{
    public class KnockoutService : TournamentServiceBase, IKnockoutService
    {
        private static readonly int[] AllowedSeedCounts = [2, 4, 8];

        private readonly ILeagueService _leagueService;

        public KnockoutService(IUnitOfWork unitOfWork, IMessageLog messageLog, IClaimsHelper claimsHelper, ILeagueService leagueService)
            : base(unitOfWork, messageLog, claimsHelper)
        {
            _leagueService = leagueService;
        }

        public async Task<int> CreateKnockoutAsync(CreateKnockoutModel model)
        {
            try
            {
                if (model.SeedSourceTournamentId is int sourceId)
                {
                    if (!AllowedSeedCounts.Contains(model.SeedCount))
                    {
                        throw new ArgumentException("Seed count must be 2, 4 or 8.");
                    }

                    var source = await Tournaments.TournamentRepository.GetFirstOrDefaultAsync(t => t.Id == sourceId, asNoTracking: true)
                        ?? throw new ArgumentException($"Seed source {sourceId} not found.");

                    if (source.Type != TournamentTypeEnum.League || source.Kind != model.Kind)
                    {
                        throw new ArgumentException("The seed source must be a league of the same kind (official or private).");
                    }

                    TournamentAccess.EnsureCanView(ClaimsHelper.GetCurrentUserFromClaims(), source);
                }

                return await CreateTournamentAsync(TournamentTypeEnum.Knockout, model.Kind, model.Name, model.StartingScore, model.InMode, model.OutMode,
                    t =>
                    {
                        t.SeedSourceTournamentId = model.SeedSourceTournamentId;
                        t.SeedCount = model.SeedSourceTournamentId is null ? null : model.SeedCount;
                    });
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error creating knockout tournament.", nameof(KnockoutService), exception);
                throw;
            }
        }

        public async Task<int> AddParticipantAsync(int tournamentId, AddTournamentParticipantModel model)
        {
            try
            {
                var tournament = await LoadManagedAsync(tournamentId, TournamentTypeEnum.Knockout, tracking: true);
                return await AddParticipantCoreAsync(tournament, model, KnockoutBracketBuilder.MaxParticipants);
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage($"Error adding participant to knockout {tournamentId}.", nameof(KnockoutService), exception);
                throw;
            }
        }

        public async Task RemoveParticipantAsync(int tournamentId, int participantId)
        {
            try
            {
                var tournament = await LoadManagedAsync(tournamentId, TournamentTypeEnum.Knockout, tracking: true);
                await RemoveParticipantCoreAsync(tournament, participantId);
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage($"Error removing participant {participantId} from knockout {tournamentId}.", nameof(KnockoutService), exception);
                throw;
            }
        }

        public async Task StartKnockoutAsync(int tournamentId, StartKnockoutModel model)
        {
            try
            {
                var tournament = await LoadManagedAsync(tournamentId, TournamentTypeEnum.Knockout, tracking: true);
                EnsureNotStarted(tournament);

                var seeded = await ApplySeedingAsync(tournament);

                // Ergänzte Gesetzte brauchen ihre Id, bevor der Baum gebaut wird.
                await UnitOfWork.SaveChangesAsync();

                var confirmed = tournament.Participants.Where(p => p.Status == TournamentParticipantStatusEnum.Confirmed).ToList();

                if (confirmed.Count is < KnockoutBracketBuilder.MinParticipants or > KnockoutBracketBuilder.MaxParticipants)
                {
                    throw new ArgumentException($"A knockout needs {KnockoutBracketBuilder.MinParticipants} to {KnockoutBracketBuilder.MaxParticipants} confirmed participants (has {confirmed.Count}).");
                }

                var roundCount = KnockoutBracketBuilder.RoundCount(confirmed.Count);
                var bestOf = model.BestOfLegsPerRound.Count == 0 ? Enumerable.Repeat(5, roundCount).ToList() : model.BestOfLegsPerRound;

                if (bestOf.Count != roundCount || bestOf.Any(b => !X01SetRules.AllowedBestOfLegs.Contains(b)))
                {
                    throw new ArgumentException($"Give {roundCount} set lengths (5, 7, 9 or 13), one per round.");
                }

                // Gesetzte nach Platz, Ungesetzte ausgelost.
                var seededIds = seeded.Select(p => p.Id).ToList();
                var unseededIds = confirmed.Where(p => p.Seed is null).Select(p => p.Id).OrderBy(_ => Random.Shared.Next()).ToList();
                var bracket = KnockoutBracketBuilder.Build(seededIds, unseededIds);

                var rounds = new Dictionary<int, TournamentRoundEntity>();
                var matches = new Dictionary<(int Round, int Match), TournamentMatchEntity>();

                for (var r = 1; r <= roundCount; r++)
                {
                    var round = new TournamentRoundEntity
                    {
                        RoundNumber = r,
                        Name = KnockoutBracketBuilder.RoundName(r, roundCount),
                        BestOfLegs = bestOf.ElementAt(r - 1)
                    };

                    rounds[r] = round;
                    tournament.Rounds.Add(round);
                }

                foreach (var b in bracket)
                {
                    var match = new TournamentMatchEntity
                    {
                        MatchNumber = b.MatchNumber,
                        HomeParticipantId = b.Home,
                        AwayParticipantId = b.Away,
                        Status = TournamentMatchStatusEnum.Scheduled,
                        NextMatchSlot = b.NextSlot
                    };

                    matches[(b.Round, b.MatchNumber)] = match;
                    rounds[b.Round].Matches.Add(match);
                }

                // Nachfolger verknüpfen (Navigation, Ids vergibt die Datenbank).
                foreach (var b in bracket.Where(b => b.NextMatchNumber is not null))
                {
                    matches[(b.Round, b.MatchNumber)].NextMatch = matches[(b.Round + 1, b.NextMatchNumber!.Value)];
                }

                tournament.Status = TournamentStatusEnum.Running;
                tournament.StartedAt = DateTime.UtcNow;

                await UnitOfWork.SaveChangesAsync();
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage($"Error starting knockout {tournamentId}.", nameof(KnockoutService), exception);
                throw;
            }

            // Freilose sofort werten (eigene Speicherung: braucht die vergebenen Ids).
            await AdvanceAsync(tournamentId);
        }

        public async Task ScheduleRoundAsync(int tournamentId, int roundId, DateTime scheduledAtUtc)
        {
            try
            {
                var tournament = await LoadManagedAsync(tournamentId, TournamentTypeEnum.Knockout, tracking: false);
                await ScheduleRoundCoreAsync(tournament, roundId, scheduledAtUtc);
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage($"Error scheduling round {roundId} of knockout {tournamentId}.", nameof(KnockoutService), exception);
                throw;
            }
        }

        public async Task<TournamentOverview> GetKnockoutAsync(int tournamentId)
        {
            try
            {
                return ToOverview(await LoadVisibleAsync(tournamentId, TournamentTypeEnum.Knockout));
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage($"Error loading knockout {tournamentId}.", nameof(KnockoutService), exception);
                throw;
            }
        }

        public async Task AdvanceAsync(int tournamentId)
        {
            try
            {
                var tournament = await Tournaments.TournamentRepository.GetFirstOrDefaultAsync(
                    t => t.Id == tournamentId && t.Type == TournamentTypeEnum.Knockout,
                    include: q => q.Include(t => t.Rounds).ThenInclude(r => r.Matches)
                        .Include(t => t.Participants).ThenInclude(p => p.User))
                    ?? throw new ArgumentException($"Knockout tournament {tournamentId} not found.");

                if (tournament.Status != TournamentStatusEnum.Running)
                {
                    return;
                }

                // Gelöschte Konten spielen nicht mehr: ihr nächster Gegner gewinnt kampflos.
                var deleted = tournament.Participants.Where(p => p.User?.DeletedAt != null).Select(p => p.Id).ToHashSet();
                var now = DateTime.UtcNow;

                var all = tournament.Rounds.SelectMany(r => r.Matches).ToDictionary(m => m.Id);

                foreach (var round in tournament.Rounds.OrderBy(r => r.RoundNumber))
                {
                    foreach (var match in round.Matches.OrderBy(m => m.MatchNumber))
                    {
                        // Freilos in Runde 1: genau eine Seite besetzt.
                        if (round.RoundNumber == 1 && match.Status == TournamentMatchStatusEnum.Scheduled &&
                            (match.HomeParticipantId is null) != (match.AwayParticipantId is null))
                        {
                            match.Status = TournamentMatchStatusEnum.Bye;
                            match.WinnerParticipantId = match.HomeParticipantId ?? match.AwayParticipantId;
                        }

                        if (match.Status == TournamentMatchStatusEnum.Scheduled && match.HomeParticipantId is int home && match.AwayParticipantId is int away
                            && (deleted.Contains(home) || deleted.Contains(away)))
                        {
                            TournamentWithdrawal.Forfeit(match, loserIsHome: deleted.Contains(home), now);
                        }

                        if (match.WinnerParticipantId is not int winner || match.NextMatchId is not int nextId)
                        {
                            continue;
                        }

                        var next = all[nextId];

                        if (match.NextMatchSlot == 1 && next.HomeParticipantId is null)
                        {
                            next.HomeParticipantId = winner;
                        }
                        else if (match.NextMatchSlot == 2 && next.AwayParticipantId is null)
                        {
                            next.AwayParticipantId = winner;
                        }
                    }
                }

                var final = all.Values.Single(m => m.NextMatchId is null);

                if (final.Status == TournamentMatchStatusEnum.Finished && final.WinnerParticipantId is not null)
                {
                    tournament.Status = TournamentStatusEnum.Finished;
                    tournament.FinishedAt = DateTime.UtcNow;
                    tournament.WinnerParticipantId = final.WinnerParticipantId;
                }

                await UnitOfWork.SaveChangesAsync();
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage($"Error advancing knockout {tournamentId}.", nameof(KnockoutService), exception);
                throw;
            }
        }

        /// <summary>
        /// Setzplätze aus der abgeschlossenen Quell-Liga: Platz 1..N bekommen Seed 1..N. Fehlt ein Gesetzter im Teilnehmerfeld,
        /// wird er ergänzt. Ohne Quelle gibt es keine Gesetzten.
        /// </summary>
        private async Task<List<TournamentParticipantEntity>> ApplySeedingAsync(TournamentEntity tournament)
        {
            foreach (var p in tournament.Participants)
            {
                p.Seed = null;
            }

            if (tournament.SeedSourceTournamentId is not int sourceId || tournament.SeedCount is not int seedCount)
            {
                return [];
            }

            var source = await Tournaments.TournamentRepository.GetFirstOrDefaultAsync(
                t => t.Id == sourceId,
                asNoTracking: true,
                include: q => q.Include(t => t.Participants))
                ?? throw new ArgumentException($"Seed source {sourceId} not found.");

            if (source.Status != TournamentStatusEnum.Finished)
            {
                throw new BusinessRuleException("The seed source league is not finished yet.");
            }

            var table = await _leagueService.GetTableAsync(sourceId);

            if (table.Count < seedCount)
            {
                throw new BusinessRuleException($"The seed source league has fewer than {seedCount} participants.");
            }

            var seeded = new List<TournamentParticipantEntity>();

            foreach (var row in table.Take(seedCount))
            {
                var leagueParticipant = source.Participants.Single(p => p.Id == row.ParticipantId);

                var participant = tournament.Participants.FirstOrDefault(p =>
                    (leagueParticipant.UserId is not null && p.UserId == leagueParticipant.UserId) ||
                    (leagueParticipant.DartBotId is not null && p.DartBotId == leagueParticipant.DartBotId));

                if (participant is null)
                {
                    participant = leagueParticipant.UserId is int userId
                        ? await NewUserParticipantAsync(tournament, userId)
                        : await NewBotParticipantAsync(leagueParticipant.DartBotId!.Value);
                    tournament.Participants.Add(participant);
                }

                participant.Status = TournamentParticipantStatusEnum.Confirmed;
                participant.Seed = row.Position;
                seeded.Add(participant);
            }

            return seeded;
        }
    }
}
