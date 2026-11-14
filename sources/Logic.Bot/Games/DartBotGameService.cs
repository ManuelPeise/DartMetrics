using Data.DartContext.Entities.Bot;
using Logic.Shared;
using Logic.Shared.Rules;
using Microsoft.EntityFrameworkCore;
using Shared.Enums.Bot;
using Shared.Interfaces.Authentication;
using Shared.Interfaces.Bot;
using Shared.Interfaces.Logger;
using Shared.Interfaces.UnitsOfWork;
using Shared.Models.Authentication;
using Shared.Models.Bot;

namespace Logic.Bot.Games
{
    /// <summary>
    /// Spielablauf Mensch gegen Bot. Spieler 1 = Mensch, Spieler 2 = Bot.
    /// Jeder Aufruf wird mit genau einem SaveChangesAsync() gespeichert.
    /// Am Spielende wird nur der Status gesetzt; XP, Elo und Lernen übernimmt ein eigener Service.
    /// </summary>
    public class DartBotGameService : ALogicBase, IDartBotGameService
    {
        private const int UserPlayerNumber = 1;
        private const int BotPlayerNumber = 2;
        private const int DartsPerVisit = 3;

        private static readonly int[] AllowedStartingScores = [301, 501];
        private static readonly int[] AllowedBestOfLegs = [1, 3, 5, 7, 9, 11];

        private readonly IClaimsHelper _claimsHelper;
        private readonly IBotVisitPlayer _botVisitPlayer;
        private readonly IDartBotLearningService _learningService;
        private readonly X01Rules _rules = new();

        public DartBotGameService(
            IUnitOfWork unitOfWork,
            IMessageLog messageLog,
            IClaimsHelper claimsHelper,
            IBotVisitPlayer botVisitPlayer,
            IDartBotLearningService learningService) : base(unitOfWork, messageLog)
        {
            _claimsHelper = claimsHelper;
            _botVisitPlayer = botVisitPlayer;
            _learningService = learningService;
        }

        public async Task<DartBotGameState> StartGameAsync(StartDartBotGameModel model)
        {
            try
            {
                var currentUser = GetCurrentUser();

                ValidateStartModel(model);

                var botUnitOfWork = UnitOfWork.DartBotUnitOfWork;

                var bot = await botUnitOfWork.DartBotRepository.GetFirstOrDefaultAsync(
                    b => b.Id == model.DartBotId && b.IsActive,
                    include: q => q.Include(b => b.ProfileVersions))
                    ?? throw new KeyNotFoundException($"Active dart bot with id {model.DartBotId} was not found.");

                // Aktuelle Profilversion = höchste VersionNumber.
                var profileVersion = bot.ProfileVersions.MaxBy(p => p.VersionNumber)
                    ?? throw new InvalidOperationException($"Dart bot {bot.Id} has no profile version.");

                var hasRunningGame = await botUnitOfWork.DartBotGamePlayerRepository.AnyAsync(p =>
                    p.UserId == currentUser.UserId &&
                    p.Game.Status == DartBotGameStatusEnum.Running &&
                    p.Game.Players.Any(other => other.DartBotId == bot.Id));

                if (hasRunningGame)
                {
                    throw new ArgumentException("There is already a running game against this bot. Finish or abort it first.");
                }

                var user = await UnitOfWork.UserUnitOfWork.UserRepository.GetByIdAsync(currentUser.UserId, asNoTracking: true);

                var activeModels = await botUnitOfWork.DartBotActiveModelRepository.GetAllAsync(asNoTracking: true);

                var now = DateTime.UtcNow;

                var userPlayer = new DartBotGamePlayerEntity
                {
                    PlayerNumber = UserPlayerNumber,
                    PlayerType = DartBotPlayerTypeEnum.User,
                    UserId = user.Id,
                    DisplayName = user.Alias
                };

                var botPlayer = new DartBotGamePlayerEntity
                {
                    PlayerNumber = BotPlayerNumber,
                    PlayerType = DartBotPlayerTypeEnum.Bot,
                    DartBot = bot,
                    ProfileVersion = profileVersion,
                    // Kein aktives Modell -> regelbasierte Zielwahl; das Spiel behält die Modelle vom Start.
                    TargetModelId = activeModels.FirstOrDefault(m => m.ModelType == DartBotModelTypeEnum.TargetSelection)?.ModelId,
                    ThrowModelId = activeModels.FirstOrDefault(m => m.ModelType == DartBotModelTypeEnum.ThrowPrediction)?.ModelId,
                    DisplayName = bot.Name
                };

                var game = new DartBotGameEntity
                {
                    GameMode = model.GameMode,
                    Status = DartBotGameStatusEnum.Running,
                    StartingScore = model.StartingScore,
                    BestOfLegs = model.BestOfLegs,
                    StartedAt = now,
                    Players = [userPlayer, botPlayer]
                };

                StartLeg(game, legNumber: 1, model.UserStarts ? userPlayer : botPlayer, now);

                await botUnitOfWork.DartBotGameRepository.AddAsync(game);

                // Hat der Bot den Anwurf, spielt er sofort.
                PlayBotUntilUserTurn(game, now);

                await UnitOfWork.SaveChangesAsync();

                return BuildState(game);
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error starting dart bot game.", nameof(DartBotGameService), exception);

                throw;
            }
        }

