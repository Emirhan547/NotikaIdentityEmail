namespace NotikaIdentityEmail.Models
{
    public class EmailSettings
    {
        public string SenderName { get; set; } = null!;
        public string SenderEmail { get; set; } = null!;

        public string SmtpServer { get; set; } = null!;
        public int SmtpPort { get; set; }
        public bool UseSsl { get; set; }

        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}
