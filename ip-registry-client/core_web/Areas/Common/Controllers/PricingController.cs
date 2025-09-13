using Microsoft.AspNetCore.Mvc;

namespace core_web.Areas.Common.Controllers
{
    public class PricingController : Controller
    {
        [Area("Common")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
