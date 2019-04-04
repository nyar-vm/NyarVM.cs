namespace Std.Data.Text.Brainfuck;


/// <summary>

///     Brainfuck 文本解析器，将 Brainfuck 源码解析为命令 AST


/// </summary>
public sealed class BrainfuckParser
{
    private readonly string _source;
    private int _position;

    
/// <summary>
    
///     创建 Brainfuck 解析器
    

/// </summary>
    
/// <param name="source">Brainfuck 源码。</param>
    public BrainfuckParser(string source)
    {
        _source = source;
        _position = 0;
    }

    
/// <summary>
    
///     解析 Brainfuck 源码为命令列表
    

/// </summary>
    
/// <returns>解析后的命令 AST 列表。</returns>
    public IReadOnlyList<BfCommand> Parse()
    {
        return ParseBlock();
    }

    
/// <summary>
    
///     解析代码块（到文件结束或匹配的 ]）
    

/// </summary>
    private List<BfCommand> ParseBlock()
    {
        var commands = new List<BfCommand>();

        while (_position < _source.Length)
        {
            var c = _source[_position];

            switch (c)
            {
                case '>':
                    commands.Add(new BfCmdMove(1));
                    _position++;
                    break;

                case '<':
                    commands.Add(new BfCmdMove(-1));
                    _position++;
                    break;

                case '+':
                    commands.Add(new BfCmdAdd(1));
                    _position++;
                    break;

                case '-':
                    commands.Add(new BfCmdAdd(-1));
                    _position++;
                    break;

                case '.':
                    commands.Add(new BfCmdOutput());
                    _position++;
                    break;

                case ',':
                    commands.Add(new BfCmdInput());
                    _position++;
                    break;

                case '[':
                    _position++;
                    var loopBody = ParseBlock();
                    commands.Add(new BfCmdLoop(loopBody));
                    break;

                case ']':
                    _position++;
                    return commands;

                default:
                    _position++;
                    break;
            }
        }

        return commands;
    }
}