        public async Task<DartBotGameState> AddUserVisitAsync(DartBotUserVisitModel model)
        {
            try
            {
                var currentUser = GetCurrentUser();

                var game = await LoadGameAsync(model.GameId, currentUser.UserId);

                if (game.Status != DartBotGameStatusEnum.Running)
                {
                    throw new ArgumentException("The game is not running.");
                }

                var leg = GetCurrentLeg(game);
                var userPlayer = GetPlayer(game, UserPlayerNumber);

                if (GetPlayerOnTurn(game, leg) != userPlayer)
                {
                    throw new ArgumentException("It is not the user's turn.");
                }

                var now = DateTime.UtcNow;

                var visit = CreateUserVisit(game, leg, userPlayer, model.Throws);
                leg.Visits.Add(visit);

                if (visit.IsCheckout)
                {
                    FinishLeg(game, leg, userPlayer, now);
                }

                PlayBotUntilUserTurn(game, now);

                // Spiel entschieden -> Lernschritt in derselben Transaktion (genau einmal, siehe LearningProcessedAt).
                if (game.Status == DartBotGameStatusEnum.Finished)
                {
                    await _learningService.ApplyGameResultAsync(game, now);
                }

                await UnitOfWork.SaveChangesAsync();

                return BuildState(game);
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage($"Error adding user visit to dart bot game {model.GameId}.", nameof(DartBotGameService), exception);

                throw;
            }
        }

        public async Task AbortGameAsync(int gameId)
        {
            try
            {
                var currentUser = GetCurrentUser();

                var game = await LoadGameAsync(gameId, currentUser.UserId);

                if (game.Status != DartBotGameStatusEnum.Running)
                {
                    throw new ArgumentException("Only running games can be aborted.");
                }

                var now = DateTime.UtcNow;

                foreach (var openLeg in game.Legs.Where(l => l.FinishedAt == null))
                {
                    openLeg.FinishedAt = now;
                }

                game.Status = DartBotGameStatusEnum.Aborted;
                game.FinishedAt = now;

                await UnitOfWork.SaveChangesAsync();
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage($"Error aborting dart bot game {gameId}.", nameof(DartBotGameService), exception);

                throw;
            }
        }

        public async Task<DartBotGameState> GetGameAsync(int gameId)
        {
            try
            {
                var currentUser = GetCurrentUser();

                // Getrackt geladen (ohne Speichern), damit EF alle Navigationen verknüpft.
                var game = await LoadGameAsync(gameId, currentUser.UserId);

                return BuildState(game);
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage($"Error loading dart bot game {gameId}.", nameof(DartBotGameService), exception);

                throw;
            }
        }

        public async Task<List<DartBotSummary>> GetAvailableBotsAsync()
        {
            try
            {
                GetCurrentUser();

                var bots = await UnitOfWork.DartBotUnitOfWork.DartBotRepository.GetWhereAsync(b => b.IsActive, asNoTracking: true);

                return bots
                    .OrderBy(b => b.DevelopmentLevel)
                    .ThenBy(b => b.Name)
                    .Select(b => new DartBotSummary
                    {
                        Id = b.Id,
                        Name = b.Name,
                        Alias = b.Alias,
                        DevelopmentLevel = b.DevelopmentLevel,
                        Rating = b.Rating
                    })
                    .ToList();
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error loading available dart bots.", nameof(DartBotGameService), exception);

                throw;
            }
        }

        // ------------------------------------------------------------------
        // Spielablauf
        // ------------------------------------------------------------------

