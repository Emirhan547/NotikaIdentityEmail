namespace NotikaIdentityEmail.Models
{
    public class MessageWithSenderInfoViewModel
    {
        public int MessageId { get; set; }
        public string Subject { get; set; }
        public string MessageDetail { get; set; }
        public string SenderEmail { get; set; }
        public string SenderName { get; set; }
        public string SenderSurname { get; set; }
        public string CategoryName { get; set; }

        public DateTime SendDate { get; set; }

    }
}
