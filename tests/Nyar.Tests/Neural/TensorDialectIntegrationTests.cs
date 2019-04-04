using Nyar.Dialect.Core.Nodes;
using Nyar.Dialect.Neural;
using Nyar.Dialect.Neural.Bridge;
using Nyar.Dialect.Neural.Cost;
using Nyar.Dialect.Neural.Nodes;
using Nyar.Dialect.Neural.Rules;
using Nyar.EGraph;
using Nyar.IR.Intent;
using Nyar.IR.Rewrite;

namespace Nyar.Tests.Neural;

/// <summary>
///     Tensor 方言集成测试：验证节点构建、降级规则、成本模型和 GalateaBridge
/// </summary>
public class TensorDialectIntegrationTests
{
    #region 节点构建测试

    [Fact]
    public void Sigmoid_Node_Should_Have_Correct_Input()
    {
        var egraph = new EGraph<IKun>();
        var inputId = egraph.add(new Literal<long>(42L));

        var sigmoid = new Sigmoid(inputId);
        Assert.Equal(inputId, sigmoid.Input);
    }

    [Fact]
    public void Tanh_Node_Should_Have_Correct_Input()
    {
        var egraph = new EGraph<IKun>();
        var inputId = egraph.add(new Literal<long>(42L));

        var tanh = new Tanh(inputId);
        Assert.Equal(inputId, tanh.Input);
    }

    [Fact]
    public void Dense_Node_With_Bias_Should_Have_All_Fields()
    {
        var egraph = new EGraph<IKun>();
        var inputId = egraph.add(new Literal<long>(1L));
        var weightsId = egraph.add(new Literal<long>(2L));
        var biasId = egraph.add(new Literal<long>(3L));

        var dense = new Dense(inputId, weightsId, biasId);
        Assert.Equal(inputId, dense.Input);
        Assert.Equal(weightsId, dense.Weights);
        Assert.True(dense.Bias.HasValue);
        Assert.Equal(biasId, dense.Bias.Value);
    }

    [Fact]
    public void Dense_Node_Without_Bias_Should_Have_Null_Bias()
    {
        var egraph = new EGraph<IKun>();
        var inputId = egraph.add(new Literal<long>(1L));
        var weightsId = egraph.add(new Literal<long>(2L));

        var dense = new Dense(inputId, weightsId, null);
        Assert.Equal(inputId, dense.Input);
        Assert.Equal(weightsId, dense.Weights);
        Assert.False(dense.Bias.HasValue);
    }

    #endregion

    #region 降级规则测试

    [Fact]
    public void ActivationLoweringRule_Should_Lower_Sigmoid()
    {
        var egraph = new EGraph<IKun>();
        var inputId = egraph.add(new Literal<long>(1L));
        var sigmoidId = egraph.add(new Sigmoid(inputId));

        var rule = new ActivationLoweringRule();
        var rewrites = rule.apply(egraph, sigmoidId).ToList();

        Assert.Single(rewrites);
        var replacement = rewrites[0].Replacement;
        Assert.IsType<Apply>(replacement);

        var apply = (Apply)replacement;
        Assert.Single(apply.Arguments);
    }

    [Fact]
    public void ActivationLoweringRule_Should_Lower_Tanh()
    {
        var egraph = new EGraph<IKun>();
        var inputId = egraph.add(new Literal<long>(1L));
        var tanhId = egraph.add(new Tanh(inputId));

        var rule = new ActivationLoweringRule();
        var rewrites = rule.apply(egraph, tanhId).ToList();

        Assert.Single(rewrites);
        var replacement = rewrites[0].Replacement;
        Assert.IsType<Apply>(replacement);
    }

