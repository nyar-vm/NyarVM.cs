using Oak.Diagnostics;
using Xunit;

namespace Oak.DejaVu.Tests;

public sealed class DejaVuAdvancedTests
{
    #region 深度嵌套（长继承）

    [Fact]
    public void DeepNesting_IfInIfInIf_ShouldParse()
    {
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template = "<% if a %><% if b %><% if c %>deep<% end %><% end %><% end %>";

        var result = parser.Parse(template);

        var ifA = result.Nodes[0] as DejaVuIfNode;
        Assert.NotNull(ifA);
        Assert.Equal("a", ifA.Condition);

        var ifB = ifA.Children[0] as DejaVuIfNode;
        Assert.NotNull(ifB);
        Assert.Equal("b", ifB.Condition);

        var ifC = ifB.Children[0] as DejaVuIfNode;
        Assert.NotNull(ifC);
        Assert.Equal("c", ifC.Condition);

        var text = ifC.Children[0] as DejaVuTextNode;
        Assert.NotNull(text);
        Assert.Equal("deep", text.Text);
    }

    [Fact]
    public void DeepNesting_LoopInLoopInLoop_ShouldParse()
    {
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template = "<% loop a %><% loop b %><% loop c %>deep<% end %><% end %><% end %>";

        var result = parser.Parse(template);

        var loopA = result.Nodes[0] as DejaVuLoopNode;
        Assert.NotNull(loopA);
        Assert.Equal("a", loopA.Expression);

        var loopB = loopA.Children[0] as DejaVuLoopNode;
        Assert.NotNull(loopB);
        Assert.Equal("b", loopB.Expression);

        var loopC = loopB.Children[0] as DejaVuLoopNode;
        Assert.NotNull(loopC);
        Assert.Equal("c", loopC.Expression);

        var text = loopC.Children[0] as DejaVuTextNode;
        Assert.NotNull(text);
        Assert.Equal("deep", text.Text);
    }

    [Fact]
    public void DeepNesting_FiveLevels_ShouldParse()
    {
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template =
            "<% if a %><% loop b %><% if c %><% loop d %><% if e %>deepest<% end %><% end %><% end %><% end %><% end %>";

        var result = parser.Parse(template);

        Assert.Single(result.Nodes);
        var ifA = result.Nodes[0] as DejaVuIfNode;
        Assert.NotNull(ifA);
        Assert.Equal("a", ifA.Condition);

        var loopB = ifA.Children[0] as DejaVuLoopNode;
        Assert.NotNull(loopB);

        var ifC = loopB.Children[0] as DejaVuIfNode;
        Assert.NotNull(ifC);

        var loopD = ifC.Children[0] as DejaVuLoopNode;
        Assert.NotNull(loopD);

        var ifE = loopD.Children[0] as DejaVuIfNode;
        Assert.NotNull(ifE);
        Assert.Equal("e", ifE.Condition);

        var text = ifE.Children[0] as DejaVuTextNode;
        Assert.NotNull(text);
        Assert.Equal("deepest", text.Text);
    }

    #endregion

    #region 多重嵌套（if/loop/match 交叉）

    [Fact]
    public void CrossNesting_IfInLoopInMatch_ShouldParse()
    {
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template =
            "<% match value %><% loop items %><% if active %>content<% end %><% end %><% end %>";

        var result = parser.Parse(template);

        var match = result.Nodes[0] as DejaVuMatchNode;
        Assert.NotNull(match);
        Assert.Equal("value", match.Expression);

        var loop = match.Children[0] as DejaVuLoopNode;
        Assert.NotNull(loop);
        Assert.Equal("items", loop.Expression);

        var ifNode = loop.Children[0] as DejaVuIfNode;
        Assert.NotNull(ifNode);
        Assert.Equal("active", ifNode.Condition);

        var text = ifNode.Children[0] as DejaVuTextNode;
        Assert.NotNull(text);
        Assert.Equal("content", text.Text);
    }

