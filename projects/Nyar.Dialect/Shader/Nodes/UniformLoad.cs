using Nyar.IR.Intent;

namespace Nyar.Dialect.Shader.Nodes;

[AlgebraNode]
public sealed partial record UniformLoad(Id buffer, Id index) : AlgebraNode;