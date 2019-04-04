using System.Text;
using System.Text.Json;
using Std.Data.Binary.Frame;
using Std.Data.Binary.Gltf.Data;

namespace Std.Data.Binary.Gltf.Decode;

/// <summary>
///     GLTF / GLB 模型解码器，的GLTF JSON 的GLB 二进制格式解码为 C# 数据结构的
/// </summary>
/// <remarks>
///     GLTF 使用 JSON 格式描述场景，GLB 的GLTF 的二进制容器格式的
///     解码器支持两种输入格式，解析为统一的C# 数据结构的
/// </remarks>
public sealed class GltfDecoder
{
    /// <summary>
    ///     的GLTF JSON 字符串解码模型数据的
    /// </summary>
    /// <param name="jsonContent">
    ///     JSON 内容的/param>
    ///     <returns>解码后的模型数据的/returns>
    public GltfModelData decode_json(string jsonContent)
    {
        using var document = JsonDocument.Parse(jsonContent);
        var root = document.RootElement;

        return new GltfModelData
        {
            asset = parse_asset(root.GetProperty("asset")),
            scene = root.TryGetProperty("scene", out var scene) ? scene.GetInt32() : null,
            scenes = parse_array(root, "scenes", parse_scene),
            nodes = parse_array(root, "nodes", parse_node),
            meshes = parse_array(root, "meshes", parse_mesh),
            buffers = parse_array(root, "buffers", parse_buffer),
            buffer_views = parse_array(root, "bufferViews", parse_buffer_view),
            accessors = parse_array(root, "accessors", parse_accessor),
            materials = parse_array(root, "materials", parse_material),
            textures = parse_array(root, "textures", parse_texture),
            images = parse_array(root, "images", parse_image),
            samplers = parse_array(root, "samplers", parse_sampler),
            skins = parse_array(root, "skins", parse_skin),
            animations = parse_array(root, "animations", parse_animation)
        };
    }

    /// <summary>
    ///     的GLB 二进制数据解码模型数据的
    /// </summary>
    /// <param name="data">
    ///     GLB 二进制数据的/param>
    ///     <returns>解码后的模型数据的/returns>
    public GltfModelData decode_glb(ReadOnlySpan<byte> data)
    {
        if (data.Length < 20) throw new InvalidDataException("GLB 文件数据过短");

        var buffer = new ByteBuffer(data);

        if (!GlbHeader.try_read(ref buffer, out var header)) throw new InvalidDataException("GLB 文件头部读取失败");

        if (!header.magic.as_span().SequenceEqual(GltfConstants.glb_magic_number))
            throw new InvalidDataException("GLB 文件魔数不匹配。");

        if (!GlbChunkHeader.try_read(ref buffer, out var jsonChunkHeader))
            throw new InvalidDataException("GLB 文件 JSON 块头部读取失败。");

        if (jsonChunkHeader.type != GltfConstants.chunk_type_json)
            throw new InvalidDataException("GLB 文件第一个块必须是 JSON 类型。");

        var jsonBytes = buffer.read_bytes((int)jsonChunkHeader.length);
        var jsonContent = Encoding.UTF8.GetString(jsonBytes);

        return decode_json(jsonContent);
    }

    #region 私有解析方法

    private static IReadOnlyList<T> parse_array<T>(JsonElement root, string propertyName, Func<JsonElement, T> parser)
    {
        if (!root.TryGetProperty(propertyName, out var array)) return [];

        var result = new List<T>(array.GetArrayLength());

        foreach (var element in array.EnumerateArray()) result.Add(parser(element));

        return result;
    }

    private static GltfAsset parse_asset(JsonElement element)
    {
        return new GltfAsset
        {
            version = element.GetProperty("version").GetString() ?? "2.0",
            generator = element.TryGetProperty("generator", out var generator) ? generator.GetString() : null,
            copyright = element.TryGetProperty("copyright", out var copyright) ? copyright.GetString() : null
        };
    }

    private static GltfScene parse_scene(JsonElement element)
    {
        return new GltfScene
        {
            name = element.TryGetProperty("name", out var name) ? name.GetString() : null,
            nodes = parse_int_array(element, "nodes")
        };
    }

    private static GltfNode parse_node(JsonElement element)
    {
        return new GltfNode
        {
            name = element.TryGetProperty("name", out var name) ? name.GetString() : null,
            children = parse_int_array(element, "children"),
            mesh = element.TryGetProperty("mesh", out var mesh) ? mesh.GetInt32() : null,
            skin = element.TryGetProperty("skin", out var skin) ? skin.GetInt32() : null,
            matrix = parse_float_array(element, "matrix"),
            translation = parse_float_array(element, "translation"),
            rotation = parse_float_array(element, "rotation"),
            scale = parse_float_array(element, "scale")
        };
    }

