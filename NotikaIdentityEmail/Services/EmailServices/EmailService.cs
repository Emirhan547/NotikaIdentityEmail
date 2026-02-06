using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using NotikaIdentityEmail.Logging;
using NotikaIdentityEmail.Models;

namespace NotikaIdentityEmail.Services.EmailServices
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IOptions<EmailSettings> emailSettings,
            ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                _emailSettings.SenderName,
                _emailSettings.SenderEmail));

            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            message.Body = new BodyBuilder
            {
                HtmlBody = body
            }.ToMessageBody();

            using var smtp = new SmtpClient();

            var secureOption = _emailSettings.UseSsl
                ? SecureSocketOptions.SslOnConnect   // 465
                : SecureSocketOptions.StartTls;      // 587

            await smtp.ConnectAsync(
                _emailSettings.SmtpServer,
                _emailSettings.SmtpPort,
                secureOption);

            await smtp.AuthenticateAsync(
                _emailSettings.Username,
                _emailSettings.Password);

            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);

            using (_logger.BeginScope(BuildSystemScope(to)))
            {
                _logger.LogInformation("E-posta başarıyla gönderildi");
            }
        }

        private static Dictionary<string, object?> BuildSystemScope(string? userEmail)
        {
            var scope = new Dictionary<string, object?>
            {
                ["OperationType"] = LogContextValues.OperationSystem
            };

            if (!string.IsNullOrWhiteSpace(userEmail))
            {
                scope["UserEmail"] = userEmail;
            }

            return scope;
        }
    }
}
