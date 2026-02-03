namespace NotikaIdentityEmail.Models
{
    public class MessageTrashViewModel
    {
        public int MessageId { get; set; }
        public string Subject { get; set; }
        public string SenderEmail { get; set; }
        public string ReceiverEmail { get; set; }
        public string CategoryName { get; set; }
        public DateTime SendDate { get; set; }
        public bool IsRead { get; set; }
    }
}