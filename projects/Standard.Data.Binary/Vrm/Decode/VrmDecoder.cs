using Std.Data.Binary.Frame;
using Std.Data.Binary.Vrm.Data;

namespace Std.Data.Binary.Vrm.Decode;

/// <summary>
///     VRM 解码器，的glTF 二进制数据中解析 VRM 扩展的
/// </summary>
public ref struct VrmDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始的<see cref="VrmDecoder" /> 结构的新实例的
    /// </summary>
    public VrmDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     当前位置的
    /// </summary>
    public int position
    {
        get => _buffer.position;
        set => _buffer.position = value;
    }

    /// <summary>
    ///     解码 VRM 模型数据的
    /// </summary>
    public VrmModelData decode()
    {
        var version = detect_vrm_version();

        return new VrmModelData
        {
            version = version
        };
    }

    /// <summary>
    ///     检的VRM 版本的
    /// </summary>
    public VrmVersion detect_vrm_version()
    {
        if (_buffer.length < 4) return VrmVersion.unknown;

        var magic = _buffer.read_string(4);

        if (magic == "glTF") return VrmVersion.vrm0;

        return VrmVersion.unknown;
    }
}