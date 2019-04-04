using System.Text;
using System.Text.Json;
using Atlas.Cloud.Interfaces;
using Atlas.Cloud.Models;

namespace Atlas.Cloud.Tongyi;

/// <summary>
/// 通义万相图像生成服务实现，调用阿里云 DashScope 异步图像合成 API
/// </summary>
public sealed class TongyiImageGeneration : IImageGenerationService
{
    private readonly HttpClient _http;
    private readonly string _api_key;
    private readonly string _base_url;

    private const string SubmitUrl = "https://dashscope.aliyuncs.com/api/v1/services/aigc/text2image/image-synthesis";
    private const string PollUrl = "https://dashscope.aliyuncs.com/api/v1/tasks/{0}";

    /// <summary>
    /// 初始化通义万相图像生成服务
    /// </summary>
    /// <param name="apiKey">API 密钥</param>
    /// <param name="httpClient">HTTP 客户端</param>
    public TongyiImageGeneration(string apiKey, HttpClient? httpClient = null)
    {
        _api_key = apiKey;
        _base_url = SubmitUrl;
        _http = httpClient ?? new HttpClient();
    }

    /// <inheritdoc />
    public async Task<ImageGenerationResponse> generate(ImageGenerationRequest request, CancellationToken ct = default)
    {
        try
        {
            var task_id = await submit_task(request, ct);

            if (task_id is null)
            {
                return ImageGenerationResponse.fail("提交图像生成任务失败");
            }

            var images = await poll_result(task_id, request.model, ct);
            return images;
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

    private async Task<string?> submit_task(ImageGenerationRequest request, CancellationToken ct)
    {
        var body = new
        {
            model = request.model,
            input = new { prompt = request.prompt },
            parameters = new
            {
                size = request.size,
                n = request.n
            }
        };

        var json = JsonSerializer.Serialize(body);

        using var http_request = new HttpRequestMessage(HttpMethod.Post, _base_url);
        http_request.Headers.Add("Authorization", $"Bearer {_api_key}");
        http_request.Headers.Add("X-DashScope-Async", "enable");
        http_request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.SendAsync(http_request, ct);
        var response_body = await response.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(response_body);
        var root = doc.RootElement;

        if (root.TryGetProperty("output", out var output) && output.TryGetProperty("task_id", out var task_id_el))
        {
            return task_id_el.get_string();
        }

        return null;
    }

    private async Task<ImageGenerationResponse> poll_result(string taskId, string model, CancellationToken ct)
    {
        var poll_url = string.Format(PollUrl, taskId);

        for (var i = 0; i < 60; i++)
        {
            await Task.Delay(2000, ct);

            using var http_request = new HttpRequestMessage(HttpMethod.Get, poll_url);
            http_request.Headers.Add("Authorization", $"Bearer {_api_key}");

            var response = await _http.SendAsync(http_request, ct);
            var response_body = await response.Content.ReadAsStringAsync(ct);

            using var doc = JsonDocument.Parse(response_body);
            var root = doc.RootElement;

            var task_status = root.TryGetProperty("output", out var output)
                              && output.TryGetProperty("task_status", out var status_el)
                ? status_el.get_string() ?? string.Empty
                : string.Empty;

            if (task_status == "SUCCEEDED")
            {
                var images = new List<byte[]>();

                if (output.TryGetProperty("results", out var results_el))
                {
                    foreach (var item in results_el.EnumerateArray())
                    {
                        if (item.TryGetProperty("b64_image", out var b64_el))
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

            if (task_status == "FAILED")
            {
                var message = output.TryGetProperty("message", out var msg_el) ? msg_el.get_string() ?? "任务失败" : "任务失败";
                return ImageGenerationResponse.fail(message);
            }
        }

        return ImageGenerationResponse.fail("图像生成任务超时");
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
