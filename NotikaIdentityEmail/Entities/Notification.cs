namespace NotikaIdentityEmail.Entities
{
    public class Notification
    {
        public int NotificationId { get; set; }
        public string Title { get; set; } = "Sistem Bildirimi";
        public string Detail { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? RecipientEmail { get; set; }
        public string? RecipientRole { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsRead { get; set; }
    }
}
