using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Enums.User;
using Shared.Interfaces.Training;
using Shared.Models.Training;

namespace Service.Api.Controllers.Training
{
    [Authorize(Roles = nameof(UserRoleEnum.Player))]
    public class AroundTheClockTrainingController: ApiControllerBase
    {
        private readonly IAroundTheClockTraining _aroundTheClockTraining;

        public AroundTheClockTrainingController(IAroundTheClockTraining aroundTheClockTraining)
        {
            _aroundTheClockTraining = aroundTheClockTraining;
        }

        [HttpPost(Name = "StartAroundTheClockTraining")]
        public async Task<IActionResult> StartTraining([FromBody] StartAroundTheClockTrainingModel model)
        {
            var trainingId = await _aroundTheClockTraining.StartTrainingAsync(model);

            return Ok(trainingId);
        }

        [HttpPost(Name = "AddAroundTheClockTrainingThrow")]
        public async Task<IActionResult> AddThrow([FromBody] AroundTheClockTrainingThrow model)
        {
            var result = await _aroundTheClockTraining.AddThrowAsync(model);

            return Ok(result);
        }

        [HttpPost(Name = "FinishAroundTheClockTraining")]
        public async Task<IActionResult> FinishTraining([FromQuery] int trainingId)
        {
            await _aroundTheClockTraining.FinishTrainingAsync(trainingId);

            return Ok();
        }

        [HttpGet(Name = "GetAroundTheClockTrainingResult")]
        public async Task<IActionResult> GetResult([FromQuery] int trainingId)
        {
            var result = await _aroundTheClockTraining.GetResultAsync(trainingId);

            return Ok(result);
        }
    }
}
