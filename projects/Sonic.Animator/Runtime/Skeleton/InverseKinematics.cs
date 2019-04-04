using System;
using System.Numerics;

namespace Animator.Runtime.Skeleton;

/// <summary>
///     简易逆运动学求解器（双骨骼 CCD 算法）
/// </summary>
public static class InverseKinematics
{
    /// <summary>
    ///     使用循环坐标下降法求解逆运动学
    /// </summary>
    /// <param name="jointPositions">关节位置数组（从根到末端）</param>
    /// <param name="targetX">目标点 X</param>
    /// <param name="targetY">目标点 Y</param>
    /// <param name="iterationCount">迭代次数</param>
    public static void solve_cyclic_coordinate_descent(Span<Vector2> jointPositions, float targetX, float targetY,
        int iterationCount)
    {
        var target = new Vector2(targetX, targetY);

        for (var iteration = 0; iteration < iterationCount; iteration++)
        for (var jointIndex = jointPositions.Length - 2; jointIndex >= 0; jointIndex--)
        {
            var joint = jointPositions[jointIndex];
            var endEffector = jointPositions[jointPositions.Length - 1];

            var toEnd = Vector2.Normalize(endEffector - joint);
            var toTarget = Vector2.Normalize(target - joint);

            var angle = (float)Math.Atan2(
                toEnd.X * toTarget.Y - toEnd.Y * toTarget.X,
                toEnd.X * toTarget.X + toEnd.Y * toTarget.Y
            );

            for (var childIndex = jointIndex + 1; childIndex < jointPositions.Length; childIndex++)
            {
                var offset = jointPositions[childIndex] - joint;
                var cos = (float)Math.Cos(angle);
                var sin = (float)Math.Sin(angle);
                var rotated = new Vector2(
                    offset.X * cos - offset.Y * sin,
                    offset.X * sin + offset.Y * cos
                );
                jointPositions[childIndex] = joint + rotated;
            }
        }
    }
}