using System.Numerics;
using System.Text;
using Nyar.Types;
using Std.Data.Binary.NyarIR.Data;
using ValueType = Nyar.Types.ValueType;

namespace Nyar.VM.NyarVM;

/// <summary>
///     算术操作
/// </summary>
public static class ArithmeticOps
{
    /// <summary>
    ///     执行算术操作
    /// </summary>
    /// <param name="headCode">操作码。</param>
    /// <param name="stack">值栈。</param>
    /// <returns>是否成功执行。</returns>
    public static bool execute(NyarHeadCode headCode, ValueStack stack)
    {
        return headCode switch
        {
            _ when is_i32_binary(headCode) => execute_i32_binary(headCode, stack),
            _ when is_i32_unary(headCode) => execute_i32_unary(headCode, stack),
            _ when is_i32_compare(headCode) => execute_i32_compare(headCode, stack),
            _ when is_i64_binary(headCode) => execute_i64_binary(headCode, stack),
            _ when is_i64_unary(headCode) => execute_i64_unary(headCode, stack),
            _ when is_ref_compare(headCode) => execute_ref_compare(headCode, stack),
            _ when is_f32_binary(headCode) => execute_f32_binary(headCode, stack),
            _ when is_f32_unary(headCode) => execute_f32_unary(headCode, stack),
            _ when is_f64_binary(headCode) => execute_f64_binary(headCode, stack),
            _ when is_f64_unary(headCode) => execute_f64_unary(headCode, stack),
            _ when is_conversion(headCode) => execute_conversion(headCode, stack),
            _ when is_utf8_op(headCode) => execute_utf8_op(headCode, stack),
            _ when is_big_int_binary(headCode) => execute_big_int_binary(headCode, stack),
            _ => false
        };
    }

    #region i32 二元操作

    private static bool is_i32_binary(NyarHeadCode op)
    {
        return op is NyarHeadCode.i32_add or NyarHeadCode.i32_sub or NyarHeadCode.i32_mul
            or NyarHeadCode.i32_div_s or NyarHeadCode.i32_div_u
            or NyarHeadCode.i32_rem_s or NyarHeadCode.i32_rem_u
            or NyarHeadCode.i32_and or NyarHeadCode.i32_or or NyarHeadCode.i32_xor
            or NyarHeadCode.i32_shl or NyarHeadCode.i32_shr_s or NyarHeadCode.i32_shr_u;
    }

    private static bool execute_i32_binary(NyarHeadCode headCode, ValueStack stack)
    {
        var b = stack.pop();
        var a = stack.pop();
        var ai = a.i32;
        var bi = b.i32;

        var result = headCode switch
        {
            NyarHeadCode.i32_add => Value.from_int(ai + bi),
            NyarHeadCode.i32_sub => Value.from_int(ai - bi),
            NyarHeadCode.i32_mul => Value.from_int(ai * bi),
            NyarHeadCode.i32_div_s => bi != 0
                ? Value.from_int(ai / bi)
                : throw new NyarRuntimeException("i32 有符号除法: 除数为零"),
            NyarHeadCode.i32_div_u => bi != 0
                ? Value.from_int((int)((uint)ai / (uint)bi))
                : throw new NyarRuntimeException("i32 无符号除法: 除数为零"),
            NyarHeadCode.i32_rem_s => bi != 0
                ? Value.from_int(ai % bi)
                : throw new NyarRuntimeException("i32 有符号取余: 除数为零"),
            NyarHeadCode.i32_rem_u => bi != 0
                ? Value.from_int((int)((uint)ai % (uint)bi))
                : throw new NyarRuntimeException("i32 无符号取余: 除数为零"),
            NyarHeadCode.i32_and => Value.from_int(ai & bi),
            NyarHeadCode.i32_or => Value.from_int(ai | bi),
            NyarHeadCode.i32_xor => Value.from_int(ai ^ bi),
            NyarHeadCode.i32_shl => Value.from_int(ai << (bi & 0x1F)),
            NyarHeadCode.i32_shr_s => Value.from_int(ai >> (bi & 0x1F)),
            NyarHeadCode.i32_shr_u => Value.from_int((int)((uint)ai >> (bi & 0x1F))),
            _ => Value.@null
        };

        stack.push(result);
        return true;
    }

    #endregion

    #region i32 一元操作

    private static bool is_i32_unary(NyarHeadCode op)
    {
        return op is NyarHeadCode.i32_neg or NyarHeadCode.i32_not;
    }

