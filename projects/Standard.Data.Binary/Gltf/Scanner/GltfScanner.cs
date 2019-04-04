using System.Text;
using System.Text.Json;
using Std.Data.Binary.Frame;
using Std.Data.Binary.Gltf.Data;

namespace Std.Data.Binary.Gltf.Scanner;

/// <summary>
///     GLTF / GLB 模型扫描器，基于 <see cref="SpanScanner" /> 提供的GLTF JSON 的GLB 二进制文件的快速元信息扫描的
/// </summary>
/// <remarks>
///     GLTF 使用 JSON 格式描述场景，GLB 的GLTF 的二进制容器格式的
///     扫描器支持两种格式，快速提取版本、场景统计、缓冲区大小等元信息的
/// </remarks>
public ref struct GltfScanner
{
    private static ReadOnlySpan<byte> _glb_magic => GltfConstants.glb_magic_number;

    private SpanScanner _scanner;

    /// <summary>
    ///     初始的<see cref="GltfScanner" /> 结构的新实例的
    /// </summary>
    /// <param name="data">要扫描的 GLTF/GLB 字节数据的/param>
    public GltfScanner(ReadOnlySpan<byte> data)
    {
        _scanner = new SpanScanner(data);
    }

    /// <summary>
    ///     获取底层扫描器，提供位置管理、魔数匹配等通用操作的
    /// </summary>
    public SpanScanner scanner => _scanner;

    /// <summary>
    ///     判断数据是否的GLB 二进制格式的
    /// </summary>
    /// <returns>如果的GLB 格式则返的true的/returns>
    public bool is_glb_format()
    {
        return _scanner.match_magic(_glb_magic);
    }

    /// <summary>
    ///     扫描 GLB 文件头，提取版本和长度信息的
    /// </summary>
    /// <returns>包含版本、文件长度和 JSON 块长度的元组的/returns>
    public (uint Version, uint TotalLength, uint JsonChunkLength, uint JsonChunkType) scan_glb_header()
    {
        if (!is_glb_format()) throw new InvalidDataException("数据不是有效的GLB 格式");

        _scanner.consume_magic(_glb_magic);
        var version = _scanner.buffer.read_u32_le();
        var totalLength = _scanner.buffer.read_u32_le();
        var jsonChunkLength = _scanner.buffer.read_u32_le();
        var jsonChunkType = _scanner.buffer.read_u32_le();

        return (version, totalLength, jsonChunkLength, jsonChunkType);
    }

    /// <summary>
    ///     扫描 GLTF/GLB 文件，提取场景统计信息的
    /// </summary>
    /// <returns>场景统计信息的/returns>
    public GltfStatistics scan_statistics()
    {
        string jsonContent;

        if (is_glb_format())
        {
            var (_, _, jsonChunkLength, _) = scan_glb_header();
            var jsonBytes = _scanner.buffer.read_bytes((int)jsonChunkLength);
            jsonContent = Encoding.UTF8.GetString(jsonBytes);
        }
        else
        {
            jsonContent = Encoding.UTF8.GetString(_scanner.data);
        }

        using var document = JsonDocument.Parse(jsonContent);
        var root = document.RootElement;

        var asset = root.GetProperty("asset");
        var version = asset.GetProperty("version").GetString() ?? "2.0";

        var sceneCount = root.TryGetProperty("scenes", out var scenes) ? scenes.GetArrayLength() : 0;
        var nodeCount = root.TryGetProperty("nodes", out var nodes) ? nodes.GetArrayLength() : 0;
        var meshCount = root.TryGetProperty("meshes", out var meshes) ? meshes.GetArrayLength() : 0;
        var materialCount = root.TryGetProperty("materials", out var materials) ? materials.GetArrayLength() : 0;
        var textureCount = root.TryGetProperty("textures", out var textures) ? textures.GetArrayLength() : 0;
        var imageCount = root.TryGetProperty("images", out var images) ? images.GetArrayLength() : 0;
        var animationCount = root.TryGetProperty("animations", out var animations) ? animations.GetArrayLength() : 0;
        var skinCount = root.TryGetProperty("skins", out var skins) ? skins.GetArrayLength() : 0;

        var totalBufferSize = 0L;

        if (root.TryGetProperty("buffers", out var buffers))
            foreach (var buffer in buffers.EnumerateArray())
                if (buffer.TryGetProperty("byteLength", out var byteLength))
                    totalBufferSize += byteLength.GetInt64();

        var hasBinaryChunk = false;

        if (is_glb_format())
        {
            var (_, _, jsonChunkLength, _) = scan_glb_header();
            hasBinaryChunk = _scanner.length > 20 + (int)jsonChunkLength;
        }

        return new GltfStatistics
        {
            version = version,
            scene_count = sceneCount,
            node_count = nodeCount,
            mesh_count = meshCount,
            material_count = materialCount,
            texture_count = textureCount,
            image_count = imageCount,
            animation_count = animationCount,
            skin_count = skinCount,
            total_buffer_size = totalBufferSize,
            has_binary_chunk = hasBinaryChunk
        };
    }

    /// <summary>
    ///     扫描 GLTF/GLB 文件，提取外部资源引用列表的
    /// </summary>
    /// <returns>外部资源 URI 列表的/returns>
    public List<string> scan_external_resources()
    {
        var resources = new List<string>();
        string jsonContent;

        if (is_glb_format())
        {
            var (_, _, jsonChunkLength, _) = scan_glb_header();
            var jsonBytes = _scanner.buffer.read_bytes((int)jsonChunkLength);
            jsonContent = Encoding.UTF8.GetString(jsonBytes);
        }
        else
        {
            jsonContent = Encoding.UTF8.GetString(_scanner.data);
        }

        using var document = JsonDocument.Parse(jsonContent);
        var root = document.RootElement;

        if (root.TryGetProperty("buffers", out var buffers))
            foreach (var buffer in buffers.EnumerateArray())
                if (buffer.TryGetProperty("uri", out var uri) && !is_data_uri(uri.GetString()!))
                    resources.Add(uri.GetString()!);

        if (root.TryGetProperty("images", out var images))
            foreach (var image in images.EnumerateArray())
                if (image.TryGetProperty("uri", out var uri) && !is_data_uri(uri.GetString()!))
                    resources.Add(uri.GetString()!);

        return resources;
    }

    private static bool is_data_uri(string uri)
    {
        return uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
///     GLTF 文件统计信息的
/// </summary>
public sealed class GltfStatistics
{
    /// <summary>
    ///     GLTF 版本的
    /// </summary>
    public string version { get; init; } = "2.0";

    /// <summary>
    ///     场景数量的
    /// </summary>
    public int scene_count { get; init; }

    /// <summary>
    ///     节点数量的
    /// </summary>
    public int node_count { get; init; }

    /// <summary>
    ///     网格数量的
    /// </summary>
    public int mesh_count { get; init; }

    /// <summary>
    ///     材质数量的
    /// </summary>
    public int material_count { get; init; }

    /// <summary>
    ///     纹理数量的
    /// </summary>
    public int texture_count { get; init; }

    /// <summary>
    ///     图像数量的
    /// </summary>
    public int image_count { get; init; }

    /// <summary>
    ///     动画数量的
    /// </summary>
    public int animation_count { get; init; }

    /// <summary>
    ///     蒙皮数量的
    /// </summary>
    public int skin_count { get; init; }

    /// <summary>
    ///     缓冲区总大小（字节）的
    /// </summary>
    public long total_buffer_size { get; init; }

    /// <summary>
    ///     是否包含二进制数据块（仅 GLB）的
    /// </summary>
    public bool has_binary_chunk { get; init; }
}