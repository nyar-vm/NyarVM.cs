using Xunit;

namespace Oak.DejaVu.Tests;

public sealed class DejaVuParserTests
{
    [Fact]
    public void ParseDoraTemplateTest()
    {
        // Arrange
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template = "Hello <% name %>, welcome to <% place %>!";

        // Act
        var result = parser.Parse(template);

        // Assert
        Assert.Equal(5, result.Nodes.Count);
        Assert.Equal("dora", result.TemplateType);

        var node1 = result.Nodes[0] as DejaVuTextNode;
        Assert.NotNull(node1);
        Assert.Equal(DejaVuNodeType.Text, node1.NodeType);
        Assert.Equal("Hello ", node1.Text);

        var node2 = result.Nodes[1] as DejaVuCodeNode;
        Assert.NotNull(node2);
        Assert.Equal(DejaVuNodeType.Code, node2.NodeType);
        Assert.Equal("name", node2.Code);

        var node3 = result.Nodes[2] as DejaVuTextNode;
        Assert.NotNull(node3);
        Assert.Equal(DejaVuNodeType.Text, node3.NodeType);
        Assert.Equal(", welcome to ", node3.Text);

        var node4 = result.Nodes[3] as DejaVuCodeNode;
        Assert.NotNull(node4);
        Assert.Equal(DejaVuNodeType.Code, node4.NodeType);
        Assert.Equal("place", node4.Code);

        var node5 = result.Nodes[4] as DejaVuTextNode;
        Assert.NotNull(node5);
        Assert.Equal(DejaVuNodeType.Text, node5.NodeType);
        Assert.Equal("!", node5.Text);
    }

    [Fact]
    public void ParseDokiTemplateTest()
    {
        // Arrange
        var parser = new DejaVuParser(DejaVuLanguage.Doki);
        var template = "Hello {% name %}, welcome to {% place %}!";

        // Act
        var result = parser.Parse(template);

        // Assert
        Assert.Equal(5, result.Nodes.Count);
        Assert.Equal("doki", result.TemplateType);

        var node1 = result.Nodes[0] as DejaVuTextNode;
        Assert.NotNull(node1);
        Assert.Equal(DejaVuNodeType.Text, node1.NodeType);
        Assert.Equal("Hello ", node1.Text);

        var node2 = result.Nodes[1] as DejaVuCodeNode;
        Assert.NotNull(node2);
        Assert.Equal(DejaVuNodeType.Code, node2.NodeType);
        Assert.Equal("name", node2.Code);

        var node3 = result.Nodes[2] as DejaVuTextNode;
        Assert.NotNull(node3);
        Assert.Equal(DejaVuNodeType.Text, node3.NodeType);
        Assert.Equal(", welcome to ", node3.Text);

        var node4 = result.Nodes[3] as DejaVuCodeNode;
        Assert.NotNull(node4);
        Assert.Equal(DejaVuNodeType.Code, node4.NodeType);
        Assert.Equal("place", node4.Code);

        var node5 = result.Nodes[4] as DejaVuTextNode;
        Assert.NotNull(node5);
        Assert.Equal(DejaVuNodeType.Text, node5.NodeType);
        Assert.Equal("!", node5.Text);
    }

    [Fact]
    public void ParseEmptyTemplateTest()
    {
        // Arrange
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template = string.Empty;

        // Act
        var result = parser.Parse(template);

        // Assert
        Assert.Empty(result.Nodes);
        Assert.Equal("dora", result.TemplateType);
    }

    [Fact]
    public void ParseOnlyTextTemplateTest()
    {
        // Arrange
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template = "Hello World!";

        // Act
        var result = parser.Parse(template);

        // Assert
        Assert.Single(result.Nodes);
        var node = result.Nodes[0] as DejaVuTextNode;
        Assert.NotNull(node);
        Assert.Equal(DejaVuNodeType.Text, node.NodeType);
        Assert.Equal("Hello World!", node.Text);
    }

    [Fact]
    public void ParseOnlyCodeTemplateTest()
    {
        // Arrange
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template = "<% code %>";

        // Act
        var result = parser.Parse(template);

        // Assert
        Assert.Single(result.Nodes);
        var node = result.Nodes[0] as DejaVuCodeNode;
        Assert.NotNull(node);
        Assert.Equal(DejaVuNodeType.Code, node.NodeType);
        Assert.Equal("code", node.Code);
    }

