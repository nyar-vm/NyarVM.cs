using System.Text;
using Atlas.Cloud.Interfaces;

namespace Atlas.Cloud.Twilio;

/// <summary>
/// Twilio 短信服务实现，使用 Twilio REST API Basic Auth 认证
/// </summary>
public sealed class TwilioSmsProvider : ISmsProvider
{
    private readonly HttpClient _http;
    private readonly string _account_sid;
    private readonly string _auth_token;
    private readonly string _from_number;

    /// <summary>
    /// 初始化 Twilio 短信服务
    /// </summary>
    /// <param name="accountSid">Twilio Account SID</param>
    /// <param name="authToken">Twilio Auth Token</param>
    /// <param name="fromNumber">发送方号码</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public TwilioSmsProvider(
        string accountSid, string authToken, string fromNumber,
        HttpClient? httpClient = null)
    {
        _account_sid = accountSid;
        _auth_token = authToken;
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
        var url = $"https://api.twilio.com/2010-04-01/Accounts/{_account_sid}/Messages.json";

        var message = templateCode;

        if (templateParams is not null)
        {
            foreach (var kvp in templateParams)
            {
                message = message.Replace($"{{{kvp.Key}}}", kvp.Value);
            }
        }

        var body = $"To={Uri.EscapeDataString(phoneNumber)}"
                   + $"&From={Uri.EscapeDataString(_from_number)}"
                   + $"&Body={Uri.EscapeDataString(message)}";

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded")
        };

        var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_account_sid}:{_auth_token}"));
        request.Headers.Add("Authorization", $"Basic {authValue}");

        var response = await _http.SendAsync(request, cancel);
        var responseBody = await response.Content.ReadAsStringAsync(cancel);

        using var doc = System.Text.Json.JsonDocument.Parse(responseBody);

        if (response.IsSuccessStatusCode)
        {
            var sid = doc.RootElement.TryGetProperty("sid", out var sidElem)
                ? sidElem.get_string()
                : null;
            return SmsResult.ok(sid);
        }

        var error = doc.RootElement.TryGetProperty("message", out var msg)
            ? msg.get_string() ?? "未知错误"
            : "未知错误";
        return SmsResult.fail(error);
    }
}
