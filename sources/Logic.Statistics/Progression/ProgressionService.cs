using Data.DartContext.Entities.User;
using Logic.Shared;
using Microsoft.EntityFrameworkCore;
using Shared.Enums.Bot;
using Shared.Enums.Statistics;
using Shared.Enums.Tournament;
using Shared.Interfaces.Authentication;
using Shared.Interfaces.Logger;
using Shared.Interfaces.Progression;
using Shared.Interfaces.UnitsOfWork;
using Shared.Models.Progression;

namespace Logic.Statistics.Progression
{
    /// <summary>
    /// Level, Erfolge und Rekorde des Spielers aus dem ganzen Verlauf (idempotent):
    /// - Level/XP: nur beendete Spiele (Bot-Spiele gegen Menschen, Turnier-Matches), am Benutzer gespeichert.
    /// - Erfolge: Katalog im Code; neu erreichte werden mit dem Datum des auslösenden Spiels gespeichert.
    /// - Rekorde: werden bei jedem Abruf berechnet.
    /// </summary>
    public class ProgressionService : ALogicBase, IProgressionService
    {
        private readonly IClaimsHelper _claimsHelper;

        public ProgressionService(IUnitOfWork unitOfWork, IMessageLog messageLog, IClaimsHelper claimsHelper) : base(unitOfWork, messageLog)
        {
            _claimsHelper = claimsHelper;
        }

        public async Task<List<AchievementItem>> EvaluateAsync(int userId)
        {
            try
            {
                var (_, newlyUnlocked) = await EvaluateCoreAsync(userId);
                return newlyUnlocked;
            }
            catch (Exception exception)
            {
                // Der Fortschritt darf ein beendetes Spiel nie scheitern lassen.
                await MessageLog.LogMessage($"Error evaluating progression of user {userId}.", nameof(ProgressionService), exception);
                return [];
            }
        }

        public async Task<PlayerProgress> GetMyProgressAsync()
        {
            try
            {
                var currentUser = _claimsHelper.GetCurrentUserFromClaims();

                if (currentUser == null || currentUser.UserId == 0)
                {
                    throw new UnauthorizedAccessException("Current user is not authenticated.");
                }

                var (facts, _) = await EvaluateCoreAsync(currentUser.UserId);
                var user = await UnitOfWork.UserUnitOfWork.UserRepository.GetByIdAsync(currentUser.UserId);
                var unlocked = await UnitOfWork.UserUnitOfWork.UserAchievementRepository.GetWhereAsync(a => a.UserId == currentUser.UserId);
                var unlockedByKey = unlocked.ToDictionary(a => a.AchievementKey);

                var progress = new PlayerProgress
                {
                    Level = user.Level,
                    ExperiencePoints = user.ExperiencePoints,
                    LevelStartXp = PlayerLevel.XpForLevel(user.Level),
                    NextLevelXp = PlayerLevel.XpForLevel(Math.Min(user.Level + 1, PlayerLevel.MaxLevel)),
                    Records = RecordCalculator.Calculate(facts),
                    Achievements = AchievementCatalog.All.Select(definition =>
                    {
                        var state = definition.Evaluate(facts);
                        return ToItem(definition, state, unlockedByKey.GetValueOrDefault(definition.Key));
                    }).ToList()
                };

                // Neu freigeschaltete einmal melden.
                var unseen = unlocked.Where(a => !a.IsSeen).ToList();
                progress.NewlyUnlocked = progress.Achievements.Where(a => unseen.Any(u => u.AchievementKey == a.Key)).ToList();

                foreach (var achievement in unseen)
                {
                    achievement.IsSeen = true;
                }

                if (unseen.Count > 0)
                {
                    await UnitOfWork.SaveChangesAsync();
                }

                return progress;
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error loading player progress.", nameof(ProgressionService), exception);
                throw;
            }
        }

        private async Task<(ProgressionFacts Facts, List<AchievementItem> NewlyUnlocked)> EvaluateCoreAsync(int userId)
        {
            var facts = await LoadFactsAsync(userId);
            var users = UnitOfWork.UserUnitOfWork;
            var user = await users.UserRepository.GetByIdAsync(userId);

            var experience = PlayerLevel.ExperienceFor(facts);
            var changed = false;

            // Nie Abstieg (z. B. falls ein Spiel später nicht mehr gefunden wird).
            if (experience > user.ExperiencePoints)
            {
                user.ExperiencePoints = experience;
                user.Level = Math.Max(user.Level, PlayerLevel.LevelFor(experience));
                changed = true;
            }

            var existing = (await users.UserAchievementRepository.GetWhereAsync(a => a.UserId == userId, asNoTracking: true))
                .Select(a => a.AchievementKey)
                .ToHashSet();
            var newlyUnlocked = new List<AchievementItem>();

            foreach (var definition in AchievementCatalog.All.Where(d => !existing.Contains(d.Key)))
            {
                var state = definition.Evaluate(facts);

                if (state.AchievedAt is not { } achievedAt)
                {
                    continue;
                }

                var entity = new UserAchievementEntity { UserId = userId, AchievementKey = definition.Key, UnlockedAt = achievedAt };
                await users.UserAchievementRepository.AddAsync(entity);
                newlyUnlocked.Add(ToItem(definition, state, entity));
                changed = true;
            }

            if (changed)
            {
                try
                {
                    await UnitOfWork.SaveChangesAsync();
                }
                catch (DbUpdateException) when (newlyUnlocked.Count > 0)
                {
                    // Parallel ausgewertet (Unique Index): die andere Auswertung hat die Erfolge schon gespeichert.
                    newlyUnlocked.Clear();
                }
            }

            return (facts, newlyUnlocked);
        }

