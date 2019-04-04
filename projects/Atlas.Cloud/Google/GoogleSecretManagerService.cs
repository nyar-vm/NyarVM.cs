using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;

namespace Atlas.Cloud.Google;

/// <summary>
/// Google Secret Manager 密钥管理实现，使用 OAuth2 Bearer 认证
/// </summary>
public sealed class GoogleSecretManagerService : IKeyVaultService
{
    private readonly HttpClient _http;
    private readonly string _access_token;
    private readonly string _project_id;
    private const string _endpoint = "secretmanager.googleapis.com";

    /// <summary>
    /// 初始化 Google Secret Manager 密钥管理服务
    /// </summary>
    /// <param name="accessToken">OAuth2 访问令牌</param>
    /// <param name="projectId">GCP 项目 ID</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public GoogleSecretManagerService(
        string accessToken, string projectId, HttpClient? httpClient = null)
    {
        _access_token = accessToken;
        _project_id = projectId;
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<string?> get_secret(string keyName, CancellationToken cancel = default)
    {
        var url = $"https://{_endpoint}/v1/projects/{_project_id}/secrets/{keyName}/versions/latest:access";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Authorization", $"Bearer {_access_token}");

        var response = await _http.SendAsync(request, cancel);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(cancel);
        using var doc = JsonDocument.Parse(responseBody);

        if (doc.RootElement.TryGetProperty("payload", out var payload))
        {
            var base64Data = payload.TryGetProperty("data", out var data)
                ? data.get_string()
                : null;

            if (base64Data is null)
            {
                return null;
            }

            var bytes = Convert.FromBase64String(base64Data);
            return Encoding.UTF8.GetString(bytes);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task set_secret(string keyName, string value, CancellationToken cancel = default)
    {
        var existing = await try_secret_exists(keyName, cancel);

        if (!existing)
        {
            var createUrl = $"https://{_endpoint}/v1/projects/{_project_id}/secrets";
            var createBody = JsonSerializer.Serialize(new
            {
                replication = new { automatic = new { } }
            });

            var createRequest = new HttpRequestMessage(HttpMethod.Post, createUrl)
            {
                Content = new StringContent(createBody, Encoding.UTF8, "application/json")
            };
            createRequest.Headers.Add("Authorization", $"Bearer {_access_token}");

            var createResponse = await _http.SendAsync(createRequest, cancel);

            if (!createResponse.IsSuccessStatusCode)
            {
                var errorBody = await createResponse.Content.ReadAsStringAsync(cancel);
                throw new InvalidOperationException($"Google Secret Manager 创建密钥失败: {errorBody}");
            }
        }

        var versionUrl = $"https://{_endpoint}/v1/projects/{_project_id}/secrets/{keyName}:addVersion";
        var base64Value = Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        var versionBody = JsonSerializer.Serialize(new { payload = new { data = base64Value } });

        var versionRequest = new HttpRequestMessage(HttpMethod.Post, versionUrl)
        {
            Content = new StringContent(versionBody, Encoding.UTF8, "application/json")
        };
        versionRequest.Headers.Add("Authorization", $"Bearer {_access_token}");

        var versionResponse = await _http.SendAsync(versionRequest, cancel);
        versionResponse.EnsureSuccessStatusCode();
    }

    /// <inheritdoc />
    public async Task delete_secret(string keyName, CancellationToken cancel = default)
    {
        var url = $"https://{_endpoint}/v1/projects/{_project_id}/secrets/{keyName}";
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        request.Headers.Add("Authorization", $"Bearer {_access_token}");

        var response = await _http.SendAsync(request, cancel);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return;
        }

        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// 检查密钥是否存在
    /// </summary>
    /// <param name="keyName">密钥名称</param>
    /// <param name="cancel">取消令牌</param>
    /// <returns>是否存在</returns>
    private async Task<bool> try_secret_exists(string keyName, CancellationToken cancel)
    {
        try
        {
            var url = $"https://{_endpoint}/v1/projects/{_project_id}/secrets/{keyName}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {_access_token}");

            var response = await _http.SendAsync(request, cancel);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }
}
