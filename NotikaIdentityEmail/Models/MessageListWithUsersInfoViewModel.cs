namespace NotikaIdentityEmail.Models
{
    public class MessageListWithUsersInfoViewModel
    {
        public int MessageId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string MessageDetail { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime SendDate { get; set; }
    }
}