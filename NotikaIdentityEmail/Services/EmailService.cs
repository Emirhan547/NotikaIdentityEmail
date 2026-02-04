using Microsoft.Extensions.Options;
using NotikaIdentityEmail.Logging;
using NotikaIdentityEmail.Models;
using Serilog;
using MimeKit;
using MailKit.Net.Smtp;

namespace NotikaIdentityEmail.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            Log.Information(LogMessages.EmailSendStarted, to, subject);

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

                Log.Information(LogMessages.EmailSendSucceeded, to);
            }
            catch (Exception ex)
            {
                Log.Error(ex, LogMessages.EmailSendFailed, to);
                throw;
            }
        }
    }
}
