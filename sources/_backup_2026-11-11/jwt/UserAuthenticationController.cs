using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Shared.Interfaces.Authentication;
using Shared.Models.Authentication;

namespace Service.Api.Controllers.Authentication
{
    public class UserAuthenticationController: ApiControllerBase
    {
        private readonly IUserAuthentication _userAuthentication;
        private readonly JwtOptions _jwtOptions;

        public UserAuthenticationController(IUserAuthentication userAuthentication, IOptions<JwtOptions> options)
        {
            _userAuthentication = userAuthentication;
            _jwtOptions = options.Value;
        }

        [HttpPost(Name = "AuthenticateUser")]
        public IActionResult AuthenticateUser([FromBody] AuthenticationRequest request)
        {
            var tokenresponse = _userAuthentication.AuthenticateUser(request, _jwtOptions.Key, _jwtOptions.ExpireDays, _jwtOptions.RefreshTokenExpireDays);
            
            return Ok(tokenresponse);
        }
    }
}
