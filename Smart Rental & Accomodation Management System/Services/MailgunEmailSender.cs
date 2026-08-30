using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;

namespace Smart_Rental___Accomodation_Management_System.Services
{
    public class MailgunOptions
    {
        // Both blank by default — leave unset to fall back to SmtpEmailSender (which itself
        // just logs when no SMTP host is configured either).
        public string? Domain { get; set; }
        public string? ApiKey { get; set; }
        public string BaseUrl { get; set; } = "https://api.mailgun.net";
        public string FromEmail { get; set; } = "no-reply@smartrental.local";
        public string FromName { get; set; } = "Smart Rental";
    }

    // Sends via Mailgun's HTTP API (https://api.mailgun.net/v3/{domain}/messages) using HTTP basic
    // auth with "api" as the username and the Mailgun API key as the password. Only constructed by
    // Program.cs when both Mailgun:Domain and Mailgun:ApiKey are configured.
    public class MailgunEmailSender : IEmailSender
    {
        private readonly HttpClient _httpClient;
        private readonly MailgunOptions _options;
        private readonly ILogger<MailgunEmailSender> _logger;

        public MailgunEmailSender(HttpClient httpClient, IOptions<MailgunOptions> options, ILogger<MailgunEmailSender> logger)
        {
            _options = options.Value;
            _logger = logger;

            httpClient.BaseAddress = new Uri(_options.BaseUrl);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"api:{_options.ApiKey}")));
            _httpClient = httpClient;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var form = new Dictionary<string, string>
            {
                ["from"] = $"{_options.FromName} <{_options.FromEmail}>",
                ["to"] = toEmail,
                ["subject"] = subject,
                ["html"] = htmlMessage
            };

            using var content = new FormUrlEncodedContent(form);
            using var response = await _httpClient.PostAsync($"/v3/{_options.Domain}/messages", content);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Mailgun send to {To} failed with {Status}: {Body}", toEmail, response.StatusCode, body);
            }
        }
    }
}
