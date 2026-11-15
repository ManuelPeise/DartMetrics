using Data.DartContext.Entities.Tournament;
using Logic.Tournament.Match;
using Microsoft.EntityFrameworkCore;
using Shared.Enums.Bot;
using Shared.Enums.Tournament;
using Shared.Interfaces.Authentication;
using Shared.Interfaces.Bot;
using Shared.Interfaces.Logger;
using Shared.Interfaces.Tournament;
using Shared.Interfaces.UnitsOfWork;
using Shared.Models.Bot;
using Shared.Models.Shared;
using Shared.Models.Tournament;

namespace Logic.Tournament.Play
{
    public class TournamentMatchService : TournamentServiceBase, ITournamentMatchService
    {
        private readonly IBotVisitPlayer _botVisitPlayer;
        private readonly ITargetSelectorFactory _targetSelectorFactory;
        private readonly ILeagueService _leagueService;
        private readonly IKnockoutService _knockoutService;

        public TournamentMatchService(
            IUnitOfWork unitOfWork,
            IMessageLog messageLog,
            IClaimsHelper claimsHelper,
            IBotVisitPlayer botVisitPlayer,
            ITargetSelectorFactory targetSelectorFactory,
            ILeagueService leagueService,
            IKnockoutService knockoutService) : base(unitOfWork, messageLog, claimsHelper)
        {
            _botVisitPlayer = botVisitPlayer;
            _targetSelectorFactory = targetSelectorFactory;
            _leagueService = leagueService;
            _knockoutService = knockoutService;
        }

        public async Task<TournamentMatchState> StartMatchAsync(int matchId)
        {
            try
            {
                var match = await LoadMatchAsync(matchId);
                var user = ClaimsHelper.GetCurrentUserFromClaims();
                TournamentAccess.EnsureCanManage(user, match.Round.Tournament);

                EnsureStartable(match);

                if (IsBot(match.HomeParticipant) && IsBot(match.AwayParticipant))
                {
                    throw new BusinessRuleException("Bot-vs-bot matches start automatically at the scheduled time.");
                }

                await BeginAsync(match, user.UserId);

                // Hat ein Bot den Anwurf, wirft er gleich.
                var engine = Replay(match);
                PlayBotTurns(match, engine);

                await SaveAndFinalizeAsync(match);
                return ToState(match, Replay(match), canEnter: true);
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage($"Error starting tournament match {matchId}.", nameof(TournamentMatchService), exception);
                throw;
            }
        }

        public async Task<TournamentMatchState> AddDartAsync(int matchId, AddTournamentDartModel model)
        {
            try
            {
                var match = await LoadMatchAsync(matchId);
                TournamentAccess.EnsureCanManage(ClaimsHelper.GetCurrentUserFromClaims(), match.Round.Tournament);

                if (match.Status != TournamentMatchStatusEnum.Running)
                {
                    throw new BusinessRuleException("The match is not running.");
                }

                // Zweites Gerät mit veraltetem Stand: lieber 409 als ein Dart am falschen Platz.
                if (model.ConcurrencyStamp is Guid stamp && stamp != match.ConcurrencyStamp)
                {
                    throw new DbUpdateConcurrencyException("The match was changed in the meantime. Reload and enter the dart again.");
                }

                var engine = Replay(match);

                if (IsBot(SideOf(match, engine.CurrentPlayer)))
                {
                    throw new BusinessRuleException("A bot is at the oche.");
                }

                ApplyDart(match, engine, model.Segment, model.Multiplier, null, null);
                PlayBotTurns(match, engine);

                await SaveAndFinalizeAsync(match);
                return ToState(match, Replay(match), canEnter: true);
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage($"Error adding dart to tournament match {matchId}.", nameof(TournamentMatchService), exception);
                throw;
            }
        }