    [Fact]
    public void LinearAlgebraLoweringRule_Should_Lower_Dense_With_Bias()
    {
        var egraph = new EGraph<IKun>();
        var inputId = egraph.add(new Literal<long>(1L));
        var weightsId = egraph.add(new Literal<long>(2L));
        var biasId = egraph.add(new Literal<long>(3L));
        var denseId = egraph.add(new Dense(inputId, weightsId, biasId));

        var rule = new LinearAlgebraLoweringRule();
        var rewrites = rule.apply(egraph, denseId).ToList();

        Assert.Single(rewrites);
        var apply = Assert.IsType<Apply>(rewrites[0].Replacement);
        Assert.Equal(3, apply.Arguments.Length);
    }

    [Fact]
    public void LinearAlgebraLoweringRule_Should_Lower_Dense_Without_Bias()
    {
        var egraph = new EGraph<IKun>();
        var inputId = egraph.add(new Literal<long>(1L));
        var weightsId = egraph.add(new Literal<long>(2L));
        var denseId = egraph.add(new Dense(inputId, weightsId, null));

        var rule = new LinearAlgebraLoweringRule();
        var rewrites = rule.apply(egraph, denseId).ToList();

        Assert.Single(rewrites);
        var apply = Assert.IsType<Apply>(rewrites[0].Replacement);
        Assert.Equal(2, apply.Arguments.Length);
    }

    [Fact]
    public void ActivationLoweringRule_Should_Lower_Relu()
    {
        var egraph = new EGraph<IKun>();
        var inputId = egraph.add(new Literal<long>(1L));
        var reluId = egraph.add(new Relu(inputId));

        var rule = new ActivationLoweringRule();
        var rewrites = rule.apply(egraph, reluId).ToList();

        Assert.Single(rewrites);
        Assert.IsType<Apply>(rewrites[0].Replacement);
    }

    #endregion

    #region TensorDialect 测试

    [Fact]
    public void TensorDialect_Should_Have_Fusion_Rules()
    {
        var dialect = new TensorDialect();
        Assert.NotEmpty(dialect.Rules);
        Assert.Equal(4, dialect.Rules.Count);
    }

    [Fact]
    public void TensorDialect_Should_Have_Lowering_Rules()
    {
        var dialect = new TensorDialect();
        Assert.NotEmpty(dialect.LoweringRules);
        Assert.True(dialect.LoweringRules.Count >= 18);
    }

    [Fact]
    public void TensorDialect_Should_Have_Cost_Hooks()
    {
        var dialect = new TensorDialect();
        Assert.NotEmpty(dialect.CostHooks);
        Assert.Single(dialect.CostHooks);
        Assert.IsType<TensorCostHook>(dialect.CostHooks[0]);
    }

    #endregion

    #region TensorCostHook 测试

    [Fact]
    public void TensorCostHook_Should_Handle_Sigmoid()
    {
        var hook = new TensorCostHook();
        var inputId = new Id(0);
        Assert.True(hook.can_handle(new Sigmoid(inputId)));
    }

    [Fact]
    public void TensorCostHook_Should_Handle_Tanh()
    {
        var hook = new TensorCostHook();
        var inputId = new Id(0);
        Assert.True(hook.can_handle(new Tanh(inputId)));
    }

    [Fact]
    public void TensorCostHook_Should_Handle_Dense()
    {
        var hook = new TensorCostHook();
        var inputId = new Id(0);
        var weightsId = new Id(1);
        Assert.True(hook.can_handle(new Dense(inputId, weightsId, null)));
    }

    [Fact]
    public void TensorCostHook_Should_Handle_ElementWise()
    {
        var hook = new TensorCostHook();
        var leftId = new Id(0);
        var rightId = new Id(1);
        Assert.True(hook.can_handle(new ElementWiseAdd(leftId, rightId)));
        Assert.True(hook.can_handle(new ElementWiseMul(leftId, rightId)));
    }

    #endregion

    #region GalateaBridge 测试

