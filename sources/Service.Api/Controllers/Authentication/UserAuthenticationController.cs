using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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

        [AllowAnonymous]
        [HttpPost(Name = "UserLogin")]
        public async Task<IActionResult> UserLogin([FromBody] AuthenticationRequest request)
        {
            var tokenResponse = await _userAuthentication.AuthenticateUser(request, _jwtOptions);

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.Token) || string.IsNullOrEmpty(tokenResponse.RefreshToken))
            {
                ClearTokensFromHeader();
                Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Unauthorized();
            }

            AddTokensAsReadOnlyHeader(tokenResponse.Token, tokenResponse.RefreshToken);

            return Ok();
        }

        [HttpPost(Name = "UserLogout")]
        public async Task UserLogout()
        {
            await _userAuthentication.LogoutUser();

            ClearTokensFromHeader();
        }
    }
}