        public async Task<TournamentMatchState> GetMatchStateAsync(int matchId)
        {
            try
            {
                var match = await LoadMatchAsync(matchId, tracking: false);
                var user = ClaimsHelper.GetCurrentUserFromClaims();
                TournamentAccess.EnsureCanView(user, match.Round.Tournament);

                var engine = Replay(match);
                var canEnter = match.Status == TournamentMatchStatusEnum.Running
                               && TournamentAccess.CanManage(user, match.Round.Tournament)
                               && !IsBot(SideOf(match, engine.CurrentPlayer));

                return ToState(match, engine, canEnter);
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage($"Error loading tournament match {matchId}.", nameof(TournamentMatchService), exception);
                throw;
            }
        }

        public async Task<int> PlayDueBotMatchesAsync(DateTime nowUtc)
        {
            var played = 0;

            // Fällige starten: beide Seiten Bots, Termin erreicht, Turnier läuft.
            var due = await Tournaments.TournamentMatchRepository.GetWhereAsync(
                m => m.Status == TournamentMatchStatusEnum.Scheduled
                     && m.HomeParticipant != null && m.HomeParticipant.ParticipantType == TournamentParticipantTypeEnum.Bot
                     && m.AwayParticipant != null && m.AwayParticipant.ParticipantType == TournamentParticipantTypeEnum.Bot
                     && m.Round.ScheduledAt != null && m.Round.ScheduledAt <= nowUtc
                     && m.Round.Tournament.Status == TournamentStatusEnum.Running,
                asNoTracking: true);

            var running = await Tournaments.TournamentMatchRepository.GetWhereAsync(
                m => m.Status == TournamentMatchStatusEnum.Running
                     && m.HomeParticipant!.ParticipantType == TournamentParticipantTypeEnum.Bot
                     && m.AwayParticipant!.ParticipantType == TournamentParticipantTypeEnum.Bot,
                asNoTracking: true);

            foreach (var id in due.Select(m => m.Id).Concat(running.Select(m => m.Id)).Distinct())
            {
                try
                {
                    var match = await LoadMatchAsync(id);

                    if (match.Status == TournamentMatchStatusEnum.Scheduled)
                    {
                        await BeginAsync(match, null);
                    }

                    var engine = Replay(match);

                    if (!engine.IsFinished)
                    {
                        PlayBotVisit(match, engine);
                        played++;
                    }

                    await SaveAndFinalizeAsync(match);
                }
                catch (Exception exception)
                {
                    // Ein fehlerhaftes Match hält die anderen nicht auf.
                    await MessageLog.LogMessage($"Error playing bot match {id}.", nameof(TournamentMatchService), exception);
                }
            }

            return played;
        }

        // ---------- Ablauf ----------

        private static void EnsureStartable(TournamentMatchEntity match)
        {
            if (match.Round.Tournament.Status != TournamentStatusEnum.Running)
            {
                throw new BusinessRuleException("The tournament is not running.");
            }

            if (match.Status != TournamentMatchStatusEnum.Scheduled || match.HomeParticipant is null || match.AwayParticipant is null)
            {
                throw new BusinessRuleException("The match cannot be started (already started, or opponents not known yet).");
            }
        }

        /// <summary>Status Running, Startzeit, Spielleiter, Bot-Profil und aktives Zielwahl-Modell je Seite.</summary>
        private async Task BeginAsync(TournamentMatchEntity match, int? conductorUserId)
        {
            var activeModel = await UnitOfWork.DartBotUnitOfWork.DartBotActiveModelRepository.GetFirstOrDefaultAsync(
                a => a.ModelType == DartBotModelTypeEnum.TargetSelection,
                asNoTracking: true);

            if (IsBot(match.HomeParticipant))
            {
                match.HomeProfileVersionId = await LatestProfileVersionIdAsync(match.HomeParticipant!.DartBotId!.Value);
                match.HomeTargetModelId = activeModel?.ModelId;
            }

            if (IsBot(match.AwayParticipant))
            {
                match.AwayProfileVersionId = await LatestProfileVersionIdAsync(match.AwayParticipant!.DartBotId!.Value);
                match.AwayTargetModelId = activeModel?.ModelId;
            }

            match.Status = TournamentMatchStatusEnum.Running;
            match.StartedAt = DateTime.UtcNow;
            match.ConductedByUserId = conductorUserId;

            await UnitOfWork.SaveChangesAsync();

            // Navigationen für die Bot-Züge nachladen.
            await LoadSnapshotsAsync(match);
        }