    private static GltfMesh parse_mesh(JsonElement element)
    {
        return new GltfMesh
        {
            name = element.TryGetProperty("name", out var name) ? name.GetString() : null,
            primitives = parse_array(element, "primitives", parse_primitive),
            weights = parse_float_array(element, "weights")
        };
    }

    private static GltfPrimitive parse_primitive(JsonElement element)
    {
        var attributes = new Dictionary<string, int>();

        if (element.TryGetProperty("attributes", out var attrs))
            foreach (var attr in attrs.EnumerateObject())
                attributes[attr.Name] = attr.Value.GetInt32();

        return new GltfPrimitive
        {
            attributes = attributes,
            indices = element.TryGetProperty("indices", out var indices) ? indices.GetInt32() : null,
            material = element.TryGetProperty("material", out var material) ? material.GetInt32() : null,
            mode = element.TryGetProperty("mode", out var mode) ? mode.GetInt32() : 4
        };
    }

    private static GltfBuffer parse_buffer(JsonElement element)
    {
        return new GltfBuffer
        {
            byte_length = element.GetProperty("byteLength").GetInt32(),
            uri = element.TryGetProperty("uri", out var uri) ? uri.GetString() : null,
            name = element.TryGetProperty("name", out var name) ? name.GetString() : null
        };
    }

    private static GltfBufferView parse_buffer_view(JsonElement element)
    {
        return new GltfBufferView
        {
            buffer = element.GetProperty("buffer").GetInt32(),
            byte_offset = element.TryGetProperty("byteOffset", out var offset) ? offset.GetInt32() : 0,
            byte_length = element.GetProperty("byteLength").GetInt32(),
            byte_stride = element.TryGetProperty("byteStride", out var stride) ? stride.GetInt32() : null,
            target = element.TryGetProperty("target", out var target) ? target.GetInt32() : null,
            name = element.TryGetProperty("name", out var name) ? name.GetString() : null
        };
    }

    private static GltfAccessor parse_accessor(JsonElement element)
    {
        return new GltfAccessor
        {
            buffer_view = element.GetProperty("bufferView").GetInt32(),
            byte_offset = element.TryGetProperty("byteOffset", out var offset) ? offset.GetInt32() : 0,
            component_type = element.GetProperty("componentType").GetInt32(),
            count = element.GetProperty("count").GetInt32(),
            type = element.GetProperty("type").GetString() ?? string.Empty,
            min = parse_float_array(element, "min"),
            max = parse_float_array(element, "max"),
            normalized = element.TryGetProperty("normalized", out var normalized) && normalized.GetBoolean(),
            name = element.TryGetProperty("name", out var name) ? name.GetString() : null
        };
    }

    private static GltfMaterial parse_material(JsonElement element)
    {
        return new GltfMaterial
        {
            name = element.TryGetProperty("name", out var name) ? name.GetString() : null,
            pbr_metallic_roughness = element.TryGetProperty("pbrMetallicRoughness", out var pbr)
                ? parse_pbr_metallic_roughness(pbr)
                : null,
            normal_texture = element.TryGetProperty("normalTexture", out var normal)
                ? parse_normal_texture_info(normal)
                : null,
            occlusion_texture = element.TryGetProperty("occlusionTexture", out var occlusion)
                ? parse_occlusion_texture_info(occlusion)
                : null,
            emissive_texture = element.TryGetProperty("emissiveTexture", out var emissive)
                ? parse_texture_info(emissive)
                : null,
            emissive_factor = parse_float_array(element, "emissiveFactor"),
            alpha_mode = element.TryGetProperty("alphaMode", out var alphaMode)
                ? alphaMode.GetString() ?? "OPAQUE"
                : "OPAQUE",
            alpha_cutoff = element.TryGetProperty("alphaCutoff", out var alphaCutoff)
                ? alphaCutoff.GetSingle()
                : 0.5f,
            double_sided = element.TryGetProperty("doubleSided", out var doubleSided) && doubleSided.GetBoolean()
        };
    }

    private static GltfPbrMetallicRoughness parse_pbr_metallic_roughness(JsonElement element)
    {
        return new GltfPbrMetallicRoughness
        {
            base_color_factor = parse_float_array(element, "baseColorFactor") ?? [1.0f, 1.0f, 1.0f, 1.0f],
            base_color_texture = element.TryGetProperty("baseColorTexture", out var baseColor)
                ? parse_texture_info(baseColor)
                : null,
            metallic_factor = element.TryGetProperty("metallicFactor", out var metallic)
                ? metallic.GetSingle()
                : 1.0f,
            roughness_factor = element.TryGetProperty("roughnessFactor", out var roughness)
                ? roughness.GetSingle()
                : 1.0f,
            metallic_roughness_texture = element.TryGetProperty("metallicRoughnessTexture", out var metallicRoughness)
                ? parse_texture_info(metallicRoughness)
                : null
        };
    }

