using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Enums.User;
using Shared.Interfaces.Training;
using Shared.Models.Training;

namespace Service.Api.Controllers.Training
{
    [Authorize(Roles = nameof(UserRoleEnum.Player))]
    public class CheckoutTrainingController: ApiControllerBase
    {
        private readonly ICheckoutTraining _checkoutTraining;

        public CheckoutTrainingController(ICheckoutTraining checkoutTraining)
        {
            _checkoutTraining = checkoutTraining;
        }

        [HttpPost(Name = "StartCheckoutTraining")]
        public async Task<IActionResult> StartCheckoutTraining([FromBody] StartCheckoutTrainingModel model)
        {
            var trainingId = await _checkoutTraining.StartTrainingAsync(model);
            return Ok(trainingId);
        }


        [HttpPost(Name = "AddCheckoutTrainingThrow")]
        public async Task<IActionResult> AddCheckoutTrainingThrow([FromBody] CheckoutTrainingThrow model)
        {
            await _checkoutTraining.AddThrowAsync(model);

            return Ok();
        }

        [HttpPost(Name = "FinishCheckoutTraining")]
        public async Task<IActionResult> FinishCheckoutTraining([FromBody] TrainingSessionRequestModel model)
        {
            await _checkoutTraining.FinishTrainingAsync(model.TrainingId);

            return Ok();
        }

        [HttpGet(Name = "GetCheckoutTrainingResult")]
        public async Task<IActionResult> GetCheckoutTrainingResult([FromQuery] int trainingId)
        {
            var result = await _checkoutTraining.GetResultAsync(trainingId);

            return Ok(result);
        }
    }
}
