namespace Std.Data.Text.Scss;

/// <summary>
///     样式选择器
/// </summary>
public readonly struct StyleSelector : IEquatable<StyleSelector>
{
    /// <summary>
    ///     选择器类型
    /// </summary>
    public readonly StyleSelectorType type;


    /// <summary>
    ///     选择器值
    /// </summary>
    public readonly string value;

    public StyleSelector(StyleSelectorType type, string value)
    {
        this.type = type;
        this.value = value;
    }


    /// <inheritdoc />
    public bool Equals(StyleSelector other)
    {
        return type == other.type && value == other.value;
    }


    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is StyleSelector other && Equals(other);
    }


    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(type, value);
    }


    /// <summary>
    ///     相等运算符
    /// </summary>
    public static bool operator ==(StyleSelector left, StyleSelector right)
    {
        return left.Equals(right);
    }


    /// <summary>
    ///     不等运算符
    /// </summary>
    public static bool operator !=(StyleSelector left, StyleSelector right)
    {
        return !left.Equals(right);
    }


    /// <summary>
    ///     创建类型选择器
    /// </summary>
    public static StyleSelector by_type(string typeName)
    {
        return new StyleSelector(StyleSelectorType.type, typeName);
    }


    /// <summary>
    ///     创建 ID 选择器
    /// </summary>
    public static StyleSelector by_id(string id)
    {
        return new StyleSelector(StyleSelectorType.id, id);
    }


    /// <summary>
    ///     创建类选择器
    /// </summary>
    public static StyleSelector by_class(string className)
    {
        return new StyleSelector(StyleSelectorType.@class, className);
    }


    /// <summary>
    ///     创建伪类选择器
    /// </summary>
    public static StyleSelector by_pseudo_class(string name)
    {
        return new StyleSelector(StyleSelectorType.pseudo_class, name);
    }


    /// <summary>
    ///     创建父引用选择器
    /// </summary>
    public static StyleSelector parent_ref(string suffix = "")
    {
        return new StyleSelector(StyleSelectorType.parent_ref, "&" + suffix);
    }
}