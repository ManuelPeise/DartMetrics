using Data.DartContext.Entities.User;
using Data.DartContext.Security;
using Logic.Shared;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Logic.Shared.Helpers;
using Logic.Shared.Mail;
using Org.BouncyCastle.Pkcs;
using Shared.Enums.Email;
using Shared.Enums.User;
using Shared.Interfaces.Authentication;
using Shared.Interfaces.Email;
using Shared.Interfaces.Logger;
using Shared.Interfaces.UnitsOfWork;
using Shared.Models.Authentication;
using Shared.Models.Email;
using Shared.Models.Options;
using Shared.Models.User;

namespace Logic.Authentication
{
    public class UserRegistration : ALogicBase, IUserRegistration
    {
        private readonly IEmailClient _emailClient;
        private readonly IClaimsHelper _claimsHelper;

        public UserRegistration(IUnitOfWork unitOfWork, IMessageLog messageLog, IEmailClient emailClient, IClaimsHelper claimsHelper) : base(unitOfWork, messageLog)
        {
            _emailClient = emailClient;
            _claimsHelper = claimsHelper;
        }

        public async Task<bool> RegisterUser(UserRegistrationRequestModel registrationRequestModel, JwtOptions jwtOptions, ServerOptions serverOptions)
        {
            try
            {
                ArgumentException.ThrowIfNullOrEmpty(registrationRequestModel.FirstName, nameof(registrationRequestModel.FirstName));
                ArgumentException.ThrowIfNullOrEmpty(registrationRequestModel.LastName, nameof(registrationRequestModel.LastName));
                ArgumentException.ThrowIfNullOrEmpty(registrationRequestModel.EmailAddress, nameof(registrationRequestModel.EmailAddress));
                ArgumentException.ThrowIfNullOrEmpty(registrationRequestModel.UserCredentials.Password, nameof(registrationRequestModel.UserCredentials.Password));
                ArgumentException.ThrowIfNullOrEmpty(registrationRequestModel.UserCredentials.PasswordReplication, nameof(registrationRequestModel.UserCredentials.PasswordReplication));

                if (!PasswordHasher.IsMatchingPassword(registrationRequestModel.UserCredentials.Password, registrationRequestModel.UserCredentials.PasswordReplication))
                {
                    throw new ArgumentException("Password and PasswordReplication do not match.");
                }

                PasswordPolicy.Validate(registrationRequestModel.UserCredentials.Password, nameof(registrationRequestModel.UserCredentials.Password));

                var userEntity = await UnitOfWork.UserUnitOfWork.UserRepository.GetFirstOrDefaultAsync(
                    u => u.EmailAddress == registrationRequestModel.EmailAddress,
                    include: u => u.Include(x => x.UserCredentials));

                var hashedPassword = PasswordHasher.HashPassword(registrationRequestModel.UserCredentials.Password);

                if (userEntity != null)
                {
                    if (userEntity.EmailVerifiedAt != null)
                    {
                        throw new ArgumentException($"User with email {registrationRequestModel.EmailAddress} already exists in database.");
                    }

                    await UpdateUnverifiedUser(userEntity, registrationRequestModel, hashedPassword);

                    await SendVerificationMail(userEntity, jwtOptions, serverOptions);

                    return true;
                }

                var roleId = await GetDefaultRoleId();

                var newUserEntity = new UserEntity
                {
                    FirstName = registrationRequestModel.FirstName,
                    LastName = registrationRequestModel.LastName,
                    DisplayName = registrationRequestModel.DisplayName,
                    EmailAddress = registrationRequestModel.EmailAddress,
                    EmailVerifiedAt = null,
                    IsActive = false,
                    UserCredentials = new UserCredentialsEntity
                    {
                        PasswordHash = hashedPassword,
                        PasswordChangedAt = DateTime.UtcNow
                    },
                    UserRoles = new List<UserRoleEntity>
                    {
                        new UserRoleEntity
                        {
                            RoleId = roleId
                        }
                    }
                };

                await UnitOfWork.UserUnitOfWork.UserRepository.AddAsync(newUserEntity);

                var result = await UnitOfWork.SaveChangesAsync();

                if (result)
                {

                    await SendVerificationMail(newUserEntity, jwtOptions, serverOptions);

                    return true;
                }
                else
                {
                    throw new InvalidOperationException("Failed to save the new user to the database.");
                }
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage($"Error occurred during user registration", nameof(UserRegistration), exception);

                return false;
            }
        }

