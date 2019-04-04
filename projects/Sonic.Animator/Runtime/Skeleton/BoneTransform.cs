namespace Animator.Runtime.Skeleton;

/// <summary>
///     骨骼关节变换数据
/// </summary>
public readonly struct BoneTransform
{
    /// <summary>局部坐标 X 偏移</summary>
    public readonly float local_offset_x;

    /// <summary>局部坐标 Y 偏移</summary>
    public readonly float local_offset_y;

    /// <summary>旋转角度（弧度）</summary>
    public readonly float rotation;

    /// <summary>X 轴缩放</summary>
    public readonly float scale_x;

    /// <summary>Y 轴缩放</summary>
    public readonly float scale_y;

    /// <summary>剪切 X</summary>
    public readonly float shear_x;

    /// <summary>剪切 Y</summary>
    public readonly float shear_y;

    public BoneTransform(float localOffsetX, float localOffsetY, float rotation, float scaleX, float scaleY,
        float shearX, float shearY)
    {
        local_offset_x = localOffsetX;
        local_offset_y = localOffsetY;
        this.rotation = rotation;
        scale_x = scaleX;
        scale_y = scaleY;
        shear_x = shearX;
        shear_y = shearY;
    }

    /// <summary>默认单位变换</summary>
    public static BoneTransform identity => new(0f, 0f, 0f, 1f, 1f, 0f, 0f);
}