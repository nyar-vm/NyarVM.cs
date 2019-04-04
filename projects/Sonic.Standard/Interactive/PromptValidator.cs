namespace Sonic.Interactive;

/// <summary>
/// 输入验证器，提供常用的验证逻辑
/// </summary>
public static class PromptValidator
{
    /// <summary>
    /// 创建非空验证器
    /// </summary>
    /// <param name="message">验证失败时的错误消息</param>
    /// <returns>验证函数</returns>
    public static Func<string, string?> required(string? message = null)
    {
        return input => string.IsNullOrWhiteSpace(input) ? (message ?? "此字段不能为空") : null;
    }

    /// <summary>
    /// 创建最小长度验证器
    /// </summary>
    /// <param name="minLength">最小长度</param>
    /// <param name="message">验证失败时的错误消息</param>
    /// <returns>验证函数</returns>
    public static Func<string, string?> min_length(int minLength, string? message = null)
    {
        return input => input.Length < minLength ? (message ?? $"长度不能少于 {minLength} 个字符") : null;
    }

    /// <summary>
    /// 创建最大长度验证器
    /// </summary>
    /// <param name="maxLength">最大长度</param>
    /// <param name="message">验证失败时的错误消息</param>
    /// <returns>验证函数</returns>
    public static Func<string, string?> max_length(int maxLength, string? message = null)
    {
        return input => input.Length > maxLength ? (message ?? $"长度不能超过 {maxLength} 个字符") : null;
    }

    /// <summary>
    /// 创建正则表达式验证器
    /// </summary>
    /// <param name="pattern">正则表达式模式</param>
    /// <param name="message">验证失败时的错误消息</param>
    /// <returns>验证函数</returns>
    public static Func<string, string?> regex(string pattern, string? message = null)
    {
        return input => !System.Text.RegularExpressions.Regex.IsMatch(input, pattern) ? (message ?? "输入格式不正确") : null;
    }

    /// <summary>
    /// 创建范围验证器（数值类型）
    /// </summary>
    /// <typeparam name="T">数值类型</typeparam>
    /// <param name="min">最小值</param>
    /// <param name="max">最大值</param>
    /// <param name="message">验证失败时的错误消息</param>
    /// <returns>验证函数</returns>
    public static Func<string, string?> range<T>(T min, T max, string? message = null) where T : IComparable<T>
    {
        return input =>
        {
            try
            {
                var value = (T)Convert.ChangeType(input, typeof(T));
                if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
                {
                    return message ?? $"值必须在 {min} 和 {max} 之间";
                }

                return null;
            }
            catch
            {
                return message ?? $"请输入 {min} 到 {max} 之间的数值";
            }
        };
    }

    /// <summary>
    /// 创建邮箱验证器
    /// </summary>
    /// <param name="message">验证失败时的错误消息</param>
    /// <returns>验证函数</returns>
    public static Func<string, string?> email(string? message = null)
    {
        return regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", message ?? "请输入有效的邮箱地址");
    }

    /// <summary>
    /// 创建文件路径验证器
    /// </summary>
    /// <param name="mustExist">文件是否必须存在</param>
    /// <param name="message">验证失败时的错误消息</param>
    /// <returns>验证函数</returns>
    public static Func<string, string?> file_path(bool mustExist = false, string? message = null)
    {
        return input =>
        {
            var invalidChars = System.IO.Path.GetInvalidPathChars();
            if (input.IndexOfAny(invalidChars) >= 0)
            {
                return message ?? "文件路径包含非法字符";
            }

            if (mustExist && !System.IO.File.Exists(input))
            {
                return message ?? "文件不存在";
            }

            return null;
        };
    }

    /// <summary>
    /// 创建目录路径验证器
    /// </summary>
    /// <param name="mustExist">目录是否必须存在</param>
    /// <param name="message">验证失败时的错误消息</param>
    /// <returns>验证函数</returns>
    public static Func<string, string?> directory_path(bool mustExist = false, string? message = null)
    {
        return input =>
        {
            var invalidChars = System.IO.Path.GetInvalidPathChars();
            if (input.IndexOfAny(invalidChars) >= 0)
            {
                return message ?? "目录路径包含非法字符";
            }

            if (mustExist && !System.IO.Directory.Exists(input))
            {
                return message ?? "目录不存在";
            }

            return null;
        };
    }

    /// <summary>
    /// 组合多个验证器
    /// </summary>
    /// <param name="validators">验证器列表</param>
    /// <returns>组合后的验证函数</returns>
    public static Func<string, string?> combine(params Func<string, string?>[] validators)
    {
        return input =>
        {
            foreach (var validator in validators)
            {
                var error = validator(input);
                if (error is not null)
                {
                    return error;
                }
            }

            return null;
        };
    }
}
