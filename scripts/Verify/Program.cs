using Nyar.VM.TextVM;
using Nyar.VM.TextVM.Compiler;

static void Assert(Boolean condition, String message)
{
    if (!condition)
    {
        Console.Error.WriteLine($"  FAIL: {message}");
        Environment.ExitCode = 1;
    }
    else
    {
        Console.WriteLine($"  PASS: {message}");
    }
}

static void InspectDfa(String pattern, String label)
{
    Console.WriteLine($"\n--- {label} ---");
    var (node, _) = PatternParser.Parse(pattern);
    if (node is null) { Console.WriteLine("  Parse failed"); return; }
    var folded = BooleanFolder.Fold(node, out _);
    var builder = new DFABuilder(folded, 1024);
    builder.Build();
    Console.WriteLine($"  States: {builder.StateCount}");
    Console.WriteLine(builder.ToDotFormat());
}

Console.WriteLine("TextVM DFA 管线验证");
Console.WriteLine("======================");

InspectDfa("a|b", "DFA a|b");
InspectDfa("[a-z]+", "DFA [a-z]+");

// 1. 简单交替 "a|b"
Console.WriteLine("\n1. a|b on 'xcay' -> Match(2,3)");
{
    var q = StaticQuery.Compile("a|b", TextEncoding.Utf8, TvmOperation.Find);
    var m = q.Find("xcay"u8.ToArray());
    Assert(m is not null, "match != null");
    if (m is not null)
    {
        Assert(m.Value.Start == 2 && m.Value.End == 3,
            $"expected (2,3) actual ({m.Value.Start},{m.Value.End})");
    }
}

// 2. 字符类 "[a-z]+"
Console.WriteLine("\n2. [a-z]+ on 'hello 123 world' -> Match(0,5)");
{
    var q = StaticQuery.Compile("[a-z]+", TextEncoding.Utf8, TvmOperation.Find);
    var m = q.Find("hello 123 world"u8.ToArray());
    Assert(m is not null, "match != null");
    if (m is not null)
    {
        Assert(m.Value.Start == 0 && m.Value.End == 5,
            $"expected (0,5) actual ({m.Value.Start},{m.Value.End})");
    }
}

// 3. 数字序列 "[0-9]+"
Console.WriteLine("\n3. [0-9]+ on 'a1b23c456' -> FindAll 3 matches");
{
    var q = StaticQuery.Compile("[0-9]+", TextEncoding.Utf8, TvmOperation.Find);
    var matches = q.FindAll("a1b23c456"u8.ToArray()).ToArray();
    Assert(matches.Length == 3, $"expected 3 actual {matches.Length}");
    if (matches.Length == 3)
    {
        Assert(matches[0].Start == 1 && matches[0].End == 2, "match 0 at (1,2)");
        Assert(matches[1].Start == 3 && matches[1].End == 5, "match 1 at (3,5)");
        Assert(matches[2].Start == 6 && matches[2].End == 9, "match 2 at (6,9)");
    }
}

// 4. 无匹配
Console.WriteLine("\n4. abc on 'xyz' -> null");
{
    var q = StaticQuery.Compile("abc", TextEncoding.Utf8, TvmOperation.Find);
    var m = q.Find("xyz"u8.ToArray());
    Assert(m is null, "no match");
}

// 5. 替换
Console.WriteLine("\n5. [0-9]+ replace -> 'price: NUM and NUM'");
{
    var q = StaticQuery.Compile("[0-9]+", TextEncoding.Utf8, TvmOperation.Replace);
    var result = q.Replace("price: 123 and 456"u8.ToArray(), "NUM"u8.ToArray());
    var resultStr = System.Text.Encoding.UTF8.GetString(result);
    Assert(resultStr == "price: NUM and NUM",
        $"expected 'price: NUM and NUM' got '{resultStr}'");
}

// 6. IsMatch
Console.WriteLine("\n6. hello exists");
{
    var q = StaticQuery.Compile("hello", TextEncoding.Utf8, TvmOperation.Exists);
    Assert(q.Exists("hello world"u8.ToArray()), "'hello world' -> true");
    Assert(q.Exists("world hello"u8.ToArray()), "'world hello' -> true");
    Assert(!q.Exists("xyz"u8.ToArray()), "'xyz' -> false");
}

Console.WriteLine($"\n======================");
Console.WriteLine(Environment.ExitCode == 0 ? "ALL PASS" : "SOME FAILED");