    [Fact]
    public void CrossNesting_MatchInIfInLoop_ShouldParse()
    {
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template =
            "<% loop items %><% if active %><% match type %>content<% end %><% end %><% end %>";

        var result = parser.Parse(template);

        var loop = result.Nodes[0] as DejaVuLoopNode;
        Assert.NotNull(loop);

        var ifNode = loop.Children[0] as DejaVuIfNode;
        Assert.NotNull(ifNode);

        var match = ifNode.Children[0] as DejaVuMatchNode;
        Assert.NotNull(match);
        Assert.Equal("type", match.Expression);

        var text = match.Children[0] as DejaVuTextNode;
        Assert.NotNull(text);
        Assert.Equal("content", text.Text);
    }

    [Fact]
    public void CrossNesting_BlockInIfInLoopInMatch_ShouldParse()
    {
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template =
            "<% match value %><% loop items %><% if active %><% block content %>inner<% end %><% end %><% end %><% end %>";

        var result = parser.Parse(template);

        var match = result.Nodes[0] as DejaVuMatchNode;
        Assert.NotNull(match);

        var loop = match.Children[0] as DejaVuLoopNode;
        Assert.NotNull(loop);

        var ifNode = loop.Children[0] as DejaVuIfNode;
        Assert.NotNull(ifNode);

        var block = ifNode.Children[0] as DejaVuBlockNode;
        Assert.NotNull(block);
        Assert.Equal("content", block.Name);

        var text = block.Children[0] as DejaVuTextNode;
        Assert.NotNull(text);
        Assert.Equal("inner", text.Text);
    }

    #endregion

    #region 菱形嵌套（并行分支各自嵌套）

    [Fact]
    public void DiamondNesting_IfElse_BothBranchesNested_ShouldParse()
    {
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template =
            "<% if mode %><% loop a %>A<% end %><% else %><% loop b %>B<% end %><% end %>";

        var result = parser.Parse(template);

        var ifNode = result.Nodes[0] as DejaVuIfNode;
        Assert.NotNull(ifNode);
        Assert.Equal("mode", ifNode.Condition);

        var thenLoop = ifNode.Children[0] as DejaVuLoopNode;
        Assert.NotNull(thenLoop);
        Assert.Equal("a", thenLoop.Expression);

        var elseLoop = ifNode.ElseChildren[0] as DejaVuLoopNode;
        Assert.NotNull(elseLoop);
        Assert.Equal("b", elseLoop.Expression);
    }

    [Fact]
    public void DiamondNesting_IfElseIfElse_AllBranchesNested_ShouldParse()
    {
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template =
            "<% if x %><% loop a %>A<% end %><% else if y %><% loop b %>B<% end %><% else %><% loop c %>C<% end %><% end %>";

        var result = parser.Parse(template);

        var ifNode = result.Nodes[0] as DejaVuIfNode;
        Assert.NotNull(ifNode);

        var thenLoop = ifNode.Children[0] as DejaVuLoopNode;
        Assert.NotNull(thenLoop);
        Assert.Equal("a", thenLoop.Expression);

        Assert.Single(ifNode.ElseIfNodes);
        var elseIfLoop = ifNode.ElseIfNodes[0].Children[0] as DejaVuLoopNode;
        Assert.NotNull(elseIfLoop);
        Assert.Equal("b", elseIfLoop.Expression);

        var elseLoop = ifNode.ElseChildren[0] as DejaVuLoopNode;
        Assert.NotNull(elseLoop);
        Assert.Equal("c", elseLoop.Expression);
    }

    [Fact]
    public void DiamondNesting_ParallelIfsInLoop_ShouldParse()
    {
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template =
            "<% loop items %><% if a %>A<% end %><% if b %>B<% end %><% end %>";

        var result = parser.Parse(template);

        var loop = result.Nodes[0] as DejaVuLoopNode;
        Assert.NotNull(loop);
        Assert.Equal(2, loop.Children.Count);

        var ifA = loop.Children[0] as DejaVuIfNode;
        Assert.NotNull(ifA);
        Assert.Equal("a", ifA.Condition);

        var ifB = loop.Children[1] as DejaVuIfNode;
        Assert.NotNull(ifB);
        Assert.Equal("b", ifB.Condition);
    }

