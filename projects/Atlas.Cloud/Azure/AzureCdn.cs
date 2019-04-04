using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;
using Atlas.Cloud.Models;

namespace Atlas.Cloud.Azure;

/// <summary>
/// Azure CDN 服务实现，使用 OAuth2 Bearer 认证
/// </summary>
public sealed class AzureCdn : ICdnService
{
    private readonly HttpClient _http;
    private readonly string _tenant_id;
    private readonly string _client_id;
    private readonly string _client_secret;
    private readonly string _subscription_id;
    private readonly string _resource_group_name;
    private readonly string _profile_name;
    private string? _access_token;
    private DateTimeOffset _token_expiry;

    private const string TokenUrl = "https://login.microsoftonline.com/{0}/oauth2/v2.0/token";

    /// <summary>
    /// 初始化 Azure CDN 服务
    /// </summary>
    /// <param name="tenantId">租户标识</param>
    /// <param name="clientId">客户端标识</param>
    /// <param name="clientSecret">客户端密钥</param>
    /// <param name="subscriptionId">订阅标识</param>
    /// <param name="resourceGroupName">资源组名称</param>
    /// <param name="profileName">CDN 配置文件名称</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public AzureCdn(
        string tenantId, string clientId, string clientSecret,
        string subscriptionId, string resourceGroupName, string profileName,
        HttpClient? httpClient = null)
    {
        _tenant_id = tenantId;
        _client_id = clientId;
        _client_secret = clientSecret;
        _subscription_id = subscriptionId;
        _resource_group_name = resourceGroupName;
        _profile_name = profileName;
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<CdnResult> purge(string[] urls, CancellationToken ct = default)
    {
        return await execute_cdn_action("purge", urls, ct);
    }

    /// <inheritdoc />
    public async Task<CdnResult> prefetch(string[] urls, CancellationToken ct = default)
    {
        return await execute_cdn_action("load", urls, ct);
    }

    private async Task<CdnResult> execute_cdn_action(string action, string[] urls, CancellationToken ct)
    {
        try
        {
            var token = await get_access_token(ct);

            var endpoint_name = extract_endpoint_name(urls.FirstOrDefault() ?? string.Empty);

            var base_path = $"/subscriptions/{_subscription_id}/resourceGroups/{_resource_group_name}"
                            + $"/providers/Microsoft.Cdn/profiles/{_profile_name}/endpoints/{endpoint_name}";
            var api_url = $"https://management.azure.com{base_path}/{action}?api-version=2024-02-01";

            var body = new
            {
                contentPaths = urls.Select(u => new Uri(u).AbsolutePath).Distinct().ToList()
            };

            var json = JsonSerializer.Serialize(body);

            using var http_request = new HttpRequestMessage(HttpMethod.Post, api_url);
            http_request.Headers.Add("Authorization", $"Bearer {token}");
            http_request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(http_request, ct);

            if (response.IsSuccessStatusCode)
            {
                var task_id = response.Headers.Location?.ToString();
                return CdnResult.ok(task_id);
            }

            var error_body = await response.Content.ReadAsStringAsync(ct);
            return CdnResult.fail($"Azure CDN {action} 失败: {response.StatusCode} - {error_body}");
        }
        catch (HttpRequestException ex)
        {
            return CdnResult.fail($"HTTP 请求失败: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return CdnResult.fail($"JSON 解析失败: {ex.Message}");
        }
    }

    private async Task<string> get_access_token(CancellationToken ct)
    {
        if (_access_token is not null && _token_expiry > DateTimeOffset.UtcNow)
        {
            return _access_token;
        }

        var token_url = string.Format(TokenUrl, _tenant_id);

        var body = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _client_id,
            ["client_secret"] = _client_secret,
            ["scope"] = "https://management.azure.com/.default"
        };

        using var http_request = new HttpRequestMessage(HttpMethod.Post, token_url);
        http_request.Content = new FormUrlEncodedContent(body);

        var response = await _http.SendAsync(http_request, ct);
        var response_body = await response.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(response_body);
        var root = doc.RootElement;

        _access_token = root.GetProperty("access_token").get_string()!;
        var expires_in = root.TryGetProperty("expires_in", out var exp_el) ? exp_el.GetInt32() : 3600;
        _token_expiry = DateTimeOffset.UtcNow.AddSeconds(expires_in - 60);

        return _access_token;
    }

    private static string extract_endpoint_name(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return "default";
        }

        try
        {
            var host = new Uri(url).Host;
            var parts = host.Split('.');
            return parts[0];
        }
        catch (UriFormatException)
        {
            return "default";
        }
    }
}
