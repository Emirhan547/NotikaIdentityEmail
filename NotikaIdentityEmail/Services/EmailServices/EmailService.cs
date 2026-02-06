using Microsoft.Extensions.Options;
using NotikaIdentityEmail.Logging;
using NotikaIdentityEmail.Models;
using MimeKit;
using MailKit.Net.Smtp;

namespace NotikaIdentityEmail.Services.EmailServices
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;
        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task SendAsync(string to, string subject, string body)
        {
           

            try
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

                using var client = new SmtpClient();

                await client.ConnectAsync(
                    _emailSettings.SmtpServer,
                    _emailSettings.SmtpPort,
                    _emailSettings.UseSsl);

                await client.AuthenticateAsync(
                    _emailSettings.Username,
                    _emailSettings.Password);

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

              
            }
            catch (Exception ex)
            {
                using (_logger.BeginScope(BuildSystemScope(to)))
                {
                    _logger.LogError(ex, LogMessages.UnexpectedError);
                }
                throw;
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