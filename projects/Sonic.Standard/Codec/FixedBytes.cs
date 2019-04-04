using System.Runtime.InteropServices;

namespace Std.Codec;

#region 固定长度字节类型

/// <summary>
///     4 字节固定长度字节类型，用于魔数、固定长度字段等场景。
/// </summary>
[InlineArray(4)]
public struct FixedBytes4
{
    private byte _element0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> as_span()
    {
        return MemoryMarshal.CreateReadOnlySpan(ref _element0, 4);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FixedBytes4 from_span(ReadOnlySpan<byte> source)
    {
        var result = new FixedBytes4();
        source.CopyTo(MemoryMarshal.CreateSpan(ref result._element0, 4));
        return result;
    }
}

/// <summary>
///     8 字节固定长度字节类型，用于魔数、固定长度字段等场景。
/// </summary>
[InlineArray(8)]
public struct FixedBytes8
{
    private byte _element0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> as_span()
    {
        return MemoryMarshal.CreateReadOnlySpan(ref _element0, 8);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FixedBytes8 from_span(ReadOnlySpan<byte> source)
    {
        var result = new FixedBytes8();
        source.CopyTo(MemoryMarshal.CreateSpan(ref result._element0, 8));
        return result;
    }
}

/// <summary>
///     16 字节固定长度字节类型，用于 GUID、固定长度字段等场景。
/// </summary>
[InlineArray(16)]
public struct FixedBytes16
{
    private byte _element0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> as_span()
    {
        return MemoryMarshal.CreateReadOnlySpan(ref _element0, 16);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FixedBytes16 from_span(ReadOnlySpan<byte> source)
    {
        var result = new FixedBytes16();
        source.CopyTo(MemoryMarshal.CreateSpan(ref result._element0, 16));
        return result;
    }
}

/// <summary>
///     32 字节固定长度字节类型，用于哈希值、固定长度字段等场景。
/// </summary>
[InlineArray(32)]
public struct FixedBytes32
{
    private byte _element0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> as_span()
    {
        return MemoryMarshal.CreateReadOnlySpan(ref _element0, 32);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FixedBytes32 from_span(ReadOnlySpan<byte> source)
    {
        var result = new FixedBytes32();
        source.CopyTo(MemoryMarshal.CreateSpan(ref result._element0, 32));
        return result;
    }
}

/// <summary>
///     56 字节固定长度字节类型，用于固定长度字段等场景。
/// </summary>
[InlineArray(56)]
public struct FixedBytes56
{
    private byte _element0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> as_span()
    {
        return MemoryMarshal.CreateReadOnlySpan(ref _element0, 56);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FixedBytes56 from_span(ReadOnlySpan<byte> source)
    {
        var result = new FixedBytes56();
        source.CopyTo(MemoryMarshal.CreateSpan(ref result._element0, 56));
        return result;
    }
}

/// <summary>
///     64 字节固定长度字节类型，用于签名、固定长度字段等场景。
/// </summary>
[InlineArray(64)]
public struct FixedBytes64
{
    private byte _element0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> as_span()
    {
        return MemoryMarshal.CreateReadOnlySpan(ref _element0, 64);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FixedBytes64 from_span(ReadOnlySpan<byte> source)
    {
        var result = new FixedBytes64();
        source.CopyTo(MemoryMarshal.CreateSpan(ref result._element0, 64));
        return result;
    }
}

#endregion