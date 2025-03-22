using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RestSharp;

namespace file_upload_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PatentSimilarityApiController : ControllerBase
    {
        private readonly RestClient client = new("http://localhost:8000");

        [HttpPost("train-patent")]
        public async Task<IActionResult> TrainPatent([FromBody] PatentTrainDto patent)
        {
            var request = new RestRequest("/train_patent", Method.Post);
            request.AddJsonBody(patent);

            var response = await client.ExecuteAsync(request);
            return response.IsSuccessful ? Ok(response.Content) : StatusCode(500, response.Content);
        }

        [HttpPost("check-patent-similarity")]
        public async Task<IActionResult> CheckPatentSimilarity([FromBody] PatentDto patent)
        {
            var request = new RestRequest("/check_patent_similarity", Method.Post);
            request.AddJsonBody(patent);

            var response = await client.ExecuteAsync(request);
            return response.IsSuccessful ? Ok(response.Content) : StatusCode(500, response.Content);
        }

    }

    // DTO for patent training
    public class PatentTrainDto
    {
        public string Patent_id { get; set; }
        public string Title { get; set; }
        public string Abstract { get; set; }
        public string Claims { get; set; }
    }

    // DTO for patent fields
    public class PatentDto
    {
        public string Title { get; set; }
        public string Abstract { get; set; }
        public string Claims { get; set; }
    }

}
