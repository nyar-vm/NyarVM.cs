using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;
using Atlas.Cloud.Models;

namespace Atlas.Cloud.Volcengine;

/// <summary>
/// 火山引擎图像生成服务实现，调用火山引擎视觉智能 API
/// </summary>
public sealed class VolcengineImageGeneration : IImageGenerationService
{
    private readonly HttpClient _http;
    private readonly string _api_key;
    private readonly string _endpoint_id;
    private readonly string _base_url;

    /// <summary>
    /// 初始化火山引擎图像生成服务
    /// </summary>
    /// <param name="apiKey">API 密钥</param>
    /// <param name="endpointId">端点标识</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public VolcengineImageGeneration(string apiKey, string endpointId, HttpClient? httpClient = null)
    {
        _api_key = apiKey;
        _endpoint_id = endpointId;
        _base_url = "https://visual.volcengineapi.com/";
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<ImageGenerationResponse> generate(ImageGenerationRequest request, CancellationToken ct = default)
    {
        try
        {
            var body = new
            {
                req_key = "high_aes",
                binary_data_base64 = Array.Empty<string>(),
                prompt = request.prompt,
                model = request.model,
                return_url = true,
                logo_info = new { add_logo = false }
            };

            var json = JsonSerializer.Serialize(body);

            var url = $"{_base_url}?Action=CVProcess&Version=2022-08-31&EndpointId={_endpoint_id}";

            using var http_request = new HttpRequestMessage(HttpMethod.Post, url);
            http_request.Headers.Add("Authorization", $"Bearer {_api_key}");
            http_request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(http_request, ct);
            var response_body = await response.Content.ReadAsStringAsync(ct);

            using var doc = JsonDocument.Parse(response_body);
            var root = doc.RootElement;

            var code = root.TryGetProperty("code", out var code_el) ? code_el.GetInt32() : -1;
            var message = root.TryGetProperty("message", out var msg_el) ? msg_el.get_string() ?? string.Empty : string.Empty;

            if (code != 10000)
            {
                return ImageGenerationResponse.fail($"火山引擎错误: {message}");
            }

            var images = new List<byte[]>();

            if (root.TryGetProperty("data", out var data_el) && data_el.TryGetProperty("binary_data_base64", out var b64_array))
            {
                foreach (var item in b64_array.EnumerateArray())
                {
                    var b64 = item.get_string() ?? string.Empty;
                    images.Add(Convert.FromBase64String(b64));
                }
            }
            else if (root.TryGetProperty("data", out data_el) && data_el.TryGetProperty("image_urls", out var url_array))
            {
                foreach (var item in url_array.EnumerateArray())
                {
                    var img_url = item.get_string();
                    if (img_url is not null)
                    {
                        var img_data = await download_image(img_url, ct);
                        if (img_data is not null)
                        {
                            images.Add(img_data);
                        }
                    }
                }
            }

            return ImageGenerationResponse.ok(request.model, images);
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
