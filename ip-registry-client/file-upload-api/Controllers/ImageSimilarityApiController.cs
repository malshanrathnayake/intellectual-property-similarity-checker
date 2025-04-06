using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RestSharp;

namespace file_upload_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageSimilarityApiController : ControllerBase
    {
        private readonly RestClient client = new("http://localhost:7000");

        [HttpPost("train-image")]
        public async Task<IActionResult> TrainImage(IFormFile image)
        {
            var request = new RestRequest("/upload_and_train_image", Method.Post);

            await using var ms = new MemoryStream();
            await image.CopyToAsync(ms);
            request.AddFile("image", ms.ToArray(), image.FileName, "image/jpeg");

            var response = await client.ExecuteAsync(request);
            return response.IsSuccessful ? Ok(response.Content) : StatusCode(500, response.Content);
        }

        [HttpPost("check-image")]
        public async Task<IActionResult> CheckImage(IFormFile image)
        {
            var request = new RestRequest("/check_image_similarity", Method.Post);

            await using var ms = new MemoryStream();
            await image.CopyToAsync(ms);
            request.AddFile("image", ms.ToArray(), image.FileName, "image/jpeg");

            var response = await client.ExecuteAsync(request);
            return response.IsSuccessful ? Ok(response.Content) : StatusCode(500, response.Content);
        }

    }
}
