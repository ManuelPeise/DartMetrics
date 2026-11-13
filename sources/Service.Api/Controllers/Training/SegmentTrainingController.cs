using Microsoft.AspNetCore.Mvc;
using Shared.Interfaces.Training;
using Shared.Models.Training;


namespace Service.Api.Controllers.Training
{
    public class SegmentTrainingController : ApiControllerBase
    {
        private readonly ISegmentTraining _segmentTraining;

        public SegmentTrainingController(ISegmentTraining segmentTraining)
        {
            _segmentTraining = segmentTraining;
        }

        [HttpPost(Name = "StartSegmentTraining")]
        public async Task<IActionResult> StartSegmentTraining([FromBody] StartSegmentTrainingModel model)
        {
            var trainingId = await _segmentTraining.StartTrainingAsync(model);
            return Ok(trainingId);
        }


        [HttpPost(Name = "AddSegmentTrainingThrow")]
        public async Task<IActionResult> AddSegmentTrainingThrow([FromBody] SegmentTrainingThrow model)
        {
            await _segmentTraining.AddThrowAsync(model);
            
            return Ok();
        }

        [HttpPost(Name = "FinishSegmentTraining")]
        public async Task<IActionResult> FinishSegmentTraining([FromRoute] int trainingId)
        {
            await _segmentTraining.FinishTrainingAsync(trainingId);

            return Ok();
        }

        [HttpGet(Name = "GetSegmentTrainingResult")]
        public async Task<IActionResult> GetSegmentTrainingResult([FromRoute] int trainingId)
        {
            var result = await _segmentTraining.GetResultAsync(trainingId);

            return Ok(result);
        }
    }
}