        /// <summary>Solange das Spiel läuft und der Bot am Zug ist, spielt er (auch über ein neues Leg hinweg).</summary>
        private void PlayBotUntilUserTurn(DartBotGameEntity game, DateTime now)
        {
            var botPlayer = GetPlayer(game, BotPlayerNumber);

            while (game.Status == DartBotGameStatusEnum.Running)
            {
                var leg = GetCurrentLeg(game);

                if (GetPlayerOnTurn(game, leg) != botPlayer)
                {
                    return;
                }

                var visit = PlayBotVisit(game, leg, botPlayer);
                leg.Visits.Add(visit);

                if (visit.IsCheckout)
                {
                    FinishLeg(game, leg, botPlayer, now);
                }
            }
        }

        private DartBotGameVisitEntity PlayBotVisit(DartBotGameEntity game, DartBotGameLegEntity leg, DartBotGamePlayerEntity botPlayer)
        {
            var userPlayer = GetPlayer(game, UserPlayerNumber);
            var profile = botPlayer.ProfileVersion
                ?? throw new InvalidOperationException("Bot player has no profile version loaded.");

            var scoreBefore = GetRemaining(game, leg, botPlayer);
            var opponentScoreBefore = GetRemaining(game, leg, userPlayer);

            var result = _botVisitPlayer.PlayVisit(new BotVisitContext(
                scoreBefore,
                opponentScoreBefore,
                new BotSkillProfile(
                    profile.ThrowPrecision,
                    profile.ThrowConsistency,
                    profile.TacticalIntelligence,
                    profile.PressureResistance,
                    profile.RiskTaking)));

            var visit = NewVisit(game, leg, botPlayer, scoreBefore, opponentScoreBefore);
            visit.ScoreAfter = result.ScoreAfter;
            visit.IsBust = result.IsBust;
            visit.IsCheckout = result.IsCheckout;
            visit.DartsUsed = result.Throws.Count;

            foreach (var dart in result.Throws)
            {
                visit.Throws.Add(new DartBotGameThrowEntity
                {
                    ThrowNumber = dart.ThrowNumber,
                    TargetSegment = dart.TargetSegment,
                    TargetMultiplier = dart.TargetMultiplier,
                    TargetX = dart.TargetX,
                    TargetY = dart.TargetY,
                    Segment = dart.Segment,
                    Multiplier = dart.Multiplier,
                    Score = dart.Score,
                    HitX = dart.HitX,
                    HitY = dart.HitY,
                    ScoreBeforeThrow = dart.ScoreBeforeThrow,
                    DartsLeftInVisit = dart.DartsLeftInVisit
                });
            }

            return visit;
        }

        /// <summary>
        /// Prüft und erstellt die Aufnahme des Menschen nach X01Rules (Double-Out).
        /// Drei Darts, außer Bust oder Checkout beenden die Aufnahme vorher.
        /// </summary>
        private DartBotGameVisitEntity CreateUserVisit(
            DartBotGameEntity game,
            DartBotGameLegEntity leg,
            DartBotGamePlayerEntity userPlayer,
            List<DartBotUserThrowModel>? throws)
        {
            if (throws is null || throws.Count is < 1 or > DartsPerVisit)
            {
                throw new ArgumentException($"A visit must contain 1 to {DartsPerVisit} darts.");
            }

            var botPlayer = GetPlayer(game, BotPlayerNumber);
            var scoreBefore = GetRemaining(game, leg, userPlayer);

            var visit = NewVisit(game, leg, userPlayer, scoreBefore, GetRemaining(game, leg, botPlayer));

            var remaining = scoreBefore;
            var visitScore = 0;
            var isFinished = false;

            for (var index = 0; index < throws.Count; index++)
            {
                if (isFinished)
                {
                    throw new ArgumentException("No further darts are allowed after a bust or checkout.");
                }

                var dart = throws[index];
                var throwNumber = index + 1;

                if (!_rules.IsValidSegment(dart.Segment) || !_rules.IsValidMultiplier(dart.Segment, dart.Multiplier))
                {
                    throw new ArgumentException($"Dart {throwNumber} is not a valid throw ({dart.Segment} x {dart.Multiplier}).");
                }

                var score = _rules.CalculateScore(dart.Segment, dart.Multiplier);

                visit.Throws.Add(new DartBotGameThrowEntity
                {
                    ThrowNumber = throwNumber,
                    Segment = dart.Segment,
                    Multiplier = dart.Multiplier,
                    Score = score,
                    ScoreBeforeThrow = remaining,
                    DartsLeftInVisit = DartsPerVisit - throwNumber
                });

                if (_rules.IsBust(scoreBefore, visitScore + score, dart.Segment, dart.Multiplier))
                {
                    visit.IsBust = true;
                    isFinished = true;
                    continue;
                }

                if (_rules.IsCheckout(scoreBefore, visitScore + score, dart.Segment, dart.Multiplier))
                {
                    visit.IsCheckout = true;
                    isFinished = true;
                    continue;
                }

                visitScore += score;
                remaining -= score;
            }

            if (!isFinished && throws.Count < DartsPerVisit)
            {
                throw new ArgumentException($"A visit needs {DartsPerVisit} darts unless it ends with a bust or checkout. Record misses as 0 x 0.");
            }

            visit.ScoreAfter = visit.IsBust ? scoreBefore : visit.IsCheckout ? 0 : remaining;
            visit.DartsUsed = visit.Throws.Count;

            return visit;
        }

