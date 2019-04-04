using System;

namespace Animator.Extensions;

/// <summary>
///     坐标转换工具
/// </summary>
public static class CoordinateConverter
{
    /// <summary>
    ///     将局部坐标转换为世界坐标
    /// </summary>
    /// <param name="localX">局部 X 坐标</param>
    /// <param name="localY">局部 Y 坐标</param>
    /// <param name="translateX">平移 X</param>
    /// <param name="translateY">平移 Y</param>
    /// <param name="scaleRatio">缩放比例</param>
    /// <param name="rotation">旋转角度（弧度）</param>
    /// <returns>世界坐标 (X, Y)</returns>
    public static (float WorldX, float WorldY) local_to_world(float localX, float localY, float translateX,
        float translateY, float scaleRatio, float rotation)
    {
        var scaledX = localX * scaleRatio;
        var scaledY = localY * scaleRatio;
        var cos = (float)Math.Cos(rotation);
        var sin = (float)Math.Sin(rotation);
        var rotatedX = scaledX * cos - scaledY * sin;
        var rotatedY = scaledX * sin + scaledY * cos;
        return (rotatedX + translateX, rotatedY + translateY);
    }

    /// <summary>
    ///     将世界坐标转换为局部坐标
    /// </summary>
    /// <param name="worldX">世界 X 坐标</param>
    /// <param name="worldY">世界 Y 坐标</param>
    /// <param name="translateX">平移 X</param>
    /// <param name="translateY">平移 Y</param>
    /// <param name="scaleRatio">缩放比例</param>
    /// <param name="rotation">旋转角度（弧度）</param>
    /// <returns>局部坐标 (X, Y)</returns>
    public static (float LocalX, float LocalY) world_to_local(float worldX, float worldY, float translateX,
        float translateY, float scaleRatio, float rotation)
    {
        var deltaX = worldX - translateX;
        var deltaY = worldY - translateY;
        var cos = (float)Math.Cos(-rotation);
        var sin = (float)Math.Sin(-rotation);
        var rotatedX = deltaX * cos - deltaY * sin;
        var rotatedY = deltaX * sin + deltaY * cos;
        var inverseScale = scaleRatio != 0f ? 1f / scaleRatio : 0f;
        return (rotatedX * inverseScale, rotatedY * inverseScale);
    }

    /// <summary>
    ///     将归一化坐标（0~1）转换为像素坐标
    /// </summary>
    /// <param name="normalizedX">归一化 X 坐标</param>
    /// <param name="normalizedY">归一化 Y 坐标</param>
    /// <param name="canvasWidth">画布宽度</param>
    /// <param name="canvasHeight">画布高度</param>
    /// <returns>像素坐标 (X, Y)</returns>
    public static (float PixelX, float PixelY) normalized_to_pixel(float normalizedX, float normalizedY,
        float canvasWidth, float canvasHeight)
    {
        return (normalizedX * canvasWidth, normalizedY * canvasHeight);
    }
}