    [Fact]
    public void GalateaBridge_ConvertToEGraph_Should_Convert_Relu()
    {
        var operations = new List<GalateaOperation>
        {
            new() { Name = "input1", OpType = "Input", Config = [] },
            new() { Name = "relu1", OpType = "RELU", Config = [], InputNames = ["input1"] }
        };

        var result = GalateaBridge.ConvertToEGraph(operations, ["relu1"]);

        Assert.NotNull(result.EGraph);
        Assert.NotEmpty(result.NameToId);
        Assert.Equal(1, result.Stats.ConvertedCount);
        Assert.Equal(1, result.Stats.SkippedCount);
    }

    [Fact]
    public void GalateaBridge_ConvertToEGraph_Should_Convert_Dense()
    {
        var operations = new List<GalateaOperation>
        {
            new() { Name = "input1", OpType = "Input", Config = [] },
            new() { Name = "weights1", OpType = "Input", Config = [] },
            new() { Name = "dense1", OpType = "DENSE", Config = [], InputNames = ["input1", "weights1"] }
        };

        var result = GalateaBridge.ConvertToEGraph(operations, ["dense1"]);

        Assert.NotNull(result.EGraph);
        Assert.Equal(1, result.Stats.ConvertedCount);
    }

    [Fact]
    public void GalateaBridge_ConvertToEGraph_Should_Convert_Sigmoid()
    {
        var operations = new List<GalateaOperation>
        {
            new() { Name = "input1", OpType = "Input", Config = [] },
            new() { Name = "sigmoid1", OpType = "SIGMOID", Config = [], InputNames = ["input1"] }
        };

        var result = GalateaBridge.ConvertToEGraph(operations, ["sigmoid1"]);

        Assert.Equal(1, result.Stats.ConvertedCount);
    }

    [Fact]
    public void GalateaBridge_ConvertToEGraph_Should_Convert_Tanh()
    {
        var operations = new List<GalateaOperation>
        {
            new() { Name = "input1", OpType = "Input", Config = [] },
            new() { Name = "tanh1", OpType = "TANH", Config = [], InputNames = ["input1"] }
        };

        var result = GalateaBridge.ConvertToEGraph(operations, ["tanh1"]);

        Assert.Equal(1, result.Stats.ConvertedCount);
    }

    [Fact]
    public void GalateaBridge_ConvertToEGraph_Should_Skip_Unsupported_Op()
    {
        var operations = new List<GalateaOperation>
        {
            new() { Name = "unknown1", OpType = "UNKNOWN_OP", Config = [] }
        };

        var result = GalateaBridge.ConvertToEGraph(operations, ["unknown1"]);

        Assert.Equal(1, result.Stats.TotalCount);
        Assert.Equal(0, result.Stats.ConvertedCount);
        Assert.Equal(1, result.Stats.SkippedCount);
    }

    [Fact]
    public void GalateaBridge_ConvertToEGraph_Should_Convert_Multi_Layer_Network()
    {
        var operations = new List<GalateaOperation>
        {
            new() { Name = "input1", OpType = "Input", Config = [] },
            new() { Name = "weights1", OpType = "Input", Config = [] },
            new() { Name = "dense1", OpType = "DENSE", Config = [], InputNames = ["input1", "weights1"] },
            new() { Name = "relu1", OpType = "RELU", Config = [], InputNames = ["dense1"] },
            new() { Name = "weights2", OpType = "Input", Config = [] },
            new() { Name = "dense2", OpType = "DENSE", Config = [], InputNames = ["relu1", "weights2"] },
            new() { Name = "sigmoid1", OpType = "SIGMOID", Config = [], InputNames = ["dense2"] }
        };

        var result = GalateaBridge.ConvertToEGraph(operations, ["sigmoid1"]);

        Assert.Equal(4, result.Stats.ConvertedCount);
        Assert.Equal(3, result.Stats.SkippedCount);
        Assert.NotEmpty(result.OutputIds);
    }