        private static DartBotGameVisitEntity NewVisit(
            DartBotGameEntity game,
            DartBotGameLegEntity leg,
            DartBotGamePlayerEntity player,
            int scoreBefore,
            int opponentScoreBefore)
        {
            return new DartBotGameVisitEntity
            {
                // DartBotGameId wird von EF aus Leg und Player übernommen (zusammengesetzte FKs).
                Leg = leg,
                Player = player,
                TurnNumber = leg.Visits.Count + 1,
                VisitNumber = leg.Visits.Count(v => v.Player == player) + 1,
                ScoreBefore = scoreBefore,
                OpponentScoreBefore = opponentScoreBefore
            };
        }

        /// <summary>Leg abschließen; ist das Spiel entschieden, Spiel abschließen, sonst nächstes Leg mit wechselndem Anwurf.</summary>
        private static void FinishLeg(DartBotGameEntity game, DartBotGameLegEntity leg, DartBotGamePlayerEntity winner, DateTime now)
        {
            leg.WinnerPlayer = winner;
            leg.FinishedAt = now;

            var legsToWin = game.BestOfLegs / 2 + 1;
            var legsWon = game.Legs.Count(l => l.WinnerPlayer == winner);

            if (legsWon >= legsToWin)
            {
                game.WinnerPlayer = winner;
                game.Status = DartBotGameStatusEnum.Finished;
                game.FinishedAt = now;
                return;
            }

            var firstLegStarter = game.Legs.Single(l => l.LegNumber == 1).StartingPlayer;
            var otherPlayer = game.Players.Single(p => p != firstLegStarter);
            var nextLegNumber = leg.LegNumber + 1;

            // Anwurf wechselt je Leg: ungerade Legs wie Leg 1, gerade Legs der andere Spieler.
            StartLeg(game, nextLegNumber, nextLegNumber % 2 == 1 ? firstLegStarter : otherPlayer, now);
        }

        private static void StartLeg(DartBotGameEntity game, int legNumber, DartBotGamePlayerEntity startingPlayer, DateTime now)
        {
            game.Legs.Add(new DartBotGameLegEntity
            {
                LegNumber = legNumber,
                StartingPlayer = startingPlayer,
                StartedAt = now
            });
        }

        // ------------------------------------------------------------------
        // Hilfsmethoden
        // ------------------------------------------------------------------

        private CurrentUser GetCurrentUser()
        {
            var currentUser = _claimsHelper.GetCurrentUserFromClaims();

            if (currentUser == null || currentUser.UserId == 0)
            {
                throw new UnauthorizedAccessException("Current user not found.");
            }

            return currentUser;
        }

        private static void ValidateStartModel(StartDartBotGameModel model)
        {
            if (!AllowedStartingScores.Contains(model.StartingScore))
            {
                throw new ArgumentException("Starting score must be 301 or 501.");
            }

            if (!AllowedBestOfLegs.Contains(model.BestOfLegs))
            {
                throw new ArgumentException($"Best of legs must be one of: {string.Join(", ", AllowedBestOfLegs)}.");
            }

            if (model.GameMode is not (DartBotGameModeEnum.Training or DartBotGameModeEnum.Match or DartBotGameModeEnum.Tournament))
            {
                throw new ArgumentException("Only Training, Match or Tournament games can be started against a bot.");
            }
        }

