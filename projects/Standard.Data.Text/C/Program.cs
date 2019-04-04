using Std.Data.Text.C.Converter;
using Std.Data.Text.C.Rewrite;

namespace Std.Data.Text.C;


/// <summary>

///     Nyar.C 示例入口


/// </summary>
public static class Program
{
    
/// <summary>
    
///     程序入口
    

/// </summary>
    public static void Main()
    {
        System.Console.WriteLine("╔══════════════════════════════════════════╗");
        System.Console.WriteLine("║       Nyar.C 端到端管线验证              ║");
        System.Console.WriteLine("╚══════════════════════════════════════════╝");

        RunMvpTest();
        RunFullTest();
    }

    
/// <summary>
    
///     MVP 测试：int main() { return 1 + 2; } → 返回 3
    

/// </summary>
    private static void RunMvpTest()
    {
        System.Console.WriteLine("\n┌──────────────────────────────────────────┐");
        System.Console.WriteLine("│ M0: C 端到端 MVP 验证                     │");
        System.Console.WriteLine("└──────────────────────────────────────────┘");

        var source = "int main() { return 1 + 2; }";
        System.Console.WriteLine($"\n源码: {source}");

        var result = RunPipeline(source);
        System.Console.WriteLine($"\n✅ 执行结果: main() = {result}");

        if (result == 3)
            System.Console.WriteLine("✅ M0 验证通过: 1 + 2 = 3");
        else
            System.Console.WriteLine($"❌ M0 验证失败: 期望 3, 实际 {result}");
    }

    
/// <summary>
    
///     完整测试：多函数 C 程序
    

/// </summary>
    private static void RunFullTest()
    {
        System.Console.WriteLine("\n┌──────────────────────────────────────────┐");
        System.Console.WriteLine("│ 完整管线验证                               │");
        System.Console.WriteLine("└──────────────────────────────────────────┘");

        var source = @"
int add(int a, int b) {
    return a + b;
}

int main() {
    return add(1, 2);
}
";
        System.Console.WriteLine($"\n源码:\n{source}");

        var result = RunPipeline(source);
        System.Console.WriteLine($"\n✅ 执行结果: main() = {result}");

        if (result == 3)
            System.Console.WriteLine("✅ 完整管线验证通过: add(1, 2) = 3");
        else
            System.Console.WriteLine($"❌ 完整管线验证失败: 期望 3, 实际 {result}");
    }

    
/// <summary>
    
///     运行完整的 C → AST → IKun → EGraph → 提取 → 执行 管线
    

/// </summary>
    private static long RunPipeline(string source)
    {
        var pipeline = new CPipeline();
        var ast = pipeline.Parse(source);

        var converter = new CToIntentConverter();
        var moduleId = converter.ConvertTranslationUnit((CTranslationUnit)ast);

        System.Console.WriteLine($"  AST → IKun 转换完成, EGraph 等价类数量: {converter.EGraph.Classes.Count}");

        var saturationEngine = new SaturationEngine<Oa>(CAlgebraicRules.AllRules, 5);
        var saturationResult = saturationEngine.Run(converter.EGraph);
        System.Console.WriteLine(
            $"  EGraph 饱和优化: {saturationResult.Iterations} 轮, {saturationResult.TotalUnions} 次合并, 饱和={saturationResult.IsSaturated}");

        var costModel = new SimpleCostModel();
        var extractor = new Extractor(converter.EGraph, costModel);
        var tree = extractor.Extract(moduleId);

        var interpreter = new IKunInterpreter();
        if (tree is not Oa.Module moduleTree)
        {
            System.Console.WriteLine($"  ❌ 提取结果不是 Module 节点: {tree.GetType().Name}");
            return -1;
        }

        var result = interpreter.ExecuteModule(moduleTree);
        return result;
    }

    
/// <summary>
    
///     简单成本模型：常量最便宜，运算次之，函数调用最贵
    

/// </summary>
    private sealed class SimpleCostModel : ICostModel
    {
        public CostVector node_cost(Oa node)
        {
            var cost = node switch
            {
                Literal<long> or Literal<double> or Literal<string> or Literal<bool> or Literal<object?> => 1,
                Sym => 1,
                Add or Sub or Mul or Div or Rem or Cmp => 2,
                Neg or Not => 2,
                Lambda => 3,
                Apply => 5,
                Choice => 3,
                Oa.Seq => 2,
                StateUp => 2,
                Ret => 1,
                Export => 1,
                Mod => 1,
                _ => 10
            };
            return CostVector.FromLatency(cost);
        }

        public int compare(CostVector a, CostVector b)
        {
            return a.CompareTo(b);
        }
    }
}
