using core_web.Areas.Pdf.Controllers;
using core_web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xunit;

namespace ip_registry_client.Tests
{
    public class PdfSimilarityTests
    {
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<BlockchainService> _mockBlockchainService;

        public PdfSimilarityTests()
        {
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockBlockchainService = new Mock<BlockchainService>(null!, null!); // pass dummy deps
        }

        private LandingController CreateController(HttpResponseMessage similarityResponse)
        {
            var handler = new FakeHttpMessageHandler(similarityResponse);
            var client = new HttpClient(handler);
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

            return new LandingController(_mockHttpClientFactory.Object, _mockBlockchainService.Object);
        }

        // ✅ SUCCESS CASE: GET Index returns a View
        [Fact]
        public async Task Index_Get_ReturnsView()
        {
            var controller = CreateController(new HttpResponseMessage(HttpStatusCode.OK));
            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName); // uses default view
        }

        // ❌ FAILURE CASE: Missing fields should return error
        [Fact]
        public async Task Index_Post_MissingFields_ReturnsError()
        {
            var controller = CreateController(new HttpResponseMessage(HttpStatusCode.OK));
            var result = await controller.Index("", "", null);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.Contains(controller.ModelState, kvp => kvp.Value.Errors.Any());
        }

        // ❌ FAILURE CASE: Similarity ≥ 0.6 -> Should throw and set error
        [Fact]
        public async Task Index_Post_HighSimilarity_ReturnsError()
        {
            var similarityResponse = new LandingController.SimilarityResponse
            {
                similar_books = new List<LandingController.SimilarPdf>
                {
                    new LandingController.SimilarPdf { Similarity = 0.8 }
                }
            };

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(similarityResponse), Encoding.UTF8, "application/json")
            };

            var controller = CreateController(httpResponse);

            var fileMock = new Mock<IFormFile>();
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake pdf"));
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default))
                .Returns<Stream, CancellationToken>((s, c) => stream.CopyToAsync(s));
            fileMock.Setup(f => f.FileName).Returns("dup.pdf");

            var result = await controller.Index("Title", "Author", fileMock.Object);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Contains("similarity too high", viewResult.ViewData["Error"].ToString());
        }

        // === Helper Fake Handler for mocking HttpClient ===
        private class FakeHttpMessageHandler : HttpMessageHandler
        {
            private readonly HttpResponseMessage _response;

            public FakeHttpMessageHandler(HttpResponseMessage response)
            {
                _response = response;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(_response);
            }
        }
    }
}