    private static GltfTextureInfo parse_texture_info(JsonElement element)
    {
        return new GltfTextureInfo
        {
            index = element.GetProperty("index").GetInt32(),
            tex_coord = element.TryGetProperty("texCoord", out var texCoord) ? texCoord.GetInt32() : 0
        };
    }

    private static GltfNormalTextureInfo parse_normal_texture_info(JsonElement element)
    {
        return new GltfNormalTextureInfo
        {
            index = element.GetProperty("index").GetInt32(),
            tex_coord = element.TryGetProperty("texCoord", out var texCoord) ? texCoord.GetInt32() : 0,
            scale = element.TryGetProperty("scale", out var scale) ? scale.GetSingle() : 1.0f
        };
    }

    private static GltfOcclusionTextureInfo parse_occlusion_texture_info(JsonElement element)
    {
        return new GltfOcclusionTextureInfo
        {
            index = element.GetProperty("index").GetInt32(),
            tex_coord = element.TryGetProperty("texCoord", out var texCoord) ? texCoord.GetInt32() : 0,
            strength = element.TryGetProperty("strength", out var strength) ? strength.GetSingle() : 1.0f
        };
    }

    private static GltfTexture parse_texture(JsonElement element)
    {
        return new GltfTexture
        {
            name = element.TryGetProperty("name", out var name) ? name.GetString() : null,
            sampler = element.TryGetProperty("sampler", out var sampler) ? sampler.GetInt32() : null,
            source = element.TryGetProperty("source", out var source) ? source.GetInt32() : null
        };
    }

    private static GltfImage parse_image(JsonElement element)
    {
        return new GltfImage
        {
            name = element.TryGetProperty("name", out var name) ? name.GetString() : null,
            mime_type = element.TryGetProperty("mimeType", out var mimeType) ? mimeType.GetString() : null,
            uri = element.TryGetProperty("uri", out var uri) ? uri.GetString() : null,
            buffer_view = element.TryGetProperty("bufferView", out var bufferView) ? bufferView.GetInt32() : null
        };
    }

    private static GltfSampler parse_sampler(JsonElement element)
    {
        return new GltfSampler
        {
            mag_filter = element.TryGetProperty("magFilter", out var magFilter) ? magFilter.GetInt32() : 9729,
            min_filter = element.TryGetProperty("minFilter", out var minFilter) ? minFilter.GetInt32() : 9987,
            wrap_s = element.TryGetProperty("wrapS", out var wrapS) ? wrapS.GetInt32() : 10497,
            wrap_t = element.TryGetProperty("wrapT", out var wrapT) ? wrapT.GetInt32() : 10497,
            name = element.TryGetProperty("name", out var name) ? name.GetString() : null
        };
    }

    private static GltfSkin parse_skin(JsonElement element)
    {
        return new GltfSkin
        {
            name = element.TryGetProperty("name", out var name) ? name.GetString() : null,
            inverse_bind_matrices = element.TryGetProperty("inverseBindMatrices", out var ibm)
                ? ibm.GetInt32()
                : null,
            joints = parse_int_array(element, "joints"),
            skeleton = element.TryGetProperty("skeleton", out var skeleton) ? skeleton.GetInt32() : null
        };
    }

    private static GltfAnimation parse_animation(JsonElement element)
    {
        return new GltfAnimation
        {
            name = element.TryGetProperty("name", out var name) ? name.GetString() : null,
            channels = parse_array(element, "channels", parse_animation_channel),
            samplers = parse_array(element, "samplers", parse_animation_sampler)
        };
    }

    private static GltfAnimationChannel parse_animation_channel(JsonElement element)
    {
        return new GltfAnimationChannel
        {
            sampler = element.GetProperty("sampler").GetInt32(),
            target = parse_animation_channel_target(element.GetProperty("target"))
        };
    }

    private static GltfAnimationChannelTarget parse_animation_channel_target(JsonElement element)
    {
        return new GltfAnimationChannelTarget
        {
            node = element.TryGetProperty("node", out var node) ? node.GetInt32() : null,
            path = element.GetProperty("path").GetString() ?? string.Empty
        };
    }

    private static GltfAnimationSampler parse_animation_sampler(JsonElement element)
    {
        return new GltfAnimationSampler
        {
            input = element.GetProperty("input").GetInt32(),
            output = element.GetProperty("output").GetInt32(),
            interpolation = element.TryGetProperty("interpolation", out var interpolation)
                ? interpolation.GetString() ?? "LINEAR"
                : "LINEAR"
        };
    }

    private static IReadOnlyList<int> parse_int_array(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var array)) return [];

        var result = new List<int>(array.GetArrayLength());

        foreach (var item in array.EnumerateArray()) result.Add(item.GetInt32());

        return result;
    }

    private static IReadOnlyList<float>? parse_float_array(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var array)) return null;

        var result = new List<float>(array.GetArrayLength());

        foreach (var item in array.EnumerateArray()) result.Add(item.GetSingle());

        return result;
    }

    #endregion
}