    [Fact]
    public void ParseIfStatementTest()
    {
        // Arrange
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template = "<% if condition %>Hello World!<% end if %>";

        // Act
        var result = parser.Parse(template);

        // Assert
        Assert.Equal(1, result.Nodes.Count);
        var ifNode = result.Nodes[0] as DejaVuIfNode;
        Assert.NotNull(ifNode);
        Assert.Equal(DejaVuNodeType.If, ifNode.NodeType);
        Assert.Equal("condition", ifNode.Condition);
        Assert.Single(ifNode.Children);
        var textNode = ifNode.Children[0] as DejaVuTextNode;
        Assert.NotNull(textNode);
        Assert.Equal("Hello World!", textNode.Text);
    }

    [Fact]
    public void ParseLoopStatementTest()
    {
        // Arrange
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template = "<% loop items %>Item: <% item %><% end loop %>";

        // Act
        var result = parser.Parse(template);

        // Assert
        Assert.Equal(1, result.Nodes.Count);
        var loopNode = result.Nodes[0] as DejaVuLoopNode;
        Assert.NotNull(loopNode);
        Assert.Equal(DejaVuNodeType.Loop, loopNode.NodeType);
        Assert.Equal("items", loopNode.Expression);
        Assert.Equal(2, loopNode.Children.Count);
        var textNode1 = loopNode.Children[0] as DejaVuTextNode;
        Assert.NotNull(textNode1);
        Assert.Equal("Item: ", textNode1.Text);
        var codeNode = loopNode.Children[1] as DejaVuCodeNode;
        Assert.NotNull(codeNode);
        Assert.Equal("item", codeNode.Code);
    }

    [Fact]
    public void ParseMatchStatementTest()
    {
        // Arrange
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template = "<% match value %>Value: <% value %><% end match %>";

        // Act
        var result = parser.Parse(template);

        // Assert
        Assert.Equal(1, result.Nodes.Count);
        var matchNode = result.Nodes[0] as DejaVuMatchNode;
        Assert.NotNull(matchNode);
        Assert.Equal(DejaVuNodeType.Match, matchNode.NodeType);
        Assert.Equal("value", matchNode.Expression);
        Assert.Equal(2, matchNode.Children.Count);
        var textNode1 = matchNode.Children[0] as DejaVuTextNode;
        Assert.NotNull(textNode1);
        Assert.Equal("Value: ", textNode1.Text);
        var codeNode = matchNode.Children[1] as DejaVuCodeNode;
        Assert.NotNull(codeNode);
        Assert.Equal("value", codeNode.Code);
    }

    [Fact]
    public void ParseNestedStatementsTest()
    {
        // Arrange
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template = "<% loop items %><% if item != 'Banana' %>Item: <% item %><% end if %><% end loop %>";

        // Act
        var result = parser.Parse(template);

        // Assert
        Assert.Equal(1, result.Nodes.Count);
        var loopNode = result.Nodes[0] as DejaVuLoopNode;
        Assert.NotNull(loopNode);
        Assert.Equal(DejaVuNodeType.Loop, loopNode.NodeType);
        Assert.Equal("items", loopNode.Expression);
        Assert.Single(loopNode.Children);
        var ifNode = loopNode.Children[0] as DejaVuIfNode;
        Assert.NotNull(ifNode);
        Assert.Equal(DejaVuNodeType.If, ifNode.NodeType);
        Assert.Equal("item != 'Banana'", ifNode.Condition);
        Assert.Equal(2, ifNode.Children.Count);
        var textNode1 = ifNode.Children[0] as DejaVuTextNode;
        Assert.NotNull(textNode1);
        Assert.Equal("Item: ", textNode1.Text);
        var codeNode = ifNode.Children[1] as DejaVuCodeNode;
        Assert.NotNull(codeNode);
        Assert.Equal("item", codeNode.Code);
    }

