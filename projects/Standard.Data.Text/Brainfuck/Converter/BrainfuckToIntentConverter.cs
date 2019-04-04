using System.Collections.Immutable;

namespace Std.Data.Text.Brainfuck.Converter;


/// <summary>

///     将 Brainfuck 源码转换为 Nyar IR（IKun）意图节点

///     文本解析委托 Oak.Brainfuck.BrainfuckParser


/// </summary>
public sealed class BrainfuckToIntentConverter
{
    private readonly EGraph<IKun> _egraph = new();

    
/// <summary>
    
///     获取内部 EGraph
    

/// </summary>
    public EGraph<IKun> EGraph => _egraph;

    
/// <summary>
    
///     将 Brainfuck 源码字符串转换为意图图
    

/// </summary>
    public Id Convert(string source)
    {
        var parser = new BrainfuckParser(source);
        var commands = parser.Parse();
        return ConvertCommands(commands);
    }

    
/// <summary>
    
///     转换命令列表为意图节点
    

/// </summary>
    private Id ConvertCommands(IReadOnlyList<BfCommand> commands)
    {
        if (commands.Count == 0)
        {
            return _egraph.Add(new BfSeq(ImmutableArray<Id>.Empty));
        }

        var optimized = OptimizeCommands(commands);
        var children = optimized.Select(ConvertCommand).ToImmutableArray();

        if (children.Length == 1)
        {
            return children[0];
        }

        return _egraph.Add(new BfSeq(children));
    }

    
/// <summary>
    
///     转换单个命令为意图节点
    

/// </summary>
    private Id ConvertCommand(BfCommand command)
    {
        return command switch
        {
            BfCmdMove move => _egraph.Add(new BfTapeMove(move.Offset)),
            BfCmdAdd add => _egraph.Add(new BfCellAdd(add.Value)),
            BfCmdOutput => _egraph.Add(new BfOutput()),
            BfCmdInput => _egraph.Add(new BfInput()),
            BfCmdLoop loop => ConvertLoop(loop),
            _ => _egraph.Add(new BfSeq(ImmutableArray<Id>.Empty))
        };
    }

    
/// <summary>
    
///     转换循环命令
    

/// </summary>
    private Id ConvertLoop(BfCmdLoop loop)
    {
        var bodyId = ConvertCommands(loop.Body);
        return _egraph.Add(new BfLoop(ImmutableArray.Create(bodyId)));
    }

    
/// <summary>
    
///     优化命令列表：合并相邻操作
    

/// </summary>
    private IReadOnlyList<BfCommand> OptimizeCommands(IReadOnlyList<BfCommand> commands)
    {
        var result = new List<BfCommand>();
        var i = 0;

        while (i < commands.Count)
        {
            var cmd = commands[i];

            if (cmd is BfCmdMove)
            {
                var offset = 0;
                while (i < commands.Count && commands[i] is BfCmdMove move)
                {
                    offset += move.Offset;
                    i++;
                }

                if (offset != 0)
                {
                    result.Add(new BfCmdMove(offset));
                }

                continue;
            }

            if (cmd is BfCmdAdd)
            {
                var value = 0;
                while (i < commands.Count && commands[i] is BfCmdAdd add)
                {
                    value += add.Value;
                    i++;
                }

                if (value != 0)
                {
                    result.Add(new BfCmdAdd(value));
                }

                continue;
            }

            if (cmd is BfCmdLoop loop)
            {
                result.Add(new BfCmdLoop(OptimizeCommands(loop.Body).ToList()));
                i++;
                continue;
            }

            result.Add(cmd);
            i++;
        }

        return result;
    }
}

#region Brainfuck 意图节点 (IKun)


/// <summary>

///     Brainfuck 模块意图节点


/// </summary>
public sealed record BfModule(ImmutableArray<Id> Body) : IKun;


/// <summary>

///     序列意图节点


/// </summary>
public sealed record BfSeq(ImmutableArray<Id> Children) : IKun;


/// <summary>

///     磁带移动意图节点


/// </summary>
public sealed record BfTapeMove(int Offset) : IKun;


/// <summary>

///     单元格增减意图节点


/// </summary>
public sealed record BfCellAdd(int Value) : IKun;


/// <summary>

///     输出意图节点


/// </summary>
public sealed record BfOutput : IKun;


/// <summary>

///     输入意图节点


/// </summary>
public sealed record BfInput : IKun;


/// <summary>

///     循环意图节点


/// </summary>
public sealed record BfLoop(ImmutableArray<Id> Body) : IKun;

#endregion
