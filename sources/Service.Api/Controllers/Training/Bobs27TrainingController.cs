using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Enums.User;
using Shared.Interfaces.Training;
using Shared.Models.Training;

namespace Service.Api.Controllers.Training
{
    [Authorize(Roles = nameof(UserRoleEnum.Player))]
    public class Bobs27TrainingController : ApiControllerBase
    {
        private readonly IBobs27Training _bobs27Training;

        public Bobs27TrainingController(IBobs27Training bobs27Training)
        {
            _bobs27Training = bobs27Training;
        }

        [HttpGet(Name = "GetRunningBobs27Training")]
        public async Task<IActionResult> GetRunningBobs27Training()
        {
            var running = await _bobs27Training.GetRunningTrainingAsync();

            // 204, wenn nichts offen ist (der Client zeigt dann die Einstellungen).
            return running is null ? NoContent() : Ok(running);
        }

        [HttpPost(Name = "StartBobs27Training")]
        public async Task<IActionResult> StartBobs27Training([FromBody] StartBobs27TrainingModel model) =>
            Ok(await _bobs27Training.StartTrainingAsync(model));

        [HttpPost(Name = "AddBobs27TrainingTurn")]
        public async Task<IActionResult> AddBobs27TrainingTurn([FromBody] Bobs27TrainingTurnModel model) =>
            Ok(await _bobs27Training.AddTurnAsync(model));

        [HttpPost(Name = "UndoBobs27TrainingTurn")]
        public async Task<IActionResult> UndoBobs27TrainingTurn([FromBody] Bobs27TrainingUndoModel model) =>
            Ok(await _bobs27Training.UndoLastTurnAsync(model));

        [HttpPost(Name = "FinishBobs27Training")]
        public async Task<IActionResult> FinishBobs27Training([FromBody] TrainingSessionRequestModel model)
        {
            await _bobs27Training.FinishTrainingAsync(model.TrainingId);
            return Ok();
        }

        [HttpGet(Name = "GetBobs27Training")]
        public async Task<IActionResult> GetBobs27Training([FromQuery] int trainingId) =>
            Ok(await _bobs27Training.GetTrainingAsync(trainingId));
    }
}
