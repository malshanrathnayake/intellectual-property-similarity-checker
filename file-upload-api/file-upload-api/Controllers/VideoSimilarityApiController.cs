using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RestSharp;

namespace file_upload_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VideoSimilarityApiController : ControllerBase
    {
        private readonly RestClient client = new("http://localhost:6000");

        [HttpPost("train-video")]
        public async Task<IActionResult> TrainVideo(IFormFile video)
        {
            var request = new RestRequest("/upload_and_train_video", Method.Post);

            await using var ms = new MemoryStream();
            await video.CopyToAsync(ms);
            request.AddFile("video", ms.ToArray(), video.FileName, "video/mp4");

            var response = await client.ExecuteAsync(request);
            return response.IsSuccessful ? Ok(response.Content) : StatusCode(500, response.Content);
        }

        [HttpPost("check-video")]
        public async Task<IActionResult> CheckVideo(IFormFile video)
        {
            var request = new RestRequest("/check_video_similarity", Method.Post);

            await using var ms = new MemoryStream();
            await video.CopyToAsync(ms);
            request.AddFile("video", ms.ToArray(), video.FileName, "video/mp4");

            var response = await client.ExecuteAsync(request);
            return response.IsSuccessful ? Ok(response.Content) : StatusCode(500, response.Content);
        }

    }
}
