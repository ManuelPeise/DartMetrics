using Data.DartContext.Entities.Bot;
using Data.DartContext.Entities.Tournament;
using Data.DartContext.Entities.User;
using Shared.Enums.Tournament;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.Enums.Bot;
using Shared.Interfaces.Bot;
using Shared.Interfaces.UnitsOfWork;
using Shared.Models.Options;

namespace Logic.Bot.Learning
{
    /// <summary>
    /// Lernschritt nach einem Spiel gegen einen Menschen:
    /// 1. XP vergeben (Spiel, Sieg, gewonnene Legs).
    /// 2. Elo anpassen – für Bot und Mensch, beide mit den Werten vor dem Spiel.
    /// 3. Level aus der Gesamt-XP bestimmen; bei Aufstieg neue Profilversion mit höheren Fähigkeiten.
    /// RiskTaking und PlayingStyle sind Vorlieben und bleiben beim Aufstieg unverändert.
    /// </summary>
    public class DartBotLearningService : IDartBotLearningService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly DartBotOptions _options;

        public DartBotLearningService(IUnitOfWork unitOfWork, IOptions<DartBotOptions> options)
        {
            _unitOfWork = unitOfWork;
            _options = options.Value;
        }

        public async Task ApplyGameResultAsync(DartBotGameEntity game, DateTime now)
        {
            if (game.Status != DartBotGameStatusEnum.Finished || game.GameMode == DartBotGameModeEnum.BotVsBot)
            {
                return;
            }

            var userPlayer = game.Players.FirstOrDefault(p => p.PlayerType == DartBotPlayerTypeEnum.User);

            if (userPlayer is null)
            {
                return;
            }

            // Beide Seiten rechnen mit den Werten VOR dem Spiel.
            var user = userPlayer.UserId is { } userId
                ? await _unitOfWork.UserUnitOfWork.UserRepository.GetFirstOrDefaultAsync(u => u.Id == userId)
                : null;
            var userRatingBefore = userPlayer.RatingBefore ?? user?.Rating ?? _options.Learning.HumanOpponentRating;
            int? botRatingBefore = null;

            foreach (var botPlayer in game.Players.Where(p => p.PlayerType == DartBotPlayerTypeEnum.Bot))
            {
                botRatingBefore ??= botPlayer.RatingBefore;

                if (botPlayer.LearningProcessedAt == null)
                {
                    botRatingBefore = await ApplyToBotAsync(game, botPlayer, userRatingBefore, now);
                }
            }

            if (user is not null && userPlayer.LearningProcessedAt == null && botRatingBefore is { } opponentRating)
            {
                var actual = game.WinnerPlayer == userPlayer ? 1.0 : 0.0;

                userPlayer.RatingBefore = userRatingBefore;
                userPlayer.RatingAfter = NewRating(userRatingBefore, opponentRating, actual);
                userPlayer.LearningProcessedAt = now;
                user.Rating = userPlayer.RatingAfter.Value;
            }
        }

        /// <summary>3-Dart-Durchschnitt eines Spielers im Spiel (Punkte je Dart × 3), null ohne Darts.</summary>
        private static float? GameAverage(DartBotGameEntity game, DartBotGamePlayerEntity player)
        {
            var visits = game.Legs.SelectMany(l => l.Visits).Where(v => v.DartBotGamePlayerId == player.Id || v.Player == player).ToList();
            var darts = visits.Sum(v => v.DartsUsed);

            return darts == 0 ? null : MathF.Round(visits.Sum(v => v.ScoreBefore - v.ScoreAfter) * 3f / darts, 2);
        }

        /// <summary>Elo-Formel mit K aus den Optionen, nie unter 0.</summary>
        private int NewRating(int ownRating, int opponentRating, double actual)
        {
            var expected = 1.0 / (1.0 + Math.Pow(10, (opponentRating - ownRating) / 400.0));

            return Math.Max(0, (int)Math.Round(ownRating + _options.Learning.EloK * (actual - expected)));
        }

        /// <returns>Elo des Bots vor dem Spiel.</returns>
        private async Task<int> ApplyToBotAsync(DartBotGameEntity game, DartBotGamePlayerEntity botPlayer, int opponentRating, DateTime now)
        {
            var learning = _options.Learning;

            // Bot mit allen Profilversionen getrackt laden (aktuelle Version = höchste VersionNumber).
            var bot = await _unitOfWork.DartBotUnitOfWork.DartBotRepository.GetByIdAsync(
                botPlayer.DartBotId ?? throw new InvalidOperationException("Bot player without bot."),
                include: q => q.Include(b => b.ProfileVersions));

            var isWinner = game.WinnerPlayer == botPlayer;
            var legsWon = game.Legs.Count(l => l.WinnerPlayer == botPlayer);

            // 1. XP
            var xpGained = (long)learning.XpPerGame
                + (isWinner ? learning.XpPerWin : 0)
                + (long)legsWon * learning.XpPerLegWon;

            bot.ExperiencePoints += xpGained;

            // 2. Elo gegen das Elo des Menschen
            var ratingBefore = bot.Rating;
            var ratingAfter = NewRating(ratingBefore, opponentRating, isWinner ? 1.0 : 0.0);

            bot.Rating = ratingAfter;

            // Selbstvertrauen und gespielter Ø für den Verlauf.
            bot.CurrentForm = BotForm.AfterResult(bot.CurrentForm, isWinner);
            botPlayer.AverageInGame = GameAverage(game, botPlayer);

            // 3. Level-Aufstieg
            ApplyLevelUp(bot, game, reasonSuffix: "");

            botPlayer.XpGained = xpGained;
            botPlayer.RatingBefore = ratingBefore;
            botPlayer.RatingAfter = ratingAfter;
            botPlayer.LearningProcessedAt = now;

            return ratingBefore;
        }

