using file_upload_api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RestSharp;
using System.Text.Json;

namespace file_upload_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PdfSimilarityApiController : ControllerBase
    {
        private readonly RestClient _similarityClient = new("http://localhost:5000");
        private readonly BlockchainService _blockchainService;

        public PdfSimilarityApiController(BlockchainService blockchainService)
        {
            _blockchainService = blockchainService;
        }

        [HttpPost("train")]
        public async Task<IActionResult> TrainPdf(IFormFile pdf)
        {
            if (pdf == null)
                return BadRequest("PDF file required.");

            var request = new RestRequest("/upload_and_train", Method.Post);
            await using var ms = new MemoryStream();
            await pdf.CopyToAsync(ms);
            request.AddFile("pdf", ms.ToArray(), pdf.FileName, "application/pdf");

            var response = await _similarityClient.ExecuteAsync(request);

            if (!response.IsSuccessful)
                return StatusCode(500, response.Content);

            return Ok(response.Content);
        }

        [HttpPost("check_similarity")]
        public async Task<IActionResult> CheckAndRegister(IFormFile pdf)
        {
            if (pdf == null)
                return BadRequest("PDF file required.");

            // Step 1: Check similarity
            var similarityRequest = new RestRequest("/check_similarity", Method.Post);
            await using var similarityMs = new MemoryStream();
            await pdf.CopyToAsync(similarityMs);
            similarityRequest.AddFile("pdf", similarityMs.ToArray(), pdf.FileName, "application/pdf");

            var similarityResponse = await _similarityClient.ExecuteAsync(similarityRequest);

            if (!similarityResponse.IsSuccessful)
                return StatusCode(500, similarityResponse.Content);

            var similarityData = JsonSerializer.Deserialize<SimilarityResponse>(similarityResponse.Content);
            double similarityScore = similarityData?.similar_pdfs?.FirstOrDefault()?.similarity ?? 0;

            if (similarityScore >= 0.6)
                return BadRequest($"PDF similarity too high ({similarityScore:P2}). Cannot register.");

            // Step 2: Upload PDF to IPFS and Blockchain
            await using var pdfStream = new MemoryStream();
            await pdf.CopyToAsync(pdfStream);
            pdfStream.Position = 0; // reset stream

            var ipfsHash = await _blockchainService.UploadToIPFS(pdfStream, pdf.FileName);

            // User wallet address stored after Moralis Authentication
            //var userWalletAddress = HttpContext.Session.GetString("UserWalletAddress");
            var userWalletAddress = "0x87dc98819FAe36Ec910852D901f3eb4EBE8b024a";

            var tokenId = await _blockchainService.RegisterPdfOnBlockchain(ipfsHash, userWalletAddress);

            return Ok(new
            {
                Similarity = similarityScore,
                IPFS_Hash = ipfsHash,
                BlockchainTokenId = tokenId
            });
        }

        public class SimilarityResponse
        {
            public List<PdfSimilarity> similar_pdfs { get; set; }
        }

        public class PdfSimilarity
        {
            public string filename { get; set; }
            public double similarity { get; set; }
        }
    }

}
