using Microsoft.AspNetCore.Mvc;

namespace core_web.ViewComponents
{
    public class HeaderViewComponent : ViewComponent
    {
        public class BreadcrumbItem
        {
            public string Title { get; set; }
            public string? Url { get; set; }
            public bool IsActive { get; set; }
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var routeData = ViewContext.RouteData;
            var area = routeData.Values["area"]?.ToString();
            var controller = routeData.Values["controller"]?.ToString();
            var action = routeData.Values["action"]?.ToString();

            var breadcrumbs = new List<BreadcrumbItem>
            {
                //new BreadcrumbItem { Title = "Home", Url = Url.Action("Index", "Landing", new { area = "Dashboard" }) }
            };

            if (!string.IsNullOrEmpty(area))
                breadcrumbs.Add(new BreadcrumbItem { Title = area });

            if (!string.IsNullOrEmpty(controller))
                breadcrumbs.Add(new BreadcrumbItem { Title = controller, Url = $"/{area}/{controller}", IsActive = true });

            //if (!string.IsNullOrEmpty(action))
                //breadcrumbs.Add(new BreadcrumbItem { Title = action, IsActive = true });

            return View(breadcrumbs);
        }
    }
}
