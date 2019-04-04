using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Std.Codec;
using Std.Data.Binary.Frame;
using Std.Data.Binary.Gltf.Data;

namespace Std.Data.Binary.Gltf.Encode;

/// <summary>
///     GLTF / GLB 模型编码器，的C# 数据结构编码的GLTF JSON 的GLB 二进制格式的
/// </summary>
/// <remarks>
///     GLTF 使用 JSON 格式描述场景，GLB 的GLTF 的二进制容器格式�?
///     编码器支持两种输出格式，生成符合 Khronos GLTF 2.0 规范的数据的
/// </remarks>
public sealed class GltfEncoder
{
    /// <summary>
    ///     将模型数据编码为 GLTF JSON 字符串的
    /// </summary>
    /// <param name="data">
    ///     模型数据�?param>
    ///     <returns>JSON 字符串的/returns>
    public string encode_json(GltfModelData data)
    {
        var root = new JsonObject
        {
            ["asset"] = encode_asset(data.asset)
        };

        if (data.scene.HasValue) root["scene"] = data.scene.Value;

        if (data.scenes.Count > 0) root["scenes"] = new JsonArray([.. data.scenes.Select(encode_scene)]);

        if (data.nodes.Count > 0) root["nodes"] = new JsonArray([.. data.nodes.Select(encode_node)]);

        if (data.meshes.Count > 0) root["meshes"] = new JsonArray([.. data.meshes.Select(encode_mesh)]);

        if (data.buffers.Count > 0) root["buffers"] = new JsonArray([.. data.buffers.Select(encode_buffer)]);

        if (data.buffer_views.Count > 0)
            root["bufferViews"] = new JsonArray([.. data.buffer_views.Select(encode_buffer_view)]);

        if (data.accessors.Count > 0)
            root["accessors"] = new JsonArray([.. data.accessors.Select(encode_accessor)]);

        if (data.materials.Count > 0)
            root["materials"] = new JsonArray([.. data.materials.Select(encode_material)]);

        if (data.textures.Count > 0) root["textures"] = new JsonArray([.. data.textures.Select(encode_texture)]);

        if (data.images.Count > 0) root["images"] = new JsonArray([.. data.images.Select(encode_image)]);

        if (data.samplers.Count > 0) root["samplers"] = new JsonArray([.. data.samplers.Select(encode_sampler)]);

        if (data.skins.Count > 0) root["skins"] = new JsonArray([.. data.skins.Select(encode_skin)]);

        if (data.animations.Count > 0)
            root["animations"] = new JsonArray([.. data.animations.Select(encode_animation)]);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        return root.ToJsonString(options);
    }

    /// <summary>
    ///     将模型数据编码为 GLB 二进制格式的
    /// </summary>
    /// <param name="data">
    ///     模型数据�?param>
    ///     <param name="binaryData">
    ///         二进制缓冲区数据�?param>
    ///         <returns>GLB 二进制数据的/returns>
    public byte[] encode_glb(GltfModelData data, byte[]? binaryData = null)
    {
        var json = encode_json(data);
        var jsonBytes = Encoding.UTF8.GetBytes(json);

        var jsonPadding = (4 - jsonBytes.Length % 4) % 4;
        var binaryPadding = binaryData != null ? (4 - binaryData.Length % 4) % 4 : 0;

        var jsonChunkLength = jsonBytes.Length + jsonPadding;
        var binaryChunkLength = binaryData?.Length + binaryPadding ?? 0;

        var totalLength = 12 + 8 + jsonChunkLength;

        if (binaryData != null) totalLength += 8 + binaryChunkLength;

        var output = new byte[totalLength];
        var writer = new ByteBufferWriter(output);

        var header = new GlbHeader
        {
            magic = FixedBytes4.from_span(GltfConstants.glb_magic_number),
            version = GltfConstants.glb_version,
            length = (uint)totalLength
        };
        header.write_to(ref writer);

        var jsonChunkHeader = new GlbChunkHeader
        {
            length = (uint)jsonChunkLength,
            type = GltfConstants.chunk_type_json
        };
        jsonChunkHeader.write_to(ref writer);
        writer.write(jsonBytes);

        for (var i = 0; i < jsonPadding; i++) writer.write_u8(GltfConstants.padding_byte);

        if (binaryData != null)
        {
            var binChunkHeader = new GlbChunkHeader
            {
                length = (uint)binaryChunkLength,
                type = GltfConstants.chunk_type_bin
            };
            binChunkHeader.write_to(ref writer);
            writer.write(binaryData);

            for (var i = 0; i < binaryPadding; i++) writer.write_u8(0);
        }

        return output;
    }

    #region 私有编码方法

    private static JsonObject encode_asset(GltfAsset asset)
    {
        var obj = new JsonObject
        {
            ["version"] = asset.version
        };

        if (asset.generator != null) obj["generator"] = asset.generator;

        if (asset.copyright != null) obj["copyright"] = asset.copyright;

        return obj;
    }