    private static bool execute_i32_unary(NyarHeadCode headCode, ValueStack stack)
    {
        var a = stack.pop();
        var result = headCode switch
        {
            NyarHeadCode.i32_neg => Value.from_int(-a.i32),
            NyarHeadCode.i32_not => Value.from_int(~a.i32),
            _ => Value.@null
        };

        stack.push(result);
        return true;
    }

    #endregion

    #region i32 比较操作

    private static bool is_i32_compare(NyarHeadCode op)
    {
        return op is NyarHeadCode.i32_eq or NyarHeadCode.i32_ne
            or NyarHeadCode.i32_lt_s or NyarHeadCode.i32_lt_u
            or NyarHeadCode.i32_le_s or NyarHeadCode.i32_le_u
            or NyarHeadCode.i32_gt_s or NyarHeadCode.i32_gt_u
            or NyarHeadCode.i32_ge_s or NyarHeadCode.i32_ge_u;
    }

    private static bool execute_i32_compare(NyarHeadCode headCode, ValueStack stack)
    {
        var b = stack.pop();
        var a = stack.pop();
        var ai = a.i32;
        var bi = b.i32;

        var result = headCode switch
        {
            NyarHeadCode.i32_eq => Value.from_bool(ai == bi),
            NyarHeadCode.i32_ne => Value.from_bool(ai != bi),
            NyarHeadCode.i32_lt_s => Value.from_bool(ai < bi),
            NyarHeadCode.i32_lt_u => Value.from_bool((uint)ai < (uint)bi),
            NyarHeadCode.i32_le_s => Value.from_bool(ai <= bi),
            NyarHeadCode.i32_le_u => Value.from_bool((uint)ai <= (uint)bi),
            NyarHeadCode.i32_gt_s => Value.from_bool(ai > bi),
            NyarHeadCode.i32_gt_u => Value.from_bool((uint)ai > (uint)bi),
            NyarHeadCode.i32_ge_s => Value.from_bool(ai >= bi),
            NyarHeadCode.i32_ge_u => Value.from_bool((uint)ai >= (uint)bi),
            _ => Value.@null
        };

        stack.push(result);
        return true;
    }

    #endregion

    #region i64 二元操作

    private static bool is_i64_binary(NyarHeadCode op)
    {
        return op is NyarHeadCode.i64_add or NyarHeadCode.i64_sub or NyarHeadCode.i64_mul
            or NyarHeadCode.i64_div_s or NyarHeadCode.i64_div_u;
    }

    private static bool execute_i64_binary(NyarHeadCode headCode, ValueStack stack)
    {
        var b = stack.pop();
        var a = stack.pop();
        var al = a.i64;
        var bl = b.i64;

        var result = headCode switch
        {
            NyarHeadCode.i64_add => Value.from_long(al + bl),
            NyarHeadCode.i64_sub => Value.from_long(al - bl),
            NyarHeadCode.i64_mul => Value.from_long(al * bl),
            NyarHeadCode.i64_div_s => bl != 0
                ? Value.from_long(al / bl)
                : throw new NyarRuntimeException("i64 有符号除法: 除数为零"),
            NyarHeadCode.i64_div_u => bl != 0
                ? Value.from_long((long)((ulong)al / (ulong)bl))
                : throw new NyarRuntimeException("i64 无符号除法: 除数为零"),
            _ => Value.@null
        };

        stack.push(result);
        return true;
    }

    #endregion

    #region i64 一元操作

    private static bool is_i64_unary(NyarHeadCode op)
    {
        return op is NyarHeadCode.i64_neg;
    }

    private static bool execute_i64_unary(NyarHeadCode headCode, ValueStack stack)
    {
        var a = stack.pop();
        var result = headCode switch
        {
            NyarHeadCode.i64_neg => Value.from_long(-a.i64),
            _ => Value.@null
        };

        stack.push(result);
        return true;
    }

    #endregion

    #region 引用比较

    private static bool is_ref_compare(NyarHeadCode op)
    {
        return op is NyarHeadCode.ref_eq or NyarHeadCode.ref_ne;
    }

    private static bool execute_ref_compare(NyarHeadCode headCode, ValueStack stack)
    {
        var right = stack.pop();
        var left = stack.pop();
        var equals = are_reference_operands_equal(left, right);
        stack.push(Value.from_bool(headCode == NyarHeadCode.ref_eq ? equals : !equals));
        return true;
    }

    private static bool are_reference_operands_equal(Value left, Value right)
    {
        if (left.type == ValueType.@null || right.type == ValueType.@null)
            return left.type == ValueType.@null && right.type == ValueType.@null;

        if (!is_reference_like(left) || !is_reference_like(right)) return false;

        return left == right;
    }

