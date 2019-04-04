using Std.Data.Text.Python.Converter;

namespace Std.Data.Text.Python;

public static class Program
{
    public static void Main()
    {
        System.Console.WriteLine("╔══════════════════════════════════════════╗");
        System.Console.WriteLine("║     Nyar.Python 端到端管线验证           ║");
        System.Console.WriteLine("╚══════════════════════════════════════════╝");

        RunMvpTest();
        RunFullTest();
    }

    private static void RunMvpTest()
    {
        System.Console.WriteLine("\n┌──────────────────────────────────────────┐");
        System.Console.WriteLine("│ M3: Python 端到端 MVP 验证               │");
        System.Console.WriteLine("└──────────────────────────────────────────┘");

        var source = @"
def main():
    return 1 + 2
";
        System.Console.WriteLine($"\n源码: {source.Trim()}");

        var result = RunPipeline(source);
        System.Console.WriteLine($"\n✅ 执行结果: main() = {result}");

        if (result == 3)
            System.Console.WriteLine("✅ M3 验证通过: 1 + 2 = 3");
        else
            System.Console.WriteLine($"❌ M3 验证失败: 期望 3, 实际 {result}");
    }

    private static void RunFullTest()
    {
        System.Console.WriteLine("\n┌──────────────────────────────────────────┐");
        System.Console.WriteLine("│ 完整管线验证                               │");
        System.Console.WriteLine("└──────────────────────────────────────────┘");

        var source = @"
def add(a, b):
    return a + b

def main():
    return add(1, 2)
";
        System.Console.WriteLine($"\n源码:\n{source.Trim()}");

        var result = RunPipeline(source);
        System.Console.WriteLine($"\n✅ 执行结果: main() = {result}");

        if (result == 3)
            System.Console.WriteLine("✅ 完整管线验证通过: add(1, 2) = 3");
        else
            System.Console.WriteLine($"❌ 完整管线验证失败: 期望 3, 实际 {result}");
    }

    private static long RunPipeline(string source)
    {
        var lexer = new PythonLexer();
        IReadOnlyList<GreenLeafNode> tokens;
        try
        {
            tokens = lexer.Tokenize(source);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ 词法分析失败: {ex.Message}");
            return -1;
        }

        PyModule ast;
        try
        {
            var parser = new PythonParser();
            ast = (PyModule)parser.Parse(tokens);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ 语法分析失败: {ex.Message}");
            return -1;
        }

        var converter = new PythonToIntentConverter();
        var moduleId = converter.ConvertModule(ast);
        System.Console.WriteLine($"  词法分析 → 语法分析 → AST → IKun 转换完成, EGraph 等价类数量: {converter.EGraph.Classes.Count}");

        var registry = new DialectRegistry();
        registry.register(new Nyar.Dialect.Core.CoreDialect());
        registry.register_all_rules(converter.EGraph);
        System.Console.WriteLine($"  方言降级完成, EGraph 等价类数量: {converter.EGraph.Classes.Count}");

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
