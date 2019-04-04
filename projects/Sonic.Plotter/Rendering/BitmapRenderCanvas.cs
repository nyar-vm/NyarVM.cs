using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;

namespace Plotter.Rendering;

/// <summary>
///     基于 GDI+ 的位图渲染画布，使用 <see cref="System.Drawing.Common" /> 实现位图渲染和 PNG 导出。
/// </summary>
public sealed class BitmapRenderCanvas : IRenderCanvas, IDisposable
{
    #region IDisposable

    /// <summary>
    ///     释放位图和图形上下文资源。
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _graphics.Dispose();
        _bitmap.Dispose();
        _disposed = true;
    }

    #endregion

    #region 字段

    /// <summary>
    ///     底层位图对象。
    /// </summary>
    private Bitmap _bitmap;

    /// <summary>
    ///     GDI+ 图形上下文。
    /// </summary>
    private Graphics _graphics;

    /// <summary>
    ///     当前填充颜色。
    /// </summary>
    private PlotColor _fill_color;

    /// <summary>
    ///     当前描边颜色。
    /// </summary>
    private PlotColor _stroke_color;

    /// <summary>
    ///     当前线宽。
    /// </summary>
    private float _line_width = 1.0f;

    /// <summary>
    ///     当前虚线模式。
    /// </summary>
    private float[] _line_dash = [];

    /// <summary>
    ///     当前字体。
    /// </summary>
    private PlotFont _font;

    /// <summary>
    ///     路径点集合，用于累积路径绘制操作。
    /// </summary>
    private readonly List<PathAction> _path_actions = [];

    /// <summary>
    ///     路径是否已开始。
    /// </summary>
    private bool _path_started;

    /// <summary>
    ///     对象是否已释放。
    /// </summary>
    private bool _disposed;

    #endregion

    #region 构造函数

    /// <summary>
    ///     初始化 <see cref="BitmapRenderCanvas" /> 类的新实例，默认尺寸为 800×600。
    /// </summary>
    public BitmapRenderCanvas()
        : this(800, 600)
    {
    }

    /// <summary>
    ///     初始化 <see cref="BitmapRenderCanvas" /> 类的新实例，指定画布尺寸。
    /// </summary>
    /// <param name="width">画布宽度。</param>
    /// <param name="height">画布高度。</param>
    public BitmapRenderCanvas(int width, int height)
    {
        _fill_color = new PlotColor(0, 0, 0);
        _stroke_color = new PlotColor(0, 0, 0);
        _font = new PlotFont("Microsoft YaHei", 12f);
        _bitmap = new Bitmap(width, height);
        _graphics = CreateGraphics(_bitmap);
    }

    #endregion

    #region 画布管理

    /// <summary>
    ///     获取画布宽度（像素）。
    /// </summary>
    public int width => _bitmap.Width;

    /// <summary>
    ///     获取画布高度（像素）。
    /// </summary>
    public int height => _bitmap.Height;

    /// <summary>
    ///     设置画布尺寸，将重新创建位图和图形上下文。
    /// </summary>
    /// <param name="width">新宽度。</param>
    /// <param name="height">新高度。</param>
    public void set_size(int width, int height)
    {
        var oldBitmap = _bitmap;
        var oldGraphics = _graphics;

        _bitmap = new Bitmap(width, height);
        _graphics = CreateGraphics(_bitmap);

        oldGraphics.Dispose();
        oldBitmap.Dispose();
    }

    /// <summary>
    ///     开始渲染，清除画布并准备图形上下文。
    /// </summary>
    public void begin_render()
    {
        _graphics.ResetTransform();
        _graphics.SmoothingMode = SmoothingMode.AntiAlias;
        _graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
        _graphics.Clear(Color.White);
    }

    /// <summary>
    ///     结束渲染，位图画布无需额外操作。
    /// </summary>
    public void end_render()
    {
    }

    /// <summary>
    ///     清空画布，填充白色背景。
    /// </summary>
    public void clear()
    {
        _graphics.Clear(Color.White);
    }

    #endregion

    #region 样式设置

    /// <summary>
    ///     设置填充颜色。
    /// </summary>
    /// <param name="color">填充颜色。</param>
    public void set_fill_color(PlotColor color)
    {
        _fill_color = color;
    }

    /// <summary>
    ///     设置描边颜色。
    /// </summary>
    /// <param name="color">描边颜色。</param>
    public void set_stroke_color(PlotColor color)
    {
        _stroke_color = color;
    }

    /// <summary>
    ///     设置线宽。
    /// </summary>
    /// <param name="width">线宽值。</param>
    public void set_line_width(float width)
    {
        _line_width = width;
    }

    /// <summary>
    ///     设置虚线模式。
    /// </summary>
    /// <param name="pattern">虚线段长度数组。</param>
    public void set_line_dash(float[] pattern)
    {
        _line_dash = pattern;
    }

    /// <summary>
    ///     设置字体。
    /// </summary>
    /// <param name="font">字体信息。</param>
    public void set_font(PlotFont font)
    {
        _font = font;
    }

    #endregion

    #region 图形绘制

    /// <summary>
    ///     填充矩形。
    /// </summary>
    /// <param name="x">左上角 X 坐标。</param>
    /// <param name="y">左上角 Y 坐标。</param>
    /// <param name="w">宽度。</param>
    /// <param name="h">高度。</param>
    public void fill_rect(float x, float y, float w, float h)
    {
        using var brush = new SolidBrush(ToDrawingColor(_fill_color));
        _graphics.FillRectangle(brush, x, y, w, h);
    }

    /// <summary>
    ///     描边矩形。
    /// </summary>
    /// <param name="x">左上角 X 坐标。</param>
    /// <param name="y">左上角 Y 坐标。</param>
    /// <param name="w">宽度。</param>
    /// <param name="h">高度。</param>
    public void stroke_rect(float x, float y, float w, float h)
    {
        using var pen = CreateStrokePen();
        _graphics.DrawRectangle(pen, x, y, w, h);
    }

    /// <summary>
    ///     填充圆形。
    /// </summary>
    /// <param name="cx">圆心 X 坐标。</param>
    /// <param name="cy">圆心 Y 坐标。</param>
    /// <param name="r">半径。</param>
    public void fill_circle(float cx, float cy, float r)
    {
        using var brush = new SolidBrush(ToDrawingColor(_fill_color));
        _graphics.FillEllipse(brush, cx - r, cy - r, r * 2, r * 2);
    }

    /// <summary>
    ///     描边圆形。
    /// </summary>
    /// <param name="cx">圆心 X 坐标。</param>
    /// <param name="cy">圆心 Y 坐标。</param>
    /// <param name="r">半径。</param>
    public void stroke_circle(float cx, float cy, float r)
    {
        using var pen = CreateStrokePen();
        _graphics.DrawEllipse(pen, cx - r, cy - r, r * 2, r * 2);
    }

    /// <summary>
    ///     填充椭圆。
    /// </summary>
    /// <param name="cx">中心 X 坐标。</param>
    /// <param name="cy">中心 Y 坐标。</param>
    /// <param name="rx">水平半径。</param>
    /// <param name="ry">垂直半径。</param>
    public void fill_ellipse(float cx, float cy, float rx, float ry)
    {
        using var brush = new SolidBrush(ToDrawingColor(_fill_color));
        _graphics.FillEllipse(brush, cx - rx, cy - ry, rx * 2, ry * 2);
    }

    /// <summary>
    ///     描边椭圆。
    /// </summary>
    /// <param name="cx">中心 X 坐标。</param>
    /// <param name="cy">中心 Y 坐标。</param>
    /// <param name="rx">水平半径。</param>
    /// <param name="ry">垂直半径。</param>
    public void stroke_ellipse(float cx, float cy, float rx, float ry)
    {
        using var pen = CreateStrokePen();
        _graphics.DrawEllipse(pen, cx - rx, cy - ry, rx * 2, ry * 2);
    }

    #endregion

    #region 路径绘制

    /// <summary>
    ///     开始新路径，清除之前累积的路径操作。
    /// </summary>
    public void begin_path()
    {
        _path_actions.Clear();
        _path_started = true;
    }

    /// <summary>
    ///     将路径起点移动到指定坐标。
    /// </summary>
    /// <param name="x">目标 X 坐标。</param>
    /// <param name="y">目标 Y 坐标。</param>
    public void move_to(float x, float y)
    {
        _path_actions.Add(new PathAction(PathActionKind.MoveTo, x, y));
    }

    /// <summary>
    ///     从当前点画直线到指定坐标。
    /// </summary>
    /// <param name="x">目标 X 坐标。</param>
    /// <param name="y">目标 Y 坐标。</param>
    public void line_to(float x, float y)
    {
        _path_actions.Add(new PathAction(PathActionKind.LineTo, x, y));
    }

    /// <summary>
    ///     绘制圆弧。
    /// </summary>
    /// <param name="cx">圆心 X 坐标。</param>
    /// <param name="cy">圆心 Y 坐标。</param>
    /// <param name="r">半径。</param>
    /// <param name="start_angle">起始角度（弧度）。</param>
    /// <param name="sweep_angle">扫过角度（弧度）。</param>
    public void arc(float cx, float cy, float r, float start_angle, float sweep_angle)
    {
        _path_actions.Add(new PathAction(PathActionKind.Arc, cx, cy, r, start_angle, sweep_angle));
    }

    /// <summary>
    ///     闭合当前路径。
    /// </summary>
    public void close_path()
    {
        _path_actions.Add(new PathAction(PathActionKind.ClosePath, 0, 0));
    }

    /// <summary>
    ///     使用填充颜色填充当前路径。
    /// </summary>
    public void fill()
    {
        if (!_path_started || _path_actions.Count == 0) return;

        using var path = BuildGraphicsPath();
        using var brush = new SolidBrush(ToDrawingColor(_fill_color));
        _graphics.FillPath(brush, path);
    }

    /// <summary>
    ///     使用描边颜色描边当前路径。
    /// </summary>
    public void stroke()
    {
        if (!_path_started || _path_actions.Count == 0) return;

        using var path = BuildGraphicsPath();
        using var pen = CreateStrokePen();
        _graphics.DrawPath(pen, path);
    }

    #endregion

    #region 文本绘制

    /// <summary>
    ///     填充文本。
    /// </summary>
    /// <param name="text">要绘制的文本。</param>
    /// <param name="x">文本左上角 X 坐标。</param>
    /// <param name="y">文本左上角 Y 坐标。</param>
    public void fill_text(string text, float x, float y)
    {
        using var brush = new SolidBrush(ToDrawingColor(_fill_color));
        using var font = ToDrawingFont(_font);
        _graphics.DrawString(text, font, brush, x, y);
    }

    /// <summary>
    ///     描边文本。
    /// </summary>
    /// <param name="text">要绘制的文本。</param>
    /// <param name="x">文本左上角 X 坐标。</param>
    /// <param name="y">文本左上角 Y 坐标。</param>
    public void stroke_text(string text, float x, float y)
    {
        using var pen = new Pen(ToDrawingColor(_stroke_color), _line_width);
        using var font = ToDrawingFont(_font);
        using var path = new GraphicsPath();
        path.AddString(text, font.FontFamily, (int)font.Style, font.Size, new PointF(x, y),
            StringFormat.GenericDefault);
        _graphics.DrawPath(pen, path);
    }

    #endregion

    #region 导出

    /// <summary>
    ///     导出为 PNG 格式字节数组。
    /// </summary>
    /// <returns>PNG 图片字节数组。</returns>
    public byte[] to_png_bytes()
    {
        using var stream = new MemoryStream();
        _bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }

    /// <summary>
    ///     导出为 SVG 格式字符串。位图画布不支持此操作。
    /// </summary>
    /// <returns>此方法始终抛出 <see cref="NotSupportedException" />。</returns>
    /// <exception cref="NotSupportedException">位图画布不支持 SVG 导出。</exception>
    public string to_svg_string()
    {
        throw new NotSupportedException("SVG 导出需要 SvgRenderCanvas");
    }

    /// <summary>
    ///     获取底层原生渲染上下文对象。
    /// </summary>
    /// <returns>GDI+ <see cref="Graphics" /> 实例。</returns>
    public object get_native_context()
    {
        return _graphics;
    }

    #endregion

    #region 辅助方法

    /// <summary>
    ///     创建高质量渲染的 <see cref="Graphics" /> 实例。
    /// </summary>
    /// <param name="bitmap">目标位图。</param>
    /// <returns>配置好的 <see cref="Graphics" /> 实例。</returns>
    private static Graphics CreateGraphics(Bitmap bitmap)
    {
        var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.CompositingQuality = CompositingQuality.HighQuality;
        return g;
    }

    /// <summary>
    ///     将 <see cref="PlotColor" /> 转换为 <see cref="System.Drawing.Color" />。
    /// </summary>
    /// <param name="color">绘图颜色。</param>
    /// <returns>GDI+ 颜色。</returns>
    private static Color ToDrawingColor(PlotColor color)
    {
        return Color.FromArgb(color.a, color.r, color.g, color.b);
    }

    /// <summary>
    ///     将 <see cref="PlotFont" /> 转换为 <see cref="System.Drawing.Font" />。
    /// </summary>
    /// <param name="font">绘图字体。</param>
    /// <returns>GDI+ 字体。</returns>
    private static Font ToDrawingFont(PlotFont font)
    {
        var style = font.weight == PlotFontWeight.Bold ? FontStyle.Bold : FontStyle.Regular;
        return new Font(font.family, font.size, style);
    }

    /// <summary>
    ///     根据当前描边样式创建 <see cref="Pen" /> 实例。
    /// </summary>
    /// <returns>配置好的画笔实例。</returns>
    private Pen CreateStrokePen()
    {
        var pen = new Pen(ToDrawingColor(_stroke_color), _line_width);

        if (_line_dash.Length > 0) pen.DashPattern = Array.ConvertAll(_line_dash, f => f);

        return pen;
    }

    /// <summary>
    ///     根据累积的路径操作构建 <see cref="GraphicsPath" />。
    /// </summary>
    /// <returns>构建好的图形路径。</returns>
    private GraphicsPath BuildGraphicsPath()
    {
        var path = new GraphicsPath();
        var currentX = 0f;
        var currentY = 0f;

        foreach (var action in _path_actions)
            switch (action.Kind)
            {
                case PathActionKind.MoveTo:
                    currentX = action.X;
                    currentY = action.Y;
                    break;

                case PathActionKind.LineTo:
                    path.AddLine(currentX, currentY, action.X, action.Y);
                    currentX = action.X;
                    currentY = action.Y;
                    break;

                case PathActionKind.Arc:
                    var rect = new RectangleF(
                        action.X - action.R,
                        action.Y - action.R,
                        action.R * 2,
                        action.R * 2);
                    var startDeg = action.StartAngle * (180f / (float)Math.PI);
                    var sweepDeg = action.SweepAngle * (180f / (float)Math.PI);
                    path.AddArc(rect, startDeg, sweepDeg);
                    break;

                case PathActionKind.ClosePath:
                    path.CloseFigure();
                    break;
            }

        return path;
    }

    #endregion

    #region 嵌套类型

    /// <summary>
    ///     路径操作类型。
    /// </summary>
    private enum PathActionKind
    {
        /// <summary>
        ///     移动到指定坐标。
        /// </summary>
        MoveTo,

        /// <summary>
        ///     画直线到指定坐标。
        /// </summary>
        LineTo,

        /// <summary>
        ///     绘制圆弧。
        /// </summary>
        Arc,

        /// <summary>
        ///     闭合路径。
        /// </summary>
        ClosePath
    }

    /// <summary>
    ///     记录一次路径操作的数据。
    /// </summary>
    private readonly struct PathAction
    {
        /// <summary>
        ///     路径操作类型。
        /// </summary>
        public readonly PathActionKind Kind;

        /// <summary>
        ///     X 坐标。
        /// </summary>
        public readonly float X;

        /// <summary>
        ///     Y 坐标。
        /// </summary>
        public readonly float Y;

        /// <summary>
        ///     圆弧半径。
        /// </summary>
        public readonly float R;

        /// <summary>
        ///     圆弧起始角度（弧度）。
        /// </summary>
        public readonly float StartAngle;

        /// <summary>
        ///     圆弧扫过角度（弧度）。
        /// </summary>
        public readonly float SweepAngle;

        /// <summary>
        ///     初始化移动或画线类型的路径操作。
        /// </summary>
        /// <param name="kind">操作类型。</param>
        /// <param name="x">X 坐标。</param>
        /// <param name="y">Y 坐标。</param>
        public PathAction(PathActionKind kind, float x, float y)
        {
            Kind = kind;
            X = x;
            Y = y;
            R = 0;
            StartAngle = 0;
            SweepAngle = 0;
        }

        /// <summary>
        ///     初始化圆弧类型的路径操作。
        /// </summary>
        /// <param name="kind">操作类型。</param>
        /// <param name="cx">圆心 X 坐标。</param>
        /// <param name="cy">圆心 Y 坐标。</param>
        /// <param name="r">半径。</param>
        /// <param name="startAngle">起始角度（弧度）。</param>
        /// <param name="sweepAngle">扫过角度（弧度）。</param>
        public PathAction(PathActionKind kind, float cx, float cy, float r, float startAngle, float sweepAngle)
        {
            Kind = kind;
            X = cx;
            Y = cy;
            R = r;
            StartAngle = startAngle;
            SweepAngle = sweepAngle;
        }
    }

    #endregion
}