        public async Task<bool> VerifyEmail(string token, JwtOptions jwtOptions, ServerOptions serverOptions)
        {
            try
            {
                var principal = new JwtTokenGenerator().ValidateToken(
                    token,
                    jwtOptions.Key,
                    jwtOptions.Issuer,
                    JwtTokenGenerator.GetEmailVerificationAudience(jwtOptions.Audience));

                var emailClaim = principal.FindFirst(ClaimsHelper.UserEmailClaimType)?.Value;

                if (emailClaim == null)
                {
                    throw new InvalidOperationException("Email claim not found in the token.");
                }

                var userEntity = await UnitOfWork.UserUnitOfWork.UserRepository.GetFirstOrDefaultAsync(u => u.EmailAddress == emailClaim);

                if (userEntity == null)
                {
                    throw new InvalidOperationException("User not found in the database.");
                }

                if (userEntity.EmailVerifiedAt != null)
                {
                    return true;
                }

                userEntity.EmailVerifiedAt = DateTime.UtcNow;
                userEntity.IsActive = true;

                await UnitOfWork.SaveChangesAsync();

                await SendAccountActivationMail(userEntity, serverOptions);

                return true;
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage($"Error occurred during email verification", nameof(UserRegistration), exception);

                return false;
            }
        }

        /// <summary>Höchstens eine Mail je Zweck und Adresse in diesem Abstand (Schutz vor Mail-Spam).</summary>
        private static readonly TimeSpan MailCooldown = TimeSpan.FromMinutes(2);

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, DateTime> LastMailSent = new(StringComparer.OrdinalIgnoreCase);

        private const string PasswordStampClaimType = "PasswordStamp";

        public const int PasswordResetMinutes = 60;

        public async Task ResendVerificationMail(string emailAddress, JwtOptions jwtOptions, ServerOptions serverOptions)
        {
            try
            {
                var email = emailAddress?.Trim() ?? "";
                var user = string.IsNullOrEmpty(email)
                    ? null
                    : await UnitOfWork.UserUnitOfWork.UserRepository.GetFirstOrDefaultAsync(u => u.EmailAddress == email && u.DeletedAt == null, asNoTracking: true);

                if (user is null || user.EmailVerifiedAt != null || !TryReserveMail("verify", email))
                {
                    return;
                }

                await SendVerificationMail(user, jwtOptions, serverOptions);
            }
            catch (Exception exception)
            {
                // Nach außen immer gleich antworten; Fehler nur protokollieren.
                await MessageLog.LogMessage("Error resending the verification mail.", nameof(UserRegistration), exception);
            }
        }

        public async Task RequestPasswordReset(string emailAddress, JwtOptions jwtOptions, ServerOptions serverOptions)
        {
            try
            {
                var email = emailAddress?.Trim() ?? "";
                var user = string.IsNullOrEmpty(email)
                    ? null
                    : await UnitOfWork.UserUnitOfWork.UserRepository.GetFirstOrDefaultAsync(
                        u => u.EmailAddress == email && u.DeletedAt == null,
                        asNoTracking: true,
                        include: q => q.Include(u => u.UserCredentials));

                if (user?.UserCredentials is null || !user.IsActive || user.EmailVerifiedAt == null || !TryReserveMail("reset", email))
                {
                    return;
                }

                // Der Stempel bindet den Link an das aktuelle Passwort: nach dem Zurücksetzen ist er wertlos (einmal nutzbar).
                var claims = new List<Claim>
                {
                    new(ClaimsHelper.UserIdClaimType, user.Id.ToString()),
                    new(ClaimsHelper.UserEmailClaimType, user.EmailAddress),
                    new(PasswordStampClaimType, user.UserCredentials.PasswordChangedAt.Ticks.ToString())
                };

                var token = new JwtTokenGenerator().GenerateToken(claims, jwtOptions.Key, jwtOptions.Issuer,
                    JwtTokenGenerator.GetPasswordResetAudience(jwtOptions.Audience), PasswordResetMinutes);

                var resetLink = $"{serverOptions.Host}:{serverOptions.Port}/password/reset?token={Uri.EscapeDataString(token)}";

                await _emailClient.SendMail(new EmailSettings
                {
                    ToAddress = user.EmailAddress,
                    Subject = "Reset your DartMetrics password",
                    Body = EmailBodyFactory.GetEmailBody(EmailBodyTypeEnum.PasswordReset, resetLink)
                });
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error requesting a password reset.", nameof(UserRegistration), exception);
            }
        }

