using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;

namespace Atlas.Cloud.SendGrid;

/// <summary>
/// SendGrid 邮件发送服务实现，调用 SendGrid v3 Mail Send API
/// </summary>
public sealed class SendGridEmailSender : IEmailSender
{
    private readonly HttpClient _http;
    private readonly string _api_key;
    private readonly string _from_address;

    /// <summary>
    /// 初始化 SendGrid 邮件发送服务
    /// </summary>
    /// <param name="apiKey">SendGrid API 密钥</param>
    /// <param name="fromAddress">发件人地址</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public SendGridEmailSender(string apiKey, string? fromAddress = null, HttpClient? httpClient = null)
    {
        _api_key = apiKey;
        _from_address = fromAddress ?? "noreply@atlas.local";
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task send(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            from = new { email = _from_address },
            personalizations = new[]
            {
                new { to = new[] { new { email = to } } }
            },
            subject = subject,
            content = new[]
            {
                new { type = "text/plain", value = body }
            }
        };

        var json = JsonSerializer.Serialize(payload);

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.sendgrid.com/v3/mail/send");
        request.Headers.Add("Authorization", $"Bearer {_api_key}");
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        await _http.SendAsync(request, cancellationToken);
    }
}
