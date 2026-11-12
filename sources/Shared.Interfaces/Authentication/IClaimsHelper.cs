using Data.DartContext.Entities.User;
using Shared.Models.Authentication;
using System.Security.Claims;
namespace Shared.Interfaces.Authentication
{
    public interface IClaimsHelper
    {
        CurrentUser GetCurrentUserFromClaims();
        List<Claim> GetUserClaims(UserEntity userEntity);
        List<Claim> GetUserClaimsFromToken(string token);
    }
}
