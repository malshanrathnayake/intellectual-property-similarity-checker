using Microsoft.AspNetCore.Mvc;

namespace core_web.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    public class LandingController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
