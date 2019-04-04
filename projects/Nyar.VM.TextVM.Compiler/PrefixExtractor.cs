using System.Collections.Immutable;
using System.Text;
using Nyar.VM.TextVM;

namespace Nyar.VM.TextVM.Compiler;

/// <summary>
/// 从 AST 中贪婪提取字面量前缀。
/// 每个返回的字节数组是给定编码下字面量前缀的编码结果。
/// </summary>
public static class PrefixExtractor
{
    /// <summary>
    /// 从指定的 AST 节点中提取字面量前缀。
    /// </summary>
    /// <param name="node">要分析的 AST 根节点。</param>
    /// <param name="encoding">文本编码类型。</param>
    /// <returns>字面量前缀的字节数组集合。</returns>
    public static ImmutableArray<Byte[]> Extract(AstNode node, TextEncoding encoding)
    {
        List<Byte[]> results = [];

        CollectPrefixes(node, encoding, results);

        return [.. results];
    }

    /// <summary>
    /// 递归收集字面量前缀。
    /// </summary>
    private static void CollectPrefixes(AstNode node, TextEncoding encoding, List<Byte[]> results)
    {
        switch (node)
        {
            case LiteralNode literalNode:
                results.Add(EncodeString(literalNode.Value, encoding));
                break;

            case ConcatNode concatNode:
                if (concatNode.Children.Length > 0)
                {
                    CollectPrefixes(concatNode.Children[0], encoding, results);
                }
                break;

            case AltNode altNode:
                CollectPrefixes(altNode.Left, encoding, results);
                CollectPrefixes(altNode.Right, encoding, results);
                break;

            case CaptureNode captureNode:
                CollectPrefixes(captureNode.Inner, encoding, results);
                break;

            default:
                break;
        }
    }

    /// <summary>
    /// 将字符串按指定编码编码为字节数组。
    /// </summary>
    private static Byte[] EncodeString(String value, TextEncoding encoding)
    {
        return encoding switch
        {
            TextEncoding.Ascii => Encoding.ASCII.GetBytes(value),
            TextEncoding.Utf8 => Encoding.UTF8.GetBytes(value),
            TextEncoding.Utf16Le => Encoding.Unicode.GetBytes(value),
            TextEncoding.Utf16Be => Encoding.BigEndianUnicode.GetBytes(value),
            _ => Encoding.UTF8.GetBytes(value),
        };
    }
}