        private async Task<int> LatestProfileVersionIdAsync(int botId)
        {
            var versions = await UnitOfWork.DartBotUnitOfWork.DartBotProfileVersionRepository.GetWhereAsync(v => v.DartBotId == botId, asNoTracking: true);
            return versions.MaxBy(v => v.VersionNumber)?.Id ?? throw new InvalidOperationException($"Dart bot {botId} has no profile version.");
        }

        private async Task LoadSnapshotsAsync(TournamentMatchEntity match)
        {
            var bots = UnitOfWork.DartBotUnitOfWork;

            if (match.HomeProfileVersionId is int hp && match.HomeProfileVersion is null)
            {
                match.HomeProfileVersion = await bots.DartBotProfileVersionRepository.GetByIdAsync(hp);
            }

            if (match.AwayProfileVersionId is int ap && match.AwayProfileVersion is null)
            {
                match.AwayProfileVersion = await bots.DartBotProfileVersionRepository.GetByIdAsync(ap);
            }

            if (match.HomeTargetModelId is int hm && match.HomeTargetModel is null)
            {
                match.HomeTargetModel = await bots.DartBotModelRepository.GetByIdAsync(hm);
            }

            if (match.AwayTargetModelId is int am && match.AwayTargetModel is null)
            {
                match.AwayTargetModel = await bots.DartBotModelRepository.GetByIdAsync(am);
            }
        }

        /// <summary>Bots werfen, bis ein Mensch dran ist oder das Match endet (Mensch gegen Bot: genau die Bot-Aufnahme).</summary>
        private void PlayBotTurns(TournamentMatchEntity match, X01Match engine)
        {
            // Bot gegen Bot läuft nur über den Hintergrunddienst (eine Aufnahme je Takt).
            if (IsBot(match.HomeParticipant) && IsBot(match.AwayParticipant))
            {
                return;
            }

            while (!engine.IsFinished && IsBot(SideOf(match, engine.CurrentPlayer)))
            {
                PlayBotVisit(match, engine);
            }
        }

        private void PlayBotVisit(TournamentMatchEntity match, X01Match engine)
        {
            var player = engine.CurrentPlayer;
            var opponent = player == 1 ? 2 : 1;
            var profile = (player == 1 ? match.HomeProfileVersion : match.AwayProfileVersion)
                ?? throw new InvalidOperationException("Bot profile version is not loaded.");
            var model = player == 1 ? match.HomeTargetModel : match.AwayTargetModel;

            var result = _botVisitPlayer.PlayVisit(
                new BotVisitContext(
                    engine.Remaining(player),
                    engine.Remaining(opponent),
                    new BotSkillProfile(profile.ThrowPrecision, profile.ThrowConsistency, profile.TacticalIntelligence, profile.PressureResistance, profile.RiskTaking),
                    match.Round.Tournament.InMode,
                    match.Round.Tournament.OutMode,
                    engine.IsOpened(player)),
                _targetSelectorFactory.For(model));

            foreach (var dart in result.Throws)
            {
                var outcome = ApplyDart(match, engine, dart.Segment, dart.Multiplier, dart.TargetSegment, dart.TargetMultiplier);

                if (outcome.VisitEnded)
                {
                    return;
                }
            }

            // Sicherheitsnetz: die Aufnahme muss enden, auch wenn die Simulation weniger Darts liefert.
            while (!engine.IsFinished && engine.CurrentPlayer == player && engine.CurrentVisit.Count > 0)
            {
                ApplyDart(match, engine, 0, 0, null, null);
            }
        }

