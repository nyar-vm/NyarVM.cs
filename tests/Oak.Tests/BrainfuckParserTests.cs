using Oak.Testing;

namespace Oak.Brainfuck.Tests;

public class BrainfuckParserTests : TestBase
{
    
/// <summary>
    
///     测试基本自增指令：+++ 应解析为三个 BfCmdAdd(1)
    

/// </summary>
    [Fact]
    public void Parse_BasicIncrement_ShouldReturnThreeAddCommands()
    {
        var parser = new BrainfuckParser("+++");
        var commands = ExecuteWithTimeout(() => parser.Parse(), "解析自增指令");

        Assert.Equal(3, commands.Count);
        Assert.All(commands, cmd => Assert.IsType<BfCmdAdd>(cmd));
        Assert.All(commands, cmd => Assert.Equal(1, ((BfCmdAdd)cmd).Value));
    }

    
/// <summary>
    
///     测试基本自减指令：--- 应解析为三个 BfCmdAdd(-1)
    

/// </summary>
    [Fact]
    public void Parse_BasicDecrement_ShouldReturnThreeSubCommands()
    {
        var parser = new BrainfuckParser("---");
        var commands = ExecuteWithTimeout(() => parser.Parse(), "解析自减指令");

        Assert.Equal(3, commands.Count);
        Assert.All(commands, cmd => Assert.IsType<BfCmdAdd>(cmd));
        Assert.All(commands, cmd => Assert.Equal(-1, ((BfCmdAdd)cmd).Value));
    }

    
/// <summary>
    
///     测试指针移动指令：>< 应解析为 BfCmdMove(1) 和 BfCmdMove(-1)
    

/// </summary>
    [Fact]
    public void Parse_PointerMoves_ShouldReturnMoveCommands()
    {
        var parser = new BrainfuckParser("><");
        var commands = ExecuteWithTimeout(() => parser.Parse(), "解析指针移动");

        Assert.Equal(2, commands.Count);

        var first = Assert.IsType<BfCmdMove>(commands[0]);
        Assert.Equal(1, first.Offset);

        var second = Assert.IsType<BfCmdMove>(commands[1]);
        Assert.Equal(-1, second.Offset);
    }

    
/// <summary>
    
///     测试输出和输入指令：., 应解析为 BfCmdOutput 和 BfCmdInput
    

/// </summary>
    [Fact]
    public void Parse_OutputAndInput_ShouldReturnOutputAndInputCommands()
    {
        var parser = new BrainfuckParser(".,");
        var commands = ExecuteWithTimeout(() => parser.Parse(), "解析输入输出");

        Assert.Equal(2, commands.Count);
        Assert.IsType<BfCmdOutput>(commands[0]);
        Assert.IsType<BfCmdInput>(commands[1]);
    }

    
/// <summary>
    
///     测试简单循环：[+] 应解析为一个 BfCmdLoop，内含一个 BfCmdAdd(1)
    

/// </summary>
    [Fact]
    public void Parse_SimpleLoop_ShouldReturnLoopWithAdd()
    {
        var parser = new BrainfuckParser("[+]");
        var commands = ExecuteWithTimeout(() => parser.Parse(), "解析简单循环");

        Assert.Single(commands);
        var loop = Assert.IsType<BfCmdLoop>(commands[0]);
        Assert.Single(loop.Body);
        var add = Assert.IsType<BfCmdAdd>(loop.Body[0]);
        Assert.Equal(1, add.Value);
    }

    
/// <summary>
    
///     测试嵌套循环：[>+[<-]] 应正确解析嵌套循环结构
    

/// </summary>
    [Fact]
    public void Parse_NestedLoop_ShouldReturnCorrectNestedStructure()
    {
        var parser = new BrainfuckParser("[>+[<-]]");
        var commands = ExecuteWithTimeout(() => parser.Parse(), "解析嵌套循环");

        Assert.Single(commands);
        var outer = Assert.IsType<BfCmdLoop>(commands[0]);
        Assert.Equal(3, outer.Body.Count);

        Assert.IsType<BfCmdMove>(outer.Body[0]);
        Assert.IsType<BfCmdAdd>(outer.Body[1]);

        var inner = Assert.IsType<BfCmdLoop>(outer.Body[2]);
        Assert.Equal(2, inner.Body.Count);
        Assert.IsType<BfCmdMove>(inner.Body[0]);
        Assert.IsType<BfCmdAdd>(inner.Body[1]);
    }

    
/// <summary>
    
///     测试空输入：空字符串应返回空列表
    

/// </summary>
    [Fact]
    public void Parse_EmptyInput_ShouldReturnEmptyList()
    {
        var parser = new BrainfuckParser("");
        var commands = ExecuteWithTimeout(() => parser.Parse(), "解析空输入");

        Assert.NotNull(commands);
        Assert.Empty(commands);
    }

    
/// <summary>
    
///     测试未匹配的开括号 [：验证解析不会崩溃且返回非 null 结果
    

/// </summary>
    [Fact]
    public void Parse_UnmatchedOpeningBracket_ShouldNotCrash()
    {
        var parser = new BrainfuckParser("[");
        var commands = ExecuteWithTimeout(() => parser.Parse(), "解析未匹配的开括号");

        Assert.NotNull(commands);
    }

    
/// <summary>
    
///     测试未匹配的闭括号 ]：在顶层遇到 ] 时立即终止当前块，返回空列表
    

/// </summary>
    [Fact]
    public void Parse_UnmatchedClosingBracket_ShouldReturnEmptyList()
    {
        var parser = new BrainfuckParser("]");
        var commands = ExecuteWithTimeout(() => parser.Parse(), "解析未匹配的闭括号");

        Assert.NotNull(commands);
        Assert.Empty(commands);
    }

    
/// <summary>
    
///     测试非 Brainfuck 字符会被忽略：abc 应返回空列表
    

/// </summary>
    [Fact]
    public void Parse_NonBrainfuckCharacters_ShouldBeIgnored()
    {
        var parser = new BrainfuckParser("abc");
        var commands = ExecuteWithTimeout(() => parser.Parse(), "解析非 BF 字符");

        Assert.NotNull(commands);
        Assert.Empty(commands);
    }

    
/// <summary>
    
///     测试混合指令：+>[-]<. 应正确解析所有命令
    

/// </summary>
    [Fact]
    public void Parse_MixedCommands_ShouldReturnCorrectCommands()
    {
        var parser = new BrainfuckParser("+>[-]<.");
        var commands = ExecuteWithTimeout(() => parser.Parse(), "解析混合指令");

        Assert.Equal(5, commands.Count);

        var add = Assert.IsType<BfCmdAdd>(commands[0]);
        Assert.Equal(1, add.Value);

        Assert.IsType<BfCmdMove>(commands[1]);

        var loop = Assert.IsType<BfCmdLoop>(commands[2]);
        Assert.Single(loop.Body);
        var loopAdd = Assert.IsType<BfCmdAdd>(loop.Body[0]);
        Assert.Equal(-1, loopAdd.Value);

        var moveLeft = Assert.IsType<BfCmdMove>(commands[3]);
        Assert.Equal(-1, moveLeft.Offset);

        Assert.IsType<BfCmdOutput>(commands[4]);
    }
}