    [Fact]
    public void ParseBlockStatementTest()
    {
        // Arrange
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template = "<% block content %>Hello World!<% end block %>";

        // Act
        var result = parser.Parse(template);

        // Assert
        Assert.Equal(1, result.Nodes.Count);
        var blockNode = result.Nodes[0] as DejaVuBlockNode;
        Assert.NotNull(blockNode);
        Assert.Equal(DejaVuNodeType.Block, blockNode.NodeType);
        Assert.Equal("content", blockNode.Name);
        Assert.Single(blockNode.Children);
        var textNode = blockNode.Children[0] as DejaVuTextNode;
        Assert.NotNull(textNode);
        Assert.Equal("Hello World!", textNode.Text);
    }

    [Fact]
    public void ParseExtendsStatementTest()
    {
        // Arrange
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template = "<% extends 'layout.dora' %>";

        // Act
        var result = parser.Parse(template);

        // Assert
        Assert.Equal(1, result.Nodes.Count);
        var extendsNode = result.Nodes[0] as DejaVuExtendsNode;
        Assert.NotNull(extendsNode);
        Assert.Equal(DejaVuNodeType.Extends, extendsNode.NodeType);
        Assert.Equal("'layout.dora'", extendsNode.ParentTemplate);
    }

    [Fact]
    public void ParseIncludeStatementTest()
    {
        // Arrange
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template = "<% include 'header.dora' %>";

        // Act
        var result = parser.Parse(template);

        // Assert
        Assert.Equal(1, result.Nodes.Count);
        var includeNode = result.Nodes[0] as DejaVuIncludeNode;
        Assert.NotNull(includeNode);
        Assert.Equal(DejaVuNodeType.Include, includeNode.NodeType);
        Assert.Equal("'header.dora'", includeNode.TemplatePath);
    }

    [Fact]
    public void ParseIfElseStatementTest()
    {
        // Arrange
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template = "<% if condition %>Yes<% else %>No<% end if %>";

        // Act
        var result = parser.Parse(template);

        // Assert
        Assert.Single(result.Nodes);
        var ifNode = result.Nodes[0] as DejaVuIfNode;
        Assert.NotNull(ifNode);
        Assert.Equal(DejaVuNodeType.If, ifNode.NodeType);
        Assert.Equal("condition", ifNode.Condition);
        Assert.Single(ifNode.Children);
        Assert.Single(ifNode.ElseChildren);
        var thenNode = ifNode.Children[0] as DejaVuTextNode;
        Assert.NotNull(thenNode);
        Assert.Equal("Yes", thenNode.Text);
        var elseNode = ifNode.ElseChildren[0] as DejaVuTextNode;
        Assert.NotNull(elseNode);
        Assert.Equal("No", elseNode.Text);
    }

    [Fact]
    public void ParseIfElseIfStatementTest()
    {
        // Arrange
        var parser = new DejaVuParser(DejaVuLanguage.Dora);
        var template = "<% if condition1 %>First<% else if condition2 %>Second<% else %>Third<% end if %>";

        // Act
        var result = parser.Parse(template);

        // Assert
        Assert.Single(result.Nodes);
        var ifNode = result.Nodes[0] as DejaVuIfNode;
        Assert.NotNull(ifNode);
        Assert.Equal(DejaVuNodeType.If, ifNode.NodeType);
        Assert.Equal("condition1", ifNode.Condition);
        Assert.Single(ifNode.Children);
        Assert.Single(ifNode.ElseIfNodes);
        Assert.Single(ifNode.ElseChildren);

        var thenNode = ifNode.Children[0] as DejaVuTextNode;
        Assert.NotNull(thenNode);
        Assert.Equal("First", thenNode.Text);

        var elseIfNode = ifNode.ElseIfNodes[0];
        Assert.Equal("condition2", elseIfNode.Condition);
        Assert.Single(elseIfNode.Children);
        var elseIfTextNode = elseIfNode.Children[0] as DejaVuTextNode;
        Assert.NotNull(elseIfTextNode);
        Assert.Equal("Second", elseIfTextNode.Text);

        var elseNode = ifNode.ElseChildren[0] as DejaVuTextNode;
        Assert.NotNull(elseNode);
        Assert.Equal("Third", elseNode.Text);
    }
}