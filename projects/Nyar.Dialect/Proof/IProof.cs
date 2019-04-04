using Nyar.ObjectAlgebra;

namespace Nyar.Dialect.Proof;

/// <summary>
///     Proof 方言的 OA 接口定义。
///     该接口声明定理、证明组合子与依赖类型相关操作。
/// </summary>
[Dialect("proof")]
public interface IProof<T>
{
    /// <summary>
    ///     定理声明。
    /// </summary>
    [Operator("theorem")]
    Term<T> Theorem(string name, Term<T> proposition);

    /// <summary>
    ///     证明构造。
    /// </summary>
    [Operator("proof")]
    Term<T> Proof(Term<T> goal, Term<T> tactic);

    /// <summary>
    ///     策略调用。
    /// </summary>
    [Operator("tactic")]
    Term<T> Tactic(string name, IReadOnlyList<Term<T>> arguments);

    /// <summary>
    ///     重写规则。
    /// </summary>
    [Operator("rewrite_rule")]
    Term<T> RewriteRule(Term<T> pattern, Term<T> target, Term<T>? condition);

    /// <summary>
    ///     归纳证明。
    /// </summary>
    [Operator("induction")]
    Term<T> Induction(Term<T> variable, Term<T> baseCase, Term<T> inductiveStep);

    /// <summary>
    ///     证明检查。
    /// </summary>
    [Operator("check")]
    Term<T> Check(Term<T> proposition, string solver);

    /// <summary>
    ///     证明提取。
    /// </summary>
    [Operator("extract")]
    Term<T> Extract(Term<T> proof, string targetLanguage);

    /// <summary>
    ///     自反性证明。
    /// </summary>
    [Operator("refl")]
    Term<T> Refl(Term<T> term);

    /// <summary>
    ///     对称性证明。
    /// </summary>
    [Operator("sym")]
    Term<T> Sym(Term<T> proof);

    /// <summary>
    ///     传递性证明。
    /// </summary>
    [Operator("trans")]
    Term<T> Trans(Term<T> proof1, Term<T> proof2);

    /// <summary>
    ///     同余性证明。
    /// </summary>
    [Operator("cong")]
    Term<T> Cong(Term<T> function, IReadOnlyList<Term<T>> proofs);

    /// <summary>
    ///     替换证明。
    /// </summary>
    [Operator("subst")]
    Term<T> Subst(Term<T> proof, Term<T> variable, Term<T> term);

    /// <summary>
    ///     向量类型。
    /// </summary>
    [Operator("vector_type")]
    Term<T> VectorType(Term<T> elementType, Term<T> length);

    /// <summary>
    ///     向量构造。
    /// </summary>
    [Operator("vector_cons")]
    Term<T> VectorCons(Term<T> head, Term<T> tail, Term<T> lengthProof);

    /// <summary>
    ///     空向量。
    /// </summary>
    [Operator("vector_nil")]
    Term<T> VectorNil();

    /// <summary>
    ///     长度相等证明。
    /// </summary>
    [Operator("length_eq_proof")]
    Term<T> LengthEqProof(Term<T> left, Term<T> right);
}