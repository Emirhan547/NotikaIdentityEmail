namespace NotikaIdentityEmail.Areas.Admin.Models
{
    public class CategorySidebarItemViewModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryIconUrl { get; set; } = string.Empty;
        public int MessageCount { get; set; }
    }
}
