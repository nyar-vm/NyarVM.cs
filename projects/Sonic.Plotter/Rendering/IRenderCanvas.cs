namespace Plotter.Rendering;

/// <summary>
///     统一 2D 绘图上下文抽象接口，所有渲染后端实现此接口。
///     提供画布管理、样式设置、图形绘制和导出能力。
/// </summary>
public interface IRenderCanvas
{
    #region 画布管理

    /// <summary>
    ///     获取画布宽度（单位：像素）。
    /// </summary>
    int width { get; }

    /// <summary>
    ///     获取画布高度（单位：像素）。
    /// </summary>
    int height { get; }

    /// <summary>
    ///     设置画布尺寸。
    /// </summary>
    /// <param name="width">画布宽度（单位：像素）。</param>
    /// <param name="height">画布高度（单位：像素）。</param>
    void set_size(int width, int height);

    /// <summary>
    ///     开始渲染帧，在绘制操作之前调用。
    /// </summary>
    void begin_render();

    /// <summary>
    ///     结束渲染帧，在所有绘制操作完成后调用。
    /// </summary>
    void end_render();

    /// <summary>
    ///     清空画布内容，恢复为初始状态。
    /// </summary>
    void clear();

    #endregion

    #region 样式设置

    /// <summary>
    ///     设置后续填充操作的颜色。
    /// </summary>
    /// <param name="color">填充颜色。</param>
    void set_fill_color(PlotColor color);

    /// <summary>
    ///     设置后续描边操作的颜色。
    /// </summary>
    /// <param name="color">描边颜色。</param>
    void set_stroke_color(PlotColor color);

    /// <summary>
    ///     设置后续描边操作的线宽。
    /// </summary>
    /// <param name="width">线宽（单位：像素）。</param>
    void set_line_width(float width);

    /// <summary>
    ///     设置后续描边操作的虚线模式。
    /// </summary>
    /// <param name="pattern">虚线模式数组，交替表示线段长度和间隔长度（单位：像素）。null 表示实线。</param>
    void set_line_dash(float[] pattern);

    /// <summary>
    ///     设置后续文本绘制操作的字体。
    /// </summary>
    /// <param name="font">字体描述信息。</param>
    void set_font(PlotFont font);

    #endregion

    #region 图形绘制

    /// <summary>
    ///     填充矩形。
    /// </summary>
    /// <param name="x">矩形左上角 X 坐标。</param>
    /// <param name="y">矩形左上角 Y 坐标。</param>
    /// <param name="w">矩形宽度。</param>
    /// <param name="h">矩形高度。</param>
    void fill_rect(float x, float y, float w, float h);

    /// <summary>
    ///     描边矩形。
    /// </summary>
    /// <param name="x">矩形左上角 X 坐标。</param>
    /// <param name="y">矩形左上角 Y 坐标。</param>
    /// <param name="w">矩形宽度。</param>
    /// <param name="h">矩形高度。</param>
    void stroke_rect(float x, float y, float w, float h);

    /// <summary>
    ///     填充圆形。
    /// </summary>
    /// <param name="cx">圆心 X 坐标。</param>
    /// <param name="cy">圆心 Y 坐标。</param>
    /// <param name="r">半径。</param>
    void fill_circle(float cx, float cy, float r);

    /// <summary>
    ///     描边圆形。
    /// </summary>
    /// <param name="cx">圆心 X 坐标。</param>
    /// <param name="cy">圆心 Y 坐标。</param>
    /// <param name="r">半径。</param>
    void stroke_circle(float cx, float cy, float r);

    /// <summary>
    ///     填充椭圆。
    /// </summary>
    /// <param name="cx">椭圆中心 X 坐标。</param>
    /// <param name="cy">椭圆中心 Y 坐标。</param>
    /// <param name="rx">水平半径。</param>
    /// <param name="ry">垂直半径。</param>
    void fill_ellipse(float cx, float cy, float rx, float ry);

    /// <summary>
    ///     描边椭圆。
    /// </summary>
    /// <param name="cx">椭圆中心 X 坐标。</param>
    /// <param name="cy">椭圆中心 Y 坐标。</param>
    /// <param name="rx">水平半径。</param>
    /// <param name="ry">垂直半径。</param>
    void stroke_ellipse(float cx, float cy, float rx, float ry);

    #endregion

    #region 路径绘制

    /// <summary>
    ///     开始新路径，清除当前路径的所有子路径。
    /// </summary>
    void begin_path();

    /// <summary>
    ///     将路径的当前点移动到指定坐标，不创建连线。
    /// </summary>
    /// <param name="x">目标 X 坐标。</param>
    /// <param name="y">目标 Y 坐标。</param>
    void move_to(float x, float y);

    /// <summary>
    ///     从当前点到指定坐标绘制一条直线段。
    /// </summary>
    /// <param name="x">终点 X 坐标。</param>
    /// <param name="y">终点 Y 坐标。</param>
    void line_to(float x, float y);

    /// <summary>
    ///     绘制圆弧，从起始角度到结束角度沿逆时针方向绘制。
    /// </summary>
    /// <param name="cx">圆心 X 坐标。</param>
    /// <param name="cy">圆心 Y 坐标。</param>
    /// <param name="r">半径。</param>
    /// <param name="start_angle">起始角度（弧度）。</param>
    /// <param name="end_angle">结束角度（弧度）。</param>
    void arc(float cx, float cy, float r, float start_angle, float end_angle);

    /// <summary>
    ///     从当前点到当前子路径起始点绘制一条直线，闭合当前子路径。
    /// </summary>
    void close_path();

    /// <summary>
    ///     使用当前填充样式填充当前路径的内部区域。
    /// </summary>
    void fill();

    /// <summary>
    ///     使用当前描边样式沿当前路径绘制轮廓线。
    /// </summary>
    void stroke();

    #endregion

    #region 文本绘制

    /// <summary>
    ///     在指定坐标处填充文本。
    /// </summary>
    /// <param name="text">要绘制的文本内容。</param>
    /// <param name="x">文本基线起始 X 坐标。</param>
    /// <param name="y">文本基线起始 Y 坐标。</param>
    void fill_text(string text, float x, float y);

    /// <summary>
    ///     在指定坐标处描边文本轮廓。
    /// </summary>
    /// <param name="text">要绘制的文本内容。</param>
    /// <param name="x">文本基线起始 X 坐标。</param>
    /// <param name="y">文本基线起始 Y 坐标。</param>
    void stroke_text(string text, float x, float y);

    #endregion

    #region 导出

    /// <summary>
    ///     将当前画布内容导出为 PNG 格式的字节数组。
    /// </summary>
    /// <returns>PNG 图像的字节数组。</returns>
    byte[] to_png_bytes();

    /// <summary>
    ///     将当前画布内容导出为 SVG 格式的字符串。
    /// </summary>
    /// <returns>SVG 文档字符串。</returns>
    string to_svg_string();

    /// <summary>
    ///     获取底层渲染后端的原生上下文对象，用于高级定制。
    ///     返回类型取决于具体后端实现。
    /// </summary>
    /// <returns>原生渲染上下文对象。</returns>
    object get_native_context();

    #endregion
}