    [Fact]
    public void DiamondNesting_NestedIfElseInLoop_ShouldParse()
    {
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template =
            "<% loop items %><% if a %>A1<% else %>A2<% end %><% if b %>B1<% else %>B2<% end %><% end %>";

        var result = parser.Parse(template);

        var loop = result.Nodes[0] as DejaVuLoopNode;
        Assert.NotNull(loop);
        Assert.Equal(2, loop.Children.Count);

        var ifA = loop.Children[0] as DejaVuIfNode;
        Assert.NotNull(ifA);
        Assert.Equal("a", ifA.Condition);
        Assert.Single(ifA.Children);
        Assert.Single(ifA.ElseChildren);

        var ifB = loop.Children[1] as DejaVuIfNode;
        Assert.NotNull(ifB);
        Assert.Equal("b", ifB.Condition);
        Assert.Single(ifB.Children);
        Assert.Single(ifB.ElseChildren);
    }

    #endregion

    #region end 栈匹配（裸 end）

    [Fact]
    public void EndStackMatch_SimpleIf_ShouldClose()
    {
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template = "<% if condition %>Hello<% end %>";

        var result = parser.Parse(template);

        var ifNode = result.Nodes[0] as DejaVuIfNode;
        Assert.NotNull(ifNode);
        Assert.Equal("condition", ifNode.Condition);

        foreach (var child in ifNode.Children)
        {
            Assert.IsNotType<DejaVuCodeNode>(child);
        }

        Assert.Single(ifNode.Children);
    }

    [Fact]
    public void EndStackMatch_SimpleLoop_ShouldClose()
    {
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template = "<% loop items %>Item<% end %>";

        var result = parser.Parse(template);

        var loopNode = result.Nodes[0] as DejaVuLoopNode;
        Assert.NotNull(loopNode);
        Assert.Single(loopNode.Children);
    }

    [Fact]
    public void EndStackMatch_SimpleMatch_ShouldClose()
    {
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template = "<% match value %>Case<% end %>";

        var result = parser.Parse(template);

        var matchNode = result.Nodes[0] as DejaVuMatchNode;
        Assert.NotNull(matchNode);
        Assert.Single(matchNode.Children);
    }

    [Fact]
    public void EndStackMatch_SimpleBlock_ShouldClose()
    {
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template = "<% block content %>Content<% end %>";

        var result = parser.Parse(template);

        var blockNode = result.Nodes[0] as DejaVuBlockNode;
        Assert.NotNull(blockNode);
        Assert.Single(blockNode.Children);
    }

    [Fact]
    public void EndStackMatch_NestedIfLoop_ShouldCloseInOrder()
    {
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template = "<% if a %><% loop b %>X<% end %><% end %>";

        var result = parser.Parse(template);

        var ifNode = result.Nodes[0] as DejaVuIfNode;
        Assert.NotNull(ifNode);

        var loopNode = ifNode.Children[0] as DejaVuLoopNode;
        Assert.NotNull(loopNode);
        Assert.Equal("b", loopNode.Expression);
    }

    [Fact]
    public void EndStackMatch_IfElseWithEnd_ShouldClose()
    {
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template = "<% if a %>Yes<% else %>No<% end %>";

        var result = parser.Parse(template);

        var ifNode = result.Nodes[0] as DejaVuIfNode;
        Assert.NotNull(ifNode);
        Assert.Single(ifNode.Children);
        Assert.Single(ifNode.ElseChildren);
    }

    [Fact]
    public void EndStackMatch_IfElseIfElseWithEnd_ShouldClose()
    {
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template = "<% if a %>A<% else if b %>B<% else %>C<% end %>";

        var result = parser.Parse(template);

        var ifNode = result.Nodes[0] as DejaVuIfNode;
        Assert.NotNull(ifNode);
        Assert.Single(ifNode.Children);
        Assert.Single(ifNode.ElseIfNodes);
        Assert.Single(ifNode.ElseChildren);
    }

