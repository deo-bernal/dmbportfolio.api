using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Dmb.Service.Services;

public sealed class ResendHttpEmailService
{
    private const string DefaultApiBaseUrl = "https://api.resend.com";
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ResendHttpEmailService> _logger;

    public ResendHttpEmailService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ResendHttpEmailService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toEmail);

        var fromEmail = _configuration["Email:FromEmail"]
            ?? throw new InvalidOperationException("Email:FromEmail is not configured.");
        var apiKey = _configuration["Email:ResendApiKey"]
            ?? throw new InvalidOperationException("Email:ResendApiKey is not configured.");
        var apiBaseUrl = (_configuration["Email:ResendApiBaseUrl"] ?? DefaultApiBaseUrl).TrimEnd('/');

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{apiBaseUrl}/emails");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(new
        {
            from = fromEmail,
            to = new[] { toEmail },
            subject,
            html = htmlBody
        });

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Resend API send failed. Status: {StatusCode}, To: {ToEmail}, Subject: {Subject}, Body: {ResponseBody}",
                    (int)response.StatusCode,
                    toEmail,
                    subject,
                    responseBody);
                response.EnsureSuccessStatusCode();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Resend API send threw exception. To: {ToEmail}, Subject: {Subject}",
                toEmail,
                subject);
            throw;
        }
    }
}
