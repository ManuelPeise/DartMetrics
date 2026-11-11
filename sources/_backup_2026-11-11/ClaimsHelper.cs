using Data.DartContext.Entities.User;
using Microsoft.AspNetCore.Http;
using Shared.Enums.User;
using Shared.Interfaces.Authentication;
using Shared.Models.Authentication;
using System.Security.Claims;

namespace Logic.Shared.Helpers
{
    public class ClaimsHelper: IClaimsHelper
    {
        public const string UserIdClaimType = "UserId";
        public const string UserEmailClaimType = "UserEmail";
        public const string UserRoleClaimType = "UserRole";


        private readonly HttpContext _httpContext;
        
        public ClaimsHelper(HttpContext httpContext)
        {
            _httpContext = httpContext;
        }

        public CurrentUser GetCurrentUserFromClaims()
        {
            if(_httpContext == null || _httpContext.User == null)
            {
                throw new InvalidOperationException("HttpContext or User is not available.");
            }

            var userIdClaim = GetClaimValue(UserIdClaimType);
            var userEmailClaim = GetClaimValue(UserEmailClaimType);
            var userRolesClaim = GetClaimValue(UserRoleClaimType);

            if (string.IsNullOrEmpty(userIdClaim) || string.IsNullOrEmpty(userEmailClaim) || string.IsNullOrEmpty(userRolesClaim))
            {
                throw new InvalidOperationException("Required claims are missing.");
            }
            
            return new CurrentUser
            {
                UserId = int.Parse(userIdClaim),
                Email = userEmailClaim,
                Roles = new List<UserRoleEnum>(userRolesClaim.Split(',').Select(role => Enum.Parse<UserRoleEnum>(role)))
            };
        }

        public List<Claim> GetUserClaims(UserEntity userEntity)
        {
            if (userEntity == null)
            {
                throw new ArgumentNullException(nameof(userEntity));
            }
            
            var roles = userEntity.UserRoles?.Select(ur => ur.Role.UserRole).ToList() ?? new List<UserRoleEnum>();

            var claims = new List<Claim>
            {
                new Claim(UserIdClaimType, userEntity.Id.ToString()),
                new Claim(UserEmailClaimType, userEntity.EmailAddress),
                new Claim(UserRoleClaimType, string.Join(",", roles))
            };

            return claims;
        }

        private string? GetClaimValue(string claimType)
        {
            if (string.IsNullOrEmpty(claimType))
            {
                throw new ArgumentException("Claim type cannot be null or empty.", nameof(claimType));
            }

            var claim = _httpContext.User?.FindFirst(claimType);
            
            return claim?.Value;
        }
    }
}
