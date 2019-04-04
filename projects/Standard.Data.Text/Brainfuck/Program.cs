using Std.Data.Text.Brainfuck.Converter;

namespace Std.Data.Text.Brainfuck;


/// <summary>

///     Brainfuck 方言示例入口


/// </summary>
internal static class Program
{
    
/// <summary>
    
///     程序入口点
    

/// </summary>
    public static void Main()
    {
        System.Console.WriteLine("╔══════════════════════════════════════════╗");
        System.Console.WriteLine("║     🔮 Nyar.Brainfuck 意图转换示例       ║");
        System.Console.WriteLine("╚══════════════════════════════════════════╝");

        RunIncrementTest();
        RunLoopTest();
        RunHelloWorldTest();
        RunAddTest();
    }

    
/// <summary>
    
///     简单递增测试：+++
    

/// </summary>
    private static void RunIncrementTest()
    {
        System.Console.WriteLine("\n┌──────────────────────────────────────────┐");
        System.Console.WriteLine("│ M0: 简单递增意图转换                      │");
        System.Console.WriteLine("└──────────────────────────────────────────┘");

        var source = "+++";
        System.Console.WriteLine($"源码: {source}");

        var (egraph, rootId) = Convert(source);
        PrintIntent(egraph, rootId, 0);
    }

    
/// <summary>
    
///     循环测试：[-]
    

/// </summary>
    private static void RunLoopTest()
    {
        System.Console.WriteLine("\n┌──────────────────────────────────────────┐");
        System.Console.WriteLine("│ M1: 清零循环意图转换                      │");
        System.Console.WriteLine("└──────────────────────────────────────────┘");

        var source = "[-]";
        System.Console.WriteLine($"源码: {source}");

        var (egraph, rootId) = Convert(source);
        PrintIntent(egraph, rootId, 0);
    }

    
/// <summary>
    
///     Hello World 测试
    

/// </summary>
    private static void RunHelloWorldTest()
    {
        System.Console.WriteLine("\n┌──────────────────────────────────────────┐");
        System.Console.WriteLine("│ M2: Hello World 意图转换                  │");
        System.Console.WriteLine("└──────────────────────────────────────────┘");

        var source = "++++++++[>++++[>++>+++>+++>+<<<<-]>+>+>->>+[<]<-]>>.>---.+++++++..+++.>>.<-.<.+++.------.--------.>>+.>++.";
        System.Console.WriteLine($"源码: {source[..50]}...");

        var (egraph, rootId) = Convert(source);
        PrintIntent(egraph, rootId, 0);

        System.Console.WriteLine($"\n  EGraph 等价类数量: {egraph.Classes.Count}");
    }

    
/// <summary>
    
///     加法测试：将两个单元相加
    

/// </summary>
    private static void RunAddTest()
    {
        System.Console.WriteLine("\n┌──────────────────────────────────────────┐");
        System.Console.WriteLine("│ M3: 加法程序意图转换                      │");
        System.Console.WriteLine("└──────────────────────────────────────────┘");

        var source = ">++<+++[->+<]";
        System.Console.WriteLine($"源码: {source}");
        System.Console.WriteLine("语义: 单元0 += 单元1 (3 + 2 = 5)");

        var (egraph, rootId) = Convert(source);
        PrintIntent(egraph, rootId, 0);
    }

    
/// <summary>
    
///     转换 Brainfuck 源码为意图图
    

/// </summary>
    private static (EGraph<IKun> EGraph, Id RootId) Convert(string source)
    {
        var converter = new BrainfuckToIntentConverter();
        var rootId = converter.Convert(source);
        return (converter.EGraph, rootId);
    }

    
/// <summary>
    
///     打印意图节点
    

/// </summary>
    private static void PrintIntent(EGraph<IKun> egraph, Id id, int indent)
    {
        var prefix = new string(' ', indent * 2);
        var eclass = egraph.GetClass(id);

        if (eclass is null)
        {
            System.Console.WriteLine($"{prefix}[null]");
            return;
        }

        var node = eclass.Nodes.FirstOrDefault();
        if (node is null)
        {
            System.Console.WriteLine($"{prefix}[empty]");
            return;
        }

        switch (node)
        {
            case BfTapeMove bfTapeMove:
                System.Console.WriteLine($"{prefix}🔮 BfTapeMove: offset={bfTapeMove.Offset}");
                break;

            case BfCellAdd bfCellAdd:
                System.Console.WriteLine($"{prefix}🔮 BfCellAdd: value={bfCellAdd.Value}");
                break;

            case BfOutput bfOutput:
                System.Console.WriteLine($"{prefix}🔮 BfOutput (.)");
                break;

            case BfInput bfInput:
                System.Console.WriteLine($"{prefix}🔮 BfInput (,)");
                break;

            case BfLoop bfLoop:
                System.Console.WriteLine($"{prefix}🔮 BfLoop ([...])");
                foreach (var child in bfLoop.Body)
                {
                    PrintIntent(egraph, child, indent + 1);
                }

                break;

            case BfSeq bfSeq:
                System.Console.WriteLine($"{prefix}🔮 BfSeq");
                foreach (var child in bfSeq.Children)
                {
                    PrintIntent(egraph, child, indent + 1);
                }

                break;

            case BfModule bfModule:
                System.Console.WriteLine($"{prefix}🔮 BfModule");
                foreach (var child in bfModule.Body)
                {
                    PrintIntent(egraph, child, indent + 1);
                }

                break;

            default:
                System.Console.WriteLine($"{prefix}{node.GetType().Name}");
                break;
        }
    }
}
