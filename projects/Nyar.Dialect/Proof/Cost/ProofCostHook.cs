using Nyar.IR.Intent;
using Nyar.Optimizer.CostModels;
using Nyar.Types;
using ProofNode = Nyar.Dialect.Proof.Nodes.Proof;
using RewriteRuleNode = Nyar.Dialect.Proof.Nodes.RewriteRule;
using TheoremNode = Nyar.Dialect.Proof.Nodes.Theorem;
using TacticNode = Nyar.Dialect.Proof.Nodes.Tactic;
using InductionNode = Nyar.Dialect.Proof.Nodes.Induction;
using CheckNode = Nyar.Dialect.Proof.Nodes.Check;
using ExtractNode = Nyar.Dialect.Proof.Nodes.Extract;
using ReflNode = Nyar.Dialect.Proof.Nodes.Refl;
using SymNode = Nyar.Dialect.Proof.Nodes.Sym;
using TransNode = Nyar.Dialect.Proof.Nodes.Trans;
using CongNode = Nyar.Dialect.Proof.Nodes.Cong;
using SubstNode = Nyar.Dialect.Proof.Nodes.Subst;
using VectorTypeNode = Nyar.Dialect.Proof.Nodes.VectorType;
using VectorConsNode = Nyar.Dialect.Proof.Nodes.VectorCons;
using VectorNilNode = Nyar.Dialect.Proof.Nodes.VectorNil;
using LengthEqProofNode = Nyar.Dialect.Proof.Nodes.LengthEqProof;

namespace Nyar.Dialect.Proof.Cost;

/// <summary>
///     Proof 方言节点的成本估算钩子
/// </summary>
public sealed class ProofCostHook : ICostModelHook
{
    /// <inheritdoc />
    public bool CanHandle(AlgebraNode node)
    {
        return node is TheoremNode or ProofNode or TacticNode or RewriteRuleNode
            or InductionNode or CheckNode or ExtractNode or ReflNode or SymNode
            or TransNode or CongNode or SubstNode or VectorTypeNode
            or VectorConsNode or VectorNilNode or LengthEqProofNode;
    }

    /// <inheritdoc />
    public CostVector Estimate(AlgebraNode node)
    {
        return node switch
        {
            TheoremNode => CostVector.zero,
            ProofNode => CostVector.from_latency(100),
            TacticNode => CostVector.from_latency(50),
            RewriteRuleNode => CostVector.from_latency(20),
            InductionNode => CostVector.from_latency(200),
            CheckNode => new CostVector(1000, 0, 0, 0),
            ExtractNode => CostVector.from_latency(100),
            ReflNode => CostVector.from_latency(1),
            SymNode => CostVector.from_latency(5),
            TransNode => CostVector.from_latency(10),
            CongNode => CostVector.from_latency(20),
            SubstNode => CostVector.from_latency(15),
            VectorTypeNode => CostVector.zero,
            VectorConsNode => CostVector.from_latency(5),
            VectorNilNode => CostVector.from_latency(1),
            LengthEqProofNode => CostVector.from_latency(10),
            _ => CostVector.zero
        };
    }
}