    private static bool is_reference_like(Value value)
    {
        return value.type is ValueType.@object or ValueType.utf8 or ValueType.closure or ValueType.continuation
            or ValueType.effect or ValueType.witness_table;
    }

    #endregion

    #region f32 二元操作

    private static bool is_f32_binary(NyarHeadCode op)
    {
        return op is NyarHeadCode.f32_add or NyarHeadCode.f32_sub or NyarHeadCode.f32_mul or NyarHeadCode.f32_div;
    }

    private static bool execute_f32_binary(NyarHeadCode headCode, ValueStack stack)
    {
        var b = stack.pop();
        var a = stack.pop();
        var af = a.f64;
        var bf = b.f64;

        var result = headCode switch
        {
            NyarHeadCode.f32_add => Value.from_double(af + bf),
            NyarHeadCode.f32_sub => Value.from_double(af - bf),
            NyarHeadCode.f32_mul => Value.from_double(af * bf),
            NyarHeadCode.f32_div => Value.from_double(af / bf),
            _ => Value.@null
        };

        stack.push(result);
        return true;
    }

    #endregion

    #region f32 一元操作

    private static bool is_f32_unary(NyarHeadCode op)
    {
        return op is NyarHeadCode.f32_neg;
    }

    private static bool execute_f32_unary(NyarHeadCode headCode, ValueStack stack)
    {
        var a = stack.pop();
        var result = headCode switch
        {
            NyarHeadCode.f32_neg => Value.from_double(-a.f64),
            _ => Value.@null
        };

        stack.push(result);
        return true;
    }

    #endregion

    #region f64 二元操作

    private static bool is_f64_binary(NyarHeadCode op)
    {
        return op is NyarHeadCode.f64_add or NyarHeadCode.f64_sub or NyarHeadCode.f64_mul or NyarHeadCode.f64_div;
    }

    private static bool execute_f64_binary(NyarHeadCode headCode, ValueStack stack)
    {
        var b = stack.pop();
        var a = stack.pop();
        var af = a.f64;
        var bf = b.f64;

        var result = headCode switch
        {
            NyarHeadCode.f64_add => Value.from_double(af + bf),
            NyarHeadCode.f64_sub => Value.from_double(af - bf),
            NyarHeadCode.f64_mul => Value.from_double(af * bf),
            NyarHeadCode.f64_div => Value.from_double(af / bf),
            _ => Value.@null
        };

        stack.push(result);
        return true;
    }

    #endregion

    #region f64 一元操作

    private static bool is_f64_unary(NyarHeadCode op)
    {
        return op is NyarHeadCode.f64_neg or NyarHeadCode.f64_sqrt;
    }

    private static bool execute_f64_unary(NyarHeadCode headCode, ValueStack stack)
    {
        var a = stack.pop();
        var result = headCode switch
        {
            NyarHeadCode.f64_neg => Value.from_double(-a.f64),
            NyarHeadCode.f64_sqrt => Value.from_double(Math.Sqrt(a.f64)),
            _ => Value.@null
        };

        stack.push(result);
        return true;
    }

    #endregion

    #region f64 比较操作

    private static bool is_f64_compare(NyarHeadCode op)
    {
        return op is NyarHeadCode.f64_eq or NyarHeadCode.f64_ne
            or NyarHeadCode.f64_lt or NyarHeadCode.f64_le
            or NyarHeadCode.f64_gt or NyarHeadCode.f64_ge;
    }

    private static bool execute_f64_compare(NyarHeadCode headCode, ValueStack stack)
    {
        var b = stack.pop();
        var a = stack.pop();
        var af = a.f64;
        var bf = b.f64;

        var result = headCode switch
        {
            NyarHeadCode.f64_eq => Value.from_bool(af == bf),
            NyarHeadCode.f64_ne => Value.from_bool(af != bf),
            NyarHeadCode.f64_lt => Value.from_bool(af < bf),
            NyarHeadCode.f64_le => Value.from_bool(af <= bf),
            NyarHeadCode.f64_gt => Value.from_bool(af > bf),
            NyarHeadCode.f64_ge => Value.from_bool(af >= bf),
            _ => Value.@null
        };

        stack.push(result);
        return true;
    }

    #endregion

    #region 类型转换

    private static bool is_conversion(NyarHeadCode op)
    {
        return op is NyarHeadCode.i32_extend_i64_s or NyarHeadCode.i32_extend_i64_u
            or NyarHeadCode.i64_trunc_i32_s or NyarHeadCode.i64_trunc_i32_u
            or NyarHeadCode.i32_to_f32_s or NyarHeadCode.i32_to_f64_s
            or NyarHeadCode.i64_to_f64 or NyarHeadCode.f64_to_i32 or NyarHeadCode.f64_to_i64;
    }