    private static JsonObject encode_scene(GltfScene scene)
    {
        var obj = new JsonObject();

        if (scene.name != null) obj["name"] = scene.name;

        if (scene.nodes.Count > 0) obj["nodes"] = new JsonArray(scene.nodes.Select(n => (JsonNode)n).ToArray());

        return obj;
    }

    private static JsonObject encode_node(GltfNode node)
    {
        var obj = new JsonObject();

        if (node.name != null) obj["name"] = node.name;

        if (node.children.Count > 0) obj["children"] = new JsonArray(node.children.Select(n => (JsonNode)n).ToArray());

        if (node.mesh.HasValue) obj["mesh"] = node.mesh.Value;

        if (node.skin.HasValue) obj["skin"] = node.skin.Value;

        if (node.matrix != null) obj["matrix"] = new JsonArray(node.matrix.Select(n => (JsonNode)n).ToArray());

        if (node.translation != null)
            obj["translation"] = new JsonArray(node.translation.Select(n => (JsonNode)n).ToArray());

        if (node.rotation != null) obj["rotation"] = new JsonArray(node.rotation.Select(n => (JsonNode)n).ToArray());

        if (node.scale != null) obj["scale"] = new JsonArray(node.scale.Select(n => (JsonNode)n).ToArray());

        return obj;
    }

    private static JsonObject encode_mesh(GltfMesh mesh)
    {
        var obj = new JsonObject();

        if (mesh.name != null) obj["name"] = mesh.name;

        obj["primitives"] = new JsonArray([.. mesh.primitives.Select(encode_primitive)]);

        if (mesh.weights != null) obj["weights"] = new JsonArray(mesh.weights.Select(n => (JsonNode)n).ToArray());

        return obj;
    }

    private static JsonObject encode_primitive(GltfPrimitive primitive)
    {
        var obj = new JsonObject
        {
            ["attributes"] = new JsonObject(primitive.attributes.Select(a =>
                new KeyValuePair<string, JsonNode?>(a.Key, a.Value)))
        };

        if (primitive.indices.HasValue) obj["indices"] = primitive.indices.Value;

        if (primitive.material.HasValue) obj["material"] = primitive.material.Value;

        if (primitive.mode != 4) obj["mode"] = primitive.mode;

        return obj;
    }

    private static JsonObject encode_buffer(GltfBuffer buffer)
    {
        var obj = new JsonObject
        {
            ["byteLength"] = buffer.byte_length
        };

        if (buffer.uri != null) obj["uri"] = buffer.uri;

        if (buffer.name != null) obj["name"] = buffer.name;

        return obj;
    }

    private static JsonObject encode_buffer_view(GltfBufferView bufferView)
    {
        var obj = new JsonObject
        {
            ["buffer"] = bufferView.buffer,
            ["byteOffset"] = bufferView.byte_offset,
            ["byteLength"] = bufferView.byte_length
        };

        if (bufferView.byte_stride.HasValue) obj["byteStride"] = bufferView.byte_stride.Value;

        if (bufferView.target.HasValue) obj["target"] = bufferView.target.Value;

        if (bufferView.name != null) obj["name"] = bufferView.name;

        return obj;
    }

    private static JsonObject encode_accessor(GltfAccessor accessor)
    {
        var obj = new JsonObject
        {
            ["bufferView"] = accessor.buffer_view,
            ["componentType"] = accessor.component_type,
            ["count"] = accessor.count,
            ["type"] = accessor.type
        };

        if (accessor.byte_offset != 0) obj["byteOffset"] = accessor.byte_offset;

        if (accessor.min != null) obj["min"] = new JsonArray(accessor.min.Select(n => (JsonNode)n).ToArray());

        if (accessor.max != null) obj["max"] = new JsonArray(accessor.max.Select(n => (JsonNode)n).ToArray());

        if (accessor.normalized) obj["normalized"] = true;

        if (accessor.name != null) obj["name"] = accessor.name;

        return obj;
    }

    private static JsonObject encode_material(GltfMaterial material)
    {
        var obj = new JsonObject();

        if (material.name != null) obj["name"] = material.name;

        if (material.pbr_metallic_roughness != null)
            obj["pbrMetallicRoughness"] = encode_pbr_metallic_roughness(material.pbr_metallic_roughness);

        if (material.normal_texture != null) obj["normalTexture"] = encode_normal_texture_info(material.normal_texture);

        if (material.occlusion_texture != null)
            obj["occlusionTexture"] = encode_occlusion_texture_info(material.occlusion_texture);

        if (material.emissive_texture != null) obj["emissiveTexture"] = encode_texture_info(material.emissive_texture);

        if (material.emissive_factor != null)
            obj["emissiveFactor"] = new JsonArray(material.emissive_factor.Select(n => (JsonNode)n).ToArray());

        if (material.alpha_mode != "OPAQUE") obj["alphaMode"] = material.alpha_mode;

        if (System.Math.Abs(material.alpha_cutoff - 0.5f) > float.Epsilon) obj["alphaCutoff"] = material.alpha_cutoff;

        if (material.double_sided) obj["doubleSided"] = true;

        return obj;
    }