        /// <summary>Ein Dart: Regeln über <see cref="X01Match"/>, gespeichert als Leg/Aufnahme/Wurf.</summary>
        private static X01DartOutcome ApplyDart(TournamentMatchEntity match, X01Match engine, int segment, int multiplier, int? targetSegment, int? targetMultiplier)
        {
            var legNumber = engine.LegNumber;
            var starter = engine.LegStarter;
            var throwNumber = engine.CurrentVisit.Count + 1;
            var visitStart = engine.VisitStartScore;
            var player = engine.CurrentPlayer;
            var now = DateTime.UtcNow;

            var outcome = engine.Throw(segment, multiplier);

            var leg = match.Legs.FirstOrDefault(l => l.LegNumber == legNumber);
            if (leg is null)
            {
                leg = new TournamentMatchLegEntity { LegNumber = legNumber, StartingPlayerNumber = starter, StartedAt = now };
                match.Legs.Add(leg);
            }

            TournamentMatchVisitEntity visit;
            if (throwNumber == 1)
            {
                visit = new TournamentMatchVisitEntity
                {
                    PlayerNumber = player,
                    VisitNumber = leg.Visits.Count + 1,
                    ScoreBefore = visitStart
                };
                leg.Visits.Add(visit);
            }
            else
            {
                visit = leg.Visits.MaxBy(v => v.VisitNumber)!;
            }

            visit.Throws.Add(new TournamentMatchThrowEntity
            {
                ThrowNumber = throwNumber,
                Segment = segment,
                Multiplier = multiplier,
                Score = outcome.Score,
                ScoreBeforeThrow = outcome.ScoreBeforeThrow,
                TargetSegment = targetSegment,
                TargetMultiplier = targetMultiplier
            });

            visit.ScoreAfter = outcome.RemainingAfter;
            visit.IsBust = outcome.IsBust;
            visit.IsCheckout = outcome.IsCheckout;
            visit.DartsUsed = throwNumber;

            if (outcome.LegWinner is int legWinner)
            {
                leg.WinnerPlayerNumber = legWinner;
                leg.FinishedAt = now;
            }

            match.HomeLegs = engine.LegsWon(1);
            match.AwayLegs = engine.LegsWon(2);

            if (outcome.MatchWinner is int winner)
            {
                match.Status = TournamentMatchStatusEnum.Finished;
                match.FinishedAt = now;
                match.WinnerParticipantId = winner == 1 ? match.HomeParticipantId : match.AwayParticipantId;
            }

            // Jeder Dart ändert das Match: neuer Stempel, damit ein zweites Gerät mit altem Stand 409 bekommt.
            match.ConcurrencyStamp = Guid.NewGuid();

            return outcome;
        }

        private async Task SaveAndFinalizeAsync(TournamentMatchEntity match)
        {
            await UnitOfWork.SaveChangesAsync();

            if (match.Status != TournamentMatchStatusEnum.Finished)
            {
                return;
            }

            var tournament = match.Round.Tournament;

            if (tournament.Type == TournamentTypeEnum.League)
            {
                await _leagueService.FinishLeagueIfCompleteAsync(tournament.Id);
            }
            else
            {
                await _knockoutService.AdvanceAsync(tournament.Id);
            }
        }

        // ---------- Laden und Abbilden ----------

        private async Task<TournamentMatchEntity> LoadMatchAsync(int matchId, bool tracking = true)
        {
            return await Tournaments.TournamentMatchRepository.GetFirstOrDefaultAsync(
                m => m.Id == matchId,
                asNoTracking: !tracking,
                include: q => q
                    .Include(m => m.Round).ThenInclude(r => r.Tournament)
                    .Include(m => m.HomeParticipant)
                    .Include(m => m.AwayParticipant)
                    .Include(m => m.HomeProfileVersion)
                    .Include(m => m.AwayProfileVersion)
                    .Include(m => m.HomeTargetModel)
                    .Include(m => m.AwayTargetModel)
                    .Include(m => m.Legs).ThenInclude(l => l.Visits).ThenInclude(v => v.Throws))
                ?? throw new ArgumentException($"Tournament match {matchId} not found.");
        }

