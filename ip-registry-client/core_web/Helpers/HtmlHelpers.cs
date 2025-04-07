using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;

namespace core_web.Helpers
{
    public static class HtmlHelpers
    {
        public static IHtmlContent RenderAction(this IHtmlHelper helper, string action, object parameters = null)
        {
            var controller = (string)helper.ViewContext.RouteData.Values["controller"];
            return RenderAction(helper, action, controller, null, parameters);
        }

        public static IHtmlContent RenderAction(this IHtmlHelper helper, string action, string controller, object parameters = null)
        {
            return RenderAction(helper, action, controller, null, parameters);
        }

        public static IHtmlContent RenderAction(this IHtmlHelper helper, string action, string controller, string area, object parameters = null)
        {
            if (string.IsNullOrWhiteSpace(action)) throw new ArgumentNullException(nameof(action));
            if (string.IsNullOrWhiteSpace(controller)) throw new ArgumentNullException(nameof(controller));

            return RenderActionAsync(helper, action, controller, area, parameters).GetAwaiter().GetResult();
        }

        private static async Task<IHtmlContent> RenderActionAsync(this IHtmlHelper helper, string action, string controller, string area, object parameters = null)
        {
            var currentHttpContext = helper.ViewContext.HttpContext;
            var httpContextFactory = GetServiceOrFail<IHttpContextFactory>(currentHttpContext);
            var actionInvokerFactory = GetServiceOrFail<IActionInvokerFactory>(currentHttpContext);
            var actionSelector = GetServiceOrFail<IActionDescriptorCollectionProvider>(currentHttpContext);

            var newHttpContext = httpContextFactory.Create(currentHttpContext.Features);
            using var outputStream = new MemoryStream();
            newHttpContext.Response.Body = outputStream;

            var routeValues = new RouteValueDictionary(parameters ?? new { });
            routeValues["controller"] = controller;
            routeValues["action"] = action;
            if (!string.IsNullOrEmpty(area)) routeValues["area"] = area;

            var routeData = new RouteData();
            foreach (var router in helper.ViewContext.RouteData.Routers)
                routeData.PushState(router, null, null);

            routeData.PushState(null, routeValues, null);

            // Try to find matching action descriptor
            var actionDescriptor = actionSelector.ActionDescriptors.Items
                .FirstOrDefault(x =>
                    string.Equals(x.RouteValues["controller"], controller, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(x.RouteValues["action"], action, StringComparison.OrdinalIgnoreCase) &&
                    (string.IsNullOrEmpty(area) || string.Equals(x.RouteValues["area"], area, StringComparison.OrdinalIgnoreCase)));

            if (actionDescriptor == null)
            {
                throw new InvalidOperationException($"Action '{action}' in controller '{controller}'{(area != null ? $" with area '{area}'" : "")} not found.");
            }

            var actionContext = new ActionContext(newHttpContext, routeData, actionDescriptor);
            var invoker = actionInvokerFactory.CreateInvoker(actionContext);

            if (invoker == null)
            {
                throw new InvalidOperationException("Could not create action invoker.");
            }

            await invoker.InvokeAsync();

            newHttpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(newHttpContext.Response.Body);
            var content = await reader.ReadToEndAsync();

            return new HtmlString(content);
        }

        private static TService GetServiceOrFail<TService>(HttpContext context)
        {
            return context.RequestServices.GetService(typeof(TService)) is TService service
                ? service
                : throw new InvalidOperationException($"Service '{typeof(TService).Name}' not available.");
        }
    }
}
