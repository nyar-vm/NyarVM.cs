using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;

namespace Atlas.Cloud.Azure;

/// <summary>
/// Azure Key Vault 密钥管理实现，使用 OAuth2 + REST API
/// </summary>
public sealed class AzureKeyVaultService : IKeyVaultService
{
    private readonly HttpClient _http;
    private readonly string _vault_name;
    private readonly string _tenant_id;
    private readonly string _client_id;
    private readonly string _client_secret;

    /// <summary>
    /// 缓存的 OAuth2 访问令牌
    /// </summary>
    private string? _cached_token;

    /// <summary>
    /// 访问令牌过期时间
    /// </summary>
    private DateTimeOffset _token_expires;

    /// <summary>
    /// 初始化 Azure Key Vault 密钥管理服务
    /// </summary>
    /// <param name="vaultName">Key Vault 名称</param>
    /// <param name="tenantId">Azure AD 租户 ID</param>
    /// <param name="clientId">应用程序客户端 ID</param>
    /// <param name="clientSecret">应用程序客户端密钥</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public AzureKeyVaultService(
        string vaultName, string tenantId, string clientId, string clientSecret,
        HttpClient? httpClient = null)
    {
        _vault_name = vaultName;
        _tenant_id = tenantId;
        _client_id = clientId;
        _client_secret = clientSecret;
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<string?> get_secret(string keyName, CancellationToken cancel = default)
    {
        var token = await get_access_token(cancel);
        var url = $"https://{_vault_name}.vault.azure.net/secrets/{keyName}?api-version=7.4";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Authorization", $"Bearer {token}");

        var response = await _http.SendAsync(request, cancel);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(cancel);
        using var doc = JsonDocument.Parse(responseBody);

        return doc.RootElement.TryGetProperty("value", out var valueElement)
            ? valueElement.get_string()
            : null;
    }

    /// <inheritdoc />
    public async Task set_secret(string keyName, string value, CancellationToken cancel = default)
    {
        var token = await get_access_token(cancel);
        var url = $"https://{_vault_name}.vault.azure.net/secrets/{keyName}?api-version=7.4";

        var body = JsonSerializer.Serialize(new { value });
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Put, url) { Content = content };
        request.Headers.Add("Authorization", $"Bearer {token}");

        var response = await _http.SendAsync(request, cancel);
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task delete_secret(string keyName, CancellationToken cancel = default)
    {
        var token = await get_access_token(cancel);
        var url = $"https://{_vault_name}.vault.azure.net/secrets/{keyName}?api-version=7.4";

        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        request.Headers.Add("Authorization", $"Bearer {token}");

        var response = await _http.SendAsync(request, cancel);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return;
        }

        response.EnsureSuccessStatusCode();
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

        var tokenUrl = $"https://login.microsoftonline.com/{_tenant_id}/oauth2/v2.0/token";

        var body = $"client_id={Uri.EscapeDataString(_client_id)}"
                   + $"&client_secret={Uri.EscapeDataString(_client_secret)}"
                   + $"&scope={Uri.EscapeDataString("https://vault.azure.net/.default")}"
                   + $"&grant_type=client_credentials";

        var content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");

        var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl) { Content = content };

        var response = await _http.SendAsync(request, cancel);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(cancel);
        using var doc = JsonDocument.Parse(responseBody);

        _cached_token = doc.RootElement.GetProperty("access_token").get_string()
                        ?? throw new InvalidOperationException("Azure OAuth2 令牌响应中缺少 access_token");

        var expiresIn = doc.RootElement.TryGetProperty("expires_in", out var expElement)
            ? expElement.GetInt32()
            : 3600;

        _token_expires = DateTimeOffset.UtcNow.AddSeconds(expiresIn - 60);

        return _cached_token;
    }
}
