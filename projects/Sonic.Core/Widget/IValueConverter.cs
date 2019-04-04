using System;

namespace Core.Widget;

/// <summary>
///     IValueConverter 接口
/// </summary>
public interface IValueConverter
{
    /// <summary>
    ///     将值转换为目标类型
    /// </summary>
    object? convert(object? value, Type target_type);

    /// <summary>
    ///     将值转换回源类型
    /// </summary>
    object? convert_back(object? value, Type source_type);
}