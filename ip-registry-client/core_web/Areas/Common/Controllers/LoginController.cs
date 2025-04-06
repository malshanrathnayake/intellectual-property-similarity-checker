using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace core_web.Areas.Common.Controllers
{
    [Area("Common")]
    public class LoginController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<LoginController> _logger;

        public LoginController(IHttpClientFactory httpClientFactory, ILogger<LoginController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Landing", new { area = "Dashboard" });
            }

            return View();
        }

        public async Task<IActionResult> GetChallenge([FromBody] ChallengeRequestModel model)
        {
            var moralisUrl = "https://authapi.moralis.io/challenge/request/evm";

            var payload = new
            {
                domain = model.Domain,
                chainId = model.ChainId,
                address = model.Address,
                statement = "Sign in with your Ethereum wallet",
                uri = model.Uri,
                version = "1",
                expirationTime = DateTime.Now.AddMinutes(5).ToString("o"),
                timeout = 30
            };

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            httpClient.DefaultRequestHeaders.Add("X-API-Key", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJub25jZSI6ImM5MzBkMjkxLTI5MmItNGNiNy04M2NiLTZjMGNjNDM0ZGExOSIsIm9yZ0lkIjoiNDI2ODUwIiwidXNlcklkIjoiNDM5MDUxIiwidHlwZUlkIjoiNGMxY2U2ZTctMzdlMy00NWI1LTk3YzQtOWM5N2Y0MWIwZjU5IiwidHlwZSI6IlBST0pFQ1QiLCJpYXQiOjE3Mzc0Njk2NzgsImV4cCI6NDg5MzIyOTY3OH0.XPAe6UmYHBR4ArNzW1zDS1DT8RSo3sbhrklbRlQjtBk");

            var jsonPayload = JsonSerializer.Serialize(payload);
            var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var moralisResponse = await httpClient.PostAsync(moralisUrl, httpContent);
            var responseContent = await moralisResponse.Content.ReadAsStringAsync();

            if (!moralisResponse.IsSuccessStatusCode)
            {
                return BadRequest(responseContent);
            }

            var challengeData = JsonSerializer.Deserialize<MoralisChallengeResponse>(responseContent);
            return Ok(challengeData);
        }

        public async Task<IActionResult> Authenticate([FromBody] MetaMaskAuthRequest request)
        {

            var moralisUrl = "https://authapi.moralis.io/challenge/verify/evm";

            var payload = new
            {
                message = request.Message,
                signature = request.Signature,
                account = request.Address
            };

            using var httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            httpClient.DefaultRequestHeaders.Add("X-API-Key", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJub25jZSI6ImM5MzBkMjkxLTI5MmItNGNiNy04M2NiLTZjMGNjNDM0ZGExOSIsIm9yZ0lkIjoiNDI2ODUwIiwidXNlcklkIjoiNDM5MDUxIiwidHlwZUlkIjoiNGMxY2U2ZTctMzdlMy00NWI1LTk3YzQtOWM5N2Y0MWIwZjU5IiwidHlwZSI6IlBST0pFQ1QiLCJpYXQiOjE3Mzc0Njk2NzgsImV4cCI6NDg5MzIyOTY3OH0.XPAe6UmYHBR4ArNzW1zDS1DT8RSo3sbhrklbRlQjtBk");

            var jsonPayload = JsonSerializer.Serialize(payload);

            var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var moralisResponse = await httpClient.PostAsync(moralisUrl, httpContent);
            var responseContent = await moralisResponse.Content.ReadAsStringAsync();

            if (!moralisResponse.IsSuccessStatusCode)
            {
                return Unauthorized("Signature verification failed.");
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, request.Address),
                new Claim("WalletAddress", request.Address)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), authProperties);

            return Json(new { success = true, redirectUrl = Url.Action("Index", "Landing", new { area = "Dashboard" }) });

        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index");
        }

        public class MetaMaskAuthRequest
        {
            public string Message { get; set; }
            public string Signature { get; set; }
            public string Address { get; set; }
        }

        public class ChallengeRequestModel
        {
            public string Domain { get; set; }
            public int ChainId { get; set; }
            public string Address { get; set; }
            public string Uri { get; set; }
        }

        public class MoralisChallengeResponse
        {
            public string id { get; set; }
            public string message { get; set; }
            public string profileId { get; set; }
        }
    }
}
