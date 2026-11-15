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