    #endregion

    #region end 显式匹配（end if / end loop 等）

    [Fact]
    public void EndExplicitMatch_EndIf_ShouldClose()
    {
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template = "<% if condition %>Hello<% end if %>";

        var result = parser.Parse(template);

        var ifNode = result.Nodes[0] as DejaVuIfNode;
        Assert.NotNull(ifNode);
        Assert.Single(ifNode.Children);
    }

    [Fact]
    public void EndExplicitMatch_EndLoop_ShouldClose()
    {
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template = "<% loop items %>Item<% end loop %>";

        var result = parser.Parse(template);

        var loopNode = result.Nodes[0] as DejaVuLoopNode;
        Assert.NotNull(loopNode);
        Assert.Single(loopNode.Children);
    }

    [Fact]
    public void EndExplicitMatch_EndMatch_ShouldClose()
    {
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template = "<% match value %>Case<% end match %>";

        var result = parser.Parse(template);

        var matchNode = result.Nodes[0] as DejaVuMatchNode;
        Assert.NotNull(matchNode);
        Assert.Single(matchNode.Children);
    }

    [Fact]
    public void EndExplicitMatch_EndBlock_ShouldClose()
    {
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template = "<% block content %>Content<% end block %>";

        var result = parser.Parse(template);

        var blockNode = result.Nodes[0] as DejaVuBlockNode;
        Assert.NotNull(blockNode);
        Assert.Single(blockNode.Children);
    }

    #endregion

    #region end 混合匹配（裸 end + 显式 end）

    [Fact]
    public void EndMixed_InnerExplicitOuterStack_ShouldClose()
    {
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template = "<% loop items %><% if active %>X<% end if %><% end %>";

        var result = parser.Parse(template);

        var loopNode = result.Nodes[0] as DejaVuLoopNode;
        Assert.NotNull(loopNode);

        var ifNode = loopNode.Children[0] as DejaVuIfNode;
        Assert.NotNull(ifNode);
        Assert.Equal("active", ifNode.Condition);
    }

    [Fact]
    public void EndMixed_InnerStackOuterExplicit_ShouldClose()
    {
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template = "<% loop items %><% if active %>X<% end %><% end loop %>";

        var result = parser.Parse(template);

        var loopNode = result.Nodes[0] as DejaVuLoopNode;
        Assert.NotNull(loopNode);

        var ifNode = loopNode.Children[0] as DejaVuIfNode;
        Assert.NotNull(ifNode);
        Assert.Equal("active", ifNode.Condition);
    }

    [Fact]
    public void EndMixed_DeepNestingMixedEnds_ShouldClose()
    {
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template =
            "<% if a %><% loop b %><% if c %>X<% end if %><% end %><% end if %>";

        var result = parser.Parse(template);

        var ifA = result.Nodes[0] as DejaVuIfNode;
        Assert.NotNull(ifA);

        var loopB = ifA.Children[0] as DejaVuLoopNode;
        Assert.NotNull(loopB);

        var ifC = loopB.Children[0] as DejaVuIfNode;
        Assert.NotNull(ifC);
        Assert.Equal("c", ifC.Condition);
    }

    [Fact]
    public void EndMixed_IfElseWithExplicitEndInLoop_ShouldClose()
    {
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template =
            "<% loop items %><% if active %>Yes<% else %>No<% end if %><% end %>";

        var result = parser.Parse(template);

        var loopNode = result.Nodes[0] as DejaVuLoopNode;
        Assert.NotNull(loopNode);

        var ifNode = loopNode.Children[0] as DejaVuIfNode;
        Assert.NotNull(ifNode);
        Assert.Single(ifNode.Children);
        Assert.Single(ifNode.ElseChildren);
    }

    #endregion

    #region end 类型不匹配诊断

    [Fact]
    public void EndMismatch_EndLoopInIf_ShouldReportError()
    {
        var diagnostics = new DiagnosticSink();
        var parser = new DejaVuParser(DejaVuLanguage.Dora, diagnostics);
        var template = "<% if condition %>Hello<% end loop %>";

        parser.Parse(template);

        Assert.Contains(diagnostics.Messages, d => d.Code == "EndTypeMismatch");
    }

