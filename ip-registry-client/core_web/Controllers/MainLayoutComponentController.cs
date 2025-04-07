using core_web.Models.Layout;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json;

namespace core_web.Controllers
{
    public class MainLayoutComponentController : Controller
    {
        public async Task<IActionResult> Sidebar()
        {
            var jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "sidebar.json");
            var json = await System.IO.File.ReadAllTextAsync(jsonPath);

            var sidebarItems = JsonSerializer.Deserialize<List<SidebarItem>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return View(sidebarItems);
        }

        public async Task<IActionResult> Header()
        {
            return View();
        }

        public async Task<IActionResult> Footer()
        {
            return View();
        }
    }
}