        public async Task ApplyTournamentResultAsync(TournamentMatchEntity match, DateTime now)
        {
            if (match.Status != TournamentMatchStatusEnum.Finished
                || match.LearningProcessedAt != null
                || match.Round.Tournament.Kind != TournamentKindEnum.Official
                || match.HomeParticipant is null || match.AwayParticipant is null)
            {
                return;
            }

            var home = await LoadSideAsync(match.HomeParticipant);
            var away = await LoadSideAsync(match.AwayParticipant);

            var homeWon = match.WinnerParticipantId == match.HomeParticipantId;

            // Beide Seiten mit den Werten VOR dem Match.
            var homeBefore = home.Rating;
            var awayBefore = away.Rating;
            var homeAfter = NewRating(homeBefore, awayBefore, homeWon ? 1.0 : 0.0);
            var awayAfter = NewRating(awayBefore, homeBefore, homeWon ? 0.0 : 1.0);

            match.HomeRatingBefore = homeBefore;
            match.HomeRatingAfter = homeAfter;
            match.AwayRatingBefore = awayBefore;
            match.AwayRatingAfter = awayAfter;
            match.HomeXpGained = ApplySide(home, homeAfter, homeWon, match.HomeLegs);
            match.AwayXpGained = ApplySide(away, awayAfter, !homeWon, match.AwayLegs);
            match.LearningProcessedAt = now;
        }

        private sealed record TournamentSide(UserEntity? User, DartBotEntity? Bot)
        {
            public int Rating => Bot?.Rating ?? User?.Rating ?? 0;
        }

        private async Task<TournamentSide> LoadSideAsync(TournamentParticipantEntity participant)
        {
            if (participant.DartBotId is int botId)
            {
                var bot = await _unitOfWork.DartBotUnitOfWork.DartBotRepository.GetByIdAsync(botId, include: q => q.Include(b => b.ProfileVersions));
                return new TournamentSide(null, bot);
            }

            // Anonymisierter Benutzer (UserId null): mit dem Standard-Elo eines menschlichen Gegners rechnen, nichts speichern.
            var user = participant.UserId is int userId
                ? await _unitOfWork.UserUnitOfWork.UserRepository.GetFirstOrDefaultAsync(u => u.Id == userId)
                : null;

            return user is null
                ? new TournamentSide(new UserEntity { Rating = _options.Learning.HumanOpponentRating }, null)
                : new TournamentSide(user, null);
        }

        /// <returns>XP bei Bots, sonst null.</returns>
        private long? ApplySide(TournamentSide side, int ratingAfter, bool isWinner, int legsWon)
        {
            if (side.Bot is not DartBotEntity bot)
            {
                if (side.User is not null && side.User.Id != 0)
                {
                    side.User.Rating = ratingAfter;
                }

                return null;
            }

            var learning = _options.Learning;
            var xpGained = (long)learning.XpPerGame + (isWinner ? learning.XpPerWin : 0) + (long)legsWon * learning.XpPerLegWon;

            bot.ExperiencePoints += xpGained;
            bot.Rating = ratingAfter;
            ApplyLevelUp(bot, null, reasonSuffix: " (Turnier)");

            return xpGained;
        }

        /// <summary>Level aus der Gesamt-XP; bei Aufstieg neue Profilversion mit höheren Fähigkeiten.</summary>
        private void ApplyLevelUp(DartBotEntity bot, DartBotGameEntity? game, string reasonSuffix)
        {
            var newLevel = LevelForExperience(bot.ExperiencePoints);

            if (newLevel > bot.DevelopmentLevel)
            {
                var current = bot.ProfileVersions.MaxBy(p => p.VersionNumber)
                    ?? throw new InvalidOperationException($"Dart bot {bot.Id} has no profile version.");

                var version = CreateLevelUpVersion(current, bot.DevelopmentLevel, newLevel, game);
                GrowAverage(version, bot.PotentialAverage, newLevel - bot.DevelopmentLevel);
                version.ReasonText += reasonSuffix;
                bot.ProfileVersions.Add(version);
                bot.DevelopmentLevel = newLevel;
            }
        }

