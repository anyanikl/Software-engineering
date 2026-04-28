using System.Net;
using System.Net.Mail;
using FunApi.Interfaces;

namespace FunApi.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            var host = _configuration["Email:Smtp:Host"];
            var fromAddress = _configuration["Email:FromAddress"];

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromAddress))
            {
                _logger.LogWarning(
                    "Email settings are not configured. Email to {Email} with subject '{Subject}' was logged instead of being sent. Body: {Body}",
                    toEmail,
                    subject,
                    htmlBody);

                return;
            }

            using var message = new MailMessage
            {
                From = new MailAddress(fromAddress, _configuration["Email:FromName"] ?? "FinPay"),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            using var client = new SmtpClient(host, GetPort())
            {
                EnableSsl = GetBoolean("Email:Smtp:UseSsl", true),
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            var username = _configuration["Email:Smtp:Username"];
            var password = _configuration["Email:Smtp:Password"];
            if (!string.IsNullOrWhiteSpace(username))
            {
                client.Credentials = new NetworkCredential(username, password);
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await client.SendMailAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send email to {Email} with subject '{Subject}'. The email payload was: {Body}",
                    toEmail,
                    subject,
                    htmlBody);
            }
        }

        private int GetPort()
        {
            return int.TryParse(_configuration["Email:Smtp:Port"], out var port)
                ? port
                : 587;
        }

        private bool GetBoolean(string key, bool defaultValue)
        {
            return bool.TryParse(_configuration[key], out var parsed)
                ? parsed
                : defaultValue;
        }
    }
}
