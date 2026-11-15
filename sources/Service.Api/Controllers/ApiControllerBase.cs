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

        // The API no longer sets JWT cookies itself (the BFF keeps the tokens server-side).
        // Clearing them stays, so old cookies from earlier versions are removed on login/logout.
        protected void ClearTokensFromHeader()
        {
            Response.Cookies.Delete("jwt");
            Response.Cookies.Delete("refreshToken");
        }
    }
}
