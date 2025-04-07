namespace core_web.Models.Layout
{
    public class BreadcrumbItem
    {
        public string Title { get; set; }
        public string? Url { get; set; }
        public bool IsActive { get; set; }
    }
}
