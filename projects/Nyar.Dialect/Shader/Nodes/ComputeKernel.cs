using Nyar.IR.Intent;

namespace Nyar.Dialect.Shader.Nodes;

[AlgebraNode]
public sealed partial record ComputeKernel(Id body, (int X, int Y, int Z) work_group_size) : AlgebraNode
{
    /// <summary>
    ///     工作组大小（用于生成代码兼容）
    /// </summary>
    public (int X, int Y, int Z) workGroupSize => work_group_size;
}