    [Fact]
    public void EndMismatch_EndIfInLoop_ShouldReportError()
    {
        var diagnostics = new DiagnosticSink();
        var parser = new DejaVuParser(DejaVuLanguage.Dora, diagnostics);
        var template = "<% loop items %>Item<% end if %>";

        parser.Parse(template);

        Assert.Contains(diagnostics.Messages, d => d.Code == "EndTypeMismatch");
    }

    [Fact]
    public void EndMismatch_EndMatchInIf_ShouldReportError()
    {
        var diagnostics = new DiagnosticSink();
        var parser = new DejaVuParser(DejaVuLanguage.Dora, diagnostics);
        var template = "<% if condition %>Hello<% end match %>";

        parser.Parse(template);

        Assert.Contains(diagnostics.Messages, d => d.Code == "EndTypeMismatch");
    }

    [Fact]
    public void EndMismatch_EndIfInMatch_ShouldReportError()
    {
        var diagnostics = new DiagnosticSink();
        var parser = new DejaVuParser(DejaVuLanguage.Dora, diagnostics);
        var template = "<% match value %>Case<% end if %>";

        parser.Parse(template);

        Assert.Contains(diagnostics.Messages, d => d.Code == "EndTypeMismatch");
    }

    [Fact]
    public void UnexpectedEnd_TopLevel_ShouldReportError()
    {
        var diagnostics = new DiagnosticSink();
        var parser = new DejaVuParser(DejaVuLanguage.Dora, diagnostics);
        var template = "<% end %>";

        parser.Parse(template);

        Assert.Contains(diagnostics.Messages, d => d.Code == "UnexpectedEnd");
    }

    [Fact]
    public void UnexpectedEnd_TopLevelExplicit_ShouldReportError()
    {
        var diagnostics = new DiagnosticSink();
        var parser = new DejaVuParser(DejaVuLanguage.Dora, diagnostics);
        var template = "<% end if %>";

        parser.Parse(template);

        Assert.Contains(diagnostics.Messages, d => d.Code == "UnexpectedEnd");
    }

    #endregion

    #region 复杂控制流

    [Fact]
    public void ComplexFlow_IfElseInLoopWithNestedIf_ShouldParse()
    {
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template =
            "<% loop items %><% if active %><% if featured %>STAR<% end %><% else %>INACTIVE<% end %><% end %>";

        var result = parser.Parse(template);

        var loop = result.Nodes[0] as DejaVuLoopNode;
        Assert.NotNull(loop);

        var ifActive = loop.Children[0] as DejaVuIfNode;
        Assert.NotNull(ifActive);
        Assert.Equal("active", ifActive.Condition);

        var ifFeatured = ifActive.Children[0] as DejaVuIfNode;
        Assert.NotNull(ifFeatured);
        Assert.Equal("featured", ifFeatured.Condition);

        var elseText = ifActive.ElseChildren[0] as DejaVuTextNode;
        Assert.NotNull(elseText);
        Assert.Equal("INACTIVE", elseText.Text);
    }

    [Fact]
    public void ComplexFlow_MultipleElseIfWithNesting_ShouldParse()
    {
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template =
            "<% if a %>A<% else if b %><% loop x %>BX<% end %><% else if c %>C<% else %><% loop y %>DY<% end %><% end %>";

        var result = parser.Parse(template);

        var ifNode = result.Nodes[0] as DejaVuIfNode;
        Assert.NotNull(ifNode);
        Assert.Equal(2, ifNode.ElseIfNodes.Count);

        var elseIfBLoop = ifNode.ElseIfNodes[0].Children[0] as DejaVuLoopNode;
        Assert.NotNull(elseIfBLoop);
        Assert.Equal("x", elseIfBLoop.Expression);

        var elseYLoop = ifNode.ElseChildren[0] as DejaVuLoopNode;
        Assert.NotNull(elseYLoop);
        Assert.Equal("y", elseYLoop.Expression);
    }

