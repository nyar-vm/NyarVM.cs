using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;
using Atlas.Cloud.Models;

namespace Atlas.Cloud.Azure;

/// <summary>
/// Azure OpenAI 图像生成服务实现，调用 Azure OpenAI Images API
/// </summary>
public sealed class AzureOpenAiImageGeneration : IImageGenerationService
{
    private readonly HttpClient _http;
    private readonly string _api_key;
    private readonly string _base_url;

    /// <summary>
    /// 初始化 Azure OpenAI 图像生成服务
    /// </summary>
    /// <param name="resourceName">Azure 资源名称</param>
    /// <param name="deploymentId">部署标识</param>
    /// <param name="apiKey">API 密钥</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public AzureOpenAiImageGeneration(string resourceName, string deploymentId, string apiKey, HttpClient? httpClient = null)
    {
        _api_key = apiKey;
        _base_url = $"https://{resourceName}.openai.azure.com/openai/deployments/{deploymentId}/images/generations?api-version=2024-06-01";
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<ImageGenerationResponse> generate(ImageGenerationRequest request, CancellationToken ct = default)
    {
        try
        {
            var body = new
            {
                prompt = request.prompt,
                n = request.n,
                size = request.size,
                response_format = "b64_json"
            };

            var json = JsonSerializer.Serialize(body);

            using var http_request = new HttpRequestMessage(HttpMethod.Post, _base_url);
            http_request.Headers.Add("api-key", _api_key);
            http_request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(http_request, ct);
            var response_body = await response.Content.ReadAsStringAsync(ct);

            using var doc = JsonDocument.Parse(response_body);
            var root = doc.RootElement;

            if (root.TryGetProperty("error", out var error_el))
            {
                var message = error_el.TryGetProperty("message", out var msg_el) ? msg_el.get_string() ?? "未知错误" : "未知错误";
                return ImageGenerationResponse.fail(message);
            }

            var model = root.TryGetProperty("model", out var model_el) ? model_el.get_string() ?? request.model : request.model;
            var images = new List<byte[]>();

            if (root.TryGetProperty("data", out var data_el))
            {
                foreach (var item in data_el.EnumerateArray())
                {
                    if (item.TryGetProperty("b64_json", out var b64_el))
                    {
                        var b64 = b64_el.get_string() ?? string.Empty;
                        images.Add(Convert.FromBase64String(b64));
                    }
                }
            }

            return ImageGenerationResponse.ok(model, images);
        }
        catch (HttpRequestException ex)
        {
            return ImageGenerationResponse.fail($"HTTP 请求失败: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return ImageGenerationResponse.fail($"JSON 解析失败: {ex.Message}");
        }
        catch (FormatException ex)
        {
            return ImageGenerationResponse.fail($"Base64 解码失败: {ex.Message}");
        }
    }
}