        private async Task<ProgressionFacts> LoadFactsAsync(int userId)
        {
            var loader = new PlayerHistoryLoader(UnitOfWork);
            var (legs, _, _) = await loader.LoadLegsAsync(userId, StatisticsCategoryEnum.Overall);

            var matches = new List<MatchFact>();

            // Bot-Spiele: Gegner (Name, Basis-Ø) aus dem Bot-Spieler desselben Spiels.
            var botGames = await loader.LoadBotGamesAsync(userId);
            var gameIds = botGames.Select(p => p.DartBotGameId).ToList();
            var botPlayers = (await UnitOfWork.DartBotUnitOfWork.DartBotGamePlayerRepository.GetWhereAsync(
                    p => gameIds.Contains(p.DartBotGameId) && p.PlayerType == DartBotPlayerTypeEnum.Bot,
                    asNoTracking: true,
                    include: q => q.Include(p => p.ProfileVersion)))
                .ToDictionary(p => p.DartBotGameId);

            foreach (var player in botGames)
            {
                var bot = botPlayers.GetValueOrDefault(player.DartBotGameId);
                matches.Add(new MatchFact(
                    PlayerHistoryLoader.BotKey(player),
                    player.Game.FinishedAt!.Value,
                    IsTournament: false,
                    IsWon: player.Game.WinnerPlayerId == player.Id,
                    LegsWon: player.Game.Legs.Count(l => l.WinnerPlayerId == player.Id),
                    OpponentName: bot?.DisplayName ?? "Bot",
                    BotOpponentAverage: bot?.ProfileVersion?.BaseAverage,
                    TournamentId: null,
                    WonTournament: false));
            }

            foreach (var match in await loader.LoadTournamentMatchesAsync(userId))
            {
                var own = PlayerHistoryLoader.OwnParticipant(userId, match);
                var opponent = own?.Id == match.HomeParticipantId ? match.AwayParticipant : match.HomeParticipant;
                var tournament = match.Round.Tournament;
                var ownNumber = PlayerHistoryLoader.OwnNumber(userId, match);

                matches.Add(new MatchFact(
                    PlayerHistoryLoader.TournamentKey(match),
                    match.FinishedAt!.Value,
                    IsTournament: true,
                    IsWon: own is not null && match.WinnerParticipantId == own.Id,
                    LegsWon: match.Legs.Count(l => l.WinnerPlayerNumber == ownNumber),
                    OpponentName: opponent?.DisplayName ?? "",
                    BotOpponentAverage: null,
                    TournamentId: tournament.Id,
                    WonTournament: own is not null && tournament.Status == TournamentStatusEnum.Finished && tournament.WinnerParticipantId == own.Id));
            }

            var sessions = await UnitOfWork.DartTrainingUnitOfWork.TrainingSessionRepository.GetWhereAsync(
                s => s.UserId == userId && s.FinishedAt != null,
                asNoTracking: true,
                include: q => q.Include(s => s.AroundTheClockTraining!).ThenInclude(a => a.Visits).ThenInclude(v => v.Throws));

            var trainings = sessions
                .Select(s => new TrainingFact(
                    s.FinishedAt!.Value,
                    s.AroundTheClockTraining is not null,
                    s.AroundTheClockTraining?.Visits.Sum(v => v.Throws.Count)))
                .ToList();

            var user = await UnitOfWork.UserUnitOfWork.UserRepository.GetByIdAsync(userId, asNoTracking: true);

            return new ProgressionFacts
            {
                Legs = legs.OrderBy(l => l.Date).ToList(),
                Matches = matches.OrderBy(m => m.Date).ToList(),
                Trainings = trainings.OrderBy(t => t.Date).ToList(),
                RegisteredAt = user.CreatedAt,
                Now = DateTime.UtcNow
            };
        }

        private static AchievementItem ToItem(AchievementDefinition definition, AchievementState state, UserAchievementEntity? unlocked) => new()
        {
            Key = definition.Key,
            Name = definition.Name,
            Description = definition.Description,
            Category = definition.Category,
            Tier = definition.Tier,
            IsUnlocked = unlocked is not null,
            UnlockedAt = unlocked?.UnlockedAt,
            Progress = unlocked is not null ? definition.Target : state.Progress,
            Target = definition.Target
        };
    }
}
