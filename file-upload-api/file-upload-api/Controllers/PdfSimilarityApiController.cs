using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RestSharp;

namespace file_upload_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PdfSimilarityApiController : ControllerBase
    {
        private readonly RestClient client = new("http://localhost:5000");

        [HttpPost("train")]
        public async Task<IActionResult> TrainPdf(IFormFile pdf)
        {
            if (pdf == null)
                return BadRequest("PDF file required.");

            var request = new RestRequest("/upload_and_train", Method.Post);
            await using var ms = new MemoryStream();
            await pdf.CopyToAsync(ms);
            request.AddFile("pdf", ms.ToArray(), pdf.FileName, "application/pdf");

            var response = await client.ExecuteAsync(request);

            if (!response.IsSuccessful)
                return StatusCode(500, response.Content);

            return Ok(response.Content);
        }

        [HttpPost("check")]
        public async Task<IActionResult> CheckSimilarity(IFormFile pdf)
        {
            if (pdf == null)
                return BadRequest("PDF file required.");

            var request = new RestRequest("/check_similarity", Method.Post);
            await using var ms = new MemoryStream();
            await pdf.CopyToAsync(ms);
            request.AddFile("pdf", ms.ToArray(), pdf.FileName, "application/pdf");

            var response = await client.ExecuteAsync(request);

            if (!response.IsSuccessful)
                return StatusCode(500, response.Content);

            return Ok(response.Content);
        }
    }
}
