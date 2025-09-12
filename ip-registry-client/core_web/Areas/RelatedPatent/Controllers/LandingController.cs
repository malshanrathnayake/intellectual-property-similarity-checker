using Microsoft.AspNetCore.Mvc;

namespace core_web.Areas.RelatedPatent.Controllers
{
    [Area("RelatedPatent")]
    public class LandingController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
