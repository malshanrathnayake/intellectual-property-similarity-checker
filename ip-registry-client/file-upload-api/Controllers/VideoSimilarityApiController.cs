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
            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(10)
            };

            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(video.OpenReadStream());
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("video/mp4");

            content.Add(streamContent, "video", video.FileName);

            var response = await httpClient.PostAsync("http://localhost:6000/upload_and_train_video", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return Ok(responseContent);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, errorContent);
        }


        [HttpPost("check-video")]
        public async Task<IActionResult> CheckVideo(IFormFile video)
        {
            using var httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromMinutes(10) //Increase timeout 
            };
            using var content = new MultipartFormDataContent();

            var videoStream = video.OpenReadStream();
            var streamContent = new StreamContent(videoStream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("video/mp4");

            content.Add(streamContent, "video", video.FileName);

            var response = await httpClient.PostAsync("http://localhost:6000/check_video_similarity", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return Ok(responseContent);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, errorContent);
        }


    }
}
