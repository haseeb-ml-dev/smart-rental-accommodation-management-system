using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Smart_Rental___Accomodation_Management_System.Services
{
    public class EmailSenderOptions
    {
        public string? Host { get; set; }
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string FromEmail { get; set; } = "no-reply@smartrental.local";
        public string FromName { get; set; } = "Smart Rental";
    }

    public class SmtpEmailSender : IEmailSender
    {
        private readonly EmailSenderOptions _options;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IOptions<EmailSenderOptions> options, ILogger<SmtpEmailSender> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        // No SMTP host configured (e.g. local dev without credentials) — log the message
        // instead of failing the registration/reset flow that triggered it.
        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            if (string.IsNullOrWhiteSpace(_options.Host))
            {
                _logger.LogWarning(
                    "Email not sent (no SMTP host configured in the 'Email' settings section). To: {To}, Subject: {Subject}\n{Body}",
                    toEmail, subject, htmlMessage);
                return;
            }

            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromEmail, _options.FromName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.EnableSsl,
                Credentials = string.IsNullOrEmpty(_options.Username)
                    ? null
                    : new NetworkCredential(_options.Username, _options.Password)
            };

            await client.SendMailAsync(message);
        }
    }
}
