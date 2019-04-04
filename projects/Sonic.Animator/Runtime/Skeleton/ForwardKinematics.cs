using System;
using System.Collections.Generic;

namespace Animator.Runtime.Skeleton;

/// <summary>
///     正向运动学计算器
/// </summary>
public static class ForwardKinematics
{
    /// <summary>
    ///     根据骨骼层级关系，从根骨骼向下计算所有骨骼的世界变换
    /// </summary>
    public static void compute_world_transforms(IReadOnlyList<BoneTransform> localTransforms,
        IReadOnlyList<int> parentIndices, IList<BoneTransform> worldTransforms)
    {
        for (var index = 0; index < localTransforms.Count; index++)
        {
            var local = localTransforms[index];
            var parentIndex = parentIndices[index];

            if (parentIndex < 0)
            {
                worldTransforms[index] = local;
                continue;
            }

            var parent = worldTransforms[parentIndex];
            var cos = (float)Math.Cos(parent.rotation);
            var sin = (float)Math.Sin(parent.rotation);

            var worldOffsetX = parent.local_offset_x + local.local_offset_x * cos - local.local_offset_y * sin;
            var worldOffsetY = parent.local_offset_y + local.local_offset_x * sin + local.local_offset_y * cos;
            var worldRotation = parent.rotation + local.rotation;
            var worldScaleX = parent.scale_x * local.scale_x;
            var worldScaleY = parent.scale_y * local.scale_y;

            worldTransforms[index] = new BoneTransform(worldOffsetX, worldOffsetY, worldRotation, worldScaleX,
                worldScaleY, local.shear_x, local.shear_y);
        }
    }
}