namespace Nyar.Assembler;

/// <summary>
///     统一 `witness` 相关的稳定身份计算。
///     该层使用符号级名称生成稳定 ID，避免把类型 / 协议身份计算滞后到运行时序列化阶段。
/// </summary>
public static class GenerateWitnessIdentity
{
    /// <summary>
    ///     计算协议身份 ID。
    /// </summary>
    /// <param name="traitName">协议名称。</param>
    /// <returns>稳定整型 ID。</returns>
    public static int compute_interface_id(string traitName)
    {
        return compute_stable_id(normalize_symbol_name(traitName));
    }

    /// <summary>
    ///     计算目标类型身份 ID。
    /// </summary>
    /// <param name="typeName">类型名称。</param>
    /// <returns>稳定整型 ID。</returns>
    public static int compute_type_id(string typeName)
    {
        return compute_stable_id(normalize_symbol_name(typeName));
    }

    /// <summary>
    ///     计算 witness 方法身份 ID。
    /// </summary>
    /// <param name="traitName">协议名称。</param>
    /// <param name="typeName">目标类型名称。</param>
    /// <param name="methodName">方法名称。</param>
    /// <returns>稳定整型 ID。</returns>
    public static int compute_method_id(string traitName, string typeName, string methodName)
    {
        return compute_stable_id(
            normalize_symbol_name(traitName),
            normalize_symbol_name(typeName),
            normalize_symbol_name(methodName));
    }

    /// <summary>
    ///     统一规范化符号名称，避免 `::` / `.` 混用导致身份漂移。
    /// </summary>
    /// <param name="symbolName">原始符号名称。</param>
    /// <returns>规范化名称。</returns>
    public static string normalize_symbol_name(string symbolName)
    {
        return string.IsNullOrWhiteSpace(symbolName)
            ? string.Empty
            : symbolName.Replace("::", ".", StringComparison.Ordinal);
    }

    /// <summary>
    ///     计算稳定整型 ID。
    /// </summary>
    /// <param name="parts">组成身份的符号片段。</param>
    /// <returns>稳定整型 ID。</returns>
    public static int compute_stable_id(params string[] parts)
    {
        unchecked
        {
            const int offset = unchecked((int)2166136261);
            const int prime = 16777619;
            var hash = offset;

            foreach (var part in parts)
            {
                foreach (var ch in part)
                {
                    hash ^= ch;
                    hash *= prime;
                }

                hash ^= '|';
                hash *= prime;
            }

            return hash == int.MinValue ? int.MaxValue : Math.Abs(hash);
        }
    }
}