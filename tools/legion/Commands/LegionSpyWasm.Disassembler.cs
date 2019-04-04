namespace Legion.CLI.Commands;

/// <summary>
///     legion spy wasm 子模式的字节码反汇编器（partial）。
///     包含操作码 switch 与 LEB128 解码工具。
/// </summary>
internal static partial class LegionSpyWasm
{
    #region 指令解码

    /// <summary>
    ///     解码单条 WASM 指令。
    /// </summary>
    /// <param name="body">文件字节数据。</param>
    /// <param name="p">当前读取位置（会被前移）。</param>
    /// <returns>元组：助记符文本、栈效果、是否为终止性指令。</returns>
    internal static (string mnemonic, int stackEffect, bool terminates) decode_instruction(byte[] body, ref int p)
    {
        int op = body[p++];
        string text;
        int stackEffect = 0;
        bool terminates = false;

        switch (op)
        {
            case 0x00:
                text = "unreachable";
                terminates = true;
                break;
            case 0x01:
                text = "nop";
                break;
            case 0x02:
            {
                int bt = body[p++];
                text = $"block {format_block_type(bt)}";
                break;
            }
            case 0x03:
            {
                int bt = body[p++];
                text = $"loop {format_block_type(bt)}";
                break;
            }
            case 0x04:
            {
                int bt = body[p++];
                text = $"if {format_block_type(bt)}";
                stackEffect = -1;
                break;
            }
            case 0x05:
                text = "else";
                break;
            case 0x0b:
                text = "end";
                break;
            case 0x0c:
            {
                int depth = Leb128.read_unsigned(body, ref p);
                text = $"br {depth}";
                terminates = true;
                break;
            }
            case 0x0d:
            {
                int depth = Leb128.read_unsigned(body, ref p);
                text = $"br_if {depth}";
                stackEffect = -1;
                break;
            }
            case 0x0e:
            {
                int n = Leb128.read_unsigned(body, ref p);
                var labels = new List<int>(n);
                for (int i = 0; i < n; i++)
                {
                    labels.Add(Leb128.read_unsigned(body, ref p));
                }
                int def = Leb128.read_unsigned(body, ref p);
                text = $"br_table [{string.Join(",", labels)}] default={def}";
                terminates = true;
                break;
            }
            case 0x0f:
                text = "return";
                terminates = true;
                break;
            case 0x10:
            {
                int idx = Leb128.read_unsigned(body, ref p);
                text = $"call {idx}";
                break;
            }
            case 0x11:
            {
                int typeIdx = Leb128.read_unsigned(body, ref p);
                int tableIdx = Leb128.read_unsigned(body, ref p);
                text = $"call_indirect type={typeIdx} table={tableIdx}";
                break;
            }
            case 0x1a:
                text = "drop";
                stackEffect = -1;
                break;
            case 0x1b:
                text = "select";
                stackEffect = -2;
                break;
            case 0x20:
            {
                int idx = Leb128.read_unsigned(body, ref p);
                text = $"local.get {idx}";
                stackEffect = 1;
                break;
            }
            case 0x21:
            {
                int idx = Leb128.read_unsigned(body, ref p);
                text = $"local.set {idx}";
                stackEffect = -1;
                break;
            }
            case 0x22:
            {
                int idx = Leb128.read_unsigned(body, ref p);
                text = $"local.tee {idx}";
                break;
            }
            case 0x23:
            {
                int idx = Leb128.read_unsigned(body, ref p);
                text = $"global.get {idx}";
                stackEffect = 1;
                break;
            }
            case 0x24:
            {
                int idx = Leb128.read_unsigned(body, ref p);
                text = $"global.set {idx}";
                stackEffect = -1;
                break;
            }
            case 0x28:
            {
                int align = Leb128.read_unsigned(body, ref p);
                int off = Leb128.read_unsigned(body, ref p);
                text = $"i32.load align={align} offset={off}";
                break;
            }
            case 0x29:
            {
                int align = Leb128.read_unsigned(body, ref p);
                int off = Leb128.read_unsigned(body, ref p);
                text = $"i64.load align={align} offset={off}";
                break;
            }
            case 0x2a:
            {
                int align = Leb128.read_unsigned(body, ref p);
                int off = Leb128.read_unsigned(body, ref p);
                text = $"f32.load align={align} offset={off}";
                break;
            }
            case 0x2b:
            {
                int align = Leb128.read_unsigned(body, ref p);
                int off = Leb128.read_unsigned(body, ref p);
                text = $"f64.load align={align} offset={off}";
                break;
            }
            case 0x2c:
            {
                int align = Leb128.read_unsigned(body, ref p);
                int off = Leb128.read_unsigned(body, ref p);
                text = $"i32.load8_s align={align} offset={off}";
                break;
            }
            case 0x2d:
            {
                int align = Leb128.read_unsigned(body, ref p);
                int off = Leb128.read_unsigned(body, ref p);
                text = $"i32.load8_u align={align} offset={off}";
                break;
            }
            case 0x2e:
            {
                int align = Leb128.read_unsigned(body, ref p);
                int off = Leb128.read_unsigned(body, ref p);
                text = $"i32.load16_s align={align} offset={off}";
                break;
            }
            case 0x2f:
            {
                int align = Leb128.read_unsigned(body, ref p);
                int off = Leb128.read_unsigned(body, ref p);
                text = $"i32.load16_u align={align} offset={off}";
                break;
            }
            case 0x36:
            {
                int align = Leb128.read_unsigned(body, ref p);
                int off = Leb128.read_unsigned(body, ref p);
                text = $"i32.store align={align} offset={off}";
                stackEffect = -2;
                break;
            }
            case 0x37:
            {
                int align = Leb128.read_unsigned(body, ref p);
                int off = Leb128.read_unsigned(body, ref p);
                text = $"i64.store align={align} offset={off}";
                stackEffect = -2;
                break;
            }
            case 0x38:
            {
                int align = Leb128.read_unsigned(body, ref p);
                int off = Leb128.read_unsigned(body, ref p);
                text = $"f32.store align={align} offset={off}";
                stackEffect = -2;
                break;
            }
            case 0x39:
            {
                int align = Leb128.read_unsigned(body, ref p);
                int off = Leb128.read_unsigned(body, ref p);
                text = $"f64.store align={align} offset={off}";
                stackEffect = -2;
                break;
            }
            case 0x3a:
            {
                int align = Leb128.read_unsigned(body, ref p);
                int off = Leb128.read_unsigned(body, ref p);
                text = $"i32.store8 align={align} offset={off}";
                stackEffect = -2;
                break;
            }
            case 0x3b:
            {
                int align = Leb128.read_unsigned(body, ref p);
                int off = Leb128.read_unsigned(body, ref p);
                text = $"i32.store16 align={align} offset={off}";
                stackEffect = -2;
                break;
            }
            case 0x41:
            {
                long val = Leb128.read_signed(body, ref p);
                text = $"i32.const {val}";
                stackEffect = 1;
                break;
            }
            case 0x42:
            {
                long val = Leb128.read_signed(body, ref p);
                text = $"i64.const {val}";
                stackEffect = 1;
                break;
            }
            case 0x43:
            {
                int bits = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(
                    body.AsSpan(p, 4));
                p += 4;
                float f = BitConverter.Int32BitsToSingle(bits);
                text = $"f32.const {f}";
                stackEffect = 1;
                break;
            }
            case 0x44:
            {
                long bits = System.Buffers.Binary.BinaryPrimitives.ReadInt64LittleEndian(
                    body.AsSpan(p, 8));
                p += 8;
                double d = BitConverter.Int64BitsToDouble(bits);
                text = $"f64.const {d}";
                stackEffect = 1;
                break;
            }
            case 0x45:
                text = "i32.eqz";
                break;
            case 0x46:
                text = "i32.eq";
                stackEffect = -1;
                break;
            case 0x47:
                text = "i32.ne";
                stackEffect = -1;
                break;
            case 0x48:
                text = "i32.lt_s";
                stackEffect = -1;
                break;
            case 0x49:
                text = "i32.lt_u";
                stackEffect = -1;
                break;
            case 0x4a:
                text = "i32.gt_s";
                stackEffect = -1;
                break;
            case 0x4b:
                text = "i32.gt_u";
                stackEffect = -1;
                break;
            case 0x4c:
                text = "i32.le_s";
                stackEffect = -1;
                break;
            case 0x4d:
                text = "i32.le_u";
                stackEffect = -1;
                break;
            case 0x4e:
                text = "i32.ge_s";
                stackEffect = -1;
                break;
            case 0x4f:
                text = "i32.ge_u";
                stackEffect = -1;
                break;
            case 0x50:
                text = "i64.eqz";
                break;
            case 0x51:
                text = "i64.eq";
                stackEffect = -1;
                break;
            case 0x52:
                text = "i64.ne";
                stackEffect = -1;
                break;
            case 0x53:
                text = "i64.lt_s";
                stackEffect = -1;
                break;
            case 0x54:
                text = "i64.lt_u";
                stackEffect = -1;
                break;
            case 0x55:
                text = "i64.gt_s";
                stackEffect = -1;
                break;
            case 0x56:
                text = "i64.gt_u";
                stackEffect = -1;
                break;
            case 0x57:
                text = "i64.le_s";
                stackEffect = -1;
                break;
            case 0x58:
                text = "i64.le_u";
                stackEffect = -1;
                break;
            case 0x59:
                text = "i64.ge_s";
                stackEffect = -1;
                break;
            case 0x5a:
                text = "i64.ge_u";
                stackEffect = -1;
                break;
            case 0x6a:
                text = "i32.add";
                stackEffect = -1;
                break;
            case 0x6b:
                text = "i32.sub";
                stackEffect = -1;
                break;
            case 0x6c:
                text = "i32.mul";
                stackEffect = -1;
                break;
            case 0x6d:
                text = "i32.div_s";
                stackEffect = -1;
                break;
            case 0x6e:
                text = "i32.div_u";
                stackEffect = -1;
                break;
            case 0x6f:
                text = "i32.rem_s";
                stackEffect = -1;
                break;
            case 0x70:
                text = "i32.rem_u";
                stackEffect = -1;
                break;
            case 0x71:
                text = "i32.and";
                stackEffect = -1;
                break;
            case 0x72:
                text = "i32.or";
                stackEffect = -1;
                break;
            case 0x73:
                text = "i32.xor";
                stackEffect = -1;
                break;
            case 0x74:
                text = "i32.shl";
                stackEffect = -1;
                break;
            case 0x75:
                text = "i32.shr_s";
                stackEffect = -1;
                break;
            case 0x76:
                text = "i32.shr_u";
                stackEffect = -1;
                break;
            case 0x77:
                text = "i32.rotl";
                stackEffect = -1;
                break;
            case 0x78:
                text = "i32.rotr";
                stackEffect = -1;
                break;
            case 0x7c:
                text = "i64.add";
                stackEffect = -1;
                break;
            case 0x7d:
                text = "i64.sub";
                stackEffect = -1;
                break;
            case 0x7e:
                text = "i64.mul";
                stackEffect = -1;
                break;
            case 0x7f:
                text = "i64.div_s";
                stackEffect = -1;
                break;
            case 0x80:
                text = "i64.div_u";
                stackEffect = -1;
                break;
            case 0x81:
                text = "i64.rem_s";
                stackEffect = -1;
                break;
            case 0x82:
                text = "i64.rem_u";
                stackEffect = -1;
                break;
            case 0x83:
                text = "i64.and";
                stackEffect = -1;
                break;
            case 0x84:
                text = "i64.or";
                stackEffect = -1;
                break;
            case 0x85:
                text = "i64.xor";
                stackEffect = -1;
                break;
            case 0x86:
                text = "i64.shl";
                stackEffect = -1;
                break;
            case 0x87:
                text = "i64.shr_s";
                stackEffect = -1;
                break;
            case 0x88:
                text = "i64.shr_u";
                stackEffect = -1;
                break;
            case 0x89:
                text = "i64.rotl";
                stackEffect = -1;
                break;
            case 0x8a:
                text = "i64.rotr";
                stackEffect = -1;
                break;
            case 0x8b:
                text = "f32.abs";
                break;
            case 0x8c:
                text = "f32.neg";
                break;
            case 0x8d:
                text = "f32.ceil";
                break;
            case 0x8e:
                text = "f32.floor";
                break;
            case 0x8f:
                text = "f32.trunc";
                break;
            case 0x90:
                text = "f32.nearest";
                break;
            case 0x91:
                text = "f32.sqrt";
                break;
            case 0x92:
                text = "f32.add";
                stackEffect = -1;
                break;
            case 0x93:
                text = "f32.sub";
                stackEffect = -1;
                break;
            case 0x94:
                text = "f32.mul";
                stackEffect = -1;
                break;
            case 0x95:
                text = "f32.div";
                stackEffect = -1;
                break;
            case 0x96:
                text = "f32.min";
                stackEffect = -1;
                break;
            case 0x97:
                text = "f32.max";
                stackEffect = -1;
                break;
            case 0x98:
                text = "f32.copysign";
                stackEffect = -1;
                break;
            case 0x99:
                text = "f64.abs";
                break;
            case 0x9a:
                text = "f64.neg";
                break;
            case 0x9b:
                text = "f64.ceil";
                break;
            case 0x9c:
                text = "f64.floor";
                break;
            case 0x9d:
                text = "f64.trunc";
                break;
            case 0x9e:
                text = "f64.nearest";
                break;
            case 0x9f:
                text = "f64.sqrt";
                break;
            case 0xa0:
                text = "f64.add";
                stackEffect = -1;
                break;
            case 0xa1:
                text = "f64.sub";
                stackEffect = -1;
                break;
            case 0xa2:
                text = "f64.mul";
                stackEffect = -1;
                break;
            case 0xa3:
                text = "f64.div";
                stackEffect = -1;
                break;
            case 0xa4:
                text = "f64.min";
                stackEffect = -1;
                break;
            case 0xa5:
                text = "f64.max";
                stackEffect = -1;
                break;
            case 0xa6:
                text = "f64.copysign";
                stackEffect = -1;
                break;
            case 0xa7:
                text = "i32.wrap_i64";
                break;
            case 0xa8:
                text = "i32.trunc_f32_s";
                break;
            case 0xa9:
                text = "i32.trunc_f32_u";
                break;
            case 0xaa:
                text = "i32.trunc_f64_s";
                break;
            case 0xab:
                text = "i32.trunc_f64_u";
                break;
            case 0xac:
                text = "i64.extend_i32_s";
                break;
            case 0xad:
                text = "i64.extend_i32_u";
                break;
            case 0xae:
                text = "i64.trunc_f32_s";
                break;
            case 0xaf:
                text = "i64.trunc_f32_u";
                break;
            case 0xb0:
                text = "i64.trunc_f64_s";
                break;
            case 0xb1:
                text = "i64.trunc_f64_u";
                break;
            case 0xb2:
                text = "f32.convert_i32_s";
                break;
            case 0xb3:
                text = "f32.convert_i32_u";
                break;
            case 0xb4:
                text = "f32.convert_i64_s";
                break;
            case 0xb5:
                text = "f32.convert_i64_u";
                break;
            case 0xb6:
                text = "f32.demote_f64";
                break;
            case 0xb7:
                text = "f64.convert_i32_s";
                break;
            case 0xb8:
                text = "f64.convert_i32_u";
                break;
            case 0xb9:
                text = "f64.convert_i64_s";
                break;
            case 0xba:
                text = "f64.convert_i64_u";
                break;
            case 0xbb:
                text = "f64.promote_f32";
                break;
            case 0xbc:
                text = "i32.reinterpret_f32";
                break;
            case 0xbd:
                text = "i64.reinterpret_f64";
                break;
            case 0xbe:
                text = "f32.reinterpret_i32";
                break;
            case 0xbf:
                text = "f64.reinterpret_i64";
                break;
            case 0xd0:
            {
                int heapType = body[p++];
                text = $"ref.null {format_heap_type(heapType)}";
                stackEffect = 1;
                break;
            }
            case 0xd1:
                text = "ref.is_null";
                break;
            case 0xd2:
            {
                int idx = Leb128.read_unsigned(body, ref p);
                text = $"ref.func {idx}";
                stackEffect = 1;
                break;
            }
            default:
                text = $"unknown 0x{op:x2}";
                break;
        }

        return (text, stackEffect, terminates);
    }

