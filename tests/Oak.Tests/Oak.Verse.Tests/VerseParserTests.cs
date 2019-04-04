namespace Oak.Verse.Tests;

public class VerseParserTests : VerseTestBase
{
    [Fact]
    public void Parse_EmptySource_ShouldReturnEmptyCompilationUnit()
    {
        var result = ParseWithTimeout("");

        Assert.NotNull(result);
        Assert.Empty(result.Declarations);
    }

    [Fact]
    public void Parse_SceneDecl_ShouldReturnSceneDecl()
    {
        var result = ParseWithTimeout("scene intro { }");

        Assert.Single(result.Declarations);
        var decl = Assert.IsType<SceneDecl>(result.Declarations[0]);
        Assert.Equal("intro", decl.Name);
    }

    [Fact]
    public void Parse_LabelDecl_ShouldReturnLabelDecl()
    {
        var result = ParseWithTimeout("*start");

        Assert.Single(result.Declarations);
        var decl = Assert.IsType<LabelDecl>(result.Declarations[0]);
        Assert.Equal("start", decl.Name);
    }

    [Fact]
    public void Parse_JumpStmt_ShouldReturnJumpStmt()
    {
        var result = ParseWithTimeout("scene test { jump ending }");

        var scene = Assert.IsType<SceneDecl>(result.Declarations[0]);
        Assert.NotEmpty(scene.Body);
        var jump = Assert.IsType<JumpStmt>(scene.Body[0]);
        Assert.Equal("ending", jump.Target);
    }

    [Fact]
    public void Parse_SetStmt_ShouldReturnSetStmt()
    {
        var result = ParseWithTimeout("scene test { set score = 100 }");

        var scene = Assert.IsType<SceneDecl>(result.Declarations[0]);
        Assert.NotEmpty(scene.Body);
        var set = Assert.IsType<SetStmt>(scene.Body[0]);
        Assert.Equal("score", set.VariableName);
    }

    [Fact]
    public void Parse_ReturnStmt_ShouldReturnReturnStmt()
    {
        var result = ParseWithTimeout("scene test { return }");

        var scene = Assert.IsType<SceneDecl>(result.Declarations[0]);
        Assert.NotEmpty(scene.Body);
        Assert.IsType<ReturnStmt>(scene.Body[0]);
    }

    [Fact]
    public void Parse_PauseStmt_ShouldReturnPauseStmt()
    {
        var result = ParseWithTimeout("scene test { pause }");

        var scene = Assert.IsType<SceneDecl>(result.Declarations[0]);
        Assert.NotEmpty(scene.Body);
        Assert.IsType<PauseStmt>(scene.Body[0]);
    }

    [Fact]
    public void Parse_WaitStmt_ShouldReturnWaitStmt()
    {
        var result = ParseWithTimeout("scene test { wait 2.0 }");

        var scene = Assert.IsType<SceneDecl>(result.Declarations[0]);
        Assert.NotEmpty(scene.Body);
        Assert.IsType<WaitStmt>(scene.Body[0]);
    }

    [Fact]
    public void Parse_CallStmt_ShouldReturnCallStmt()
    {
        var result = ParseWithTimeout("scene test { call helper }");

        var scene = Assert.IsType<SceneDecl>(result.Declarations[0]);
        Assert.NotEmpty(scene.Body);
        var call = Assert.IsType<CallStmt>(scene.Body[0]);
        Assert.Equal("helper", call.Target);
    }

    [Fact]
    public void Parse_CommandCall_ShouldParseWithoutError()
    {
        var result = ParseWithTimeout("scene test { @bg forest }");

        var scene = Assert.IsType<SceneDecl>(result.Declarations[0]);
        Assert.NotEmpty(scene.Body);
    }

    [Fact]
    public void Parse_IfStmt_ShouldReturnIfStmt()
    {
        var source = @"
            scene test {
                if score > 50 {
                    jump win
                } endif
            }";
        var result = ParseWithTimeout(source);

        var scene = Assert.IsType<SceneDecl>(result.Declarations[0]);
        Assert.NotEmpty(scene.Body);
        var ifStmt = Assert.IsType<IfStmt>(scene.Body[0]);
        Assert.NotNull(ifStmt.Condition);
        Assert.NotEmpty(ifStmt.ThenBody);
    }

    [Fact]
    public void Parse_FullStoryScene_ShouldParseAllElements()
    {
        var source = @"
            scene chapter1 {
                @bg school_corridor
                set affection = affection + 1
                if affection > 5 {
                    jump good_ending
                } elif affection > 2 {
                    jump normal_ending
                } else {
                    jump bad_ending
                } endif
            }";
        var result = ParseWithTimeout(source);

        var scene = Assert.IsType<SceneDecl>(result.Declarations[0]);
        Assert.Equal("chapter1", scene.Name);
        Assert.NotEmpty(scene.Body);
    }
}
