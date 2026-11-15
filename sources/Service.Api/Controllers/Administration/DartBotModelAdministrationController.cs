using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Enums.User;
using Shared.Interfaces.Administration;

namespace Service.Api.Controllers.Administration
{
    [Authorize(Roles = nameof(UserRoleEnum.Admin))]
    public class DartBotModelAdministrationController : ApiControllerBase
    {
        private readonly IDartBotModelAdministration _modelAdministration;

        public DartBotModelAdministrationController(IDartBotModelAdministration modelAdministration)
        {
            _modelAdministration = modelAdministration;
        }

        [HttpGet(Name = "GetDartBotModels")]
        public async Task<IActionResult> GetDartBotModels()
        {
            var overview = await _modelAdministration.GetModelsAsync();

            return Ok(overview);
        }

        [HttpPost(Name = "ActivateDartBotModel")]
        public async Task<IActionResult> ActivateDartBotModel([FromQuery] int modelId)
        {
            await _modelAdministration.ActivateModelAsync(modelId);

            return Ok();
        }

        [HttpPost(Name = "DeactivateDartBotModel")]
        public async Task<IActionResult> DeactivateDartBotModel()
        {
            await _modelAdministration.DeactivateTargetModelAsync();

            return Ok();
        }
    }
}
