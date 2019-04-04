using System.Buffers;
using System.Text;

namespace Nyar.VM.TextVM;

/// <summary>
/// 码点迭代器，在字节切片上按指定编码逐个读取字符。
/// 对无效序列使用 U+FFFD 替换字符，不会抛出编码异常。
/// </summary>
public ref struct CodePointIter
{
    private readonly ReadOnlySpan<Byte> _bytes;
    private readonly TextEncoding _encoding;
    private Int32 _pos;

    /// <summary>
    /// 当解码增补字符（超出 BMP）时，暂存第二个代理项。
    /// </summary>
    private Char _pendingSurrogate;

    /// <summary>
    /// 指示是否存在待返回的代理项。
    /// </summary>
    private Boolean _hasPendingSurrogate;

    /// <summary>
    /// 使用给定的字节切片和编码创建迭代器。
    /// </summary>
    /// <param name="bytes">输入字节切片。</param>
    /// <param name="encoding">文本编码类型。</param>
    public CodePointIter(ReadOnlySpan<Byte> bytes, TextEncoding encoding)
    {
        _bytes = bytes;
        _encoding = encoding;
        _pos = 0;
        _pendingSurrogate = '\0';
        _hasPendingSurrogate = false;
    }

    /// <summary>
    /// 将迭代器定位到指定的字节偏移量。
    /// </summary>
    /// <param name="byteOffset">目标字节偏移。</param>
    public void Seek(Int32 byteOffset)
    {
        _pos = byteOffset;
        _pendingSurrogate = '\0';
        _hasPendingSurrogate = false;
    }

    /// <summary>
    /// 尝试读取下一个字符。
    /// </summary>
    /// <param name="byteLen">输出参数，表示本次消耗的字节数。</param>
    /// <param name="ch">输出参数，读取到的字符；无效序列返回 U+FFFD。</param>
    /// <returns>如果还有字符可读返回 true；已到达末尾返回 false。</returns>
    public Boolean TryNext(out Int32 byteLen, out Char ch)
    {
        if (_hasPendingSurrogate)
        {
            ch = _pendingSurrogate;
            byteLen = 0;
            _hasPendingSurrogate = false;
            return true;
        }

        if (_pos >= _bytes.Length)
        {
            byteLen = 0;
            ch = '\0';
            return false;
        }

        switch (_encoding)
        {
            case TextEncoding.Ascii:
            {
                DecodeAscii(out byteLen, out ch);
                break;
            }

            case TextEncoding.Utf8:
            {
                DecodeUtf8(out byteLen, out ch);
                break;
            }

            case TextEncoding.Utf16Le:
            {
                DecodeUtf16LittleEndian(out byteLen, out ch);
                break;
            }

            case TextEncoding.Utf16Be:
            {
                DecodeUtf16BigEndian(out byteLen, out ch);
                break;
            }

            default:
            {
                byteLen = 0;
                ch = '\0';
                return false;
            }
        }

        _pos += byteLen;
        return true;
    }

    /// <summary>
    /// 解码 ASCII 字符。
    /// </summary>
    private void DecodeAscii(out Int32 byteLen, out Char ch)
    {
        Byte b = _bytes[_pos];
        if (b <= 0x7F)
        {
            byteLen = 1;
            ch = (Char)b;
        }
        else
        {
            byteLen = 1;
            ch = '\uFFFD';
        }
    }

    /// <summary>
    /// 解码 UTF-8 字符。
    /// </summary>
    private void DecodeUtf8(out Int32 byteLen, out Char ch)
    {
        Int32 remaining = _bytes.Length - _pos;
        ReadOnlySpan<Byte> slice = _bytes.Slice(_pos, remaining);

        OperationStatus status = Rune.DecodeFromUtf8(slice, out Rune rune, out Int32 consumed);

        if (status == OperationStatus.Done)
        {
            if (rune.IsBmp)
            {
                byteLen = consumed;
                ch = (Char)rune.Value;
            }
            else
            {
                Span<Char> charBuf = stackalloc Char[2];
                Int32 charsWritten = rune.EncodeToUtf16(charBuf);
                byteLen = consumed;

                if (charsWritten == 2)
                {
                    ch = charBuf[0];
                    _pendingSurrogate = charBuf[1];
                    _hasPendingSurrogate = true;
                }
                else
                {
                    ch = '\uFFFD';
                }
            }
        }
        else
        {
            byteLen = consumed > 0 ? consumed : 1;
            ch = '\uFFFD';
        }
    }

    /// <summary>
    /// 解码 UTF-16 小端字符。
    /// </summary>
    private void DecodeUtf16LittleEndian(out Int32 byteLen, out Char ch)
    {
        Int32 remaining = _bytes.Length - _pos;

        if (remaining < 2)
        {
            byteLen = remaining;
            ch = '\uFFFD';
            return;
        }

        UInt16 u16 = (UInt16)(_bytes[_pos] | (_bytes[_pos + 1] << 8));

        DecodeUtf16CodeUnit(u16, remaining, out byteLen, out ch);
    }

    /// <summary>
    /// 解码 UTF-16 大端字符。
    /// </summary>
    private void DecodeUtf16BigEndian(out Int32 byteLen, out Char ch)
    {
        Int32 remaining = _bytes.Length - _pos;

        if (remaining < 2)
        {
            byteLen = remaining;
            ch = '\uFFFD';
            return;
        }

        UInt16 u16 = (UInt16)((_bytes[_pos] << 8) | _bytes[_pos + 1]);

        DecodeUtf16CodeUnit(u16, remaining, out byteLen, out ch);
    }

    /// <summary>
    /// 根据 UTF-16 码元判断是否为代理对，并返回正确的字符和消耗字节数。
    /// </summary>
    private void DecodeUtf16CodeUnit(UInt16 u16, Int32 remaining, out Int32 byteLen, out Char ch)
    {
        if (u16 >= 0xD800 && u16 <= 0xDBFF)
        {
            // 高代理项，期望后跟低代理项
            if (remaining < 4)
            {
                byteLen = remaining;
                ch = '\uFFFD';
                return;
            }

            UInt16 low;
            if (_encoding == TextEncoding.Utf16Le)
            {
                low = (UInt16)(_bytes[_pos + 2] | (_bytes[_pos + 3] << 8));
            }
            else
            {
                low = (UInt16)((_bytes[_pos + 2] << 8) | _bytes[_pos + 3]);
            }

            if (low >= 0xDC00 && low <= 0xDFFF)
            {
                // 有效的代理对，返回高代理项
                byteLen = 4;
                ch = (Char)u16;
            }
            else
            {
                // 高代理项后没有有效的低代理项
                byteLen = 2;
                ch = '\uFFFD';
            }
        }
        else if (u16 >= 0xDC00 && u16 <= 0xDFFF)
        {
            // 孤立的低代理项
            byteLen = 2;
            ch = '\uFFFD';
        }
        else
        {
            // BMP 基本平面字符
            byteLen = 2;
            ch = (Char)u16;
        }
    }
}
