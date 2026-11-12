using Data.DartContext.Entities.User;
using Microsoft.AspNetCore.Http;
using Shared.Enums.User;
using Shared.Interfaces.Authentication;
using Shared.Models.Authentication;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Logic.Shared.Helpers
{
    public class ClaimsHelper: IClaimsHelper
    {
        public const string UserIdClaimType = "UserId";
        public const string UserEmailClaimType = "UserEmail";
        public const string UserRolesClaimType = "UserRoles";


        private readonly IHttpContextAccessor _httpContextAccessor;

        public ClaimsHelper(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // Resolved on every access, so it always points at the current request.
        private HttpContext? HttpContext => _httpContextAccessor.HttpContext;

        public CurrentUser GetCurrentUserFromClaims()
        {
            if (HttpContext?.User is null)
            {
                throw new InvalidOperationException("HttpContext or User is not available.");
            }

            var userIdClaim = GetClaimValue(UserIdClaimType);
            var userEmailClaim = GetClaimValue(UserEmailClaimType);

            if (string.IsNullOrEmpty(userIdClaim) || string.IsNullOrEmpty(userEmailClaim))
            {
                throw new InvalidOperationException("Required claims are missing.");
            }
            
            return new CurrentUser
            {
                UserId = int.Parse(userIdClaim),
                Email = userEmailClaim,
                Roles = HttpContext.User.FindAll(UserRolesClaimType)
                    .Select(c => Enum.Parse<UserRoleEnum>(c.Value))
                    .ToList()
            };
        }

        public List<Claim> GetUserClaims(UserEntity userEntity)
        {
            if (userEntity == null)
            {
                throw new ArgumentNullException(nameof(userEntity));
            }
            
            // Role navigation may not be loaded (e.g. right after creating the user) -> skip instead of crashing.
            var roles = userEntity.UserRoles?
                .Where(ur => ur.Role != null)
                .Select(ur => ur.Role.UserRole)
                .Distinct()
                .ToList() ?? new List<UserRoleEnum>();

            var claims = new List<Claim>
            {
                new Claim(UserIdClaimType, userEntity.Id.ToString()),
                new Claim(UserEmailClaimType, userEntity.EmailAddress)
            };

            // One claim per role, so [Authorize(Roles = "Admin")] works.
            claims.AddRange(roles.Select(role => new Claim(UserRolesClaimType, role.ToString())));

            return claims;
        }

        public List<Claim> GetUserClaimsFromToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException("Token cannot be null or empty.", nameof(token));
            }
            var handler = new JwtSecurityTokenHandler();

            var jwtToken = handler.ReadJwtToken(token);
            var claims = jwtToken.Claims.ToList();

            return claims;
        }

        private string? GetClaimValue(string claimType)
        {
            if (string.IsNullOrEmpty(claimType))
            {
                throw new ArgumentException("Claim type cannot be null or empty.", nameof(claimType));
            }

            var claim = HttpContext?.User.FindFirst(claimType);
            
            return claim?.Value;
        }
    }
}
