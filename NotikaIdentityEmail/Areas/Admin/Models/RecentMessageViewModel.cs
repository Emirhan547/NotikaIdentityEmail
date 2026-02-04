namespace NotikaIdentityEmail.Areas.Admin.Models
{
    public class RecentMessageViewModel
    {
        public string SenderEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public DateTime SendDate { get; set; }
        public bool IsRead { get; set; }
    }
}
