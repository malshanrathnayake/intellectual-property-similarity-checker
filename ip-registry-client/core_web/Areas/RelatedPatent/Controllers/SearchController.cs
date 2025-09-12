using Microsoft.AspNetCore.Mvc;

namespace core_web.Areas.RelatedPatent.Controllers
{
    [Area("RelatedPatent")]
    public class SearchController : Controller
    {
        public IActionResult Index()
        {
            return View();  
        }
    }
}
