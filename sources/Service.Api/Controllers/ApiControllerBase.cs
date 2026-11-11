using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


namespace Service.Api.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public abstract class ApiControllerBase : ControllerBase
    {
        protected ApiControllerBase()
        {
            
        }

        protected void AddTokensAsReadOnlyHeader(string token, string refreshToken)
        {
            if (!string.IsNullOrEmpty(token))
            {
                Response.Cookies.Append("jwt", token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddMinutes(30) // Set the expiration time for the cookie
                });

                Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddMinutes(30)
                });
            }
        }

        protected void ClearTokensFromHeader()
        {
            Response.Cookies.Delete("jwt");
            Response.Cookies.Delete("refreshToken");
        }
    }
}
