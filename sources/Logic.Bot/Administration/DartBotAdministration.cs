using Data.DartContext.Entities.Bot;
using Logic.Bot.Learning;
using Logic.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.Enums.Bot;
using Shared.Enums.User;
using Shared.Interfaces.Administration;
using Shared.Interfaces.Authentication;
using Shared.Interfaces.Logger;
using Shared.Interfaces.UnitsOfWork;
using Shared.Models.Bot;
using Shared.Models.Options;

namespace Logic.Bot.Administration
{
    /// <summary>
    /// Anlegen, Aktivieren/Deaktivieren und Auflisten von Bots. Bots werden nie gelöscht.
    /// </summary>
    public class DartBotAdministration : ALogicBase, IDartBotAdministration
    {
        private const int NameMaxLength = 100;
        private const int AliasMaxLength = 50;

        private readonly IClaimsHelper _claimsHelper;
        private readonly DartBotOptions _options;

        public DartBotAdministration(
            IUnitOfWork unitOfWork,
            IMessageLog messageLog,
            IClaimsHelper claimsHelper,
            IOptions<DartBotOptions> options) : base(unitOfWork, messageLog)
        {
            _claimsHelper = claimsHelper;
            _options = options.Value;
        }

        public async Task<int> CreateBotAsync(CreateDartBotModel model)
        {
            try
            {
                EnsureCurrentUserIsAdmin();

                var name = model.Name?.Trim();
                var alias = model.Alias?.Trim();

                if (string.IsNullOrWhiteSpace(name) || name.Length > NameMaxLength)
                {
                    throw new ArgumentException($"Name is required and may have at most {NameMaxLength} characters.");
                }

                if (string.IsNullOrWhiteSpace(alias) || alias.Length > AliasMaxLength)
                {
                    throw new ArgumentException($"Alias is required and may have at most {AliasMaxLength} characters.");
                }

                // Unique Index existiert ebenfalls; die Prüfung liefert eine verständliche Meldung.
                if (await UnitOfWork.DartBotUnitOfWork.DartBotRepository.AnyAsync(b => b.Alias == alias))
                {
                    throw new ArgumentException($"A bot with alias '{alias}' already exists.");
                }

                var start = _options.StartProfile;

                var bot = new DartBotEntity
                {
                    Name = name,
                    Alias = alias,
                    IsActive = true,
                    ExperiencePoints = DartBotLevelCurve.XpForLevel(_options, level),
                    DevelopmentLevel = level,
                    Rating = DartBotLevelCurve.RatingForStartLevel(_options, level),
                    ProfileVersions =
                    [
                        new DartBotProfileVersionEntity
                        {
                            VersionNumber = 1,
                            ThrowPrecision = DartBotLevelCurve.SkillForLevel(_options, start.ThrowPrecision, level),
                            ThrowConsistency = DartBotLevelCurve.SkillForLevel(_options, start.ThrowConsistency, level),
                            TacticalIntelligence = DartBotLevelCurve.SkillForLevel(_options, start.TacticalIntelligence, level),
                            PressureResistance = DartBotLevelCurve.SkillForLevel(_options, start.PressureResistance, level),
                            RiskTaking = start.RiskTaking,
                            PlayingStyle = model.PlayingStyle,
                            ChangeReason = DartBotProfileChangeReasonEnum.Initial,
                            ReasonText = $"Startprofil Level {level}"
                        }
                    ]
                };

                await UnitOfWork.DartBotUnitOfWork.DartBotRepository.AddAsync(bot);
                await UnitOfWork.SaveChangesAsync();

                return bot.Id;
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error creating dart bot.", nameof(DartBotAdministration), exception);

                throw;
            }
        }

        public Task DeactivateBotAsync(int botId) => SetActiveAsync(botId, false);

        public Task ActivateBotAsync(int botId) => SetActiveAsync(botId, true);

        public async Task<List<DartBotListItem>> GetBotsAsync(bool includeInactive = false)
        {
            try
            {
                EnsureCurrentUserIsAdmin();

                var bots = await UnitOfWork.DartBotUnitOfWork.DartBotRepository.GetWhereAsync(
                    b => includeInactive || b.IsActive,
                    asNoTracking: true,
                    include: q => q.Include(b => b.ProfileVersions));

                return bots
                    .OrderBy(b => b.Name)
                    .Select(ToListItem)
                    .ToList();
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error loading dart bots.", nameof(DartBotAdministration), exception);

                throw;
            }
        }

        public async Task<DartBotListItem> GetBotAsync(int botId)
        {
            try
            {
                EnsureCurrentUserIsAdmin();

                var bot = await UnitOfWork.DartBotUnitOfWork.DartBotRepository.GetByIdAsync(
                    botId,
                    asNoTracking: true,
                    include: q => q.Include(b => b.ProfileVersions));

                return ToListItem(bot);
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage($"Error loading dart bot {botId}.", nameof(DartBotAdministration), exception);

                throw;
            }
        }

        private async Task SetActiveAsync(int botId, bool isActive)
        {
            try
            {
                EnsureCurrentUserIsAdmin();

                // Getrackt laden und direkt ändern (kein Update(), siehe UnitOfWork-Hinweis).
                var bot = await UnitOfWork.DartBotUnitOfWork.DartBotRepository.GetByIdAsync(botId);

                if (bot.IsActive == isActive)
                {
                    return;
                }

                bot.IsActive = isActive;

                await UnitOfWork.SaveChangesAsync();
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage($"Error setting active state of dart bot {botId}.", nameof(DartBotAdministration), exception);

                throw;
            }
        }

        private void EnsureCurrentUserIsAdmin()
        {
            var currentUser = _claimsHelper.GetCurrentUserFromClaims();

            if (currentUser == null || currentUser.UserId == 0)
            {
                throw new Exception("Current user not found.");
            }

            if (currentUser.Roles == null || !currentUser.Roles.Contains(UserRoleEnum.Admin))
            {
                throw new UnauthorizedAccessException("Only administrators can manage dart bots.");
            }
        }

        /// <summary>Aktuelle Profilversion = höchste VersionNumber.</summary>
        private static DartBotListItem ToListItem(DartBotEntity bot)
        {
            var profile = bot.ProfileVersions.MaxBy(p => p.VersionNumber)
                ?? throw new InvalidOperationException($"Dart bot {bot.Id} has no profile version.");

            return new DartBotListItem
            {
                Id = bot.Id,
                Name = bot.Name,
                Alias = bot.Alias,
                IsActive = bot.IsActive,
                DevelopmentLevel = bot.DevelopmentLevel,
                ExperiencePoints = bot.ExperiencePoints,
                Rating = bot.Rating,
                CreatedAt = bot.CreatedAt,
                ProfileVersionNumber = profile.VersionNumber,
                ThrowPrecision = profile.ThrowPrecision,
                ThrowConsistency = profile.ThrowConsistency,
                TacticalIntelligence = profile.TacticalIntelligence,
                PressureResistance = profile.PressureResistance,
                RiskTaking = profile.RiskTaking,
                PlayingStyle = profile.PlayingStyle
            };
        }
    }
}
