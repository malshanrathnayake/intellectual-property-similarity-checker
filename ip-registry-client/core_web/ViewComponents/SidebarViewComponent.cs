using core_web.Models.Layout;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace core_web.ViewComponents
{
    public class SidebarViewComponent : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "sidebar.json");
            var json = await System.IO.File.ReadAllTextAsync(jsonPath);

            var sidebarItems = JsonSerializer.Deserialize<List<SidebarItem>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return View(sidebarItems);
        }
    }
}