        private async Task<DartBotGameEntity> LoadGameAsync(int gameId, int userId)
        {
            var game = await UnitOfWork.DartBotUnitOfWork.DartBotGameRepository.GetFirstOrDefaultAsync(
                g => g.Id == gameId && g.Players.Any(p => p.UserId == userId),
                asNoTracking: false,
                include: q => q
                    .Include(g => g.Players).ThenInclude(p => p.ProfileVersion)
                    .Include(g => g.Legs).ThenInclude(l => l.Visits).ThenInclude(v => v.Throws)
                    .AsSplitQuery());

            return game ?? throw new KeyNotFoundException($"Dart bot game with id {gameId} was not found.");
        }

        private static DartBotGamePlayerEntity GetPlayer(DartBotGameEntity game, int playerNumber) =>
            game.Players.Single(p => p.PlayerNumber == playerNumber);

        private static DartBotGameLegEntity GetCurrentLeg(DartBotGameEntity game) =>
            game.Legs.MaxBy(l => l.LegNumber)
            ?? throw new InvalidOperationException($"Dart bot game {game.Id} has no leg.");

        /// <summary>Wer ist am Zug? Ohne Aufnahme der Anwerfer, sonst der Gegner der letzten Aufnahme.</summary>
        private static DartBotGamePlayerEntity GetPlayerOnTurn(DartBotGameEntity game, DartBotGameLegEntity leg)
        {
            var lastVisit = leg.Visits.MaxBy(v => v.TurnNumber);

            if (lastVisit is null)
            {
                return leg.StartingPlayer;
            }

            return game.Players.Single(p => p != lastVisit.Player);
        }

        private static int GetRemaining(DartBotGameEntity game, DartBotGameLegEntity leg, DartBotGamePlayerEntity player)
        {
            var lastVisit = leg.Visits
                .Where(v => v.Player == player)
                .MaxBy(v => v.TurnNumber);

            return lastVisit?.ScoreAfter ?? game.StartingScore;
        }

        private static DartBotGameState BuildState(DartBotGameEntity game)
        {
            var userPlayer = GetPlayer(game, UserPlayerNumber);
            var botPlayer = GetPlayer(game, BotPlayerNumber);
            var leg = GetCurrentLeg(game);

            return new DartBotGameState
            {
                GameId = game.Id,
                GameMode = game.GameMode,
                Status = game.Status,
                StartingScore = game.StartingScore,
                BestOfLegs = game.BestOfLegs,
                BotId = botPlayer.DartBotId ?? 0,
                BotName = botPlayer.DisplayName,
                UserLegsWon = game.Legs.Count(l => l.WinnerPlayer == userPlayer),
                BotLegsWon = game.Legs.Count(l => l.WinnerPlayer == botPlayer),
                CurrentLegNumber = leg.LegNumber,
                UserRemaining = GetRemaining(game, leg, userPlayer),
                BotRemaining = GetRemaining(game, leg, botPlayer),
                IsUserTurn = game.Status == DartBotGameStatusEnum.Running && GetPlayerOnTurn(game, leg) == userPlayer,
                IsUserWinner = game.Status == DartBotGameStatusEnum.Finished ? game.WinnerPlayer == userPlayer : null,
                BotXpGained = botPlayer.XpGained,
                BotRatingBefore = botPlayer.RatingBefore,
                BotRatingAfter = botPlayer.RatingAfter,
                StartedAt = game.StartedAt,
                FinishedAt = game.FinishedAt,
                CurrentLegVisits = leg.Visits
                    .OrderBy(v => v.TurnNumber)
                    .Select(v => new DartBotVisitSummary
                    {
                        LegNumber = leg.LegNumber,
                        TurnNumber = v.TurnNumber,
                        IsBot = v.Player == botPlayer,
                        ScoreBefore = v.ScoreBefore,
                        ScoreAfter = v.ScoreAfter,
                        IsBust = v.IsBust,
                        IsCheckout = v.IsCheckout,
                        DartsUsed = v.DartsUsed,
                        Throws = v.Throws
                            .OrderBy(t => t.ThrowNumber)
                            .Select(t => new DartBotThrowSummary
                            {
                                ThrowNumber = t.ThrowNumber,
                                Segment = t.Segment,
                                Multiplier = t.Multiplier,
                                Score = t.Score,
                                TargetSegment = t.TargetSegment,
                                TargetMultiplier = t.TargetMultiplier
                            })
                            .ToList()
                    })
                    .ToList()
            };
        }
    }
}