        private static X01Match Replay(TournamentMatchEntity match)
        {
            var tournament = match.Round.Tournament;
            var darts = match.Legs
                .OrderBy(l => l.LegNumber)
                .SelectMany(l => l.Visits.OrderBy(v => v.VisitNumber))
                .SelectMany(v => v.Throws.OrderBy(t => t.ThrowNumber))
                .Select(t => (t.Segment, t.Multiplier));

            return X01Match.Replay(tournament.StartingScore, tournament.InMode, tournament.OutMode, match.Round.BestOfLegs, darts);
        }

        private static TournamentParticipantEntity? SideOf(TournamentMatchEntity match, int player) =>
            player == 1 ? match.HomeParticipant : match.AwayParticipant;

        private static bool IsBot(TournamentParticipantEntity? participant) =>
            participant?.ParticipantType == TournamentParticipantTypeEnum.Bot;

        private static TournamentMatchState ToState(TournamentMatchEntity match, X01Match engine, bool canEnter)
        {
            var tournament = match.Round.Tournament;
            var visits = match.Legs.SelectMany(l => l.Visits).ToList();

            TournamentMatchSide Side(int player, TournamentParticipantEntity? participant)
            {
                var own = visits.Where(v => v.PlayerNumber == player).ToList();
                var darts = own.Sum(v => v.DartsUsed);

                return new TournamentMatchSide
                {
                    ParticipantId = participant?.Id,
                    DisplayName = participant?.DisplayName ?? "offen",
                    ParticipantType = participant?.ParticipantType,
                    LegsWon = engine.LegsWon(player),
                    Remaining = engine.Remaining(player),
                    IsOpened = engine.IsOpened(player),
                    Average = darts == 0 ? 0 : Math.Round(own.Sum(v => v.ScoreBefore - v.ScoreAfter) * 3.0 / darts, 2)
                };
            }

            return new TournamentMatchState
            {
                MatchId = match.Id,
                TournamentId = tournament.Id,
                TournamentName = tournament.Name,
                TournamentType = tournament.Type,
                RoundName = match.Round.Name,
                BestOfLegs = match.Round.BestOfLegs,
                LegsToWin = X01SetRules.LegsToWin(match.Round.BestOfLegs),
                StartingScore = tournament.StartingScore,
                InMode = tournament.InMode,
                OutMode = tournament.OutMode,
                Status = match.Status,
                ScheduledAt = match.Round.ScheduledAt,
                Home = Side(1, match.HomeParticipant),
                Away = Side(2, match.AwayParticipant),
                CurrentPlayer = engine.CurrentPlayer,
                LegNumber = engine.LegNumber,
                LegStarter = engine.LegStarter,
                CurrentVisit = engine.CurrentVisit.Select(d => new TournamentDartItem { Segment = d.Segment, Multiplier = d.Multiplier, Score = d.Segment * d.Multiplier }).ToList(),
                CanEnterDarts = canEnter && match.Status == TournamentMatchStatusEnum.Running && !IsBot(SideOf(match, engine.CurrentPlayer)),
                WinnerParticipantId = match.WinnerParticipantId,
                ConcurrencyStamp = match.ConcurrencyStamp,
                Legs = match.Legs
                    .OrderBy(l => l.LegNumber)
                    .Select(l => new TournamentLegItem
                    {
                        LegNumber = l.LegNumber,
                        StartingPlayer = l.StartingPlayerNumber,
                        WinnerPlayer = l.WinnerPlayerNumber,
                        Visits = l.Visits
                            .OrderBy(v => v.VisitNumber)
                            .Select(v => new TournamentVisitItem
                            {
                                PlayerNumber = v.PlayerNumber,
                                VisitNumber = v.VisitNumber,
                                ScoreBefore = v.ScoreBefore,
                                ScoreAfter = v.ScoreAfter,
                                IsBust = v.IsBust,
                                IsCheckout = v.IsCheckout,
                                Darts = v.Throws.OrderBy(t => t.ThrowNumber)
                                    .Select(t => new TournamentDartItem { Segment = t.Segment, Multiplier = t.Multiplier, Score = t.Score })
                                    .ToList()
                            })
                            .ToList()
                    })
                    .ToList()
            };
        }
    }
}
