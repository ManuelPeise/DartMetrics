using Data.DartContext.Security;
using Logic.Shared;
using Microsoft.EntityFrameworkCore;
using Shared.Enums.Logger;
using Shared.Interfaces.Authentication;
using Shared.Interfaces.Logger;
using Shared.Interfaces.UnitsOfWork;
using Shared.Models.Authentication;

namespace Logic.Authentication
{
    public class UserAuthentication : ALogicBase, IUserAuthentication
    {
        private readonly IClaimsHelper _claimsHelper;

        // Used to keep the response time constant when the user does not exist (prevents user enumeration via timing).
        private static readonly Lazy<string> DummyPasswordHash = new(() => PasswordHasher.HashPassword(Guid.NewGuid().ToString()));

        public UserAuthentication(IUnitOfWork unitOfWork, IMessageLog messageLog, IClaimsHelper claimsHelper) : base(unitOfWork, messageLog)
        {
            _claimsHelper = claimsHelper;
        }

        public async Task<TokenResponse?> AuthenticateUser(AuthenticationRequest authenticationRequest, JwtOptions jwtOptions)
        {
            try
            {
                ArgumentException.ThrowIfNullOrEmpty(authenticationRequest.EmailAddress, nameof(authenticationRequest.EmailAddress));
                ArgumentException.ThrowIfNullOrEmpty(authenticationRequest.Password, nameof(authenticationRequest.Password));

                var userEntity = await UnitOfWork.UserUnitOfWork.UserRepository.GetFirstOrDefaultAsync(u => u.EmailAddress == authenticationRequest.EmailAddress, include: u => u
                        .Include(x => x.UserCredentials)
                        .Include(x => x.UserRoles).ThenInclude(ur => ur.Role));

                if (userEntity == null || userEntity.UserCredentials == null)
                {
                    PasswordHasher.VerifyPassword(authenticationRequest.Password, DummyPasswordHash.Value);
                    throw new UnauthorizedAccessException("Invalid email address or password.");
                }

                if(!userEntity.IsActive || userEntity.EmailVerifiedAt == null)
                {
                    throw new UnauthorizedAccessException("User account is not active or email is not verified.");
                }

                var isValidPassword = PasswordHasher.VerifyPassword(authenticationRequest.Password, userEntity.UserCredentials.PasswordHash);

                if (!isValidPassword)
                {
                    throw new UnauthorizedAccessException("Invalid email address or password.");
                }

                var claims = _claimsHelper.GetUserClaims(userEntity);

                var jwtTokenGenerator = new JwtTokenGenerator();

                var token = jwtTokenGenerator.GenerateToken(claims, jwtOptions.Key, jwtOptions.Issuer, jwtOptions.Audience, jwtOptions.ExpireMinutes);
                var refreshToken = jwtTokenGenerator.GenerateRefreshToken();

                var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(jwtOptions.RefreshTokenExpireDays);

                userEntity.UserCredentials.RefreshTokenHash = JwtTokenGenerator.HashRefreshToken(refreshToken);
                userEntity.UserCredentials.RefreshTokenExpiresAt = refreshTokenExpiresAt;
                userEntity.UserCredentials.RefreshTokenRevokedAt = null;

                await UnitOfWork.SaveChangesAsync();

                return new TokenResponse
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    RefreshTokenExpiresAt = refreshTokenExpiresAt,
                    AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(jwtOptions.ExpireMinutes)
                };

            }
            catch (UnauthorizedAccessException)
            {
                // Wrong password / unknown or unverified account: an expected outcome, not an error.
                // Not written to MessageLog, otherwise a brute-force attempt would flood the log table.
                return null;
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("An error occurred while authenticating the user.", "UserAuthentication", exception, LogMessageTypeEnum.Error);

                return null;

            }
        }

        public async Task<TokenResponse?> RefreshToken(RefreshTokenRequest refreshTokenRequest, JwtOptions jwtOptions)
        {
            try
            {
                // The refresh token itself identifies the user. No (possibly expired) access token is required,
                // otherwise a session could never be renewed once the access token has expired.
                if (string.IsNullOrWhiteSpace(refreshTokenRequest.RefreshToken))
                    return null;

                var refreshTokenHash = JwtTokenGenerator.HashRefreshToken(refreshTokenRequest.RefreshToken);

                var userEntity =
                    await UnitOfWork.UserUnitOfWork.UserRepository
                        .GetFirstOrDefaultAsync(
                            u => u.UserCredentials != null && u.UserCredentials.RefreshTokenHash == refreshTokenHash,
                            include: u => u
                                .Include(x => x.UserCredentials)
                                .Include(x => x.UserRoles)
                                .ThenInclude(ur => ur.Role));

                if (userEntity?.UserCredentials == null || !userEntity.IsActive)
                    return null;

                var credentials = userEntity.UserCredentials;

                if (credentials.RefreshTokenExpiresAt == null ||
                    credentials.RefreshTokenExpiresAt <= DateTime.UtcNow ||
                    credentials.RefreshTokenRevokedAt != null)
                {
                    return null;
                }

                var claims = _claimsHelper.GetUserClaims(userEntity);

                var jwtTokenGenerator = new JwtTokenGenerator();

                var token = jwtTokenGenerator.GenerateToken(
                    claims,
                    jwtOptions.Key,
                    jwtOptions.Issuer,
                    jwtOptions.Audience,
                    jwtOptions.ExpireMinutes);

                var newRefreshToken =
                    jwtTokenGenerator.GenerateRefreshToken();

                var accessTokenExpiresAt =
                    DateTimeOffset.UtcNow.AddMinutes(jwtOptions.ExpireMinutes);

                var refreshTokenExpiresAt =
                    DateTimeOffset.UtcNow.AddDays(jwtOptions.RefreshTokenExpireDays);

                credentials.RefreshTokenHash =
                    JwtTokenGenerator.HashRefreshToken(newRefreshToken);

                credentials.RefreshTokenExpiresAt =
                    refreshTokenExpiresAt.UtcDateTime;

                await UnitOfWork.SaveChangesAsync();

                return new TokenResponse
                {
                    Token = token,
                    RefreshToken = newRefreshToken,
                    AccessTokenExpiresAt = accessTokenExpiresAt,
                    RefreshTokenExpiresAt = refreshTokenExpiresAt
                };
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage(
                    "An error occurred while refreshing the token.",
                    "UserAuthentication",
                    exception,
                    LogMessageTypeEnum.Error);

                return null;
            }
        }

        public async Task<bool> LogoutUser()
        {
            try
            {
                var currentUser = _claimsHelper.GetCurrentUserFromClaims();

                if (currentUser == null)
                {
                    throw new UnauthorizedAccessException("User is not authenticated.");
                }

                var userEntity = await UnitOfWork.UserUnitOfWork.UserRepository.GetFirstOrDefaultAsync(u => u.Id == currentUser.UserId, include: u => u
                        .Include(x => x.UserCredentials));

                if(userEntity == null || userEntity.UserCredentials == null)
                {
                    throw new UnauthorizedAccessException("User not found.");
                }

                userEntity.UserCredentials.RefreshTokenHash = null;
                userEntity.UserCredentials.RefreshTokenExpiresAt = null;
                userEntity.UserCredentials.RefreshTokenRevokedAt = DateTime.UtcNow;

                await UnitOfWork.SaveChangesAsync();

                return true;
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("An error occurred while logging out the user.", "UserAuthentication", exception, LogMessageTypeEnum.Error);
                
                return false;
            }
        }
    }
}
