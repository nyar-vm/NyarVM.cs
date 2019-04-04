using Plotter.Core;

namespace Plotter.Grammar;

/// <summary>
///     数据变换构建器，用于配置数据聚合与变换方式。
/// </summary>
public sealed class DataTransformBuilder
{
    /// <summary>
    ///     所属的图表画布实例。
    /// </summary>
    private readonly ChartCanvas _canvas;

    /// <summary>
    ///     初始化 <see cref="DataTransformBuilder" /> 类的新实例。
    /// </summary>
    /// <param name="canvas">所属的图表画布。</param>
    public DataTransformBuilder(ChartCanvas canvas)
    {
        _canvas = canvas;
    }

    /// <summary>
    ///     变换类型。
    /// </summary>
    internal TransformType _transform_type { get; private set; }

    /// <summary>
    ///     目标字段名称。
    /// </summary>
    internal string _field { get; private set; } = "";

    /// <summary>
    ///     分组字段名称。
    /// </summary>
    internal string _group_by_field { get; private set; } = "";

    /// <summary>
    ///     设置数据变换类型。
    /// </summary>
    /// <param name="transform_type">变换类型。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public DataTransformBuilder with_transform_type(TransformType transform_type)
    {
        _transform_type = transform_type;
        return this;
    }

    /// <summary>
    ///     设置目标字段。
    /// </summary>
    /// <param name="field_name">目标字段名称。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public DataTransformBuilder with_field(string field_name)
    {
        _field = field_name;
        return this;
    }

    /// <summary>
    ///     设置分组字段。
    /// </summary>
    /// <param name="field_name">分组字段名称。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public DataTransformBuilder group_by(string field_name)
    {
        _group_by_field = field_name;
        return this;
    }

    /// <summary>
    ///     完成数据变换配置，返回所属图表画布。
    /// </summary>
    /// <returns>所属的图表画布实例。</returns>
    public ChartCanvas build()
    {
        return _canvas;
    }
}