        public async Task<bool> MergeConcurrentBotUpdatesAsync(DbUpdateConcurrencyException exception)
        {
            if (exception.Entries.Any(e => e.Entity is not DartBotEntity))
            {
                return false;
            }

            foreach (var entry in exception.Entries)
            {
                var bot = (DartBotEntity)entry.Entity;
                var databaseValues = await entry.GetDatabaseValuesAsync();

                if (databaseValues is null)
                {
                    return false;
                }

                // Eigene Änderung als Differenz zum geladenen Stand, auf den neuen Datenbankstand anwenden.
                var original = entry.OriginalValues;
                var xpDelta = bot.ExperiencePoints - original.GetValue<long>(nameof(DartBotEntity.ExperiencePoints));
                var ratingDelta = bot.Rating - original.GetValue<int>(nameof(DartBotEntity.Rating));

                var databaseXp = databaseValues.GetValue<long>(nameof(DartBotEntity.ExperiencePoints));
                var databaseRating = databaseValues.GetValue<int>(nameof(DartBotEntity.Rating));
                var databaseLevel = databaseValues.GetValue<int>(nameof(DartBotEntity.DevelopmentLevel));

                entry.OriginalValues.SetValues(databaseValues);

                bot.ExperiencePoints = databaseXp + xpDelta;
                bot.Rating = Math.Max(0, databaseRating + ratingDelta);

                // Level nie zurückstufen; ein Aufstieg im parallelen Spiel bleibt erhalten.
                bot.DevelopmentLevel = Math.Max(databaseLevel, Math.Max(bot.DevelopmentLevel, LevelForExperience(bot.ExperiencePoints)));
            }

            return true;
        }

        /// <summary>Höchstes Level, dessen XP-Schwelle erreicht ist (gedeckelt auf MaxLevel).</summary>
        public int LevelForExperience(long experiencePoints)
        {
            var level = 0;

            while (level < _options.MaxLevel && experiencePoints >= XpForLevel(level + 1))
            {
                level++;
            }

            return level;
        }

        /// <summary>Benötigte Gesamt-XP für ein Level.</summary>
        public long XpForLevel(int level) =>
            DartBotLevelCurve.XpForLevel(_options, level);

        private DartBotProfileVersionEntity CreateLevelUpVersion(
            DartBotProfileVersionEntity current,
            int previousLevel,
            int newLevel,
            DartBotGameEntity? game)
        {
            return new DartBotProfileVersionEntity
            {
                VersionNumber = current.VersionNumber + 1,
                ThrowPrecision = SkillForLevel(_options.StartProfile.ThrowPrecision, newLevel),
                ThrowConsistency = SkillForLevel(_options.StartProfile.ThrowConsistency, newLevel),
                TacticalIntelligence = SkillForLevel(_options.StartProfile.TacticalIntelligence, newLevel),
                PressureResistance = SkillForLevel(_options.StartProfile.PressureResistance, newLevel),
                RiskTaking = current.RiskTaking,
                PlayingStyle = current.PlayingStyle,
                // Wurf-Steckbrief V2 bleibt (Lernen wirkt bis zur Kalibrierung über ThrowPrecision).
                BaseAverage = current.BaseAverage,
                SigmaMm = current.SigmaMm,
                VerticalRatio = current.VerticalRatio,
                BiasXMm = current.BiasXMm,
                BiasYMm = current.BiasYMm,
                BadDartRate = current.BadDartRate,
                Nervousness = current.Nervousness,
                FavoriteDouble = current.FavoriteDouble,
                WeakDouble = current.WeakDouble,
                ChangeReason = DartBotProfileChangeReasonEnum.LevelUp,
                ReasonText = $"Level {previousLevel} -> {newLevel}",
                PreviousVersion = current,
                TriggeringGame = game
            };
        }

        /// <summary>Anteil des Abstands zum Potenzial, den ein Level-Aufstieg schließt (schnell am Anfang, dann langsamer).</summary>
        public const float GrowthPerLevel = 0.15f;

        /// <summary>
        /// V2: Basis-Ø wächst je Aufstieg Richtung Potenzial, die Streuung folgt der Kalibrierung. Nie Abstieg.
        /// </summary>
        public static void GrowAverage(DartBotProfileVersionEntity version, float? potential, int levelsGained)
        {
            if (version.BaseAverage is not { } average || potential is not { } target || target <= average || levelsGained <= 0)
            {
                return;
            }

            var remaining = 1f - MathF.Pow(1f - GrowthPerLevel, levelsGained);
            var grown = average + (target - average) * remaining;

            version.BaseAverage = MathF.Round(grown, 1);
            version.SigmaMm = MathF.Min(version.SigmaMm ?? float.MaxValue, Simulation.AverageCalibration.SigmaForAverage(grown));
            version.ReasonText += $", Ø {average:0.#} → {grown:0.#}";
        }

        /// <summary>Gleichmäßig vom Startwert (Level 0) bis SkillAtMaxLevel (MaxLevel).</summary>
        private float SkillForLevel(float startValue, int level) =>
            DartBotLevelCurve.SkillForLevel(_options, startValue, level);
    }
}
