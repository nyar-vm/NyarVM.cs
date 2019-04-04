using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;

namespace Atlas.Cloud.Azure;

/// <summary>
/// Azure Communication Services 短信服务实现，使用 OAuth2 Bearer 认证
/// </summary>
public sealed class AzureSmsProvider : ISmsProvider
{
    private readonly HttpClient _http;
    private readonly string _connection_string;
    private readonly string? _from_number;

    /// <summary>
    /// 缓存的 OAuth2 访问令牌
    /// </summary>
    private string? _cached_token;

    /// <summary>
    /// 访问令牌过期时间
    /// </summary>
    private DateTimeOffset _token_expires;

    /// <summary>
    /// 初始化 Azure 短信服务
    /// </summary>
    /// <param name="connectionString">Azure Communication Services 连接字符串</param>
    /// <param name="fromNumber">发送方号码</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public AzureSmsProvider(
        string connectionString, string? fromNumber = null,
        HttpClient? httpClient = null)
    {
        _connection_string = connectionString;
        _from_number = fromNumber;
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<SmsResult> send(
        string phoneNumber, string templateCode,
        IReadOnlyDictionary<string, string>? templateParams = null,
        string? signName = null,
        CancellationToken cancel = default)
    {
        var endpoint = extract_endpoint();
        var token = await get_access_token(cancel);

        var message = templateCode;

        if (templateParams is not null)
        {
            foreach (var kvp in templateParams)
            {
                message = message.Replace($"{{{kvp.Key}}}", kvp.Value);
            }
        }

        var payload = new Dictionary<string, object?>
        {
            ["to"] = new[] { phoneNumber },
            ["message"] = message
        };

        if (_from_number is not null)
        {
            payload["from"] = _from_number;
        }

        var requestBody = JsonSerializer.Serialize(payload);
        var url = $"https://{endpoint}/sms?api-version=2021-03-07";

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };

        request.Headers.Add("Authorization", $"Bearer {token}");

        var response = await _http.SendAsync(request, cancel);
        var responseBody = await response.Content.ReadAsStringAsync(cancel);

        using var doc = JsonDocument.Parse(responseBody);

        if (response.IsSuccessStatusCode)
        {
            var messageId = doc.RootElement.TryGetProperty("value", out var value)
                && value.GetArrayLength() > 0
                && value[0].TryGetProperty("messageId", out var mid)
                ? mid.get_string()
                : null;

            return SmsResult.ok(messageId);
        }

        var error = doc.RootElement.TryGetProperty("error", out var err)
            ? err.TryGetProperty("message", out var msg) ? msg.get_string() ?? "未知错误" : "未知错误"
            : "未知错误";

        return SmsResult.fail(error);
    }

    /// <summary>
    /// 从连接字符串提取端点
    /// </summary>
    /// <returns>端点主机名</returns>
    private string extract_endpoint()
    {
        var parts = _connection_string.Split(';');

        foreach (var part in parts)
        {
            if (part.StartsWith("endpoint=", StringComparison.OrdinalIgnoreCase))
            {
                var uri = part.Substring("endpoint=".Length).Trim();
                if (uri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    uri = uri.Substring("https://".Length);
                }

                return uri.TrimEnd('/');
            }
        }

        return "acs.communication.azure.com";
    }

    /// <summary>
    /// 获取 OAuth2 访问令牌，带缓存
    /// </summary>
    /// <param name="cancel">取消令牌</param>
    /// <returns>访问令牌</returns>
    private async Task<string> get_access_token(CancellationToken cancel)
    {
        if (_cached_token is not null && DateTimeOffset.UtcNow < _token_expires)
        {
            return _cached_token;
        }

        var endpoint = extract_endpoint();
        var tokenUrl = $"https://{endpoint}/identities/tokens?api-version=2022-10-01";

        var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };

        var authHeader = convert_to_basic_auth();
        request.Headers.Add("Authorization", $"Basic {authHeader}");

        var response = await _http.SendAsync(request, cancel);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(cancel);
        using var doc = JsonDocument.Parse(responseBody);

        _cached_token = doc.RootElement.TryGetProperty("accessToken", out var token)
            ? token.GetProperty("token").get_string()
              ?? throw new InvalidOperationException("Azure OAuth2 令牌响应中缺少 token")
            : throw new InvalidOperationException("Azure OAuth2 令牌响应中缺少 accessToken");

        _token_expires = DateTimeOffset.UtcNow.AddMinutes(55);

        return _cached_token;
    }

    /// <summary>
    /// 将连接字符串转换为 Basic Auth 头
    /// </summary>
    /// <returns>Base64 编码的 Basic Auth 值</returns>
    private string convert_to_basic_auth()
    {
        var parts = _connection_string.Split(';');
        var accessKey = string.Empty;

        foreach (var part in parts)
        {
            if (part.StartsWith("accesskey=", StringComparison.OrdinalIgnoreCase))
            {
                accessKey = part.Substring("accesskey=".Length);
            }
        }

        return Convert.ToBase64String(Encoding.UTF8.GetBytes($":{accessKey}"));
    }
}