    /// <summary>
    ///     格式化 block 类型字节。
    /// </summary>
    /// <param name="bt">block type 字节。</param>
    /// <returns>可读文本。</returns>
    private static string format_block_type(int bt)
    {
        return bt switch
        {
            0x40 => "void",
            0x7f => "i32",
            0x7e => "i64",
            0x7d => "f32",
            0x7c => "f64",
            _ => $"type=0x{bt:x2}"
        };
    }

    /// <summary>
    ///     格式化 ref.null 的堆类型字节。
    /// </summary>
    /// <param name="ht">heap type 字节。</param>
    /// <returns>可读文本。</returns>
    private static string format_heap_type(int ht)
    {
        return ht switch
        {
            0x6f => "extern",
            0x70 => "func",
            0x71 => "any",
            0x72 => "eq",
            0x73 => "i31",
            0x74 => "struct",
            0x75 => "array",
            0x6d => "nofunc",
            0x6e => "noextern",
            0x76 => "none",
            _ => $"heap=0x{ht:x2}"
        };
    }

    #endregion

    #region LEB128 解码

    /// <summary>
    ///     LEB128 变长整数解码工具（手动实现，用于 WASM 段扫描场景）。
    /// </summary>
    internal static class Leb128
    {
        /// <summary>
        ///     读取无符号 LEB128（32 位）。
        /// </summary>
        /// <param name="data">字节数据。</param>
        /// <param name="p">读取位置（会被前移）。</param>
        /// <returns>解码后的 32 位无符号整数。</returns>
        internal static int read_unsigned(byte[] data, ref int p)
        {
            long v = read_unsigned_64(data, ref p);
            return (int)v;
        }