    private static JsonObject encode_pbr_metallic_roughness(GltfPbrMetallicRoughness pbr)
    {
        var obj = new JsonObject();

        if (pbr.base_color_factor != null && (pbr.base_color_factor.Count != 4 ||
                                              pbr.base_color_factor[0] != 1.0f ||
                                              pbr.base_color_factor[1] != 1.0f ||
                                              pbr.base_color_factor[2] != 1.0f ||
                                              pbr.base_color_factor[3] != 1.0f))
            obj["baseColorFactor"] = new JsonArray(pbr.base_color_factor.Select(n => (JsonNode)n).ToArray());

        if (pbr.base_color_texture != null) obj["baseColorTexture"] = encode_texture_info(pbr.base_color_texture);

        if (System.Math.Abs(pbr.metallic_factor - 1.0f) > float.Epsilon) obj["metallicFactor"] = pbr.metallic_factor;

        if (System.Math.Abs(pbr.roughness_factor - 1.0f) > float.Epsilon) obj["roughnessFactor"] = pbr.roughness_factor;

        if (pbr.metallic_roughness_texture != null)
            obj["metallicRoughnessTexture"] = encode_texture_info(pbr.metallic_roughness_texture);

        return obj;
    }

    private static JsonObject encode_texture_info(GltfTextureInfo textureInfo)
    {
        var obj = new JsonObject
        {
            ["index"] = textureInfo.index
        };

        if (textureInfo.tex_coord != 0) obj["texCoord"] = textureInfo.tex_coord;

        return obj;
    }

    private static JsonObject encode_normal_texture_info(GltfNormalTextureInfo textureInfo)
    {
        var obj = encode_texture_info(textureInfo);

        if (System.Math.Abs(textureInfo.scale - 1.0f) > float.Epsilon) obj["scale"] = textureInfo.scale;

        return obj;
    }

    private static JsonObject encode_occlusion_texture_info(GltfOcclusionTextureInfo textureInfo)
    {
        var obj = encode_texture_info(textureInfo);

        if (System.Math.Abs(textureInfo.strength - 1.0f) > float.Epsilon) obj["strength"] = textureInfo.strength;

        return obj;
    }

    private static JsonObject encode_texture(GltfTexture texture)
    {
        var obj = new JsonObject();

        if (texture.name != null) obj["name"] = texture.name;

        if (texture.sampler.HasValue) obj["sampler"] = texture.sampler.Value;

        if (texture.source.HasValue) obj["source"] = texture.source.Value;

        return obj;
    }

    private static JsonObject encode_image(GltfImage image)
    {
        var obj = new JsonObject();

        if (image.name != null) obj["name"] = image.name;

        if (image.uri != null) obj["uri"] = image.uri;

        if (image.mime_type != null) obj["mimeType"] = image.mime_type;

        if (image.buffer_view.HasValue) obj["bufferView"] = image.buffer_view.Value;

        return obj;
    }

    private static JsonObject encode_sampler(GltfSampler sampler)
    {
        var obj = new JsonObject();

        if (sampler.mag_filter != 9729) obj["magFilter"] = sampler.mag_filter;

        if (sampler.min_filter != 9987) obj["minFilter"] = sampler.min_filter;

        if (sampler.wrap_s != 10497) obj["wrapS"] = sampler.wrap_s;

        if (sampler.wrap_t != 10497) obj["wrapT"] = sampler.wrap_t;

        if (sampler.name != null) obj["name"] = sampler.name;

        return obj;
    }

    private static JsonObject encode_skin(GltfSkin skin)
    {
        var obj = new JsonObject();

        if (skin.name != null) obj["name"] = skin.name;

        if (skin.inverse_bind_matrices.HasValue) obj["inverseBindMatrices"] = skin.inverse_bind_matrices.Value;

        obj["joints"] = new JsonArray(skin.joints.Select(n => (JsonNode)n).ToArray());

        if (skin.skeleton.HasValue) obj["skeleton"] = skin.skeleton.Value;

        return obj;
    }

    private static JsonObject encode_animation(GltfAnimation animation)
    {
        var obj = new JsonObject();

        if (animation.name != null) obj["name"] = animation.name;

        obj["channels"] = new JsonArray([.. animation.channels.Select(encode_animation_channel)]);
        obj["samplers"] = new JsonArray([.. animation.samplers.Select(encode_animation_sampler)]);

        return obj;
    }

    private static JsonObject encode_animation_channel(GltfAnimationChannel channel)
    {
        var o = new JsonObject
        {
            ["sampler"] = channel.sampler,
            ["target"] = encode_animation_channel_target(channel.target)
        };
        return o;
    }

    private static JsonObject encode_animation_channel_target(GltfAnimationChannelTarget target)
    {
        var obj = new JsonObject
        {
            ["path"] = target.path
        };

        if (target.node.HasValue) obj["node"] = target.node.Value;

        return obj;
    }

    private static JsonObject encode_animation_sampler(GltfAnimationSampler sampler)
    {
        var obj = new JsonObject
        {
            ["input"] = sampler.input,
            ["output"] = sampler.output
        };

        if (sampler.interpolation != "LINEAR") obj["interpolation"] = sampler.interpolation;

        return obj;
    }

    #endregion
}