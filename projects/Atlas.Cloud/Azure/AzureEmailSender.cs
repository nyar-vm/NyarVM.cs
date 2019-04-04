using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;

namespace Atlas.Cloud.Azure;

/// <summary>
/// Azure Communication Services 邮件发送服务实现，使用 OAuth2 Bearer 认证
/// </summary>
public sealed class AzureEmailSender : IEmailSender
{
    private readonly HttpClient _http;
    private readonly string _connection_string;
    private readonly string _from_address;
    private readonly string _endpoint;

    /// <summary>
    /// 初始化 Azure 邮件发送服务
    /// </summary>
    /// <param name="connectionString">Azure Communication Services 连接字符串</param>
    /// <param name="fromAddress">发件人地址</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public AzureEmailSender(string connectionString, string? fromAddress = null, HttpClient? httpClient = null)
    {
        _connection_string = connectionString;
        _from_address = fromAddress ?? extract_from_connection(connectionString, "senderAddress") ?? "noreply@atlas.local";
        _endpoint = extract_from_connection(connectionString, "endpoint") ?? "https://communication.azure.com";
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task send(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var url = $"{_endpoint}/emails:send?api-version=2023-03-31";

        var payload = new
        {
            senderAddress = _from_address,
            content = new { subject = subject, plainText = body },
            recipients = new { to = new[] { new { address = to } } }
        };

        var json = JsonSerializer.Serialize(payload);

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Authorization", $"Bearer {await get_access_token(cancellationToken)}");
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        await _http.SendAsync(request, cancellationToken);
    }

    private async Task<string> get_access_token(CancellationToken ct)
    {
        var key = extract_from_connection(_connection_string, "accesskey");
        if (key is not null)
        {
            return key;
        }

        var tenant = extract_from_connection(_connection_string, "tenantId") ?? "common";
        var client_id = extract_from_connection(_connection_string, "clientId") ?? string.Empty;
        var client_secret = extract_from_connection(_connection_string, "clientSecret") ?? string.Empty;
        var scope = "https://communication.azure.com/.default";

        var token_url = $"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token";
        var token_body = $"client_id={Uri.EscapeDataString(client_id)}&client_secret={Uri.EscapeDataString(client_secret)}&scope={Uri.EscapeDataString(scope)}&grant_type=client_credentials";

        using var token_request = new HttpRequestMessage(HttpMethod.Post, token_url);
        token_request.Content = new StringContent(token_body, Encoding.UTF8, "application/x-www-form-urlencoded");

        var token_response = await _http.SendAsync(token_request, ct);
        var token_json = await token_response.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(token_json);
        return doc.RootElement.TryGetProperty("access_token", out var token_el) ? token_el.get_string() ?? string.Empty : string.Empty;
    }

    private static string? extract_from_connection(string connStr, string key)
    {
        foreach (var part in connStr.Split(';'))
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2 && kv[0].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                return kv[1].Trim();
            }
        }

        return null;
    }
}