        /// <summary>
        ///     读取无符号 LEB128（64 位）。
        /// </summary>
        /// <param name="data">字节数据。</param>
        /// <param name="p">读取位置（会被前移）。</param>
        /// <returns>解码后的 64 位无符号整数。</returns>
        internal static long read_unsigned_64(byte[] data, ref int p)
        {
            long result = 0;
            int shift = 0;
            while (true)
            {
                int b = data[p++];
                result |= (long)(b & 0x7f) << shift;
                if ((b & 0x80) == 0)
                {
                    break;
                }

                shift += 7;
            }

            return result;
        }

        /// <summary>
        ///     读取有符号 LEB128（返回 long 以容纳 32/64 位值）。
        /// </summary>
        /// <param name="data">字节数据。</param>
        /// <param name="p">读取位置（会被前移）。</param>
        /// <returns>解码后的有符号 64 位整数。</returns>
        internal static long read_signed(byte[] data, ref int p)
        {
            long result = 0;
            int shift = 0;
            int b;
            while (true)
            {
                b = data[p++];
                result |= (long)(b & 0x7f) << shift;
                shift += 7;
                if ((b & 0x80) == 0)
                {
                    break;
                }
            }

            // 符号扩展
            if (shift < 64 && (b & 0x40) != 0)
            {
                result |= -(1L << shift);
            }

            return result;
        }
    }

    #endregion
}
