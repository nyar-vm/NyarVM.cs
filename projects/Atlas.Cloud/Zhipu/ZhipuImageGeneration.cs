using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;
using Atlas.Cloud.Models;

namespace Atlas.Cloud.Zhipu;

/// <summary>
/// 智谱 CogView 图像生成服务实现，调用智谱 AI 图像生成 API
/// </summary>
public sealed class ZhipuImageGeneration : IImageGenerationService
{
    private readonly HttpClient _http;
    private readonly string _api_key;
    private readonly string _base_url;

    /// <summary>
    /// 初始化智谱 CogView 图像生成服务
    /// </summary>
    /// <param name="apiKey">API 密钥</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public ZhipuImageGeneration(string apiKey, HttpClient? httpClient = null)
    {
        _api_key = apiKey;
        _base_url = "https://open.bigmodel.cn/api/paas/v4/images/generations";
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<ImageGenerationResponse> generate(ImageGenerationRequest request, CancellationToken ct = default)
    {
        try
        {
            var body = new
            {
                model = request.model,
                prompt = request.prompt,
                n = request.n,
                size = request.size
            };

            var json = JsonSerializer.Serialize(body);

            using var http_request = new HttpRequestMessage(HttpMethod.Post, _base_url);
            http_request.Headers.Add("Authorization", $"Bearer {_api_key}");
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
                    else if (item.TryGetProperty("url", out var url_el))
                    {
                        var url = url_el.get_string();
                        if (url is not null)
                        {
                            var img_data = await download_image(url, ct);
                            if (img_data is not null)
                            {
                                images.Add(img_data);
                            }
                        }
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

    private async Task<byte[]?> download_image(string url, CancellationToken ct)
    {
        var response = await _http.GetAsync(url, ct);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsByteArrayAsync(ct);
        }

        return null;
    }
}
