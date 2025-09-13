using Microsoft.AspNetCore.Mvc;
using core_web.Areas.RelatedPatent.Models;

namespace core_web.Areas.RelatedPatent.Controllers
{
    [Area("RelatedPatent")]
    public class SearchController : Controller
    {
        public IActionResult Index()
        {
            return View();  
        }
        [HttpPost]
        public JsonResult ValidateKeyword(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return Json(new KeywordValidationResult { Valid = false });
            }

            return Json(new KeywordValidationResult { Valid = true });
        }
    }
}
