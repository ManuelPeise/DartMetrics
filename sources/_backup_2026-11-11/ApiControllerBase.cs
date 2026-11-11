using Microsoft.AspNetCore.Mvc;


namespace Service.Api.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ApiControllerBase: ControllerBase
    {
        public ApiControllerBase()
        {
            
        }
    }
}