    private static bool execute_conversion(NyarHeadCode headCode, ValueStack stack)
    {
        var a = stack.pop();

        var result = headCode switch
        {
            NyarHeadCode.i32_extend_i64_s => Value.from_long(a.i32),
            NyarHeadCode.i32_extend_i64_u => Value.from_long((uint)a.i32),
            NyarHeadCode.i64_trunc_i32_s => Value.from_int((int)a.i64),
            NyarHeadCode.i64_trunc_i32_u => Value.from_int((int)(ulong)a.i64),
            NyarHeadCode.i32_to_f32_s => Value.from_double(a.i32),
            NyarHeadCode.i32_to_f64_s => Value.from_double(a.i32),
            NyarHeadCode.i64_to_f64 => Value.from_double(a.i64),
            NyarHeadCode.f64_to_i32 => Value.from_int((int)a.f64),
            NyarHeadCode.f64_to_i64 => Value.from_long((long)a.f64),
            _ => Value.@null
        };

        stack.push(result);
        return true;
    }

    #endregion

    #region UTF-8 文本操作

    private static bool is_utf8_op(NyarHeadCode op)
    {
        return op is NyarHeadCode.utf8_concat or NyarHeadCode.utf8_len_bytes
            or NyarHeadCode.utf8_len_chars or NyarHeadCode.utf8_substr
            or NyarHeadCode.utf8_eq or NyarHeadCode.utf8_ne;
    }

    private static bool execute_utf8_op(NyarHeadCode headCode, ValueStack stack)
    {
        switch (headCode)
        {
            case NyarHeadCode.utf8_concat:
            {
                var b = stack.pop();
                var a = stack.pop();
                var sa = a.utf8 as string ?? a.ToString();
                var sb = b.utf8 as string ?? b.ToString();
                stack.push(Value.from_string(string.Concat(sa, sb)));
                return true;
            }
            case NyarHeadCode.utf8_len_bytes:
            {
                var a = stack.pop();
                var s = a.utf8 as string ?? a.ToString();
                stack.push(Value.from_int(Encoding.UTF8.GetByteCount(s)));
                return true;
            }
            case NyarHeadCode.utf8_len_chars:
            {
                var a = stack.pop();
                var s = a.utf8 as string ?? a.ToString();
                stack.push(Value.from_int(s.Length));
                return true;
            }
            case NyarHeadCode.utf8_substr:
            {
                var len = stack.pop();
                var start = stack.pop();
                var a = stack.pop();
                var s = a.utf8 as string ?? a.ToString();
                var si = start.i32;
                var sl = len.i32;
                if (si < 0 || si > s.Length)
                    stack.push(Value.from_string(""));
                else if (si + sl > s.Length)
                    stack.push(Value.from_string(s[si..]));
                else
                    stack.push(Value.from_string(s.Substring(si, sl)));

                return true;
            }
            case NyarHeadCode.utf8_eq:
            {
                var b = stack.pop();
                var a = stack.pop();
                var sa = a.utf8 as string ?? a.ToString();
                var sb = b.utf8 as string ?? b.ToString();
                stack.push(Value.from_bool(string.Equals(sa, sb, StringComparison.Ordinal)));
                return true;
            }
            case NyarHeadCode.utf8_ne:
            {
                var b = stack.pop();
                var a = stack.pop();
                var sa = a.utf8 as string ?? a.ToString();
                var sb = b.utf8 as string ?? b.ToString();
                stack.push(Value.from_bool(!string.Equals(sa, sb, StringComparison.Ordinal)));
                return true;
            }
            default:
                return false;
        }
    }

    #endregion

    #region BigInt 操作

    private static bool is_big_int_binary(NyarHeadCode op)
    {
        return op is NyarHeadCode.big_int_add or NyarHeadCode.big_int_sub or NyarHeadCode.big_int_mul;
    }

    private static bool execute_big_int_binary(NyarHeadCode headCode, ValueStack stack)
    {
        var b = stack.pop();
        var a = stack.pop();
        var ba = a.big_int as BigInteger? ?? BigInteger.Zero;
        var bb = b.big_int as BigInteger? ?? BigInteger.Zero;

        var result = headCode switch
        {
            NyarHeadCode.big_int_add => Value.from_big_int(ba + bb),
            NyarHeadCode.big_int_sub => Value.from_big_int(ba - bb),
            NyarHeadCode.big_int_mul => Value.from_big_int(ba * bb),
            _ => Value.@null
        };

        stack.push(result);
        return true;
    }

    #endregion
}
