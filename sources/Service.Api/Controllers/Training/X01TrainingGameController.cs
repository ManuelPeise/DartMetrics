using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Enums.User;
using Shared.Interfaces.Training;
using Shared.Models.Training;

namespace Service.Api.Controllers.Training
{
    [Authorize(Roles = nameof(UserRoleEnum.Player))]
    public class X01TrainingGameController : ApiControllerBase
    {
        private readonly IX01TrainingGame _x01TrainingGame;

        public X01TrainingGameController(IX01TrainingGame x01TrainingGame)
        {
            _x01TrainingGame = x01TrainingGame;
        }

        [HttpGet(Name = "GetRunningX01Training")]
        public async Task<IActionResult> GetRunningX01Training()
        {
            var running = await _x01TrainingGame.GetRunningTrainingAsync();

            // 204, wenn nichts offen ist (der Client startet dann neu).
            return running is null ? NoContent() : Ok(running);
        }

        [HttpPost(Name = "StartX01Training")]
        public async Task<IActionResult> StartX01Training([FromBody] StartX01TrainingModel model)
        {
            var trainingId = await _x01TrainingGame.StartTrainingAsync(model);
            return Ok(trainingId);
        }

        [HttpPost(Name = "AddX01TrainingThrow")]
        public async Task<IActionResult> AddX01TrainingThrow([FromBody] X01TrainingThrow model)
        {
            var result = await _x01TrainingGame.AddThrowAsync(model);
            return Ok(result);
        }

        [HttpPost(Name = "FinishX01Training")]
        public async Task<IActionResult> FinishX01Training([FromBody] TrainingSessionRequestModel model)
        {
            await _x01TrainingGame.FinishTrainingAsync(model.TrainingId);
            return Ok();
        }

        [HttpGet(Name = "GetX01TrainingResult")]
        public async Task<IActionResult> GetX01TrainingResult([FromQuery] int trainingId)
        {
            var result = await _x01TrainingGame.GetResultAsync(trainingId);
            return Ok(result);
        }
    }
}