        public async Task<bool> ResetPassword(ResetPasswordRequestModel model, JwtOptions jwtOptions)
        {
            if (!PasswordHasher.IsMatchingPassword(model.Password, model.PasswordReplication))
            {
                throw new ArgumentException("Password and PasswordReplication do not match.");
            }

            PasswordPolicy.Validate(model.Password, nameof(model.Password));

            try
            {
                ClaimsPrincipal principal;

                try
                {
                    principal = new JwtTokenGenerator().ValidateToken(model.Token, jwtOptions.Key, jwtOptions.Issuer,
                        JwtTokenGenerator.GetPasswordResetAudience(jwtOptions.Audience));
                }
                catch (Exception)
                {
                    // Signatur, Audience oder Ablauf falsch.
                    return false;
                }

                if (!int.TryParse(principal.FindFirst(ClaimsHelper.UserIdClaimType)?.Value, out var userId) ||
                    !long.TryParse(principal.FindFirst(PasswordStampClaimType)?.Value, out var stamp))
                {
                    return false;
                }

                var user = await UnitOfWork.UserUnitOfWork.UserRepository.GetFirstOrDefaultAsync(
                    u => u.Id == userId && u.DeletedAt == null,
                    include: q => q.Include(u => u.UserCredentials));

                // Schon benutzt (Passwort seitdem geändert) oder Konto inzwischen gesperrt/gelöscht.
                if (user?.UserCredentials is null || !user.IsActive || user.UserCredentials.PasswordChangedAt.Ticks != stamp)
                {
                    return false;
                }

                var now = DateTime.UtcNow;
                user.UserCredentials.PasswordHash = PasswordHasher.HashPassword(model.Password);
                user.UserCredentials.PasswordChangedAt = now;

                // Überall abmelden: vorhandenes Refresh-Token ungültig machen.
                user.UserCredentials.RefreshTokenHash = null;
                user.UserCredentials.RefreshTokenExpiresAt = null;
                user.UserCredentials.RefreshTokenRevokedAt = now;

                await UnitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error resetting a password.", nameof(UserRegistration), exception);
                throw;
            }
        }

        private static bool TryReserveMail(string purpose, string email)
        {
            var key = $"{purpose}:{email}";
            var now = DateTime.UtcNow;

            if (LastMailSent.TryGetValue(key, out var last) && now - last < MailCooldown)
            {
                return false;
            }

            LastMailSent[key] = now;
            return true;
        }

        private async Task UpdateUnverifiedUser(UserEntity userEntity, UserRegistrationRequestModel registrationRequestModel, string hashedPassword)
        {
            userEntity.FirstName = registrationRequestModel.FirstName;
            userEntity.LastName = registrationRequestModel.LastName;
            userEntity.DisplayName = registrationRequestModel.DisplayName;

            if (userEntity.UserCredentials == null)
            {
                userEntity.UserCredentials = new UserCredentialsEntity
                {
                    PasswordHash = hashedPassword,
                    PasswordChangedAt = DateTime.UtcNow
                };
            }
            else
            {
                userEntity.UserCredentials.PasswordHash = hashedPassword;
                userEntity.UserCredentials.PasswordChangedAt = DateTime.UtcNow;
            }

            await UnitOfWork.SaveChangesAsync();
        }

        private async Task<int> GetDefaultRoleId()
        {
            var userRoleEntity = await UnitOfWork.UserUnitOfWork.RoleRepository.GetFirstOrDefaultAsync(role => role.UserRole == UserRoleEnum.Player);

            if (userRoleEntity == null)
            {
                throw new InvalidOperationException("User role not found in the database.");
            }

            return userRoleEntity.Id;
        }

        private async Task SendVerificationMail(UserEntity entity, JwtOptions jwtOptions, ServerOptions serverOptions)
        {
            var emailSettings = new EmailSettings
            {
                ToAddress = entity.EmailAddress,
                Subject = "Welcome to DartMetrics!",
                Body = GetEmailBody(entity, jwtOptions, serverOptions)
            };

            await _emailClient.SendMail(emailSettings);
        }

        private string GetEmailBody(UserEntity entity, JwtOptions jwtOptions, ServerOptions serverOptions)
        {
            // The verification token only needs id + e-mail. Do NOT use GetUserClaims here: it reads
            // UserRoles[].Role, which is not loaded for a freshly created user (only RoleId is set) -> NullReferenceException.
            var claims = new List<Claim>
            {
                new(ClaimsHelper.UserIdClaimType, entity.Id.ToString()),
                new(ClaimsHelper.UserEmailClaimType, entity.EmailAddress)
            };

            var jwtTokenGenerator = new JwtTokenGenerator();

            // Separate audience: the API's JwtBearer handler rejects this token, so it is not usable as an access token.
            var jwtToken = jwtTokenGenerator.GenerateToken(claims, jwtOptions.Key, jwtOptions.Issuer,
                JwtTokenGenerator.GetEmailVerificationAudience(jwtOptions.Audience), 1440);

            var verificationLink = $"{serverOptions.Host}:{serverOptions.Port}/registration/verify?token={Uri.EscapeDataString(jwtToken)}";

            return EmailBodyFactory.GetEmailBody(EmailBodyTypeEnum.EmailVerification, verificationLink);
        }

        private async Task SendAccountActivationMail(UserEntity entity, ServerOptions serverOptions)
        {
            var emailSettings = new EmailSettings
            {
                ToAddress = entity.EmailAddress,
                Subject = "Your DartMetrics Account is Activated!",
                Body = GetAccountActivationEmailBody(entity, serverOptions)
            };

            await _emailClient.SendMail(emailSettings);
        }

        private string GetAccountActivationEmailBody(UserEntity entity, ServerOptions serverOptions)
        {
            var applicationUrl = $"{serverOptions.Host}:{serverOptions.Port}/login";

            return EmailBodyFactory.GetEmailBody(EmailBodyTypeEnum.AccountActivation, applicationUrl);
        }
    }
}
