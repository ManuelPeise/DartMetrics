using Data.DartContext.Entities.Bot;
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
    /// 2. Elo anpassen.
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

            var hasHumanOpponent = game.Players.Any(p => p.PlayerType == DartBotPlayerTypeEnum.User);

            if (!hasHumanOpponent)
            {
                return;
            }

            foreach (var botPlayer in game.Players.Where(p => p.PlayerType == DartBotPlayerTypeEnum.Bot && p.LearningProcessedAt == null))
            {
                await ApplyToBotAsync(game, botPlayer, now);
            }
        }

        private async Task ApplyToBotAsync(DartBotGameEntity game, DartBotGamePlayerEntity botPlayer, DateTime now)
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

            // 2. Elo (menschliche Gegner mit festem Wert, solange User keine eigene Wertung haben)
            var ratingBefore = bot.Rating;
            var expected = 1.0 / (1.0 + Math.Pow(10, (learning.HumanOpponentRating - ratingBefore) / 400.0));
            var actual = isWinner ? 1.0 : 0.0;
            var ratingAfter = Math.Max(0, (int)Math.Round(ratingBefore + learning.EloK * (actual - expected)));

            bot.Rating = ratingAfter;

            // 3. Level-Aufstieg
            var newLevel = LevelForExperience(bot.ExperiencePoints);

            if (newLevel > bot.DevelopmentLevel)
            {
                var current = bot.ProfileVersions.MaxBy(p => p.VersionNumber)
                    ?? throw new InvalidOperationException($"Dart bot {bot.Id} has no profile version.");

                bot.ProfileVersions.Add(CreateLevelUpVersion(current, bot.DevelopmentLevel, newLevel, game));
                bot.DevelopmentLevel = newLevel;
            }

            botPlayer.XpGained = xpGained;
            botPlayer.RatingBefore = ratingBefore;
            botPlayer.RatingAfter = ratingAfter;
            botPlayer.LearningProcessedAt = now;
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
            (long)Math.Round(_options.Learning.LevelXpBase * Math.Pow(level, _options.Learning.LevelXpExponent));

        private DartBotProfileVersionEntity CreateLevelUpVersion(
            DartBotProfileVersionEntity current,
            int previousLevel,
            int newLevel,
            DartBotGameEntity game)
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
                ChangeReason = DartBotProfileChangeReasonEnum.LevelUp,
                ReasonText = $"Level {previousLevel} -> {newLevel}",
                PreviousVersion = current,
                TriggeringGame = game
            };
        }

        /// <summary>Gleichmäßig vom Startwert (Level 0) bis SkillAtMaxLevel (MaxLevel).</summary>
        private float SkillForLevel(float startValue, int level)
        {
            var target = Math.Max(startValue, _options.Learning.SkillAtMaxLevel);
            var progress = Math.Clamp(level / (float)_options.MaxLevel, 0f, 1f);

            return Math.Clamp(startValue + (target - startValue) * progress, 0f, 1f);
        }
    }
}
