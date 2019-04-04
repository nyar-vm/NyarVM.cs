namespace Std.Data.Text.Valkyrie.AST.Type;

/// <summary>
///     类型注解 —— 可表示简单类型、泛型、列表、数组、联合、交叉、函数等多种类型形态
/// </summary>
/// <para>支持的完整类型语法：</para>
/// <list type="bullet">
///     <item><c>i32</c>、<c>f32</c>、<c>utf8</c> — 基础简单类型</item>
///     <item><c>List&lt;i32&gt;</c> — 泛型类型</item>
///     <item><c>[i32]</c> — 数组语法糖，展开为 <c>Array&lt;i32&gt;</c>（编译器原语）</item>
///     <item><c>[i32; 8]</c> — 定长数组语法，展开为 <c>FixedArray&lt;i32, 8&gt;</c>（编译器原语）</item>
///     <item><c>i32?</c> — 可空类型（<c>IsNullable</c>）</item>
///     <item><c>i32 | f32</c> — 联合类型（<c>IsUnionType</c>）</item>
///     <item><c>A &amp; B</c> — 交叉类型（<c>IsIntersectionType</c>）</item>
///     <item><c>micro(i32) -> bool</c> — 函数类型（<c>IsFunctionType</c>）</item>
/// </list>
public abstract record TypeNode : ValkyrieNode
{
}