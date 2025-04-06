namespace core_web.Models.Layout
{
    public class SidebarItem
    {
        public string Title { get; set; }
        public string Icon { get; set; }
        public string Url { get; set; }
        public List<SidebarItem> Children { get; set; } = new();
    }
}
