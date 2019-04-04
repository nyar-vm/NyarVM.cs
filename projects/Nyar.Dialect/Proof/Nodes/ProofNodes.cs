using Nyar.IR.Intent;

namespace Nyar.Dialect.Proof.Nodes;

[AlgebraNode]
public sealed partial record Theorem(string name, Id proposition) : AlgebraNode;

[AlgebraNode]
public sealed partial record Proof(Id goal, Id tactic) : AlgebraNode;

[AlgebraNode]
public sealed partial record Tactic(string name, IReadOnlyList<Id> arguments) : AlgebraNode;

[AlgebraNode]
public sealed partial record RewriteRule(Id pattern, Id target, Id? condition) : AlgebraNode;

[AlgebraNode]
public sealed partial record Induction(Id variable, Id baseCase, Id inductiveStep) : AlgebraNode;

[AlgebraNode]
public sealed partial record Check(Id proposition, string solver) : AlgebraNode;

[AlgebraNode]
public sealed partial record Extract(Id proof, string targetLanguage) : AlgebraNode;

#region 证明组合子

[AlgebraNode]
public sealed partial record Refl(Id term) : AlgebraNode;

[AlgebraNode]
public sealed partial record Sym(Id proof) : AlgebraNode;

[AlgebraNode]
public sealed partial record Trans(Id proof1, Id proof2) : AlgebraNode;

[AlgebraNode]
public sealed partial record Cong(Id function, IReadOnlyList<Id> proofs) : AlgebraNode;

[AlgebraNode]
public sealed partial record Subst(Id proof, Id variable, Id term) : AlgebraNode;

#endregion

#region 依赖类型

[AlgebraNode]
public sealed partial record VectorType(Id elementType, Id length) : AlgebraNode;

[AlgebraNode]
public sealed partial record VectorCons(Id head, Id tail, Id lengthProof) : AlgebraNode;

[AlgebraNode]
public sealed partial record VectorNil : AlgebraNode;

[AlgebraNode]
public sealed partial record LengthEqProof(Id left, Id right) : AlgebraNode;

#endregion