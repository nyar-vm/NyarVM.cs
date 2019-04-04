using Oak.Verse.Formatter;

namespace Oak.Verse.Tests;

public class VerseFormatterRoundTripTests : VerseTestBase
{
    private void AssertRoundTrip(string source)
    {
        var unit = ParseWithTimeout(source);
        var formatted = VerseFormatter.Format(unit);
        var reparsed = ParseWithTimeout(formatted);

        Assert.Equal(unit.Declarations.Count, reparsed.Declarations.Count);
        var originalText = VerseFormatter.Format(unit);
        var reparsedText = VerseFormatter.Format(reparsed);
        Assert.Equal(originalText, reparsedText);
    }

    [Fact]
    public void Format_EmptySource_ShouldRoundTrip()
    {
        AssertRoundTrip("");
    }

    [Fact]
    public void Format_SceneDecl_ShouldRoundTrip()
    {
        AssertRoundTrip("scene intro {\r\n\r\n}");
    }

    [Fact]
    public void Format_LabelDecl_ShouldRoundTrip()
    {
        AssertRoundTrip("*start\r\n");
    }

    [Fact]
    public void Format_DialogueLine_ShouldRoundTrip()
    {
        AssertRoundTrip("\u59d3\u540d\u4e0d\u8be6: \"\u4f60\u597d\"\r\n");
    }

    [Fact]
    public void Format_NarrationLine_ShouldRoundTrip()
    {
        AssertRoundTrip("\"\u8fd9\u662f\u4e00\u6bb5\u65c1\u767d\"\r\n");
    }

    [Fact]
    public void Format_JumpStmt_ShouldRoundTrip()
    {
        AssertRoundTrip("scene test {\r\n    jump target\r\n}");
    }

    [Fact]
    public void Format_CallStmt_ShouldRoundTrip()
    {
        AssertRoundTrip("scene test {\r\n    call label\r\n}");
    }

    [Fact]
    public void Format_ReturnStmt_ShouldRoundTrip()
    {
        AssertRoundTrip("scene test {\r\n    return\r\n}");
    }

    [Fact]
    public void Format_PauseStmt_ShouldRoundTrip()
    {
        AssertRoundTrip("scene test {\r\n    pause\r\n}");
    }

    [Fact]
    public void Format_WaitStmt_ShouldRoundTrip()
    {
        AssertRoundTrip("scene test {\r\n    wait 1.5\r\n}");
    }

    [Fact]
    public void Format_SetStmt_ShouldRoundTrip()
    {
        AssertRoundTrip("scene test {\r\n    set score = 100\r\n}");
    }

    [Fact]
    public void Format_IfStmt_ShouldRoundTrip()
    {
        var source = "scene test {\r\n    if score > 50 {\r\n        jump win\r\n    }\r\n}";
        AssertRoundTrip(source);
    }

    [Fact]
    public void Format_CommandCall_ShouldRoundTrip()
    {
        AssertRoundTrip("!bgm play\r\n");
    }

    [Fact]
    public void Format_FullScene_ShouldRoundTrip()
    {
        var source = "scene chapter1 {\r\n    \"\u7b2c\u4e00\u7ae0\u5f00\u59cb\"\r\n    \u59d3\u540d\u4e0d\u8be6: \"\u4f60\u597d\"\r\n    set score = 0\r\n    if score == 0 {\r\n        jump fail\r\n    }\r\n}";
        AssertRoundTrip(source);
    }

    [Fact]
    public void Format_Menu_ShouldRoundTrip()
    {
        var source = "scene test {\r\n    menu {\r\n        \"\u9009\u9879A\" {\r\n            jump sceneA\r\n        }\r\n        \"\u9009\u9879B\" {\r\n            jump sceneB\r\n        }\r\n    }\r\n}";
        AssertRoundTrip(source);
    }
}