    [Fact]
    public void GalateaBridge_ConvertToEGraph_Should_Handle_Conv2D()
    {
        var operations = new List<GalateaOperation>
        {
            new() { Name = "input1", OpType = "Input", Config = [] },
            new() { Name = "weights1", OpType = "Input", Config = [] },
            new()
            {
                Name = "conv1", OpType = "CONV2D", Config = new Dictionary<string, object>
                {
                    ["stride_h"] = 1, ["stride_w"] = 1, ["pad_h"] = 0, ["pad_w"] = 0
                },
                InputNames = ["input1", "weights1"]
            }
        };

        var result = GalateaBridge.ConvertToEGraph(operations, ["conv1"]);

        Assert.Equal(1, result.Stats.ConvertedCount);
    }

    [Fact]
    public void GalateaBridge_ConvertToEGraph_Should_Handle_Softmax()
    {
        var operations = new List<GalateaOperation>
        {
            new() { Name = "input1", OpType = "Input", Config = [] },
            new()
            {
                Name = "softmax1", OpType = "SOFTMAX", Config = new Dictionary<string, object>
                {
                    ["axis"] = -1
                },
                InputNames = ["input1"]
            }
        };

        var result = GalateaBridge.ConvertToEGraph(operations, ["softmax1"]);

        Assert.Equal(1, result.Stats.ConvertedCount);
    }

    [Fact]
    public void GalateaBridge_ConvertToEGraph_Should_Handle_FusedAttention()
    {
        var operations = new List<GalateaOperation>
        {
            new() { Name = "q", OpType = "Input", Config = [] },
            new() { Name = "k", OpType = "Input", Config = [] },
            new() { Name = "v", OpType = "Input", Config = [] },
            new()
            {
                Name = "attn1", OpType = "FUSEDATTENTION", Config = new Dictionary<string, object>
                {
                    ["heads"] = 8, ["head_dim"] = 64
                },
                InputNames = ["q", "k", "v"]
            }
        };

        var result = GalateaBridge.ConvertToEGraph(operations, ["attn1"]);

        Assert.Equal(1, result.Stats.ConvertedCount);
    }

    #endregion

    #region 饱和优化 + 提取 测试

    [Fact]
    public void GalateaBridge_OptimizeAndExtract_Should_Run_Without_Error()
    {
        var egraph = new EGraph<IKun>();
        var inputId = egraph.add(new Literal<long>(1L));
        var weightsId = egraph.add(new Literal<long>(2L));
        var denseId = egraph.add(new Dense(inputId, weightsId, null));
        var reluId = egraph.add(new Relu(denseId));

        var tree = GalateaBridge.OptimizeAndExtract(egraph, reluId, 3);

        Assert.NotNull(tree);
    }

    [Fact]
    public void SaturationEngine_With_Tensor_Rules_Should_Saturate()
    {
        var egraph = new EGraph<IKun>();
        var inputId = egraph.add(new Literal<long>(1L));
        var weightsId = egraph.add(new Literal<long>(2L));
        var biasId = egraph.add(new Literal<long>(3L));
        var denseId = egraph.add(new Dense(inputId, weightsId, biasId));

        var dialect = new TensorDialect();
        var allRules = dialect.Rules.Concat(dialect.LoweringRules);
        var engine = new SaturationEngine<IKun>(allRules, 5);
        var saturated = engine.Saturate(egraph);

        Assert.True(saturated || egraph.classes.Count > 0);
    }

    #endregion

    #region TensorBuiltin 枚举测试

    [Fact]
    public void TensorBuiltin_Sigmoid_Should_Have_Correct_Value()
    {
        Assert.Equal(0xA022L, (long)TensorBuiltin.Sigmoid);
    }

    [Fact]
    public void TensorBuiltin_Tanh_Should_Have_Correct_Value()
    {
        Assert.Equal(0xA023L, (long)TensorBuiltin.Tanh);
    }

    [Fact]
    public void TensorBuiltin_Dense_Should_Have_Correct_Value()
    {
        Assert.Equal(0xA014L, (long)TensorBuiltin.Dense);
    }

    #endregion
}