    [Fact]
    public void ComplexFlow_SiblingLoopsWithIf_ShouldParse()
    {
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template =
            "<% loop users %><% if admin %><% loop permissions %><% name %><% end %><% end %><% end %>";

        var result = parser.Parse(template);

        var usersLoop = result.Nodes[0] as DejaVuLoopNode;
        Assert.NotNull(usersLoop);

        var ifAdmin = usersLoop.Children[0] as DejaVuIfNode;
        Assert.NotNull(ifAdmin);

        var permLoop = ifAdmin.Children[0] as DejaVuLoopNode;
        Assert.NotNull(permLoop);
        Assert.Equal("permissions", permLoop.Expression);
    }

    [Fact]
    public void ComplexFlow_BlockWithNestedControlFlow_ShouldParse()
    {
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template =
            "<% block content %><% if show %><% loop items %><% item %><% end %><% end %><% end %>";

        var result = parser.Parse(template);

        var block = result.Nodes[0] as DejaVuBlockNode;
        Assert.NotNull(block);
        Assert.Equal("content", block.Name);

        var ifShow = block.Children[0] as DejaVuIfNode;
        Assert.NotNull(ifShow);

        var itemsLoop = ifShow.Children[0] as DejaVuLoopNode;
        Assert.NotNull(itemsLoop);
    }

    [Fact]
    public void ComplexFlow_MatchWithLoopAndIf_ShouldParse()
    {
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template =
            "<% match type %><% loop items %><% if visible %><% name %><% end %><% end %><% end %>";

        var result = parser.Parse(template);

        var match = result.Nodes[0] as DejaVuMatchNode;
        Assert.NotNull(match);

        var loop = match.Children[0] as DejaVuLoopNode;
        Assert.NotNull(loop);

        var ifVisible = loop.Children[0] as DejaVuIfNode;
        Assert.NotNull(ifVisible);
        Assert.Equal("visible", ifVisible.Condition);
    }

    [Fact]
    public void ComplexFlow_DeepDiamond_ParallelIfElseInLoopInMatch_ShouldParse()
    {
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template =
            "<% match mode %><% loop rows %><% if a %>A1<% else %>A2<% end %><% if b %>B1<% else %>B2<% end %><% end %><% end %>";

        var result = parser.Parse(template);

        var match = result.Nodes[0] as DejaVuMatchNode;
        Assert.NotNull(match);

        var loop = match.Children[0] as DejaVuLoopNode;
        Assert.NotNull(loop);
        Assert.Equal(2, loop.Children.Count);

        var ifA = loop.Children[0] as DejaVuIfNode;
        Assert.NotNull(ifA);
        Assert.Single(ifA.Children);
        Assert.Single(ifA.ElseChildren);

        var ifB = loop.Children[1] as DejaVuIfNode;
        Assert.NotNull(ifB);
        Assert.Single(ifB.Children);
        Assert.Single(ifB.ElseChildren);
    }

    #endregion

    #region Doki 语言变体测试

    [Fact]
    public void Doki_DeepNesting_ShouldParse()
    {
        var parser = new DejaVuParser(DejaVuLanguage.Doki);
        var template = "{% if a %}{% loop b %}{% if c %}deep{% end %}{% end %}{% end %}";

        var result = parser.Parse(template);

        var ifA = result.Nodes[0] as DejaVuIfNode;
        Assert.NotNull(ifA);

        var loopB = ifA.Children[0] as DejaVuLoopNode;
        Assert.NotNull(loopB);

        var ifC = loopB.Children[0] as DejaVuIfNode;
        Assert.NotNull(ifC);
    }

    [Fact]
    public void Doki_EndMismatch_ShouldReportError()
    {
        var diagnostics = new DiagnosticSink();
        var parser = new DejaVuParser(DejaVuLanguage.Doki, diagnostics);
        var template = "{% if condition %}Hello{% end loop %}";

        parser.Parse(template);

        Assert.Contains(diagnostics.Messages, d => d.Code == "EndTypeMismatch");
    }

    #endregion
}
