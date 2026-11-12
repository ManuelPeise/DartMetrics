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
        public async Task<TokenResponse?> UserLogin([FromBody] AuthenticationRequest request)
        {
            var tokenResponse = await _userAuthentication.AuthenticateUser(request, _jwtOptions);

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.Token) || string.IsNullOrEmpty(tokenResponse.RefreshToken))
            {
                ClearTokensFromHeader();
                Response.StatusCode = StatusCodes.Status401Unauthorized;
                return null;
            }

            return tokenResponse;
        }

        // Anonymous on purpose: the refresh token is the credential. The access token may already be expired.
        [AllowAnonymous]
        [HttpPost(Name = "RefreshToken")]
        public async Task<ActionResult<TokenResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var tokenResponse = await _userAuthentication.RefreshToken(request, _jwtOptions);

            if (tokenResponse == null)
            {
                ClearTokensFromHeader();

                return Unauthorized();
            }

            return Ok(tokenResponse);
        }

        [HttpPost(Name = "UserLogout")]
        public async Task UserLogout()
        {
            await _userAuthentication.LogoutUser();

            ClearTokensFromHeader();
        }
    }
}
